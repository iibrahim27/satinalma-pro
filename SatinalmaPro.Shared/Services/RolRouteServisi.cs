namespace SatinalmaPro.Shared.Services;

public static class RolRouteServisi
{
    public static HashSet<string> ErisilebilirRotalar(string? rol) =>
        RolNavigasyonu.Menuler(rol)
            .Select(m => m.Route)
            .ToHashSet(StringComparer.Ordinal);

    public static string? AltSayfaSekmesi(string taban) => taban switch
    {
        "talep-duzenle" => "taleplerim",
        "teklif-onay-detay" => "teklif-onay",
        "acil-onay" => "gelen-talepler",
        "stok-aktar" => "onaylanan-malzemeler",
        _ => null
    };

    /// <summary>Stack (üst üste) sayfalar için flyout üst sekmesi — rol erişimine göre.</summary>
    public static string? StackRouteUstSekmesi(string taban, string? rol)
    {
        var erisim = ErisilebilirRotalar(rol);
        string? Ilk(params string[] adaylar) =>
            adaylar.FirstOrDefault(erisim.Contains);

        return taban switch
        {
            "talep-detay" => Ilk("onay-bekleyen", "onaylanan-talepler", "teklif-bekleyen", "red-talepler", "gelen-talepler", "taleplerim"),
            "onay-gecmisi-detay" => Ilk("gecmis-talepler", "gecmis-teklifli-onaylar", "onaylanan-talepler", "onaylanan-teklifler", "onay-gecmisi"),
            "teklif-onay-detay" => Ilk("teklif-onay"),
            "acil-onay" => Ilk("gelen-talepler"),
            "stok-aktar" => Ilk("onaylanan-malzemeler"),
            "talep-duzenle" => Ilk("taleplerim"),
            _ => AltSayfaSekmesi(taban) is { } s && erisim.Contains(s) ? s : null
        };
    }

    public static string? TalepIdCikar(string route)
    {
        var sorgu = route.Contains('?') ? route[(route.IndexOf('?') + 1)..] : "";
        foreach (var parca in sorgu.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            if (parca.StartsWith("id=", StringComparison.Ordinal))
                return Uri.UnescapeDataString(parca[3..]);
            if (parca.StartsWith("talepId=", StringComparison.Ordinal))
                return Uri.UnescapeDataString(parca[8..]);
        }

        return null;
    }

    public static string GuvenliRoute(string route, string? rol)
    {
        if (string.IsNullOrWhiteSpace(route))
            return VarsayilanRoute(rol);

        var soru = route.IndexOf('?');
        var taban = soru >= 0 ? route[..soru] : route;
        var erisim = ErisilebilirRotalar(rol);

        if (taban == "onay-gecmisi-detay")
        {
            foreach (var parent in new[] { "gecmis-talepler", "gecmis-teklifli-onaylar", "onay-gecmisi", "onaylanan-teklifler" })
            {
                if (erisim.Contains(parent))
                    return route;
            }
        }

        if (taban == "talep-detay")
        {
            foreach (var parent in new[] { "onay-bekleyen", "onaylanan-talepler", "red-talepler", "teklif-bekleyen", "gelen-talepler", "taleplerim" })
            {
                if (erisim.Contains(parent))
                    return route;
            }
        }

        if (AltSayfaSekmesi(taban) is { } sekme && erisim.Contains(sekme))
            return route;

        if (erisim.Contains(taban))
            return route;

        var talepId = TalepIdCikar(route);
        if (talepId is not null)
        {
            if (erisim.Contains("teklif-onay"))
                return $"teklif-onay-detay?id={talepId}";
            if (erisim.Contains("gelen-talepler"))
                return "//gelen-talepler";
            if (erisim.Contains("gecmis-talepler") || erisim.Contains("gecmis-teklifli-onaylar"))
                return $"onay-gecmisi-detay?id={talepId}";
            if (erisim.Contains("taleplerim"))
                return $"talep-detay?id={talepId}";
            if (erisim.Contains("onay-gecmisi"))
                return $"onay-gecmisi-detay?id={talepId}";
            if (erisim.Contains("onaylanan-talepler"))
                return $"onay-gecmisi-detay?id={talepId}";
            if (erisim.Contains("onaylanan-teklifler"))
                return $"onay-gecmisi-detay?id={talepId}";
            if (erisim.Contains("red-talepler"))
                return $"talep-detay?id={talepId}";
        }

        return VarsayilanRoute(rol);
    }

    public static string VarsayilanRoute(string? rol) =>
        ErisilebilirRotalar(rol).FirstOrDefault() ?? "main";
}

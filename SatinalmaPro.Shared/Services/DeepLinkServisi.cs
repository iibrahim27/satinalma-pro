namespace SatinalmaPro.Shared.Services;

public sealed record DeepLinkParametreleri(
    string Module,
    string Screen,
    string Action,
    string EntityType,
    string EntityId,
    string? Tab = null,
    string? EventCode = null);

public static class DeepLinkServisi
{
    public const string Scheme = "metrik";

    public static string Olustur(DeepLinkParametreleri p)
    {
        var q = new List<string>
        {
            $"module={Uri.EscapeDataString(p.Module)}",
            $"screen={Uri.EscapeDataString(p.Screen)}",
            $"action={Uri.EscapeDataString(p.Action)}",
            $"entityType={Uri.EscapeDataString(p.EntityType)}",
            $"entityId={Uri.EscapeDataString(p.EntityId)}"
        };
        if (!string.IsNullOrWhiteSpace(p.Tab))
            q.Add($"tab={Uri.EscapeDataString(p.Tab)}");
        if (!string.IsNullOrWhiteSpace(p.EventCode))
            q.Add($"eventCode={Uri.EscapeDataString(p.EventCode)}");
        return $"{Scheme}://{p.Module}/{p.Screen}?{string.Join("&", q)}";
    }

    public static DeepLinkParametreleri? Coz(string? uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
            return null;

        if (!Uri.TryCreate(uri, UriKind.Absolute, out var u))
            return null;

        if (!string.Equals(u.Scheme, Scheme, StringComparison.OrdinalIgnoreCase))
            return null;

        var module = u.Host;
        var screen = u.AbsolutePath.TrimStart('/');
        var query = ParseQuery(u.Query);

        return new DeepLinkParametreleri(
            query.GetValueOrDefault("module", module),
            query.GetValueOrDefault("screen", screen),
            query.GetValueOrDefault("action", "view"),
            query.GetValueOrDefault("entityType", ""),
            query.GetValueOrDefault("entityId", ""),
            query.GetValueOrDefault("tab"),
            query.GetValueOrDefault("eventCode"));
    }

    public static MasaustuBildirimHedef MasaustuHedef(DeepLinkParametreleri p)
    {
        var modul = p.Module.ToLowerInvariant() switch
        {
            "satinalma" => "Satınalma",
            "depo" => "Stok Yönetimi",
            "stok" => "Stok Yönetimi",
            _ when p.Module.Length > 0 => char.ToUpper(p.Module[0]) + p.Module[1..],
            _ => "Satınalma"
        };

        var sekme = p.Module.ToLowerInvariant() switch
        {
            "depo" or "stok" => MasaustuRolHaritasi.RouteToStokSekme(p.Screen) ?? p.Screen,
            _ => MasaustuRolHaritasi.RouteToSatinalmaSekme(p.Screen) ?? p.Screen
        };

        Guid? talepId = Guid.TryParse(p.EntityId, out var id) ? id : null;
        return new MasaustuBildirimHedef(modul, sekme, 0, talepId);
    }

    public static bool SatinalmaOperasyonGerektirir(string sekme) =>
        MasaustuRolHaritasi.RouteToSatinalmaSekme(sekme) is not null
        || sekme.Contains("teklif", StringComparison.OrdinalIgnoreCase)
        || sekme.Contains("onay", StringComparison.OrdinalIgnoreCase)
        || sekme is "gelen-talepler" or "red-talepler" or "alinan-malzemeler"
            or "onaylanan-malzemeler" or "gecmis-talepler" or "gecmis-teklifli-onaylar"
            or "teklif-gir" or "teklif-karsilastirma" or "teklifsiz-firma-fiyat";

    /// <summary>Android Shell route — mevcut RolRouteServisi ile uyumlu.</summary>
    public static string MobilRoute(DeepLinkParametreleri p)
    {
        if (Guid.TryParse(p.EntityId, out _))
        {
            return p.Screen switch
            {
                "gelen-talepler" or "talep-detay" => $"talep-detay?id={p.EntityId}",
                "teklif-giris" or "teklif-onay" => $"teklif-onay-detay?id={p.EntityId}",
                "onaylanan-talepler" or "siparis-detay" => $"onaylanan-malzemeler",
                _ => $"talep-detay?id={p.EntityId}"
            };
        }

        return p.Screen switch
        {
            "inbox" => "bildirimler",
            "gelen-talepler" => "gelen-talepler",
            "teklif-giris" => "teklif-gir",
            "teklif-onay" => "teklif-onay",
            _ => "bildirimler"
        };
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(query))
            return map;

        foreach (var parca in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = parca.IndexOf('=');
            if (eq <= 0) continue;
            var key = Uri.UnescapeDataString(parca[..eq]);
            var val = Uri.UnescapeDataString(parca[(eq + 1)..]);
            map[key] = val;
        }

        return map;
    }
}

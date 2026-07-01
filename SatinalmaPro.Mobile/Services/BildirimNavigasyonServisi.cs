using SatinalmaPro.Mobile.Services;
using SatinalmaPro.Shared.Models;
using SatinalmaPro.Shared.Services;

namespace SatinalmaPro.Mobile;

public static class BildirimNavigasyonServisi
{
    private static string? _bekleyenRoute;

    public static void BekleyenRouteAyarla(string? route) => _bekleyenRoute = route;

    public static async Task BekleyenRouteIsleAsync()
    {
        if (string.IsNullOrWhiteSpace(_bekleyenRoute))
            return;

        var route = _bekleyenRoute;
        _bekleyenRoute = null;
        await RouteGitAsync(route);
    }

    public static async Task RouteGitAsync(string route, OturumServisi? oturum = null)
    {
        if (string.IsNullOrWhiteSpace(route))
            return;

        oturum ??= IPlatformApplication.Current?.Services.GetService<OturumServisi>();

        await BildirimOkunduYapAsync(route, oturum);
        route = BidTemizle(route);
        route = RolRouteServisi.GuvenliRoute(route, oturum?.Rol);

        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (Shell.Current is not Shell shell)
                {
                    _bekleyenRoute = route;
                    return;
                }

                if (oturum?.GirisYapildi != true)
                {
                    _bekleyenRoute = route;
                    return;
                }

                var soru = route.IndexOf('?');
                var taban = soru >= 0 ? route[..soru] : route;
                var sorgu = soru >= 0 ? route[soru..] : "";

                if (RolRouteServisi.StackRouteUstSekmesi(taban, oturum?.Rol) is { } sekme)
                {
                    var mevcut = shell.CurrentState?.Location?.OriginalString ?? "";
                    if (!MevcutKokSekmesi(mevcut, sekme))
                        await shell.GoToAsync($"//{sekme}");
                    await shell.GoToAsync($"{taban}{sorgu}");
                    return;
                }

                var hedef = route.TrimStart('/');
                if (hedef.StartsWith("//", StringComparison.Ordinal))
                    hedef = hedef.TrimStart('/');
                await shell.GoToAsync($"//{hedef}");
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Navigasyon hatasi: {route} -> {ex}");
            _bekleyenRoute = route;

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (Shell.Current is not null)
                {
                    var guvenli = RolRouteServisi.VarsayilanRoute(oturum?.Rol);
                    try { await Shell.Current.GoToAsync($"//{guvenli}"); } catch { /* yoksay */ }
                }

                var sayfa = Application.Current?.Windows.FirstOrDefault()?.Page;
                if (sayfa is not null)
                    await sayfa.DisplayAlert(
                        "Yönlendirme",
                        "İlgili ekran açılamadı. Ana menüye yönlendirildiniz.",
                        "Tamam");
            });
        }
    }

    public static async Task BildirimdenGitAsync(BildirimKaydi bildirim, OturumServisi? oturum = null)
    {
        oturum ??= IPlatformApplication.Current?.Services.GetService<OturumServisi>();
        var route = BildirimRotaServisi.HedefRoute(bildirim, oturum?.Rol);
        var bid = bildirim.Id.ToString();
        route = route.Contains('?') ? $"{route}&bid={bid}" : $"{route}?bid={bid}";
        await RouteGitAsync(route, oturum);
    }

    public static async Task FcmVeridenGitAsync(IDictionary<string, string> veri)
    {
        var oturum = IPlatformApplication.Current?.Services.GetService<OturumServisi>();

        if (veri.TryGetValue("route", out var route) && !string.IsNullOrWhiteSpace(route))
        {
            if (veri.TryGetValue("bildirimId", out var bid) && !string.IsNullOrWhiteSpace(bid))
                route = route.Contains('?') ? $"{route}&bid={bid}" : $"{route}?bid={bid}";
            await RouteGitAsync(route, oturum);
            return;
        }

        if (veri.TryGetValue("talepId", out var talepId) && !string.IsNullOrWhiteSpace(talepId))
        {
            var hedef = $"talep-detay?id={talepId}";
            if (veri.TryGetValue("bildirimId", out var bid) && !string.IsNullOrWhiteSpace(bid))
                hedef += $"&bid={bid}";
            await RouteGitAsync(hedef, oturum);
        }
    }

    private static async Task BildirimOkunduYapAsync(string route, OturumServisi? oturum)
    {
        if (oturum?.Depo.AktifKullanici is not { } kullanici)
            return;

        var bid = BidCikar(route);
        if (bid is null)
            return;

        try
        {
            await oturum.VerileriYenileAsync();
            var bildirim = oturum.Depo.Bildirimler.FirstOrDefault(b => b.Id == bid.Value);
            if (bildirim is not null && !bildirim.Okundu)
                await oturum.Bildirimler.OkunduIsaretleAsync(bildirim);
        }
        catch
        {
            // okundu işareti isteğe bağlı
        }
    }

    private static Guid? BidCikar(string route)
    {
        var soru = route.IndexOf('?');
        if (soru < 0)
            return null;

        foreach (var parca in route[(soru + 1)..].Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            if (parca.StartsWith("bid=", StringComparison.Ordinal) &&
                Guid.TryParse(Uri.UnescapeDataString(parca[4..]), out var id))
                return id;
        }

        return null;
    }

    private static string BidTemizle(string route)
    {
        var soru = route.IndexOf('?');
        if (soru < 0)
            return route;

        var taban = route[..soru];
        var parcalar = route[(soru + 1)..]
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Where(p => !p.StartsWith("bid=", StringComparison.Ordinal))
            .ToArray();

        return parcalar.Length == 0 ? taban : $"{taban}?{string.Join("&", parcalar)}";
    }

    private static bool MevcutKokSekmesi(string location, string sekme)
    {
        if (string.IsNullOrWhiteSpace(location))
            return false;

        var soru = location.IndexOf('?', StringComparison.Ordinal);
        if (soru >= 0)
            location = location[..soru];

        var kok = location.IndexOf("//", StringComparison.Ordinal);
        var yol = kok >= 0 ? location[(kok + 2)..] : location;
        var segment = yol.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
        return string.Equals(segment, sekme, StringComparison.OrdinalIgnoreCase);
    }
}

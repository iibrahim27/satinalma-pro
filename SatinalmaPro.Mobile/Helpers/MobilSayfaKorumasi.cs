using SatinalmaPro.Mobile.Services;
using SatinalmaPro.Shared.Services;

namespace SatinalmaPro.Mobile.Helpers;

public static class MobilSayfaKorumasi
{
    public static async Task<bool> RotaErisimAsync(ContentPage sayfa, OturumServisi oturum, string route, string? mesaj = null)
    {
        if (MobilYetkiServisi.RotaGorebilir(oturum.Rol, route))
            return true;

        await sayfa.DisplayAlert("Yetki", mesaj ?? "Bu sayfaya erişim yetkiniz yok.", "Tamam");
        await Shell.Current.GoToAsync("//main");
        return false;
    }

    public static async Task<bool> StokDurumErisimAsync(ContentPage sayfa, OturumServisi oturum) =>
        await RotaErisimAsync(sayfa, oturum, "stok-durum", "Stok durumunu görüntüleme yetkiniz yok.");

    public static async Task<bool> StokHareketErisimAsync(ContentPage sayfa, OturumServisi oturum) =>
        await RotaErisimAsync(sayfa, oturum, "stok-hareket", "Stok hareketlerini görüntüleme yetkiniz yok.");

    public static async Task<bool> StokGirisErisimAsync(ContentPage sayfa, OturumServisi oturum) =>
        await RotaErisimAsync(sayfa, oturum, "stok-giris", "Stok girişi yetkiniz yok.");

    public static async Task<bool> StokCikisErisimAsync(ContentPage sayfa, OturumServisi oturum) =>
        await RotaErisimAsync(sayfa, oturum, "stok-cikis", "Stok çıkışı yetkiniz yok.");

    public static async Task<bool> StokSayimErisimAsync(ContentPage sayfa, OturumServisi oturum) =>
        await RotaErisimAsync(sayfa, oturum, "stok-sayim", "Stok sayım yetkiniz yok.");

    public static async Task<bool> AlinanMalzemeErisimAsync(ContentPage sayfa, OturumServisi oturum) =>
        await RotaErisimAsync(sayfa, oturum, "onaylanan-malzemeler", "Alınan malzemeler modülüne erişim yetkiniz yok.");

    public static async Task<bool> StackErisimAsync(ContentPage sayfa, OturumServisi oturum, string stackRoute, string? mesaj = null)
    {
        var soru = stackRoute.IndexOf('?');
        var taban = soru >= 0 ? stackRoute[..soru] : stackRoute;

        if (MobilYetkiServisi.RotaGorebilir(oturum.Rol, taban))
            return true;

        var parent = RolRouteServisi.StackRouteUstSekmesi(taban, oturum.Rol);
        if (parent is not null && MobilYetkiServisi.RotaGorebilir(oturum.Rol, parent))
            return true;

        await sayfa.DisplayAlert("Yetki", mesaj ?? "Bu sayfaya erişim yetkiniz yok.", "Tamam");
        await Shell.Current.GoToAsync("//main");
        return false;
    }
}

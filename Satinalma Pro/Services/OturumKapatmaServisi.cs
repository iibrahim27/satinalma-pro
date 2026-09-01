using System.Windows;
using SatinalmaPro.Helpers;
using SatinalmaPro.Views;

namespace SatinalmaPro.Services;

public enum OturumKapatmaSonuc
{
    Iptal,
    YerelMod,
    GirisIptal,
    Basarili,
    Hata
}

public sealed record GirisPenceresiMarka(
    string? PencereBasligi = null,
    string? Marka = null,
    string? AltBaslik = null,
    string? Aciklama = null);

/// <summary>Oturum kapatma ve yeniden giriş — Satınalma Pro ve Talep Pro ortak.</summary>
public static class OturumKapatmaServisi
{
    public static async Task<OturumKapatmaSonuc> KapatVeYenidenGirAsync(
        Window? sahip,
        GirisPenceresiMarka? marka = null,
        bool onayIste = true)
    {
        if (!OturumYoneticisi.BulutAktif)
            return OturumKapatmaSonuc.YerelMod;

        if (!OturumYoneticisi.GirisYapildi)
            return OturumKapatmaSonuc.Iptal;

        if (onayIste)
        {
            var onay = MessageBox.Show(
                "Oturumu kapatmak istiyor musunuz?",
                marka?.Marka ?? UygulamaBilgisi.Ad,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (onay != MessageBoxResult.Yes)
                return OturumKapatmaSonuc.Iptal;
        }

        try
        {
            BulutVeriSenkronu.YoklamayiDurdur();
            await BulutVeriSenkronu.BulutaGonderAsync().ConfigureAwait(true);
            OturumYoneticisi.CikisYap();

            var girisOk = marka is null
                ? GirisPenceresi.OturumAc(sahip)
                : GirisPenceresi.OturumAc(sahip, marka.PencereBasligi, marka.Marka, marka.AltBaslik, marka.Aciklama);

            if (!girisOk)
                return OturumKapatmaSonuc.GirisIptal;

            await BulutVeriSenkronu.BuluttanYukleAsync().ConfigureAwait(true);
            BulutVeriSenkronu.YoklamayiBaslat();
            BildirimYoneticisi.Baslat();
            return OturumKapatmaSonuc.Basarili;
        }
        catch (Exception ex)
        {
            HataGunlugu.Kaydet(ex, "OturumKapatmaServisi");
            MessageBox.Show(
                $"Çıkış sırasında hata: {ex.Message}",
                marka?.Marka ?? UygulamaBilgisi.Ad,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return OturumKapatmaSonuc.Hata;
        }
    }

    public static bool CikisButonuGorunur =>
        OturumYoneticisi.BulutAktif && OturumYoneticisi.GirisYapildi;
}

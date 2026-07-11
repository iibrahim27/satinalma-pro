using SatinalmaPro.Models;
using SatinalmaPro.Services;

namespace SatinalmaPro.Helpers;

/// <summary>Kritik iş akışı kayıtları — yerel + anında bulut senkronu.</summary>
public static class SatinalmaKayitYardimcisi
{
    public static async Task KaydetVeBulutaGonderAsync(SatinalmaTalep? talep = null)
    {
        if (talep is not null)
            SatinalmaTalepYardimcisi.Dokun(talep);

        SatinalmaDepo.Kaydet();
        await BulutaHemenGonderAsync();
    }

    public static async Task BulutaHemenGonderAsync()
    {
        if (!OturumYoneticisi.BulutAktif || !OturumYoneticisi.GirisYapildi)
            return;

        try
        {
            await BulutVeriSenkronu.TalepleriHemenGonderAsync();
        }
        catch (Exception ex)
        {
            // bulut gecikse yerel kayıt korunur
            HataGunlugu.Kaydet(ex, "SatinalmaKayitYardimcisi.BulutaHemenGonder");
        }
    }

    public static async Task MalKabulSonrasiBulutaGonderAsync()
    {
        if (!OturumYoneticisi.BulutAktif || !OturumYoneticisi.GirisYapildi)
            return;

        try
        {
            await BulutVeriSenkronu.MalKabulSonrasiHemenGonderAsync();
        }
        catch (Exception ex)
        {
            HataGunlugu.Kaydet(ex, "SatinalmaKayitYardimcisi.MalKabulSonrasiBulutaGonder");
        }
    }
}

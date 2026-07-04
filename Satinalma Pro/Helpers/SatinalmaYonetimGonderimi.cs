using SatinalmaPro.Models;
using SatinalmaPro.Services;
using SatinalmaPro.Shared.Helpers;

namespace SatinalmaPro.Helpers;

public static class SatinalmaYonetimGonderimi
{
    public static bool YenidenGonderebilir(SatinalmaTalep talep)
    {
        if (talep.Durum is SatinalmaTalepDurumlari.Reddedildi
            or SatinalmaTalepDurumlari.Onaylandi
            or SatinalmaTalepDurumlari.SiparisOlusturuldu
            or SatinalmaTalepDurumlari.Taslak)
            return false;

        if (talep.HerhangiKalemOnayli)
            return false;

        if (talep.TeklifsizYonetimOnayi && talep.YonetimOnayKilitli)
            return false;

        var teklifVar = (talep.Teklifler?.Count ?? 0) > 0;
        if (teklifVar)
            return talep.Durum is SatinalmaTalepDurumlari.TeklifGirisi
                or SatinalmaTalepDurumlari.Karsilastirma
                or SatinalmaTalepDurumlari.YonetimOnayinda
                or SatinalmaTalepDurumlari.ImzaSurecinde
                or SatinalmaTalepDurumlari.Hazirlaniyor;

        return talep.Durum is SatinalmaTalepDurumlari.ImzaSurecinde
            or SatinalmaTalepDurumlari.YonetimOnayinda
            or SatinalmaTalepDurumlari.Hazirlaniyor
            or SatinalmaTalepDurumlari.TeklifGirisi;
    }

    public static async Task YenidenGonderAsync(SatinalmaTalep talep)
    {
        if (!YenidenGonderebilir(talep))
            throw new InvalidOperationException("Bu talep yönetime yeniden gönderilemez.");

        var teklifVar = (talep.Teklifler?.Count ?? 0) > 0;

        if (teklifVar)
        {
            talep.Durum = SatinalmaTalepDurumlari.YonetimOnayinda;
            SatinalmaTalepYardimcisi.Dokun(talep);
            await SatinalmaKayitYardimcisi.KaydetVeBulutaGonderAsync(talep);

            try
            {
                await BildirimYoneticisi.GecersizleriOkunduYapAsync();
            }
            catch
            {
                // eski bildirimler temizlenemese de yeni bildirim gönderilir
            }

            await SatinalmaBildirimleri.TeklifOnaydaAsync(talep);
            return;
        }

        if (talep.Durum is SatinalmaTalepDurumlari.Hazirlaniyor or SatinalmaTalepDurumlari.TeklifGirisi)
            SatinalmaTalepYardimcisi.KayitOncesiHazirla(talep);

        talep.Durum = SatinalmaTalepDurumlari.ImzaSurecinde;
        SatinalmaTalepYardimcisi.Dokun(talep);
        await SatinalmaKayitYardimcisi.KaydetVeBulutaGonderAsync(talep);

        try
        {
            await BildirimYoneticisi.GecersizleriOkunduYapAsync();
            await SatinalmaBildirimleri.YonetimeGonderildiAsync(talep);
        }
        catch
        {
            // bildirim hatası kaydı engellemez
        }
    }
}

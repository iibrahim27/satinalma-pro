using SatinalmaPro.Helpers;
using SatinalmaPro.Models;

namespace SatinalmaPro.Services;

public static class IadeIslemleri
{
    public static void IadeOlustur(
        IadeKayit kayit,
        bool stoktanCikis,
        string malzeme,
        double miktar,
        string birim,
        string depoSaha,
        string teslimEdilen)
    {
        if (!KullaniciYetkileri.MalKabulVeStokAktarYapabilir())
            throw new InvalidOperationException("İade işlemi yalnızca Satınalma rolü tarafından yapılabilir.");

        if (string.IsNullOrWhiteSpace(kayit.Neden))
            throw new InvalidOperationException("İade nedeni girilmelidir.");

        if (miktar <= 0)
            throw new InvalidOperationException("Miktar sıfırdan büyük olmalıdır.");

        kayit.IadeNo = IadeDeposu.YeniIadeNoOlustur();
        kayit.Tarih = string.IsNullOrWhiteSpace(kayit.Tarih)
            ? DateTime.Now.ToString("dd.MM.yyyy")
            : kayit.Tarih;
        kayit.Durum = string.IsNullOrWhiteSpace(kayit.Durum) ? "İncelemede" : kayit.Durum;
        kayit.Miktar = $"{miktar:G} {birim}".Trim();
        kayit.MiktarSayi = miktar;
        kayit.Birim = birim;
        kayit.Malzeme = malzeme.Trim();

        if (stoktanCikis)
        {
            if (string.IsNullOrWhiteSpace(depoSaha))
                throw new InvalidOperationException("Stok çıkışı için depo / saha girilmelidir.");

            ModulVeriDeposu.Yukle();
            var tarih = kayit.Tarih;
            var belgeNo = kayit.IadeNo;
            var islemYapan = KullaniciYetkileri.AktifKullaniciAdi() ?? "";

            ModulVeriDeposu.BeginBatch();
            try
            {
                StokIslemServisi.CikisYap(
                    tarih,
                    malzeme.Trim(),
                    depoSaha.Trim(),
                    miktar,
                    belgeNo,
                    islemYapan,
                    string.IsNullOrWhiteSpace(teslimEdilen) ? kayit.Firma : teslimEdilen);
            }
            finally
            {
                ModulVeriDeposu.EndBatch();
            }

            kayit.DepoSaha = depoSaha.Trim();
            kayit.StokCikisiYapildi = true;
        }

        IadeDeposu.Ekle(kayit);
    }

    public static async Task IadeOlusturAsync(
        IadeKayit kayit,
        bool stoktanCikis,
        string malzeme,
        double miktar,
        string birim,
        string depoSaha,
        string teslimEdilen,
        CancellationToken iptal = default)
    {
        IadeOlustur(kayit, stoktanCikis, malzeme, miktar, birim, depoSaha, teslimEdilen);
        await IadeDeposu.KaydetAsync(iptal);
    }
}

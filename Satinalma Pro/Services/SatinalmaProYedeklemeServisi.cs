using System.IO;
using System.IO.Compression;
using SatinalmaPro.Models;

namespace SatinalmaPro.Services;

public static class SatinalmaProYedeklemeServisi
{
    public static string Yedekle(string hedefZipYolu)
    {
        SatinalmaProKlasor.Olustur();
        ModulVeriDeposu.KaydetTumu();
        SatinalmaDepo.Kaydet();
        UygulamaAyarDeposu.Kaydet();

        if (File.Exists(hedefZipYolu))
            File.Delete(hedefZipYolu);

        ZipFile.CreateFromDirectory(SatinalmaProKlasor.Yol, hedefZipYolu, CompressionLevel.Optimal, false);
        return hedefZipYolu;
    }

    public static void GeriYukle(string kaynakZipYolu)
    {
        if (!File.Exists(kaynakZipYolu))
            throw new FileNotFoundException("Yedek dosyası bulunamadı.", kaynakZipYolu);

        var gecici = Path.Combine(Path.GetTempPath(), $"satinalmapro_restore_{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(gecici);
            ZipFile.ExtractToDirectory(kaynakZipYolu, gecici, true);

            SatinalmaProKlasor.Olustur();
            foreach (var dosya in Directory.GetFiles(gecici, "*", SearchOption.AllDirectories))
            {
                var goreli = Path.GetRelativePath(gecici, dosya);
                var hedef = Path.Combine(SatinalmaProKlasor.Yol, goreli);
                Directory.CreateDirectory(Path.GetDirectoryName(hedef)!);
                File.Copy(dosya, hedef, true);
            }

            YenidenYukle();
        }
        finally
        {
            try { Directory.Delete(gecici, true); } catch { /* ignore */ }
        }
    }

    public static void ModulSifirla(string dosyaAdi)
    {
        switch (dosyaAdi)
        {
            case "uygulama_ayarlar.json":
                UygulamaAyarDeposu.VarsayilanaSifirla();
                break;
            case "satinalma_ayarlar.json":
                SatinalmaDepo.AyarlariSifirla();
                break;
            case "satinalma_talepler.json":
                SatinalmaDepo.TumTalepleriSifirla();
                break;
            case "alinan_malzemeler.json":
            case "stok.json":
            case "stok_hareketleri.json":
            case "agrega.json":
            case "cimento.json":
            case "akaryakit.json":
            case "filo.json":
                ModulVeriDeposu.Sifirla(dosyaAdi);
                break;
            case "finansman_gelir.json":
                FinansmanVeriDeposu.Sifirla();
                break;
            default:
                var yol = SatinalmaProKlasor.DosyaYolu(dosyaAdi);
                if (File.Exists(yol))
                    File.Delete(yol);
                break;
        }

        BulutVeriSenkronu.SifirlemeyiBulutaPlanla(dosyaAdi);
    }

    public static void TumVerileriSifirla()
    {
        foreach (var tanim in SatinalmaProVeriKatalogu.TumKayitlar)
            ModulSifirla(tanim.DosyaAdi);

        YenidenYukle();
    }

    public static void YenidenYukle()
    {
        SatinalmaDepo.YenidenYukle();
        ModulVeriDeposu.YenidenYukle();
        FinansmanVeriDeposu.YenidenYukle();
        UygulamaAyarDeposu.YenidenYukle();
    }
}

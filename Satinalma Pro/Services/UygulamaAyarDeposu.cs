using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using SatinalmaPro.Models;
using SatinalmaPro.Shared.SaaS;

namespace SatinalmaPro.Services;

public static class UygulamaAyarDeposu
{
    private static string Dosya =>
        SatinalmaProKlasor.DosyaYolu(KiraciDosyaAdi("uygulama_ayarlar.json"));

    private static string KiraciDosyaAdi(string dosyaAdi)
    {
        var tid = KiracıOturumu.TenantId;
        if (string.IsNullOrWhiteSpace(tid))
            return dosyaAdi;
        return $"{Path.GetFileNameWithoutExtension(dosyaAdi)}_{tid}{Path.GetExtension(dosyaAdi)}";
    }

    private static readonly JsonSerializerOptions JsonSecenekleri = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static UygulamaAyarlar Ayarlar { get; private set; } = new();
    private static bool _yuklendi;
    private static string? _yuklenenTenantId;

    /// <summary>Firma değişiminde eski firmanın ayarlarını bellekten temizler.</summary>
    public static void KiraciDegisti()
    {
        _yuklendi = false;
        _yuklenenTenantId = null;
        Ayarlar = new UygulamaAyarlar();
    }

    public static void Yukle()
    {
        var tid = KiracıOturumu.TenantId;
        if (_yuklendi && string.Equals(_yuklenenTenantId, tid, StringComparison.Ordinal))
            return;

        if (_yuklendi && !string.Equals(_yuklenenTenantId, tid, StringComparison.Ordinal))
            Ayarlar = new UygulamaAyarlar();

        _yuklendi = true;
        _yuklenenTenantId = tid;
        SatinalmaProKlasor.Olustur();
        SatinalmaProLogoDeposu.Olustur();

        if (File.Exists(Dosya))
        {
            try
            {
                var json = File.ReadAllText(Dosya);
                Ayarlar = JsonSerializer.Deserialize<UygulamaAyarlar>(json, JsonSecenekleri) ?? new UygulamaAyarlar();
            }
            catch
            {
                Ayarlar = new UygulamaAyarlar();
            }
        }
        else
        {
            Ayarlar = new UygulamaAyarlar();
        }

        // Oturumdaki kiracı adı varsa ayarlara yansıt (sol alt / PDF başlığı).
        if (!string.IsNullOrWhiteSpace(KiracıOturumu.TenantAd) &&
            string.IsNullOrWhiteSpace(Ayarlar.FirmaAdi))
        {
            Ayarlar.FirmaAdi = KiracıOturumu.TenantAd!;
        }

        SatinalmaFirmaBilgileriniGocEt();
        LogolariIceAktar();
        MalzemeKategoriDeposu.VarsayilanlariHazirla();
        MalzemeBirimDeposu.VarsayilanlariHazirla();
        FiloZimmetVarsayilanlariniHazirla();
    }

    private static void FiloZimmetVarsayilanlariniHazirla()
    {
        if (Ayarlar.FiloZimmetFormMaddeleri.Count > 0)
            return;

        Ayarlar.FiloZimmetFormMaddeleri =
        [
            "Araç anahtarı",
            "Ruhsat fotokopisi",
            "Yangın söndürücü",
            "İlk yardım çantası",
            "Üçgen reflektör",
            "Stepne ve kriko takımı"
        ];
        Kaydet();
    }

    private static void LogolariIceAktar()
    {
        var degisti = false;
        var firmaLogo = SatinalmaProLogoDeposu.IcIceAktar(Ayarlar.LogoDosyaYolu, "firma");
        if (firmaLogo != Ayarlar.LogoDosyaYolu)
        {
            Ayarlar.LogoDosyaYolu = firmaLogo;
            degisti = true;
        }

        var anasayfaLogo = SatinalmaProLogoDeposu.IcIceAktar(Ayarlar.AnasayfaLogoDosyaYolu, "anasayfa");
        if (anasayfaLogo != Ayarlar.AnasayfaLogoDosyaYolu)
        {
            Ayarlar.AnasayfaLogoDosyaYolu = anasayfaLogo;
            degisti = true;
        }

        if (degisti)
            Kaydet();
    }

    public static void Kaydet()
    {
        SatinalmaProKlasor.Olustur();
        var json = JsonSerializer.Serialize(Ayarlar, JsonSecenekleri);
        File.WriteAllText(Dosya, json);
        BulutVeriSenkronu.Planla("uygulama_ayarlar");
        MedyaBulutSenkronu.Planla();
    }

    public static void BuluttanYukle(string json)
    {
        try
        {
            Ayarlar = JsonSerializer.Deserialize<UygulamaAyarlar>(json, JsonSecenekleri) ?? new UygulamaAyarlar();
            // Buluttan gelen boş firma adını oturum adıyla doldur.
            if (string.IsNullOrWhiteSpace(Ayarlar.FirmaAdi) &&
                !string.IsNullOrWhiteSpace(KiracıOturumu.TenantAd))
            {
                Ayarlar.FirmaAdi = KiracıOturumu.TenantAd!;
            }
            SatinalmaProKlasor.Olustur();
            File.WriteAllText(Dosya, JsonSerializer.Serialize(Ayarlar, JsonSecenekleri));
        }
        catch
        {
            // yoksay
        }
    }

    private static void SatinalmaFirmaBilgileriniGocEt()
    {
        if (!string.IsNullOrWhiteSpace(Ayarlar.FirmaAdi)
            && !string.IsNullOrWhiteSpace(Ayarlar.LogoDosyaYolu))
            return;

        SatinalmaDepo.Yukle();
        var satinalma = SatinalmaDepo.Ayarlar;
        if (string.IsNullOrWhiteSpace(Ayarlar.FirmaAdi) && !string.IsNullOrWhiteSpace(satinalma.FirmaAdi))
            Ayarlar.FirmaAdi = satinalma.FirmaAdi;

        if (string.IsNullOrWhiteSpace(Ayarlar.LogoDosyaYolu)
            && !string.IsNullOrWhiteSpace(satinalma.LogoDosyaYolu)
            && File.Exists(SatinalmaProLogoDeposu.TamYol(satinalma.LogoDosyaYolu)))
            Ayarlar.LogoDosyaYolu = satinalma.LogoDosyaYolu;

        if (!string.IsNullOrWhiteSpace(Ayarlar.FirmaAdi) || !string.IsNullOrWhiteSpace(Ayarlar.LogoDosyaYolu))
            Kaydet();
    }

    public static void VarsayilanaSifirla()
    {
        var eskiYollar = new[]
        {
            UygulamaAyarDeposu.Ayarlar.LogoDosyaYolu,
            UygulamaAyarDeposu.Ayarlar.AnasayfaLogoDosyaYolu
        };
        foreach (var yol in eskiYollar.Select(SatinalmaProLogoDeposu.TamYol).Where(File.Exists))
        {
            try { File.Delete(yol); } catch { /* dosya kilitli olabilir */ }
        }

        SatinalmaProLogoDeposu.TumDosyalariSil();
        Ayarlar = new UygulamaAyarlar();
        Kaydet();
    }

    public static void YenidenYukle()
    {
        _yuklendi = false;
        _yuklenenTenantId = null;
        Yukle();
    }
}

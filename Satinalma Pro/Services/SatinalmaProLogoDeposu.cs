using System.IO;

namespace SatinalmaPro.Services;

/// <summary>
/// Logoları uygulama veri klasörü\logos\ altında saklar.
/// </summary>
public static class SatinalmaProLogoDeposu
{
    public static string LogolarKlasoru => Path.Combine(SatinalmaProKlasor.Yol, "logos");

    public static void Olustur()
    {
        SatinalmaProKlasor.Olustur();
        Directory.CreateDirectory(LogolarKlasoru);
    }

    /// <summary>Harici dosyayı logolar klasörüne kopyalar; göreli yol döner (örn. logos/firma_....png).</summary>
    public static string Kaydet(string kaynakDosya, string onEk)
    {
        Olustur();
        var uzanti = Path.GetExtension(kaynakDosya);
        if (string.IsNullOrWhiteSpace(uzanti))
            uzanti = ".png";

        var dosyaAdi = $"{onEk}_{DateTime.Now:yyyyMMdd_HHmmss}{uzanti}";
        var hedef = Path.Combine(LogolarKlasoru, dosyaAdi);
        File.Copy(kaynakDosya, hedef, overwrite: true);
        return GoreliYol(hedef);
    }

    /// <summary>Kayıtlı yolu (göreli veya mutlak) çözümler; dosya yoksa boş döner.</summary>
    public static string TamYol(string? kayitliYol)
    {
        if (string.IsNullOrWhiteSpace(kayitliYol))
            return "";

        if (Path.IsPathRooted(kayitliYol) && File.Exists(kayitliYol))
            return kayitliYol;

        var uygulamaAltinda = Path.Combine(SatinalmaProKlasor.Yol, kayitliYol);
        if (File.Exists(uygulamaAltinda))
            return uygulamaAltinda;

        var sadeceDosya = Path.Combine(LogolarKlasoru, Path.GetFileName(kayitliYol));
        if (File.Exists(sadeceDosya))
            return sadeceDosya;

        return "";
    }

    public static string GoreliYol(string tamYol)
    {
        if (tamYol.StartsWith(SatinalmaProKlasor.Yol, StringComparison.OrdinalIgnoreCase))
            return Path.GetRelativePath(SatinalmaProKlasor.Yol, tamYol);
        return tamYol;
    }

    /// <summary>Harici yolu içe aktarır veya mevcut iç yolu normalize eder.</summary>
    public static string IcIceAktar(string? kayitliYol, string onEk)
    {
        if (string.IsNullOrWhiteSpace(kayitliYol))
            return "";

        var tam = TamYol(kayitliYol);
        if (!string.IsNullOrEmpty(tam))
        {
            if (tam.StartsWith(LogolarKlasoru, StringComparison.OrdinalIgnoreCase))
                return GoreliYol(tam);

            if (tam.StartsWith(SatinalmaProKlasor.Yol, StringComparison.OrdinalIgnoreCase))
                return GoreliYol(tam);

            return Kaydet(tam, onEk);
        }

        if (Path.IsPathRooted(kayitliYol) && File.Exists(kayitliYol))
            return Kaydet(kayitliYol, onEk);

        return "";
    }

    public static string GorunenAd(string? kayitliYol) =>
        string.IsNullOrWhiteSpace(kayitliYol) ? "" : Path.GetFileName(kayitliYol.Replace('\\', '/'));

    /// <summary>logos klasöründeki tüm dosyaları siler (firma / anasayfa logo sıfırlama).</summary>
    public static void TumDosyalariSil()
    {
        if (!Directory.Exists(LogolarKlasoru))
            return;

        foreach (var dosya in Directory.GetFiles(LogolarKlasoru))
        {
            try { File.Delete(dosya); } catch { /* dosya kilitli olabilir */ }
        }
    }
}

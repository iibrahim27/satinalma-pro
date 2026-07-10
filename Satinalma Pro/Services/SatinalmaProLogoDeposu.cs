using System.IO;
using SatinalmaPro.Shared.SaaS;

namespace SatinalmaPro.Services;

/// <summary>
/// Logoları uygulama veri klasörü\logos\{tenantId}\ altında saklar — kiracılar birbirinin logosunu görmez.
/// </summary>
public static class SatinalmaProLogoDeposu
{
    public static string LogolarKlasoru
    {
        get
        {
            var tid = KiracıOturumu.TenantId;
            return string.IsNullOrWhiteSpace(tid)
                ? Path.Combine(SatinalmaProKlasor.Yol, "logos", "_legacy")
                : Path.Combine(SatinalmaProKlasor.Yol, "logos", tid);
        }
    }

    public static void Olustur()
    {
        SatinalmaProKlasor.Olustur();
        Directory.CreateDirectory(LogolarKlasoru);
    }

    /// <summary>Harici dosyayı kiracı logos klasörüne kopyalar; göreli yol döner.</summary>
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

        // Eski paylaşımlı logos\ klasöründen (kiracı alt klasörü öncesi) taşı.
        var legacy = Path.Combine(SatinalmaProKlasor.Yol, "logos", Path.GetFileName(kayitliYol));
        if (File.Exists(legacy) &&
            !string.Equals(Path.GetDirectoryName(legacy), LogolarKlasoru, StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                Olustur();
                var hedef = Path.Combine(LogolarKlasoru, Path.GetFileName(legacy));
                if (!File.Exists(hedef))
                    File.Copy(legacy, hedef, overwrite: false);
                return hedef;
            }
            catch
            {
                return legacy;
            }
        }

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

    /// <summary>Yalnızca aktif kiracının logos klasöründeki dosyaları siler.</summary>
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

using System.IO;

namespace SatinalmaPro.Services;

public static class FiloDosyaDeposu
{
    public static string FiloKlasoru => Path.Combine(SatinalmaProKlasor.Yol, "filo");

    public static void Olustur()
    {
        SatinalmaProKlasor.Olustur();
        Directory.CreateDirectory(FiloKlasoru);
    }

    public static string Kaydet(string kaynakDosya, string onEk)
    {
        Olustur();
        var uzanti = Path.GetExtension(kaynakDosya);
        if (string.IsNullOrWhiteSpace(uzanti))
            uzanti = ".png";

        var dosyaAdi = $"{onEk}_{DateTime.Now:yyyyMMdd_HHmmss}{uzanti}";
        var hedef = Path.Combine(FiloKlasoru, dosyaAdi);
        File.Copy(kaynakDosya, hedef, overwrite: true);
        return GoreliYol(hedef);
    }

    public static string TamYol(string? kayitliYol)
    {
        if (string.IsNullOrWhiteSpace(kayitliYol))
            return "";

        if (Path.IsPathRooted(kayitliYol) && File.Exists(kayitliYol))
            return kayitliYol;

        var uygulamaAltinda = Path.Combine(SatinalmaProKlasor.Yol, kayitliYol);
        if (File.Exists(uygulamaAltinda))
            return uygulamaAltinda;

        var sadeceDosya = Path.Combine(FiloKlasoru, Path.GetFileName(kayitliYol));
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

    public static string GorunenAd(string? kayitliYol) =>
        string.IsNullOrWhiteSpace(kayitliYol) ? "" : Path.GetFileName(kayitliYol.Replace('\\', '/'));
}

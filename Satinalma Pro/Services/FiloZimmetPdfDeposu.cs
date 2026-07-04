using System.IO;
using System.Text;

namespace SatinalmaPro.Services;

public static class FiloZimmetPdfDeposu
{
    public static string ZimmetKlasoru => Path.Combine(FiloDosyaDeposu.FiloKlasoru, "zimmetler");

    public static void Olustur()
    {
        FiloDosyaDeposu.Olustur();
        Directory.CreateDirectory(ZimmetKlasoru);
    }

    public static string YeniPdfTamYolu(string plaka, string soforAdi)
    {
        Olustur();
        var sofor = DosyaAdiTemizle(soforAdi);
        var plakaTemiz = DosyaAdiTemizle(plaka);
        var dosyaAdi = $"{sofor}_{plakaTemiz}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
        return Path.Combine(ZimmetKlasoru, dosyaAdi);
    }

    public static string GoreliYol(string tamYol) => FiloDosyaDeposu.GoreliYol(tamYol);

    public static string TamYol(string? goreliYol)
    {
        if (string.IsNullOrWhiteSpace(goreliYol))
            return "";

        var uygulamaAltinda = Path.Combine(SatinalmaProKlasor.Yol, goreliYol);
        if (File.Exists(uygulamaAltinda))
            return uygulamaAltinda;

        var filoAltinda = Path.Combine(ZimmetKlasoru, Path.GetFileName(goreliYol));
        if (File.Exists(filoAltinda))
            return filoAltinda;

        return FiloDosyaDeposu.TamYol(goreliYol);
    }

    public static bool MevcutMu(string? goreliYol) =>
        !string.IsNullOrWhiteSpace(goreliYol) && File.Exists(TamYol(goreliYol));

    public static void Sil(string? goreliYol)
    {
        var tam = TamYol(goreliYol);
        if (string.IsNullOrEmpty(tam) || !File.Exists(tam))
            return;

        try
        {
            File.Delete(tam);
        }
        catch
        {
            // yoksay
        }
    }

    public static void KlasoruAc()
    {
        Olustur();
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = ZimmetKlasoru,
            UseShellExecute = true
        });
    }

    private static string DosyaAdiTemizle(string metin)
    {
        if (string.IsNullOrWhiteSpace(metin))
            return "Sofor";

        var sb = new StringBuilder(metin.Trim());
        foreach (var c in Path.GetInvalidFileNameChars())
            sb.Replace(c, '_');

        var sonuc = sb.ToString().Replace(' ', '_');
        while (sonuc.Contains("__", StringComparison.Ordinal))
            sonuc = sonuc.Replace("__", "_", StringComparison.Ordinal);

        return sonuc.Trim('_');
    }
}

using System.IO;
using System.Text;

namespace SatinalmaYonetici.Services;

public static class HataGunlugu
{
    private static readonly object Kilit = new();

    public static string DosyaYolu
    {
        get
        {
            var klasor = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SatinalmaYonetici");
            Directory.CreateDirectory(klasor);
            return Path.Combine(klasor, "hata.log");
        }
    }

    public static void Kaydet(Exception ex, string kaynak = "")
    {
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("====");
            sb.AppendLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            if (!string.IsNullOrWhiteSpace(kaynak))
                sb.AppendLine($"Kaynak: {kaynak}");
            sb.AppendLine(ex.ToString());
            lock (Kilit)
                File.AppendAllText(DosyaYolu, sb.ToString(), Encoding.UTF8);
        }
        catch
        {
            // log yazılamazsa yoksay
        }
    }

    public static void Kaydet(string mesaj, string kaynak = "")
    {
        try
        {
            var satir =
                $"===={Environment.NewLine}{DateTime.Now:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}" +
                (string.IsNullOrWhiteSpace(kaynak) ? "" : $"Kaynak: {kaynak}{Environment.NewLine}") +
                mesaj + Environment.NewLine;
            lock (Kilit)
                File.AppendAllText(DosyaYolu, satir, Encoding.UTF8);
        }
        catch
        {
            // yoksay
        }
    }
}

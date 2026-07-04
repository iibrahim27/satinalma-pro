using System.IO;
using System.Text;

namespace SatinalmaPro.Services;

public static class HataGunlugu
{
    private static readonly object Kilit = new();
    private static readonly string Dosya = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SatinalmaPro",
        "hata-gunlugu.txt");

    public static void Kaydet(Exception ex, string kaynak)
    {
        try
        {
            lock (Kilit)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Dosya)!);
                var sb = new StringBuilder();
                sb.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {kaynak}");
                sb.AppendLine(ex.ToString());
                sb.AppendLine(new string('-', 60));
                File.AppendAllText(Dosya, sb.ToString(), Encoding.UTF8);
            }
        }
        catch
        {
            // günlük yazılamazsa uygulamayı düşürme
        }
    }
}

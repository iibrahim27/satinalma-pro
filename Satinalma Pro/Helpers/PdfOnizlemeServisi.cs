using System.IO;
using System.Windows;
using SatinalmaPro.Views.Modules;

namespace SatinalmaPro.Helpers;

public static class PdfOnizlemeServisi
{
    public static void Goster(Action<string> pdfUret, string onerilenDosyaAdi, string baslik)
    {
        var klasor = Path.Combine(Path.GetTempPath(), "SatinalmaPro", "onizleme");
        Directory.CreateDirectory(klasor);
        var dosya = Path.Combine(klasor, $"{Guid.NewGuid():N}_{onerilenDosyaAdi}");

        try
        {
            pdfUret(dosya);
            if (!File.Exists(dosya))
                throw new InvalidOperationException("PDF dosyası oluşturulamadı.");

            var pencere = new PdfOnizlemeWindow(dosya, onerilenDosyaAdi, baslik)
            {
                Owner = Application.Current?.MainWindow
            };
            pencere.ShowDialog();
        }
        catch (Exception ex)
        {
            try
            {
                if (File.Exists(dosya))
                    File.Delete(dosya);
            }
            catch { /* ignore */ }

            MessageBox.Show($"PDF oluşturulamadı:\n{ex.Message}", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

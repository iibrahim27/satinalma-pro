using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using SatinalmaPro.Helpers;

namespace SatinalmaPro.Views.Modules;

public partial class PdfOnizlemeWindow : Window
{
    private readonly string _kaynakDosya;
    private readonly string _onerilenDosyaAdi;

    public PdfOnizlemeWindow(string kaynakDosya, string onerilenDosyaAdi, string baslik)
    {
        _kaynakDosya = kaynakDosya;
        _onerilenDosyaAdi = onerilenDosyaAdi;
        InitializeComponent();
        Title = baslik;
        TxtBaslik.Text = baslik;
        TxtAlt.Text = onerilenDosyaAdi;
        Loaded += async (_, _) => await PdfYukleAsync();
        Closed += (_, _) => GeciciDosyayiSil();
    }

    private async Task PdfYukleAsync()
    {
        try
        {
            await PdfGoruntuleyici.EnsureCoreWebView2Async();
            PdfGoruntuleyici.Source = new Uri(Path.GetFullPath(_kaynakDosya));
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"PDF önizleme açılamadı:\n{ex.Message}\n\nDosya varsayılan uygulamada açılacak.",
                UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            VarsayilanUygulamadaAc();
        }
    }

    private void VarsayilanUygulamadaAc()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _kaynakDosya,
                UseShellExecute = true
            });
        }
        catch { /* ignore */ }
    }

    private void GeciciDosyayiSil()
    {
        try
        {
            if (_kaynakDosya.Contains(Path.Combine("SatinalmaPro", "onizleme"), StringComparison.OrdinalIgnoreCase)
                && File.Exists(_kaynakDosya))
                File.Delete(_kaynakDosya);
        }
        catch { /* ignore */ }
    }

    private void Yazdir_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _kaynakDosya,
                Verb = "print",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Yazdırılamadı:\n{ex.Message}", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Indir_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "PDF İndir",
            Filter = "PDF Dosyası (*.pdf)|*.pdf",
            FileName = _onerilenDosyaAdi
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            File.Copy(_kaynakDosya, dialog.FileName, overwrite: true);
            MessageBox.Show("PDF kaydedildi.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Kaydedilemedi:\n{ex.Message}", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Kapat_Click(object sender, RoutedEventArgs e) => Close();
}

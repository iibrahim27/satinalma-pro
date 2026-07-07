using System.Windows;
using SatinalmaPro.Models;
using SatinalmaPro.Services;

namespace SatinalmaPro.Views;

public partial class BildirimlerWindow : Window
{
    public BildirimlerWindow()
    {
        InitializeComponent();
        BildirimYoneticisi.BildirimlerDegisti += Yenile;
        Loaded += (_, _) => Yenile();
        Closed += (_, _) => BildirimYoneticisi.BildirimlerDegisti -= Yenile;
    }

    private void Yenile()
    {
        var liste = BildirimYoneticisi.KullaniciBildirimleri().OrderByDescending(b => b.OlusturmaTarihi).ToList();
        Liste.ItemsSource = liste;
        TxtBos.Visibility = liste.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void TumunuOkundu_Click(object sender, RoutedEventArgs e)
    {
        BtnTumunuOkundu.IsEnabled = false;
        try
        {
            await BildirimYoneticisi.TumunuOkunduIsaretleAsync();
        }
        catch (Exception ex)
        {
            HataGunlugu.Kaydet(ex, "Bildirimler.TumunuOkundu");
            MessageBox.Show($"İşlem tamamlanamadı: {ex.Message}", "Bildirimler",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            BtnTumunuOkundu.IsEnabled = true;
            Yenile();
        }
    }

    private async void Temizle_Click(object sender, RoutedEventArgs e)
    {
        var sonuc = MessageBox.Show(
            "Onay bekleyen teklif bildirimleri korunur. Diğer bildirimler silinecek.\nDevam edilsin mi?",
            "Bildirimleri Temizle",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (sonuc != MessageBoxResult.Yes)
            return;

        BtnTemizle.IsEnabled = false;
        try
        {
            await BildirimYoneticisi.TemizleAsync();
        }
        catch (Exception ex)
        {
            HataGunlugu.Kaydet(ex, "Bildirimler.Temizle");
            MessageBox.Show($"Temizleme tamamlanamadı: {ex.Message}", "Bildirimler",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            BtnTemizle.IsEnabled = true;
            Yenile();
        }
    }

    private async void Liste_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (Liste.SelectedItem is not BildirimKaydi bildirim)
            return;

        try
        {
            await BildirimYoneticisi.OkunduIsaretleAsync(bildirim);
            Liste.SelectedItem = null;
            MasaustuBildirimNavigasyon.BildirimdenGit(bildirim);
            Close();
        }
        catch (Exception ex)
        {
            HataGunlugu.Kaydet(ex, "Bildirimler.Ac");
            MessageBox.Show($"Bildirim açılamadı: {ex.Message}", "Bildirimler",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}

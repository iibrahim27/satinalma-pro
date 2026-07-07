using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;

namespace SatinalmaPro.Views.Modules;

public partial class IadeOlusturWindow : Window
{
    private sealed record KalemSecenegi(OnaylananMalzemeSatiri? Satir, string Etiket)
    {
        public static readonly KalemSecenegi Manuel = new(null, "— Manuel giriş —");
    }

    public IadeOlusturWindow()
    {
        InitializeComponent();

        CmbDurum.Items.Add("İncelemede");
        CmbDurum.Items.Add("Onaylandı");
        CmbDurum.Items.Add("Reddedildi");
        CmbDurum.SelectedIndex = 0;

        var kalemler = new List<KalemSecenegi> { KalemSecenegi.Manuel };
        kalemler.AddRange(
            SatinalmaDepo.OnaylananMalzemeleriOlustur()
                .Where(s => s.KabulEdilenMiktar > 0.0001)
                .OrderByDescending(s => s.Tarih)
                .ThenBy(s => s.Malzeme)
                .Select(s => new KalemSecenegi(
                    s,
                    $"{s.Malzeme} · Kabul: {s.KabulEdilenMiktar:G} {s.Birim} · {(string.IsNullOrWhiteSpace(s.SiparisNo) ? s.TalepNo : s.SiparisNo)}")));

        CmbKalem.ItemsSource = kalemler;
        CmbKalem.SelectedIndex = 0;
    }

    private void KalemSecildi(object sender, SelectionChangedEventArgs e)
    {
        if (CmbKalem.SelectedItem is not KalemSecenegi secim || secim.Satir is null)
            return;

        var s = secim.Satir;
        TxtSiparisNo.Text = string.IsNullOrWhiteSpace(s.SiparisNo) ? s.TalepNo : s.SiparisNo;
        TxtFirma.Text = s.Firma;
        TxtMalzeme.Text = s.Malzeme;
        TxtMiktar.Text = s.KabulEdilenMiktar.ToString("G", CultureInfo.CurrentCulture);
        TxtBirim.Text = s.Birim;
        TxtTeslimEdilen.Text = s.Firma;
    }

    private void StokCikis_Changed(object sender, RoutedEventArgs e)
    {
        var aktif = ChkStokCikis.IsChecked == true;
        LblDepo.Visibility = aktif ? Visibility.Visible : Visibility.Collapsed;
        TxtDepo.Visibility = aktif ? Visibility.Visible : Visibility.Collapsed;
        LblTeslimEdilen.Visibility = aktif ? Visibility.Visible : Visibility.Collapsed;
        TxtTeslimEdilen.Visibility = aktif ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void Kaydet_Click(object sender, RoutedEventArgs e)
    {
        if (!MiktarOku(out var miktar))
            return;

        var malzeme = TxtMalzeme.Text.Trim();
        if (string.IsNullOrWhiteSpace(malzeme))
        {
            MessageBox.Show("Malzeme adını girin.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var birim = TxtBirim.Text.Trim();
        if (string.IsNullOrWhiteSpace(birim))
            birim = "Adet";

        var neden = TxtNeden.Text.Trim();
        if (string.IsNullOrWhiteSpace(neden))
        {
            MessageBox.Show("İade nedenini girin.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtNeden.Focus();
            return;
        }

        var secim = CmbKalem.SelectedItem as KalemSecenegi;
        var kayit = new IadeKayit
        {
            SiparisNo = TxtSiparisNo.Text.Trim(),
            Firma = TxtFirma.Text.Trim(),
            Malzeme = malzeme,
            Neden = neden,
            Durum = CmbDurum.SelectedItem?.ToString() ?? "İncelemede",
            TalepId = secim?.Satir?.TalepId,
            KalemId = secim?.Satir?.KalemId
        };

        var stokCikis = ChkStokCikis.IsChecked == true;
        var depo = TxtDepo.Text.Trim();
        var teslimEdilen = TxtTeslimEdilen.Text.Trim();

        try
        {
            await IadeIslemleri.IadeOlusturAsync(
                kayit,
                stokCikis,
                malzeme,
                miktar,
                birim,
                depo,
                teslimEdilen);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var mesaj = stokCikis
            ? "İade kaydı oluşturuldu ve depodan stok çıkışı yapıldı."
            : "İade kaydı oluşturuldu.";

        MessageBox.Show(mesaj, UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
        DialogResult = true;
        Close();
    }

    private bool MiktarOku(out double miktar)
    {
        miktar = 0;
        var metin = TxtMiktar.Text.Trim();
        if (!double.TryParse(metin.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out miktar) &&
            !double.TryParse(metin, NumberStyles.Any, CultureInfo.CurrentCulture, out miktar))
        {
            MessageBox.Show("Geçerli bir miktar girin.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (miktar <= 0)
        {
            MessageBox.Show("Miktar sıfırdan büyük olmalıdır.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        return true;
    }

    private void Iptal_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

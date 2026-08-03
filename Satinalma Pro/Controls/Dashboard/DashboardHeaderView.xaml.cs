using System.Windows;
using System.Windows.Controls;
using SatinalmaPro.Helpers;
using SatinalmaPro.Services;

namespace SatinalmaPro.Controls.Dashboard;

public partial class DashboardHeaderView : UserControl
{
    public event EventHandler? BildirimTiklandi;
    public event EventHandler? AyarlarTiklandi;
    public event Action<string>? AramaMetniDegisti;

    public DashboardHeaderView()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            FirmaEtiketiniGuncelle();
            TalepProButonunuGuncelle();
        };
        OturumYoneticisi.OturumDegisti += () =>
            Dispatcher.BeginInvoke(TalepProButonunuGuncelle);
    }

    public void BreadcrumbAyarla(string metin) =>
        TxtBreadcrumb.Text = metin is "Ana Sayfa" or "Dashboard" or "Kontrol Merkezi" ? "Workspace" : metin;

    public void FirmaEtiketiniGuncelle()
    {
        var firma = UygulamaAyarDeposu.Ayarlar.FirmaAdi;
        TxtFirmaKisa.Text = string.IsNullOrWhiteSpace(firma) ? "Merkez Satınalma" : firma;
    }

    public void BildirimRozetiniGuncelle(int sayi)
    {
        BtnBildirim.Visibility = OturumYoneticisi.GirisYapildi ? Visibility.Visible : Visibility.Collapsed;
        BadgeBildirim.Visibility = sayi > 0 ? Visibility.Visible : Visibility.Collapsed;
        TxtBadgeSayi.Text = sayi > 99 ? "99+" : sayi.ToString();
        AyarlarButonunuGuncelle();
        TalepProButonunuGuncelle();
    }

    public void AyarlarButonunuGuncelle()
    {
        BtnAyarlar.Visibility = KullaniciYetkileri.ModulGorebilir("Ayarlar")
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public void TalepProButonunuGuncelle()
    {
        BtnTalepPro.Visibility = OturumYoneticisi.GirisYapildi
                                 && KullaniciYetkileri.ModulGorebilir("Satınalma")
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void BtnTalepPro_Click(object sender, RoutedEventArgs e) =>
        UygulamaKoordinasyonu.TalepProAc();

    private void BtnBildirim_Click(object sender, RoutedEventArgs e) =>
        BildirimTiklandi?.Invoke(this, EventArgs.Empty);

    private void BtnAyarlar_Click(object sender, RoutedEventArgs e)
    {
        if (!KullaniciYetkileri.ModulGorebilir("Ayarlar"))
            return;
        AyarlarTiklandi?.Invoke(this, EventArgs.Empty);
    }

    private void TxtArama_TextChanged(object sender, TextChangedEventArgs e) =>
        AramaMetniDegisti?.Invoke(TxtArama.Text);
}

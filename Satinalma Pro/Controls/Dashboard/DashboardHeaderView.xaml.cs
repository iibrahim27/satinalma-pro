using System.Windows;
using System.Windows.Controls;
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
    }

    public void BreadcrumbAyarla(string metin) => TxtBreadcrumb.Text = metin;

    public void BildirimRozetiniGuncelle(int sayi)
    {
        BtnBildirim.Visibility = OturumYoneticisi.GirisYapildi ? Visibility.Visible : Visibility.Collapsed;
        BadgeBildirim.Visibility = sayi > 0 ? Visibility.Visible : Visibility.Collapsed;
        TxtBadgeSayi.Text = sayi > 99 ? "99+" : sayi.ToString();
        AyarlarButonunuGuncelle();
    }

    public void AyarlarButonunuGuncelle()
    {
        BtnAyarlar.Visibility = KullaniciYetkileri.ModulGorebilir("Ayarlar")
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void BtnBildirim_Click(object sender, RoutedEventArgs e) =>
        BildirimTiklandi?.Invoke(this, EventArgs.Empty);

    private void BtnAyarlar_Click(object sender, RoutedEventArgs e)
    {
        if (!KullaniciYetkileri.ModulGorebilir("Ayarlar"))
            return;
        AyarlarTiklandi?.Invoke(this, EventArgs.Empty);
    }

    private void BtnTema_Click(object sender, RoutedEventArgs e)
    {
        TemaIkon.Kind = TemaIkon.Kind == DashboardIconKind.Moon
            ? DashboardIconKind.Sun
            : DashboardIconKind.Moon;
    }

    private void TxtArama_TextChanged(object sender, TextChangedEventArgs e) =>
        AramaMetniDegisti?.Invoke(TxtArama.Text);
}

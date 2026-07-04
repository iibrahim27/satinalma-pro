using System.Windows;
using System.Windows.Controls;
using SatinalmaPro.Services;

namespace SatinalmaPro.Controls.Dashboard;

public partial class DashboardHeaderView : UserControl
{
    private bool _koyuTema;

    public event EventHandler? BildirimTiklandi;
    public event EventHandler? HizliIslemTiklandi;
    public event Action<string>? HizliIslemSecildi;

    public DashboardHeaderView()
    {
        InitializeComponent();
        Loaded += (_, _) => KarsilamayiGuncelle();
    }

    public void KarsilamayiGuncelle()
    {
        var ad = OturumYoneticisi.AktifKullanici?.AdSoyad ?? "Kullanıcı";
        var hitap = IlkIsim(ad);
        TxtKarsilama.Text = $"Hoş geldiniz, {hitap} 👋";
    }

    public void BildirimRozetiniGuncelle(int sayi)
    {
        BtnBildirim.Visibility = OturumYoneticisi.GirisYapildi ? Visibility.Visible : Visibility.Collapsed;
        BadgeBildirim.Visibility = sayi > 0 ? Visibility.Visible : Visibility.Collapsed;
        TxtBadgeSayi.Text = sayi > 99 ? "99+" : sayi.ToString();
    }

    private void BtnBildirim_Click(object sender, RoutedEventArgs e) =>
        BildirimTiklandi?.Invoke(this, EventArgs.Empty);

    private void BtnTema_Click(object sender, RoutedEventArgs e)
    {
        _koyuTema = !_koyuTema;
        TemaIkon.Kind = _koyuTema ? Services.DashboardIconKind.Sun : Services.DashboardIconKind.Moon;
    }

    private void BtnHizliIslem_Click(object sender, RoutedEventArgs e)
    {
        var menu = new ContextMenu
        {
            PlacementTarget = BtnHizliIslem,
            IsOpen = true
        };

        EkleMenu("Yeni Satınalma Talebi", "Satınalma");
        EkleMenu("Stok Girişi", "Stok Yönetimi");
        EkleMenu("Alınan Malzeme Kaydı", "Alınan Malzemeler");
        HizliIslemTiklandi?.Invoke(this, EventArgs.Empty);

        void EkleMenu(string baslik, string modul)
        {
            var oge = new MenuItem { Header = baslik };
            oge.Click += (_, _) => HizliIslemSecildi?.Invoke(modul);
            menu.Items.Add(oge);
        }
    }

    private static string IlkIsim(string adSoyad)
    {
        if (string.IsNullOrWhiteSpace(adSoyad))
            return "Kullanıcı";

        var parca = adSoyad.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parca.Length > 0 ? parca[0] : adSoyad;
    }
}

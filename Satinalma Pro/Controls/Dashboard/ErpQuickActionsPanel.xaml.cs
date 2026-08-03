using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;

namespace SatinalmaPro.Controls.Dashboard;

public partial class ErpQuickActionsPanel : UserControl
{
    public event Action<string>? ModulSecildi;

    public ErpQuickActionsPanel()
    {
        InitializeComponent();
        Loaded += (_, _) => RolIcinAyarla(OturumYoneticisi.AktifKullanici?.Rol);
    }

    public void RolIcinAyarla(string? rol)
    {
        ActionsGrid.Children.Clear();
        var key = KullaniciRolleri.Normalize(rol);

        if (key == KullaniciRolleri.Depo)
        {
            Ekle("Stok Girişi", DashboardIconKind.Package, "Stok Yönetimi");
            Ekle("Stok Çıkışı", DashboardIconKind.Warehouse, "Stok Yönetimi");
            Ekle("Stok Durumu", DashboardIconKind.ClipboardList, "Stok Yönetimi");
            Ekle("Hareketler", DashboardIconKind.FileBarChart, "Stok Yönetimi");
            Ekle("Alınan Malzeme", DashboardIconKind.ShoppingCart, "Alınan Malzemeler");
            Ekle("Stok Kartı", DashboardIconKind.Package, "Stok Yönetimi");
            return;
        }

        if (key is KullaniciRolleri.Sef or KullaniciRolleri.Saha)
        {
            Ekle("Talep Pro", DashboardIconKind.ShoppingCart, "__TalepPro__");
            Ekle("Stok Durumu", DashboardIconKind.Warehouse, "Stok Yönetimi");
            Ekle("Filo", DashboardIconKind.Package, "Araç Filo Takip");
            Ekle("Alınan Malzeme", DashboardIconKind.ClipboardList, "Alınan Malzemeler");
            Ekle("Raporlar", DashboardIconKind.FileBarChart, "Raporlamalar");
            return;
        }

        if (key == KullaniciRolleri.Atolye)
        {
            Ekle("Stok Durumu", DashboardIconKind.Warehouse, "Stok Yönetimi");
            Ekle("Stok Hareket", DashboardIconKind.FileBarChart, "Stok Yönetimi");
            Ekle("Alınan Malzeme", DashboardIconKind.Package, "Alınan Malzemeler");
            Ekle("Stok Kartı", DashboardIconKind.ClipboardList, "Stok Yönetimi");
            return;
        }

        Ekle("Malzeme Kaydı", DashboardIconKind.Package, "Alınan Malzemeler");
        Ekle("Tahsilat Girişi", DashboardIconKind.Wallet, "Finansman Raporlama");
        Ekle("Stok Kartı", DashboardIconKind.Warehouse, "Stok Yönetimi");
        Ekle("Filo", DashboardIconKind.Package, "Araç Filo Takip");
        Ekle("Raporlar", DashboardIconKind.FileBarChart, "Raporlamalar");
        Ekle("Talep Pro", DashboardIconKind.ShoppingCart, "__TalepPro__");
    }

    private void Ekle(string baslik, DashboardIconKind icon, string modul)
    {
        var btn = new Button
        {
            Style = (Style)FindResource("DashQuickActionButtonStyle"),
            Tag = modul,
            Margin = new Thickness(4),
            Content = new StackPanel
            {
                Children =
                {
                    new IconControl
                    {
                        Kind = icon,
                        IconSize = 20,
                        StrokeBrush = (Brush)FindResource("DashPrimaryBrush"),
                        HorizontalAlignment = HorizontalAlignment.Center
                    },
                    new TextBlock
                    {
                        Text = baslik,
                        FontSize = 12,
                        Margin = new Thickness(0, 8, 0, 0),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Foreground = (Brush)FindResource("DashTextBrush"),
                        TextWrapping = TextWrapping.Wrap,
                        TextAlignment = TextAlignment.Center
                    }
                }
            }
        };
        btn.Click += HizliIslem_Click;
        ActionsGrid.Children.Add(btn);
    }

    private void HizliIslem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string modul)
            return;
        if (modul == "__TalepPro__")
        {
            UygulamaKoordinasyonu.TalepProAc();
            return;
        }

        ModulSecildi?.Invoke(modul);
    }
}

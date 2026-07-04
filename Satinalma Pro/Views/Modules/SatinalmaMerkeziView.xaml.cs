using System.Windows;
using System.Windows.Controls;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models.SatinalmaMerkezi;
using SatinalmaPro.Services;
using SatinalmaPro.ViewModels;
using SatinalmaPro.Views;

namespace SatinalmaPro.Views.Modules;

public partial class SatinalmaMerkeziView : UserControl, IModulKlavyeKisayollari
{
    private readonly SatinalmaMerkeziViewModel _vm;

    public SatinalmaMerkeziView()
    {
        InitializeComponent();
        _vm = new SatinalmaMerkeziViewModel();
        _vm.BildirimPanelAc += BildirimPaneliniGoster;
        _vm.OperasyonModuIstendi += OperasyonModunaGec;
        DataContext = _vm;
    }

    public void KisayolYenile() => _vm.Yukle();

    public void BildirimdenAc(Guid? talepId, int adim = 0, string sekme = "talepler") =>
        _vm.BildirimdenAc(talepId, adim, sekme);

    private void Sekme_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string ad })
            _vm.AktifSekme = ad;
    }

    private void Filtre_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string f })
            _vm.AktifFiltre = f;
    }

    private void GridTalepler_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (GridTalepler.SelectedItem is TalepSatirModel t)
            _vm.TalepSec(t);
    }

    private void GridSiparisler_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is DataGrid { SelectedItem: SiparisSatirModel s })
            _vm.SiparisSec(s);
    }

    private void BildirimPaneliniGoster()
    {
        if (Window.GetWindow(this) is not MainWindow mw)
            return;

        var pencere = new BildirimlerWindow { Owner = mw };
        pencere.ShowDialog();
        _vm.Yukle();
    }

    private void OperasyonModunaGec(Guid? talepId, string sekme)
    {
        if (Window.GetWindow(this) is MainWindow mw)
            mw.SatinalmaOperasyonModunaGec(talepId, sekme);
    }
}

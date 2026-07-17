using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;
using SatinalmaPro.Services.Procurement;
using SatinalmaPro.Shared.Procurement;

namespace SatinalmaPro.Views.Modules.Satinalma.Part1;

public partial class MalKabulTalepListesiView : UserControl
{
    public event Action<SatinalmaTalep>? TalepSecildi;
    public event Action? SiparislereGitIstendi;

    public MalKabulTalepListesiView()
    {
        InitializeComponent();
        TalepHoverOnizleme.Etkinlestir(Tablo);
        TxtYardim.Text = "Mal kabulü tamamlanan taleplerin arşivi. Satır üzerine gelerek kalem, firma ve teslim özetini görün.";
    }

    public void Yenile()
    {
        var uid = OturumYoneticisi.AktifKullanici?.Uid;
        var ad = KullaniciYetkileri.AktifKullaniciAdi();

        var liste = ProcurementTalepSorguServisi.Listele(SatinalmaRoutes.SatinalmaMalKabul)
            .OrderByDescending(t => t.Tarih)
            .ThenByDescending(t => t.TalepNo)
            .Select(t => new MalKabulTalepListeSatiri(t, uid, ad))
            .ToList();

        Tablo.ItemsSource = liste;
        var bos = liste.Count == 0;
        BosPanel.Visibility = bos ? Visibility.Visible : Visibility.Collapsed;
        Tablo.Visibility = bos ? Visibility.Collapsed : Visibility.Visible;
    }

    private void Tablo_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (Tablo.SelectedItem is MalKabulTalepListeSatiri satir)
            TalepSecildi?.Invoke(satir.Talep);
    }

    private void SiparislereGit_Click(object sender, RoutedEventArgs e) =>
        SiparislereGitIstendi?.Invoke();
}

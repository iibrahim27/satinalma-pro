using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;
using SatinalmaPro.Services.Procurement;
using SatinalmaPro.Shared.Procurement;

namespace SatinalmaPro.Views.Modules.Satinalma.Part1;

public partial class SiparisVerilenTalepListesiView : UserControl
{
    public event Action<SatinalmaTalep>? TalepSecildi;
    public event Action? OnaylananlaraGitIstendi;
    public event Action? MalKabuleGitIstendi;

    public SiparisVerilenTalepListesiView()
    {
        InitializeComponent();
        TalepHoverOnizleme.Etkinlestir(Tablo);
        TxtYardim.Text = "Sipariş verilmiş ve mal kabulü devam eden talepler. Satır üzerine gelerek kalemleri, firmayı ve kabul ilerlemesini görün.";
    }

    public void Yenile()
    {
        var uid = OturumYoneticisi.AktifKullanici?.Uid;
        var ad = KullaniciYetkileri.AktifKullaniciAdi();

        var liste = ProcurementTalepSorguServisi.Listele(SatinalmaRoutes.SatinalmaSiparis)
            .OrderByDescending(t => t.Tarih)
            .ThenByDescending(t => t.TalepNo)
            .Select(t => new SiparisVerilenTalepListeSatiri(t, uid, ad))
            .ToList();

        Tablo.ItemsSource = liste;
        BosDurumuGuncelle(liste.Count == 0);
    }

    private void BosDurumuGuncelle(bool bos)
    {
        BosPanel.Visibility = bos ? Visibility.Visible : Visibility.Collapsed;
        Tablo.Visibility = bos ? Visibility.Collapsed : Visibility.Visible;

        if (!bos)
            return;

        var tamamlananVar = SatinalmaDepo.Talepler.Any(SatinalmaPart1Filtreleri.SatinalmaMalKabulEdilmis);
        var onayBekleyenSiparis = SatinalmaDepo.Talepler.Any(SatinalmaPart1Filtreleri.SatinalmaOnaylanan);

        if (tamamlananVar)
        {
            TxtBosBaslik.Text = "Tüm siparişlerin mal kabulü tamamlandı";
            TxtBosAciklama.Text = "Tamamlanan talepleri «Mal Kabul» sekmesinden görüntüleyebilirsiniz.";
            BtnMalKabuleGit.Visibility = Visibility.Visible;
            BtnOnaylananlaraGit.Visibility = Visibility.Collapsed;
        }
        else if (onayBekleyenSiparis)
        {
            TxtBosBaslik.Text = "Sipariş verilmiş talep yok";
            TxtBosAciklama.Text = "Önce «Onaylanan Teklifler» sekmesinden talebi açıp «Sipariş Ver» ile sipariş oluşturun. Malzeme geldiğinde burada mal kabul yaparsınız.";
            BtnOnaylananlaraGit.Visibility = Visibility.Visible;
            BtnMalKabuleGit.Visibility = Visibility.Collapsed;
        }
        else
        {
            TxtBosBaslik.Text = "Mal kabul bekleyen sipariş yok";
            TxtBosAciklama.Text = "Sipariş verilmiş ve mal kabulü bekleyen talep bulunmuyor. Talep onaylandıktan ve sipariş oluşturulduktan sonra burada listelenir.";
            BtnOnaylananlaraGit.Visibility = Visibility.Collapsed;
            BtnMalKabuleGit.Visibility = Visibility.Collapsed;
        }
    }

    private void Tablo_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (Tablo.SelectedItem is SiparisVerilenTalepListeSatiri satir)
            TalepSecildi?.Invoke(satir.Talep);
    }

    private void OnaylananlaraGit_Click(object sender, RoutedEventArgs e) =>
        OnaylananlaraGitIstendi?.Invoke();

    private void MalKabuleGit_Click(object sender, RoutedEventArgs e) =>
        MalKabuleGitIstendi?.Invoke();
}

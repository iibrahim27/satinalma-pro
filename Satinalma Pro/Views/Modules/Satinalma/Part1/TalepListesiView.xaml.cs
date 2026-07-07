using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;
using SatinalmaPro.Services.Procurement;
using SatinalmaPro.Shared.Procurement;

namespace SatinalmaPro.Views.Modules.Satinalma.Part1;

public partial class TalepListesiView : UserControl
{
    public event Action<SatinalmaTalep>? TalepSecildi;

    private string _route = SatinalmaPart1Menusu.SatinalmaTalepler;

    public TalepListesiView() => InitializeComponent();

    public void Goster(string route)
    {
        _route = route;
        TxtYardim.Text = route switch
        {
            SatinalmaRoutes.YonetimGelenTalepler => "Onay, teklif iste veya red için satıra çift tıklayın.",
            SatinalmaRoutes.YonetimTeklifGirilen => "Teklif inceleme için satıra çift tıklayın.",
            SatinalmaRoutes.YonetimDirekOnaylanan => "Direk onaylanan talep detayı için çift tıklayın.",
            SatinalmaRoutes.YonetimRedVerilen => "Red verilen talep detayı için çift tıklayın.",
            SatinalmaRoutes.YonetimGecmis => "Geçmiş talep detayı için çift tıklayın.",
            SatinalmaRoutes.SatinalmaTeklifIstenen => "Teklif girmek için satıra çift tıklayın.",
            SatinalmaRoutes.SatinalmaTeklifGirilen => "Yönetim onayı bekleyen teklifler — durum takibi için listede görünür.",
            SatinalmaRoutes.SatinalmaTeklifDuzeltme => "Yönetim düzeltme notu ile geri gönderdi — teklifleri düzenleyip yeniden gönderin.",
            SatinalmaRoutes.SatinalmaKarsilastirma => "Teklifleri karşılaştırmak için satıra çift tıklayın.",
            SatinalmaRoutes.Taleplerim => "Detay için satıra çift tıklayın.",
            _ => "Detay için satıra çift tıklayın."
        };
        Yenile();
    }

    public void Yenile() => _ = YenileAsync();

    private async Task YenileAsync()
    {
        if (DesktopRoleTabManager.GetDataFilter(
                _route,
                OturumYoneticisi.AktifKullanici?.Rol,
                OturumYoneticisi.AktifKullanici?.Uid) is null)
        {
            Tablo.ItemsSource = null;
            return;
        }

        var liste = await ProcurementTalepSorguServisi.ListeleAsync(_route);
        Tablo.ItemsSource = liste
            .Select(t => new TalepListeSatiriPart1(t))
            .ToList();
    }

    private void Tablo_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (Tablo.SelectedItem is TalepListeSatiriPart1 satir)
            TalepSecildi?.Invoke(satir.Talep);
    }
}

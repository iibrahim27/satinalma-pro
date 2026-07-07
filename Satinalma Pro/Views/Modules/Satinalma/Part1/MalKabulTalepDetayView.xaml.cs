using System.Windows;
using System.Windows.Controls;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;

namespace SatinalmaPro.Views.Modules.Satinalma.Part1;

public partial class MalKabulTalepDetayView : UserControl
{
    private SatinalmaTalep? _talep;

    public event Action? Geri;

    public MalKabulTalepDetayView() => InitializeComponent();

    public void Yukle(SatinalmaTalep talep)
    {
        _talep = SatinalmaDepo.Talepler.FirstOrDefault(t => t.Id == talep.Id) ?? talep;
        ArayuzuGuncelle();
    }

    private void ArayuzuGuncelle()
    {
        var talep = GuncelTalep();
        if (talep is null)
            return;

        _talep = talep;
        var siparisNo = string.IsNullOrWhiteSpace(talep.SiparisNo) ? talep.TalepNo : talep.SiparisNo;
        TxtBaslik.Text = $"Mal Kabul — {talep.TalepNo}";
        TxtOzet.Text = $"{talep.Tarih} · {talep.TalepEden} · Sipariş No: {siparisNo} · Depo ve Alınan Malzemeler kayıtlı";

        KalemTablosu.ItemsSource = SatinalmaDepo.OnaylananMalzemeleriOlustur()
            .Where(s => s.TalepId == talep.Id)
            .Select(s => new SiparisKalemSatiri(s))
            .OrderBy(s => s.Malzeme)
            .ToList();
    }

    private SatinalmaTalep? GuncelTalep() =>
        _talep is null ? null : SatinalmaDepo.Talepler.FirstOrDefault(t => t.Id == _talep.Id) ?? _talep;

    private void Geri_Click(object sender, RoutedEventArgs e) => Geri?.Invoke();
}

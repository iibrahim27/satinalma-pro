using System.Windows;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;

namespace SatinalmaPro.Views.Modules.Satinalma.Part1;

public partial class TalepMiktarDuzenlemeWindow : Window
{
    private readonly SatinalmaTalep _talep;
    private readonly Dictionary<Guid, double> _baslangicMiktarlari;
    private readonly List<SatinalmaTalepKalemi> _kaldirilanKalemler = [];
    private bool _kaydedildi;

    public TalepMiktarDuzenlemeWindow(SatinalmaTalep talep)
    {
        _talep = talep;
        talep.Kalemler ??= [];
        _baslangicMiktarlari = talep.Kalemler.ToDictionary(k => k.Id, k => k.Miktar);

        InitializeComponent();
        TxtAciklama.Text =
            $"{talep.TalepNo} — miktarı değiştirin veya kalem silin. " +
            "Teklif girilmiş olsa bile değişiklik yapılabilir; teklifler buna göre revize edilir.";
        ListeyiYenile();
    }

    private void ListeyiYenile()
    {
        KalemTablosu.ItemsSource = null;
        KalemTablosu.ItemsSource = _talep.Kalemler.OrderBy(k => k.SiraNo).ToList();
    }

    private void KalemSil_Click(object sender, RoutedEventArgs e)
    {
        KalemTablosu.CommitEdit();
        KalemTablosu.CommitEdit();

        if (KalemTablosu.SelectedItem is not SatinalmaTalepKalemi kalem)
        {
            MessageBox.Show(
                "Silmek için bir kalem seçin.",
                UygulamaBilgisi.Ad,
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        if ((_talep.Kalemler?.Count ?? 0) <= 1)
        {
            MessageBox.Show(
                "Talepten tüm kalemler silinemez. En az bir kalem kalmalıdır.",
                UygulamaBilgisi.Ad,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var onay = MessageBox.Show(
            $"'{kalem.Malzeme}' kalemi silinsin mi?\n\nBu kalem tüm tekliflerden de kaldırılacak.",
            UygulamaBilgisi.Ad,
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (onay != MessageBoxResult.Yes)
            return;

        _talep.Kalemler!.Remove(kalem);
        _kaldirilanKalemler.Add(kalem);
        Sirala();
        ListeyiYenile();
    }

    private void Kaydet_Click(object sender, RoutedEventArgs e)
    {
        KalemTablosu.CommitEdit();
        KalemTablosu.CommitEdit();

        if ((_talep.Kalemler?.Count ?? 0) == 0)
        {
            MessageBox.Show(
                "En az bir kalem olmalıdır.",
                UygulamaBilgisi.Ad,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var hatali = _talep.Kalemler!.FirstOrDefault(k => k.Miktar <= 0);
        if (hatali is not null)
        {
            MessageBox.Show(
                $"'{hatali.Malzeme}' için sıfırdan büyük bir miktar girin.",
                UygulamaBilgisi.Ad,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        Sirala();
        _kaydedildi = true;
        DialogResult = true;
    }

    private void Vazgec_Click(object sender, RoutedEventArgs e) => Close();

    protected override void OnClosed(EventArgs e)
    {
        if (!_kaydedildi)
        {
            foreach (var kalem in _kaldirilanKalemler)
            {
                if (!_talep.Kalemler!.Contains(kalem))
                    _talep.Kalemler.Add(kalem);
            }

            foreach (var kalem in _talep.Kalemler!)
            {
                if (_baslangicMiktarlari.TryGetValue(kalem.Id, out var miktar))
                    kalem.Miktar = miktar;
            }

            Sirala();
        }

        base.OnClosed(e);
    }

    private void Sirala()
    {
        var sira = 1;
        foreach (var k in _talep.Kalemler!.OrderBy(x => x.SiraNo).ToList())
            k.SiraNo = sira++;
    }
}

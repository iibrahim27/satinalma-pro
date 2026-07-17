using System.Windows;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;

namespace SatinalmaPro.Views.Modules.Satinalma.Part1;

public partial class TalepMiktarDuzenlemeWindow : Window
{
    private readonly SatinalmaTalep _talep;
    private readonly Dictionary<Guid, double> _baslangicMiktarlari;
    private bool _kaydedildi;

    public TalepMiktarDuzenlemeWindow(SatinalmaTalep talep)
    {
        _talep = talep;
        _baslangicMiktarlari = talep.Kalemler.ToDictionary(k => k.Id, k => k.Miktar);

        InitializeComponent();
        TxtAciklama.Text = $"{talep.TalepNo} talebinde onay öncesi istenen miktarları gözden geçirin.";
        KalemTablosu.ItemsSource = talep.Kalemler.OrderBy(k => k.SiraNo).ToList();
    }

    private void Kaydet_Click(object sender, RoutedEventArgs e)
    {
        KalemTablosu.CommitEdit();
        KalemTablosu.CommitEdit();

        var hatali = _talep.Kalemler.FirstOrDefault(k => k.Miktar <= 0);
        if (hatali is not null)
        {
            MessageBox.Show(
                $"'{hatali.Malzeme}' için sıfırdan büyük bir miktar girin.",
                UygulamaBilgisi.Ad,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        _kaydedildi = true;
        DialogResult = true;
    }

    private void Vazgec_Click(object sender, RoutedEventArgs e) => Close();

    protected override void OnClosed(EventArgs e)
    {
        if (!_kaydedildi)
        {
            foreach (var kalem in _talep.Kalemler)
            {
                if (_baslangicMiktarlari.TryGetValue(kalem.Id, out var miktar))
                    kalem.Miktar = miktar;
            }
        }

        base.OnClosed(e);
    }
}

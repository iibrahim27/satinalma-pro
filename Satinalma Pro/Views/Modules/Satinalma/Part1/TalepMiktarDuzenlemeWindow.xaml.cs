using System.Globalization;
using System.Windows;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;

namespace SatinalmaPro.Views.Modules.Satinalma.Part1;

public partial class TalepMiktarDuzenlemeWindow : Window
{
    private readonly SatinalmaTalep _talep;
    private readonly Dictionary<Guid, double> _baslangicMiktarlari;
    private readonly List<SatinalmaTalepKalemi> _kaldirilanKalemler = [];
    private readonly List<SatinalmaTalepKalemi> _eklenenKalemler = [];
    private bool _kaydedildi;

    public TalepMiktarDuzenlemeWindow(SatinalmaTalep talep)
    {
        _talep = talep;
        talep.Kalemler ??= [];
        _baslangicMiktarlari = talep.Kalemler.ToDictionary(k => k.Id, k => k.Miktar);

        InitializeComponent();
        TxtAciklama.Text =
            $"{talep.TalepNo} — miktarı değiştirin, yeni kalem ekleyin veya kalem silin. " +
            "Teklif girilmiş olsa bile değişiklik yapılabilir; yeni kalemler tüm tekliflere düşer, " +
            "birim fiyatları teklif düzenlemede girilir.";

        MalzemeBirimDeposu.VarsayilanlariHazirla();
        CmbBirim.ItemsSource = MalzemeBirimDeposu.Liste.ToList();
        CmbBirim.SelectedItem = MalzemeBirimDeposu.Liste.Contains("Adet")
            ? "Adet"
            : MalzemeBirimDeposu.Liste.FirstOrDefault();

        ListeyiYenile();
    }

    private void ListeyiYenile()
    {
        KalemTablosu.ItemsSource = null;
        KalemTablosu.ItemsSource = _talep.Kalemler.OrderBy(k => k.SiraNo).ToList();
    }

    private void KalemEkle_Click(object sender, RoutedEventArgs e)
    {
        KalemTablosu.CommitEdit();
        KalemTablosu.CommitEdit();

        var malzeme = (YeniMalzemeGiris.Metin ?? "").Trim();
        if (string.IsNullOrWhiteSpace(malzeme))
        {
            MessageBox.Show(
                "Malzeme adı girin.",
                UygulamaBilgisi.Ad,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (!SayiMetniYardimcisi.CiftOku(TxtYeniMiktar.Text, out var miktar) || miktar <= 0)
        {
            MessageBox.Show(
                "Sıfırdan büyük bir miktar girin.",
                UygulamaBilgisi.Ad,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            TxtYeniMiktar.Focus();
            return;
        }

        var birim = (CmbBirim.Text ?? CmbBirim.SelectedItem as string ?? "Adet").Trim();
        if (string.IsNullOrWhiteSpace(birim))
            birim = "Adet";

        _talep.Kalemler ??= [];
        var ayni = _talep.Kalemler.FirstOrDefault(k =>
            string.Equals(k.Malzeme.Trim(), malzeme, StringComparison.OrdinalIgnoreCase)
            && string.Equals(k.Birim.Trim(), birim, StringComparison.OrdinalIgnoreCase));
        if (ayni is not null)
        {
            var onay = MessageBox.Show(
                $"«{malzeme}» zaten listede ({ayni.Miktar.ToString("N2", CultureInfo.CurrentCulture)} {ayni.Birim}).\n\n" +
                "Yine de ayrı kalem olarak eklensin mi?",
                UygulamaBilgisi.Ad,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (onay != MessageBoxResult.Yes)
                return;
        }

        var kalem = new SatinalmaTalepKalemi
        {
            SiraNo = (_talep.Kalemler.Count == 0 ? 0 : _talep.Kalemler.Max(k => k.SiraNo)) + 1,
            Malzeme = malzeme,
            Birim = birim,
            Miktar = miktar
        };
        _talep.Kalemler.Add(kalem);
        _eklenenKalemler.Add(kalem);
        Sirala();
        ListeyiYenile();

        YeniMalzemeGiris.Metin = "";
        TxtYeniMiktar.Text = "1";
        KalemTablosu.SelectedItem = kalem;
        KalemTablosu.ScrollIntoView(kalem);
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
        if (_eklenenKalemler.Contains(kalem))
            _eklenenKalemler.Remove(kalem);
        else
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

        var bosMalzeme = _talep.Kalemler!.FirstOrDefault(k => string.IsNullOrWhiteSpace(k.Malzeme));
        if (bosMalzeme is not null)
        {
            MessageBox.Show(
                "Malzeme adı boş olan kalem bırakılamaz.",
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
            foreach (var kalem in _eklenenKalemler)
                _talep.Kalemler!.Remove(kalem);

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

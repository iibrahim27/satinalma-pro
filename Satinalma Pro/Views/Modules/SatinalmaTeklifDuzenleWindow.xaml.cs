using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;

namespace SatinalmaPro.Views.Modules;

public partial class SatinalmaTeklifDuzenleWindow : Window
{
    private readonly SatinalmaTeklif _teklif;
    private readonly List<SatinalmaTalepKalemi> _kalemler;
    private readonly List<TeklifFiyatSatir> _satirlar = [];
    private bool _hucreDuzenleniyor;

    public SatinalmaTeklifDuzenleWindow(SatinalmaTeklif teklif, IEnumerable<SatinalmaTalepKalemi> kalemler, SatinalmaAyarlar ayarlar)
    {
        _teklif = teklif;
        _kalemler = kalemler.OrderBy(k => k.SiraNo).ToList();
        InitializeComponent();

        ParaBirimiSutunu.ItemsSource = ParaBirimleri.Liste;

        TxtFirmaAdi.Text = teklif.FirmaAdi;
        TxtUsdKuru.Text = KurMetni(teklif.UsdKuru > 0 ? teklif.UsdKuru : ayarlar.VarsayilanUsdKuru);
        TxtEurKuru.Text = KurMetni(teklif.EurKuru > 0 ? teklif.EurKuru : ayarlar.VarsayilanEurKuru);
        TxtVade.Text = teklif.VadeGunu > 0 ? teklif.VadeGunu.ToString() : "";
        TxtTeslim.Text = teklif.TeslimSuresi;
        TxtOdeme.Text = teklif.OdemeSekli;
        TxtAciklama.Text = teklif.Aciklama;

        SatirlariOlustur();
        FiyatGrid.ItemsSource = _satirlar;
        ToplamlariGuncelle();
    }

    private void SatirlariOlustur()
    {
        _satirlar.Clear();
        TeklifFiyatlariniHazirla();

        foreach (var kalem in _kalemler)
        {
            var fiyat = _teklif.Fiyatlar.First(f => f.KalemId == kalem.Id);
            var kdv = fiyat.KdvOrani > 0 ? fiyat.KdvOrani : _teklif.KdvOrani;
            var satir = new TeklifFiyatSatir
            {
                KalemId = kalem.Id,
                Malzeme = kalem.Malzeme,
                Miktar = kalem.Miktar,
                Birim = kalem.Birim,
                Marka = fiyat.Marka,
                ParaBirimi = string.IsNullOrWhiteSpace(fiyat.ParaBirimi) ? ParaBirimleri.Try : fiyat.ParaBirimi,
                UsdKuru = KurDegeri(TxtUsdKuru.Text),
                EurKuru = KurDegeri(TxtEurKuru.Text)
            };
            satir.MetinleriBaslat(fiyat.BirimFiyat, kdv > 0 ? kdv : 20);
            _satirlar.Add(satir);
        }
    }

    private void TeklifFiyatlariniHazirla()
    {
        _teklif.Fiyatlar ??= [];
        foreach (var kalem in _kalemler)
        {
            if (_teklif.Fiyatlar.All(f => f.KalemId != kalem.Id))
            {
                _teklif.Fiyatlar.Add(new SatinalmaTeklifFiyati
                {
                    KalemId = kalem.Id,
                    KdvOrani = _teklif.KdvOrani > 0 ? _teklif.KdvOrani : 20
                });
            }
        }
    }

    private void KurMetniDegisti(object sender, TextChangedEventArgs e)
    {
        if (_hucreDuzenleniyor)
            return;

        var usd = KurDegeri(TxtUsdKuru.Text);
        var eur = KurDegeri(TxtEurKuru.Text);
        foreach (var satir in _satirlar)
            satir.KurlariGuncelle(usd, eur);

        SatirToplamlariniGuncelle();
        ToplamlariGuncelle();
    }

    private void FiyatGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e) =>
        _hucreDuzenleniyor = true;

    private void FiyatGrid_PreparingCellForEdit(object sender, DataGridPreparingCellForEditEventArgs e)
    {
        if (e.Column is not DataGridComboBoxColumn)
            return;

        var combo = ComboKutusunuBul(e.EditingElement);
        if (combo is null)
            return;

        combo.ItemsSource = ParaBirimleri.Liste;
        combo.IsEditable = false;
    }

    private void FiyatGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.EditAction != DataGridEditAction.Commit)
            return;

        // Düzenleme TextBox'ından kaynağı hemen yaz — Kaydet tıklanınca fiyat kaçmasın.
        if (e.EditingElement is TextBox textBox)
            textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();

        if (e.Column is DataGridComboBoxColumn && e.EditingElement is FrameworkElement element)
        {
            var combo = ComboKutusunuBul(element);
            if (combo?.SelectedItem is string para && e.Row.Item is TeklifFiyatSatir satir)
                satir.ParaBirimi = para;
        }

        Dispatcher.BeginInvoke(SatirToplamlariniGuncelle);
    }

    private void FiyatGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
    {
        if (e.EditAction != DataGridEditAction.Commit)
        {
            _hucreDuzenleniyor = false;
            return;
        }

        Dispatcher.BeginInvoke(() =>
        {
            try
            {
                _hucreDuzenleniyor = false;
                SatirToplamlariniGuncelle();
                ToplamlariGuncelle();
            }
            catch
            {
                _hucreDuzenleniyor = false;
            }
        });
    }

    private void SatirToplamlariniGuncelle()
    {
        foreach (var satir in _satirlar)
            satir.GuncelleSatirToplam();
    }

    private static ComboBox? ComboKutusunuBul(DependencyObject? kok)
    {
        if (kok is null)
            return null;
        if (kok is ComboBox combo)
            return combo;

        var adet = VisualTreeHelper.GetChildrenCount(kok);
        for (var i = 0; i < adet; i++)
        {
            var bulunan = ComboKutusunuBul(VisualTreeHelper.GetChild(kok, i));
            if (bulunan is not null)
                return bulunan;
        }

        return null;
    }

    private void BekleyenDuzenlemeyiKaydet()
    {
        try
        {
            // Aktif hücredeki TextBox metnini modele zorla yaz (CommitEdit yetmeyebilir).
            AktifHucreKaynaginiGuncelle();

            if (FiyatGrid.IsKeyboardFocusWithin)
                BtnKaydet.Focus();

            // Binding / LostFocus kuyruğunun boşalması için bir tur bekle.
            Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Input);

            FiyatGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            FiyatGrid.CommitEdit(DataGridEditingUnit.Row, true);
        }
        catch (Exception ex)
        {
            HataGunlugu.Kaydet(ex, "TeklifDuzenle.BekleyenDuzenleme");
        }

        foreach (var satir in _satirlar)
            satir.MetindenDegerleriYenile();
    }

    private void AktifHucreKaynaginiGuncelle()
    {
        try
        {
            if (Keyboard.FocusedElement is TextBox odakli)
            {
                odakli.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
                return;
            }

            var hucre = FiyatGrid.CurrentCell;
            if (!hucre.IsValid || hucre.Item is not TeklifFiyatSatir satir)
                return;

            // Görsel ağaçta düzenleme TextBox'ını bul
            var satirUi = FiyatGrid.ItemContainerGenerator.ContainerFromItem(satir) as DataGridRow;
            if (satirUi is null)
                return;

            var textBox = DuzenlemeTextBoxunuBul(satirUi);
            if (textBox is null)
                return;

            textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
            if (hucre.Column?.Header?.ToString()?.Contains("Birim", StringComparison.OrdinalIgnoreCase) == true
                || Equals(hucre.Column?.SortMemberPath, nameof(TeklifFiyatSatir.BirimFiyatMetni)))
            {
                satir.BirimFiyatMetni = textBox.Text;
            }
            else if (hucre.Column?.Header?.ToString()?.Contains("KDV", StringComparison.OrdinalIgnoreCase) == true
                     || Equals(hucre.Column?.SortMemberPath, nameof(TeklifFiyatSatir.KdvOraniMetni)))
            {
                satir.KdvOraniMetni = textBox.Text;
            }
        }
        catch (Exception ex)
        {
            HataGunlugu.Kaydet(ex, "TeklifDuzenle.AktifHucre");
        }
    }

    private static TextBox? DuzenlemeTextBoxunuBul(DependencyObject kok)
    {
        if (kok is TextBox tb)
            return tb;

        var adet = VisualTreeHelper.GetChildrenCount(kok);
        for (var i = 0; i < adet; i++)
        {
            var bulunan = DuzenlemeTextBoxunuBul(VisualTreeHelper.GetChild(kok, i));
            if (bulunan is not null)
                return bulunan;
        }

        return null;
    }

    private void ToplamlariGuncelle()
    {
        if (TxtKurBilgi is null || TxtAraToplam is null)
            return;

        var usd = KurDegeri(TxtUsdKuru.Text);
        var eur = KurDegeri(TxtEurKuru.Text);
        var kurVar = usd > 0 || eur > 0;
        TxtKurBilgi.Text = kurVar
            ? $"Kur: USD {usd:N4} · EUR {eur:N4} (döviz fiyatları TL'ye çevrilir)"
            : "Döviz kuru girilmedi — TRY fiyatlar doğrudan kullanılır";

        var ara = 0m;
        var kdv = 0m;
        var genel = 0m;
        foreach (var satir in _satirlar)
        {
            var fiyat = new SatinalmaTeklifFiyati
            {
                KalemId = satir.KalemId,
                BirimFiyat = satir.BirimFiyat,
                ParaBirimi = satir.ParaBirimi,
                KdvOrani = satir.KdvOrani
            };
            var kalem = _kalemler.First(k => k.Id == satir.KalemId);
            fiyat.Hesapla(kalem.Miktar, usd, eur);
            ara += fiyat.ToplamTutar;
            kdv += fiyat.KdvTutari;
            genel += fiyat.ToplamKdvDahil;
        }

        var tr = CultureInfo.GetCultureInfo("tr-TR");
        TxtAraToplam.Text = $"KDV Hariç: {ara.ToString("N2", tr)} ₺";
        TxtKdvToplam.Text = $"KDV: {kdv.ToString("N2", tr)} ₺";
        TxtGenelToplam.Text = $"KDV Dahil: {genel.ToString("N2", tr)} ₺";
    }

    private bool FormuKaydet()
    {
        BekleyenDuzenlemeyiKaydet();
        SatirToplamlariniGuncelle();

        if (string.IsNullOrWhiteSpace(TxtFirmaAdi.Text))
        {
            MessageBox.Show("Firma adı zorunludur.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtFirmaAdi.Focus();
            return false;
        }

        if (!_satirlar.Any(s => s.BirimFiyat > 0))
        {
            var gecersizMetin = _satirlar.FirstOrDefault(s =>
                !string.IsNullOrWhiteSpace(s.BirimFiyatMetni) && s.BirimFiyat <= 0);
            MessageBox.Show(
                gecersizMetin is not null
                    ? $"Geçersiz birim fiyat: \"{gecersizMetin.BirimFiyatMetni}\" ({gecersizMetin.Malzeme}).\n\nSayısal bir değer girin (ör. 12,50)."
                    : "En az bir kalem için birim fiyat girin.",
                UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        _teklif.FirmaAdi = TxtFirmaAdi.Text.Trim();
        _teklif.UsdKuru = KurDegeri(TxtUsdKuru.Text);
        _teklif.EurKuru = KurDegeri(TxtEurKuru.Text);
        _teklif.VadeGunu = int.TryParse(TxtVade.Text, out var vade) ? vade : 0;
        _teklif.TeslimSuresi = TxtTeslim.Text.Trim();
        _teklif.OdemeSekli = TxtOdeme.Text.Trim();
        _teklif.Aciklama = TxtAciklama.Text.Trim();

        foreach (var satir in _satirlar)
        {
            var fiyat = _teklif.Fiyatlar.First(f => f.KalemId == satir.KalemId);
            fiyat.Marka = satir.Marka.Trim();
            fiyat.ParaBirimi = satir.ParaBirimi;
            fiyat.BirimFiyat = satir.BirimFiyat;
            fiyat.KdvOrani = satir.KdvOrani;
        }

        _teklif.FiyatlariHesapla(_kalemler);
        return true;
    }

    private void Kaydet_Click(object sender, RoutedEventArgs e)
    {
        if (!FormuKaydet())
            return;

        DialogResult = true;
        Close();
    }

    private void Iptal_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private static string KurMetni(decimal kur) =>
        kur > 0 ? kur.ToString("G", CultureInfo.CurrentCulture) : "";

    private static decimal KurDegeri(string? metin) =>
        SayiMetniYardimcisi.OndalikOku(metin, out var sonuc) ? sonuc : 0;
}

using SatinalmaPro.Helpers;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SatinalmaPro.Models;
using SatinalmaPro.Services;

namespace SatinalmaPro.Views.Modules;

public partial class AkaryakitDuzenleWindow : Window
{
    private readonly AkaryakitKaydi _kayit;

    public AkaryakitDuzenleWindow(AkaryakitKaydi kayit)
    {
        InitializeComponent();
        _kayit = kayit;
        PlakaListesiniDoldur();
        FormuDoldur();
    }

    private void PlakaListesiniDoldur()
    {
        CmbPlaka.Items.Clear();
        foreach (var plaka in FiloPlakaServisi.AktifPlakalar())
            CmbPlaka.Items.Add(new ComboBoxItem { Content = plaka });

        if (!string.IsNullOrWhiteSpace(_kayit.PlakaVeyaKod) &&
            !FiloPlakaServisi.AktifPlakalar().Any(p => p.Equals(_kayit.PlakaVeyaKod, StringComparison.OrdinalIgnoreCase)))
            CmbPlaka.Items.Add(new ComboBoxItem { Content = _kayit.PlakaVeyaKod });
    }

    private void FormuDoldur()
    {
        KayitTipiSec(string.IsNullOrWhiteSpace(_kayit.KayitTipi) ? "Dağıtılan" : _kayit.KayitTipi);
        TxtTarih.Text = _kayit.Tarih;
        CmbAracTipi.Text = string.IsNullOrWhiteSpace(_kayit.AracTipi) ? "Araç" : _kayit.AracTipi;
        CmbYakitTuru.Text = string.IsNullOrWhiteSpace(_kayit.YakitTuru) ? "Motorin" : _kayit.YakitTuru;
        CmbPlaka.Text = _kayit.PlakaVeyaKod;
        TxtAracAdi.Text = _kayit.AracMakineAdi;
        var miktarMetin = _kayit.Miktar > 0 ? _kayit.Miktar.ToString(CultureInfo.CurrentCulture) : "";
        TxtMiktarDagitilan.Text = miktarMetin;
        TxtMiktarAlinan.Text = miktarMetin;
        CmbBirim.Text = string.IsNullOrWhiteSpace(_kayit.Birim) ? "Lt" : _kayit.Birim;
        TxtBirimFiyati.Text = _kayit.BirimFiyati > 0
            ? _kayit.BirimFiyati.ToString(CultureInfo.CurrentCulture)
            : "";
        TxtKmSayaci.Text = _kayit.KmSayaci?.ToString(CultureInfo.CurrentCulture) ?? "";
        TxtSaatSayaci.Text = _kayit.SaatSayaci?.ToString(CultureInfo.CurrentCulture) ?? "";
        TxtTedarikci.Text = string.IsNullOrWhiteSpace(_kayit.Tedarikci) ? _kayit.Istasyon : _kayit.Tedarikci;
        TxtTeslimAlan.Text = _kayit.TeslimAlan;
        TxtSofor.Text = _kayit.SoforOperator;
        TxtSaha.Text = _kayit.Saha;
        TxtAciklama.Text = _kayit.Aciklama;

        KayitTipiGorunumunuGuncelle();
        if (DagitilanKayitMi())
            FiloBilgileriniUygula(CmbPlaka.Text.Trim(), soforuYenile: false);
        TutarHesapla(this, new RoutedEventArgs());
    }

    private void KayitTipiDegisti(object sender, SelectionChangedEventArgs e)
    {
        KayitTipiGorunumunuGuncelle();
        if (DagitilanKayitMi())
            FiloBilgileriniUygula(CmbPlaka.Text.Trim(), soforuYenile: true);
    }

    private void KayitTipiGorunumunuGuncelle()
    {
        var dagitilan = DagitilanKayitMi();
        PanelDagitilan.Visibility = dagitilan ? Visibility.Visible : Visibility.Collapsed;
        PanelAlinan.Visibility = dagitilan ? Visibility.Collapsed : Visibility.Visible;

        TxtAltBaslik.Text = dagitilan
            ? "Plaka filo parkından seçilir; araç bilgileri ve şoför otomatik doldurulur"
            : "Tank veya depo dolumu için alınan yakıt bilgilerini girin";

        if (dagitilan)
        {
            PlakaListesiniDoldur();
            if (string.IsNullOrWhiteSpace(CmbPlaka.Text) && CmbPlaka.Items.Count > 0)
                CmbPlaka.SelectedIndex = 0;
        }
    }

    private void KayitTipiSec(string tip)
    {
        for (var i = 0; i < CmbKayitTipi.Items.Count; i++)
        {
            if ((CmbKayitTipi.Items[i] as ComboBoxItem)?.Content?.ToString() == tip)
            {
                CmbKayitTipi.SelectedIndex = i;
                return;
            }
        }

        CmbKayitTipi.SelectedIndex = 0;
    }

    private string SeciliKayitTipi() =>
        (CmbKayitTipi.SelectedItem as ComboBoxItem)?.Content?.ToString()?.Trim()
        ?? CmbKayitTipi.Text.Trim();

    private bool AlinanKayitMi() =>
        AkaryakitKaydi.AlinanKayitMi(SeciliKayitTipi());

    private bool DagitilanKayitMi() => !AlinanKayitMi();

    private void TutarHesapla(object sender, RoutedEventArgs e)
    {
        if (DagitilanKayitMi())
            return;

        var miktarOk = double.TryParse(TxtMiktarAlinan.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var miktar);
        var fiyatOk = decimal.TryParse(TxtBirimFiyati.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var birimFiyati);

        TxtToplamTutar.Text = miktarOk && fiyatOk
            ? ((decimal)miktar * birimFiyati).ToString("N2", CultureInfo.CurrentCulture)
            : "";
    }

    private void Kaydet_Click(object sender, RoutedEventArgs e)
    {
        var dagitilan = DagitilanKayitMi();
        var miktarMetin = dagitilan ? TxtMiktarDagitilan.Text : TxtMiktarAlinan.Text;

        if (!double.TryParse(miktarMetin, NumberStyles.Any, CultureInfo.CurrentCulture, out var miktar))
        {
            MessageBox.Show("Miktar geçerli bir sayı olmalıdır.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        decimal birimFiyati = 0;

        if (!dagitilan)
        {
            if (!decimal.TryParse(TxtBirimFiyati.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out birimFiyati))
            {
                MessageBox.Show("Alınan yakıt için birim fiyatı zorunludur.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtTedarikci.Text))
            {
                MessageBox.Show("Tedarikçi bilgisi zorunludur.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }
        else if (string.IsNullOrWhiteSpace(CmbPlaka.Text))
        {
            MessageBox.Show("Dağıtılan yakıt için plaka seçin.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _kayit.KayitTipi = SeciliKayitTipi();
        _kayit.Tarih = TxtTarih.Text.Trim();
        _kayit.Miktar = miktar;

        if (dagitilan)
        {
            _kayit.AracTipi = CmbAracTipi.Text.Trim();
            _kayit.YakitTuru = CmbYakitTuru.Text.Trim();
            _kayit.PlakaVeyaKod = CmbPlaka.Text.Trim();
            _kayit.AracMakineAdi = TxtAracAdi.Text.Trim();
            _kayit.KmSayaci = NullableDouble(TxtKmSayaci.Text);
            _kayit.SaatSayaci = NullableDouble(TxtSaatSayaci.Text);
            _kayit.SoforOperator = TxtSofor.Text.Trim();
            _kayit.Saha = TxtSaha.Text.Trim();
            _kayit.Aciklama = TxtAciklama.Text.Trim();
            _kayit.Birim = "Lt";
            _kayit.BirimFiyati = 0;
            _kayit.FaturaNo = "";
            _kayit.Istasyon = "";
            _kayit.Tedarikci = "";
            _kayit.TeslimAlan = "";
        }
        else
        {
            _kayit.Birim = string.IsNullOrWhiteSpace(CmbBirim.Text) ? "Lt" : CmbBirim.Text.Trim();
            _kayit.BirimFiyati = birimFiyati;
            _kayit.Tedarikci = TxtTedarikci.Text.Trim();
            _kayit.TeslimAlan = TxtTeslimAlan.Text.Trim();
            _kayit.Istasyon = "";
            _kayit.FaturaNo = "";
            _kayit.AracTipi = "";
            _kayit.YakitTuru = "";
            _kayit.PlakaVeyaKod = "";
            _kayit.AracMakineAdi = "";
            _kayit.KmSayaci = null;
            _kayit.SaatSayaci = null;
            _kayit.SoforOperator = "";
            _kayit.Saha = "";
            _kayit.Aciklama = "";
        }

        _kayit.ToplamTutariHesapla();

        ModulVeriDeposu.KaydetAkaryakit();
        DialogResult = true;
        Close();
    }

    private void PlakaSecildi(object sender, SelectionChangedEventArgs e)
    {
        if (!DagitilanKayitMi())
            return;

        FiloBilgileriniUygula(CmbPlaka.Text.Trim(), soforuYenile: true);
    }

    private void PlakaMetniDegisti(object sender, RoutedEventArgs e)
    {
        if (!DagitilanKayitMi())
            return;

        FiloBilgileriniUygula(CmbPlaka.Text.Trim(), soforuYenile: true);
    }

    private void FiloBilgileriniUygula(string plaka, bool soforuYenile)
    {
        if (!DagitilanKayitMi() || string.IsNullOrWhiteSpace(plaka))
        {
            FiloBaglantisiniKaldir();
            return;
        }

        var arac = FiloPlakaServisi.AracBul(plaka);
        if (arac is null)
        {
            FiloBaglantisiniKaldir();
            return;
        }

        CmbAracTipi.Text = FiloPlakaServisi.AkaryakitAracTipi(arac.AracTipi);
        TxtAracAdi.Text = arac.MarkaModel;
        TxtSaha.Text = arac.Saha;

        if (soforuYenile)
            TxtSofor.Text = FiloPlakaServisi.AktifZimmetliSofor(plaka);

        CmbAracTipi.IsEnabled = false;
        TxtAracAdi.IsReadOnly = true;
        TxtAracAdi.Background = new SolidColorBrush(Color.FromRgb(248, 250, 252));
    }

    private void FiloBaglantisiniKaldir()
    {
        CmbAracTipi.IsEnabled = true;
        TxtAracAdi.IsReadOnly = false;
        TxtAracAdi.Background = Brushes.White;
    }

    private static double? NullableDouble(string metin) =>
        double.TryParse(metin, NumberStyles.Any, CultureInfo.CurrentCulture, out var d) ? d : null;

    private void Iptal_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

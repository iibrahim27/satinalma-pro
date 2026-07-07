using System.Globalization;
using System.Windows;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;

namespace SatinalmaPro.Views.Modules;

public partial class AlinanMalzemeAktarWindow : Window
{
    private static string _sonTarih = "";
    private static string _sonFisNo = "";
    private static string _sonTeslimAlan = "";
    private static string _sonIndirildigiSaha = "";
    private static string _sonAciklama = "";

    public double GirilenMiktar { get; private set; }
    public string GirilenFirma { get; private set; } = "";
    public decimal GirilenBirimFiyat { get; private set; }
    public string SecilenKategori { get; private set; } = "";
    public string GirilenTarih { get; private set; } = "";
    public string GirilenFisNo { get; private set; } = "";
    public string GirilenTeslimAlan { get; private set; } = "";
    public string GirilenIndirildigiSaha { get; private set; } = "";
    public string GirilenDepo { get; private set; } = "";
    public string GirilenAciklama { get; private set; } = "";
    public bool SahayaDirekt { get; private set; }
    public string GirilenSahaHedef { get; private set; } = "";

    public AlinanMalzemeAktarWindow(OnaylananMalzemeSatiri satir, double varsayilanMiktar, bool miktarDuzenlenebilir)
    {
        InitializeComponent();

        var ekSatir = miktarDuzenlenebilir
            ? $"Sipariş: {satir.SiparisMiktari:N2} {satir.Birim} · Kabul: {satir.KabulEdilenMiktar:N2} · Kalan: {satir.KalanMiktar:N2}\nFazla teslimat kabul edilir; miktar otomatik güncellenir. Kategori listesinde yoksa yazarak ekleyebilirsiniz."
            : $"Miktar: {varsayilanMiktar:N2} {satir.Birim}";
        TxtMalzeme.Text = $"{satir.Malzeme}\n{ekSatir}";

        TxtMiktar.Text = varsayilanMiktar > 0
            ? varsayilanMiktar.ToString("N2", CultureInfo.CurrentCulture)
            : "";
        TxtMiktar.IsReadOnly = !miktarDuzenlenebilir;
        if (!miktarDuzenlenebilir)
            TxtMiktar.Background = System.Windows.Media.Brushes.WhiteSmoke;

        TxtFirma.Text = string.IsNullOrWhiteSpace(satir.Firma) ? "" : satir.Firma;
        TxtBirimFiyat.Text = satir.BirimFiyati > 0
            ? satir.BirimFiyati.ToString("N2", CultureInfo.CurrentCulture)
            : "";

        MalzemeKategoriDeposu.ComboDoldur(CmbKategori);

        var onerilenFisNo = string.IsNullOrWhiteSpace(satir.SiparisNo) ? satir.TalepNo : satir.SiparisNo;
        TxtTarih.Text = string.IsNullOrWhiteSpace(_sonTarih)
            ? DateTime.Now.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)
            : _sonTarih;
        TxtFisNo.Text = string.IsNullOrWhiteSpace(_sonFisNo) ? onerilenFisNo : _sonFisNo;
        TxtTeslimAlan.Text = _sonTeslimAlan;
        TxtIndirildigiSaha.Text = _sonIndirildigiSaha;

        var varsayilanAciklama = $"Satınalma: {satir.TalepNo} — {satir.Marka} — {satir.KalemAciklamasi}".Trim(' ', '—', ' ');
        TxtAciklama.Text = string.IsNullOrWhiteSpace(_sonAciklama) ? varsayilanAciklama : _sonAciklama;
    }

    private void Tamam_Click(object sender, RoutedEventArgs e)
    {
        if (!MiktarOku(out var miktar))
            return;

        var firma = TxtFirma.Text.Trim();
        if (string.IsNullOrWhiteSpace(firma))
        {
            MessageBox.Show("Firma / tedarikçi adını girin.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtFirma.Focus();
            return;
        }

        if (!BirimFiyatOku(out var birimFiyat))
            return;

        var kategori = (CmbKategori.SelectedItem as string ?? CmbKategori.Text).Trim();
        if (string.IsNullOrWhiteSpace(kategori))
        {
            MessageBox.Show("Lütfen bir kategori seçin veya yeni kategori yazın.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            CmbKategori.Focus();
            return;
        }

        var tarih = TxtTarih.Text.Trim();
        if (!DateTime.TryParseExact(tarih, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
        {
            MessageBox.Show("Geçerli bir tarih girin (gg.aa.yyyy).", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtTarih.Focus();
            return;
        }

        var fisNo = TxtFisNo.Text.Trim();
        if (string.IsNullOrWhiteSpace(fisNo))
        {
            MessageBox.Show("Fiş no girin.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtFisNo.Focus();
            return;
        }

        var teslimAlan = TxtTeslimAlan.Text.Trim();
        if (string.IsNullOrWhiteSpace(teslimAlan))
        {
            MessageBox.Show("Teslim alan kişiyi girin.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtTeslimAlan.Focus();
            return;
        }

        var depo = TxtIndirildigiSaha.Text.Trim();
        if (string.IsNullOrWhiteSpace(depo))
        {
            var depoEtiket = ChkSahayaDirekt.IsChecked == true ? "Giriş deposunu" : "İndirildiği sahayı / depoyu";
            MessageBox.Show($"{depoEtiket} girin.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtIndirildigiSaha.Focus();
            return;
        }

        var sahayaDirekt = ChkSahayaDirekt.IsChecked == true;
        var sahaHedef = TxtSahaHedef.Text.Trim();
        if (sahayaDirekt && string.IsNullOrWhiteSpace(sahaHedef))
        {
            MessageBox.Show("Malzemenin indiği sahayı girin.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtSahaHedef.Focus();
            return;
        }

        GirilenMiktar = miktar;
        GirilenFirma = firma;
        GirilenBirimFiyat = birimFiyat;
        SecilenKategori = kategori;
        GirilenTarih = tarih;
        GirilenFisNo = fisNo;
        GirilenTeslimAlan = teslimAlan;
        GirilenDepo = depo;
        GirilenIndirildigiSaha = sahayaDirekt ? sahaHedef : depo;
        GirilenAciklama = TxtAciklama.Text.Trim();
        SahayaDirekt = sahayaDirekt;
        GirilenSahaHedef = sahaHedef;

        _sonTarih = tarih;
        _sonFisNo = fisNo;
        _sonTeslimAlan = teslimAlan;
        _sonIndirildigiSaha = depo;
        _sonAciklama = GirilenAciklama;

        DialogResult = true;
        Close();
    }

    private bool MiktarOku(out double miktar)
    {
        miktar = 0;
        var metin = TxtMiktar.Text.Trim();
        if (!double.TryParse(metin.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out miktar) &&
            !double.TryParse(metin, NumberStyles.Any, CultureInfo.CurrentCulture, out miktar))
        {
            MessageBox.Show("Geçerli bir miktar girin.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtMiktar.Focus();
            return false;
        }

        if (miktar <= 0)
        {
            MessageBox.Show("Miktar sıfırdan büyük olmalıdır.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtMiktar.Focus();
            return false;
        }

        return true;
    }

    private bool BirimFiyatOku(out decimal birimFiyat)
    {
        birimFiyat = 0;
        var metin = TxtBirimFiyat.Text.Trim();
        if (!decimal.TryParse(metin.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out birimFiyat) &&
            !decimal.TryParse(metin, NumberStyles.Any, CultureInfo.CurrentCulture, out birimFiyat))
        {
            MessageBox.Show("Geçerli bir birim fiyat girin.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtBirimFiyat.Focus();
            return false;
        }

        if (birimFiyat <= 0)
        {
            MessageBox.Show("Birim fiyat sıfırdan büyük olmalıdır.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtBirimFiyat.Focus();
            return false;
        }

        return true;
    }

    private void SahayaDirekt_Changed(object sender, RoutedEventArgs e)
    {
        if (PnlSahaHedef is null || LblDepoSaha is null)
            return;

        var aktif = ChkSahayaDirekt.IsChecked == true;
        PnlSahaHedef.Visibility = aktif ? Visibility.Visible : Visibility.Collapsed;
        LblDepoSaha.Text = aktif ? "Giriş Deposu" : "İndirildiği Saha / Depo";
    }

    private void Iptal_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

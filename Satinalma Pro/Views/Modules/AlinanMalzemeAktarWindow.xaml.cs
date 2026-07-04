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
    public string SecilenKategori { get; private set; } = "";
    public string GirilenTarih { get; private set; } = "";
    public string GirilenFisNo { get; private set; } = "";
    public string GirilenTeslimAlan { get; private set; } = "";
    public string GirilenIndirildigiSaha { get; private set; } = "";
    public string GirilenAciklama { get; private set; } = "";

    public AlinanMalzemeAktarWindow(OnaylananMalzemeSatiri satir, double varsayilanMiktar, bool miktarDuzenlenebilir)
    {
        InitializeComponent();

        var ekSatir = miktarDuzenlenebilir
            ? $"Kabul: {satir.KabulEdilenMiktar:N2} {satir.Birim} · Kalan: {satir.KalanMiktar:N2} {satir.Birim}"
            : $"Miktar: {varsayilanMiktar:N2} {satir.Birim}";
        TxtMalzeme.Text = $"{satir.Malzeme}\n{ekSatir}";

        TxtMiktar.Text = varsayilanMiktar > 0
            ? varsayilanMiktar.ToString("N2", CultureInfo.CurrentCulture)
            : "";
        TxtMiktar.IsReadOnly = !miktarDuzenlenebilir;
        if (!miktarDuzenlenebilir)
            TxtMiktar.Background = System.Windows.Media.Brushes.WhiteSmoke;

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

        var kategori = CmbKategori.SelectedItem as string ?? CmbKategori.Text.Trim();
        if (string.IsNullOrWhiteSpace(kategori))
        {
            MessageBox.Show("Lütfen bir kategori seçin.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Warning);
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

        var saha = TxtIndirildigiSaha.Text.Trim();
        if (string.IsNullOrWhiteSpace(saha))
        {
            MessageBox.Show("İndirildiği sahayı girin.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtIndirildigiSaha.Focus();
            return;
        }

        GirilenMiktar = miktar;
        SecilenKategori = kategori;
        GirilenTarih = tarih;
        GirilenFisNo = fisNo;
        GirilenTeslimAlan = teslimAlan;
        GirilenIndirildigiSaha = saha;
        GirilenAciklama = TxtAciklama.Text.Trim();

        _sonTarih = tarih;
        _sonFisNo = fisNo;
        _sonTeslimAlan = teslimAlan;
        _sonIndirildigiSaha = saha;
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

    private void Iptal_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

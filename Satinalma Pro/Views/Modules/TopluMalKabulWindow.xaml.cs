using System.Globalization;
using System.Windows;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;

namespace SatinalmaPro.Views.Modules;

public partial class TopluMalKabulWindow : Window
{
    public string SecilenKategori { get; private set; } = "";
    public string GirilenTarih { get; private set; } = "";
    public string GirilenFisNo { get; private set; } = "";
    public string GirilenTeslimAlan { get; private set; } = "";
    public string GirilenDepo { get; private set; } = "";
    public string GirilenAciklama { get; private set; } = "";
    public bool SahayaDirekt { get; private set; }
    public string GirilenSahaHedef { get; private set; } = "";

    public TopluMalKabulWindow(IReadOnlyCollection<OnaylananMalzemeSatiri> satirlar)
    {
        InitializeComponent();

        var talepNo = satirlar.Select(s => s.TalepNo).Distinct().SingleOrDefault() ?? "";
        var firma = satirlar.Select(s => s.Firma).Distinct().SingleOrDefault();
        var teklifTanimi = string.IsNullOrWhiteSpace(firma) ? talepNo : $"{firma} / {talepNo}";
        TxtOzet.Text = $"{teklifTanimi} teklifindeki {satirlar.Count:N0} kalan kalem, sipariş miktarlarına tamamlanarak tek seferde kabul edilecek. Her kalemin firma ve birim fiyatı kendi onaylı teklifinden alınır.";

        MalzemeKategoriDeposu.ComboDoldur(CmbKategori);
        TxtTarih.Text = DateTime.Now.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
        TxtFisNo.Text = satirlar.Select(s => s.SiparisNo).FirstOrDefault(s => !string.IsNullOrWhiteSpace(s))
            ?? talepNo;
        TxtAciklama.Text = $"Satınalma: {talepNo} — toplu mal kabul";
    }

    private void Tamam_Click(object sender, RoutedEventArgs e)
    {
        var kategori = (CmbKategori.SelectedItem as string ?? CmbKategori.Text).Trim();
        if (string.IsNullOrWhiteSpace(kategori))
        {
            Uyari("Lütfen bir kategori seçin veya yeni kategori yazın.", CmbKategori);
            return;
        }

        var tarih = TxtTarih.Text.Trim();
        if (!DateTime.TryParseExact(tarih, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
        {
            Uyari("Geçerli bir tarih girin (gg.aa.yyyy).", TxtTarih);
            return;
        }

        var fisNo = TxtFisNo.Text.Trim();
        if (string.IsNullOrWhiteSpace(fisNo))
        {
            Uyari("Fiş no girin.", TxtFisNo);
            return;
        }

        var teslimAlan = TxtTeslimAlan.Text.Trim();
        if (string.IsNullOrWhiteSpace(teslimAlan))
        {
            Uyari("Teslim alan kişiyi girin.", TxtTeslimAlan);
            return;
        }

        var depo = TxtDepoSaha.Text.Trim();
        if (string.IsNullOrWhiteSpace(depo))
        {
            Uyari(ChkSahayaDirekt.IsChecked == true ? "Giriş deposunu girin." : "İndirildiği sahayı / depoyu girin.", TxtDepoSaha);
            return;
        }

        var sahayaDirekt = ChkSahayaDirekt.IsChecked == true;
        var sahaHedef = TxtSahaHedef.Text.Trim();
        if (sahayaDirekt && string.IsNullOrWhiteSpace(sahaHedef))
        {
            Uyari("Malzemenin indiği sahayı girin.", TxtSahaHedef);
            return;
        }

        SecilenKategori = kategori;
        GirilenTarih = tarih;
        GirilenFisNo = fisNo;
        GirilenTeslimAlan = teslimAlan;
        GirilenDepo = depo;
        GirilenAciklama = TxtAciklama.Text.Trim();
        SahayaDirekt = sahayaDirekt;
        GirilenSahaHedef = sahaHedef;

        DialogResult = true;
        Close();
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

    private static void Uyari(string mesaj, FrameworkElement odak)
    {
        MessageBox.Show(mesaj, UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
        odak.Focus();
    }
}

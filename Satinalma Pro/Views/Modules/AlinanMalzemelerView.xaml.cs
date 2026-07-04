using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;
using Microsoft.Win32;

namespace SatinalmaPro.Views.Modules;

public partial class AlinanMalzemelerView : UserControl, IModulKlavyeKisayollari
{
    private readonly ICollectionView _gorunum;
    private readonly FiltreZamanlayici _filtreZamanlayici;
    private readonly ModulSayfalamaYoneticisi<AlinanMalzemeKaydi> _sayfalama = new();
    private bool _hesaplamaBekliyor;

    public ObservableCollection<AlinanMalzemeKaydi> Kayitlar => UygulamaVeriDeposu.AlinanMalzemeler;

    public AlinanMalzemelerView()
    {
        InitializeComponent();
        DataContext = this;
        _filtreZamanlayici = new FiltreZamanlayici(FiltreYenile);

        _gorunum = CollectionViewSource.GetDefaultView(Kayitlar);
        _gorunum.Filter = KayitFiltresi;
        MalzemeGrid.ItemsSource = _sayfalama.SayfaKayitlari;
        ModulSayfalamaYardimcisi.CubukBagla(_sayfalama, SayfalamaBar);

        MalzemeFiltre.OneriKaynaginiAyarla(MalzemeAdiOneriServisi.Ara);
        MalzemeFiltre.MetinYazildi += (_, _) => _filtreZamanlayici.Tetikle();

        Loaded += (_, _) =>
        {
            UygulamaVeriDeposu.OrnekVeriyiYukle();
            KullaniciYetkileri.ModulErisiminiUygula(this, "Alınan Malzemeler");
            MalzemeKategoriDeposu.KayitlardanSenkronizeEt();
            FiltreCombolariGuncelle();
            SayfalamayiYenile();
            OzetGuncelle();
            _ = HesaplamalariArkaPlandaYapAsync();
        };
    }

    private async Task HesaplamalariArkaPlandaYapAsync()
    {
        if (_hesaplamaBekliyor)
            return;

        _hesaplamaBekliyor = true;
        try
        {
            var kayitlar = Kayitlar.ToList();
            await Task.Run(() =>
            {
                foreach (var kayit in kayitlar)
                    kayit.ToplamTutariHesapla();
                AlinanMalzemeArtisHesaplayici.Hesapla(kayitlar);
            });

            await Dispatcher.InvokeAsync(() =>
            {
                SayfalamayiYenile();
                OzetGuncelle();
            });
        }
        finally
        {
            _hesaplamaBekliyor = false;
        }
    }

    #region Araç çubuğu

    private void ExcelYukle_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Excel Dosyası Seç",
            Filter = "Excel Dosyası (*.xlsx)|*.xlsx"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            var yeniKayitlar = AlinanMalzemeExcelService.DosyadanOku(dialog.FileName);
            if (yeniKayitlar.Count == 0)
            {
                MessageBox.Show("Dosyada aktarılacak kayıt bulunamadı.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ModulVeriDeposu.BeginBatch();
            try
            {
                foreach (var kayit in yeniKayitlar)
                {
                    kayit.Tarih = TarihYardimcisi.Normalize(kayit.Tarih);
                    Kayitlar.Add(kayit);
                }
            }
            finally
            {
                ModulVeriDeposu.EndBatch();
            }

            VeriGuncellendi();
            MessageBox.Show($"{yeniKayitlar.Count} kayıt içe aktarıldı.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Excel okunamadı:\n{ex.Message}", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SablonIndir_Click(object sender, RoutedEventArgs e) =>
        AlinanMalzemeExcelService.SablonKaydet();

    private void PdfIndir_Click(object sender, RoutedEventArgs e) =>
        AlinanMalzemePdfOlusturucu.Indir(GorunenKayitlar(), FiltreOzetiMetni());

    private void PdfYazdir_Click(object sender, RoutedEventArgs e) =>
        AlinanMalzemePdfOlusturucu.Yazdir(GorunenKayitlar(), FiltreOzetiMetni());

    private void YeniKayit_Click(object sender, RoutedEventArgs e)
    {
        var yeni = new AlinanMalzemeKaydi
        {
            Tarih = DateTime.Now.ToString("dd.MM.yyyy"),
            Birim = "Ton"
        };

        var pencere = new AlinanMalzemeDuzenleWindow(yeni) { Owner = Window.GetWindow(this) };
        if (pencere.ShowDialog() != true)
            return;

        Kayitlar.Add(yeni);
        VeriGuncellendi();
    }

    private void DisaAktar_Click(object sender, RoutedEventArgs e) =>
        AlinanMalzemeExcelService.ListeyiKaydet(GorunenKayitlar(), "AlinanMalzemeler.xlsx");

    private void Yenile_Click(object sender, RoutedEventArgs e) => KisayolYenile();

    #endregion

    #region Filtreler

    private void FiltreDegisti(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox)
            _filtreZamanlayici.Tetikle();
        else
            FiltreYenile();
    }

    private void FiltreleriTemizle_Click(object sender, RoutedEventArgs e)
    {
        TxtArama.Text = "";
        DpBaslangic.SelectedDate = null;
        DpBitis.SelectedDate = null;
        MalzemeFiltre.MetniTemizle();
        CmbKategori.SelectedIndex = 0;
        CmbTedarikci.SelectedIndex = 0;
        FiltreYenile();
    }

    private void FiltreYenile()
    {
        SayfalamayiYenile(ilkSayfayaDon: true);
        OzetGuncelle();
    }

    private void SayfalamayiYenile(bool ilkSayfayaDon = false) =>
        ModulSayfalamaYardimcisi.FiltreSonrasi(
            _sayfalama, _gorunum, k => ModulSayfalamaYardimcisi.TarihSira(k.Tarih), SayfalamaBar, ilkSayfayaDon);

    public void KisayolYenile()
    {
        MalzemeAdiOneriServisi.OnbellekSifirla();
        FiltreCombolariGuncelle();
        _ = HesaplamalariArkaPlandaYapAsync();
        FiltreYenile();
    }

    private void VeriGuncellendi()
    {
        MalzemeAdiOneriServisi.OnbellekSifirla();
        MalzemeKategoriDeposu.KayitlardanSenkronizeEt();
        foreach (var kayit in Kayitlar)
            kayit.Tarih = TarihYardimcisi.Normalize(kayit.Tarih);

        FiltreCombolariGuncelle();
        _ = HesaplamalariArkaPlandaYapAsync();
        FiltreYenile();
    }

    private static void VerileriHesapla(IEnumerable<AlinanMalzemeKaydi> kayitlar)
    {
        foreach (var kayit in kayitlar)
            kayit.ToplamTutariHesapla();

        AlinanMalzemeArtisHesaplayici.Hesapla(kayitlar);
    }

    private bool KayitFiltresi(object item)
    {
        if (item is not AlinanMalzemeKaydi kayit)
            return false;

        var arama = TxtArama.Text.Trim();
        if (!string.IsNullOrEmpty(arama) && !AramaEslesir(kayit, arama))
            return false;

        if (DpBaslangic.SelectedDate is DateTime || DpBitis.SelectedDate is DateTime)
        {
            if (!DateTime.TryParseExact(kayit.Tarih, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var tarih))
                return false;

            if (DpBaslangic.SelectedDate is DateTime b && tarih.Date < b.Date)
                return false;

            if (DpBitis.SelectedDate is DateTime e && tarih.Date > e.Date)
                return false;
        }

        var malzeme = (MalzemeFiltre.Metin ?? "").Trim();
        if (!string.IsNullOrEmpty(malzeme) &&
            !kayit.MalzemeHizmet.Contains(malzeme, StringComparison.OrdinalIgnoreCase))
            return false;

        var kategori = ComboDegeri(CmbKategori);
        if (!string.IsNullOrEmpty(kategori) && kategori != "Tümü" &&
            !kayit.Kategori.Equals(kategori, StringComparison.OrdinalIgnoreCase))
            return false;

        var tedarikci = ComboDegeri(CmbTedarikci);
        if (!string.IsNullOrEmpty(tedarikci) && tedarikci != "Tümü" &&
            !kayit.Tedarikci.Equals(tedarikci, StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    private static bool AramaEslesir(AlinanMalzemeKaydi kayit, string arama) =>
        (kayit.Tarih?.Contains(arama, StringComparison.OrdinalIgnoreCase) ?? false) ||
        (kayit.FaturaNo?.Contains(arama, StringComparison.OrdinalIgnoreCase) ?? false) ||
        (kayit.Kategori?.Contains(arama, StringComparison.OrdinalIgnoreCase) ?? false) ||
        (kayit.Tedarikci?.Contains(arama, StringComparison.OrdinalIgnoreCase) ?? false) ||
        (kayit.IndirildigiSaha?.Contains(arama, StringComparison.OrdinalIgnoreCase) ?? false) ||
        (kayit.TeslimAlan?.Contains(arama, StringComparison.OrdinalIgnoreCase) ?? false) ||
        (kayit.Aciklama?.Contains(arama, StringComparison.OrdinalIgnoreCase) ?? false) ||
        (kayit.MalzemeHizmet?.Contains(arama, StringComparison.OrdinalIgnoreCase) ?? false);

    private static string? ComboDegeri(ComboBox combo) =>
        (combo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? combo.Text;

    private void FiltreCombolariGuncelle()
    {
        var seciliKategori = ComboDegeri(CmbKategori);
        var seciliTedarikci = ComboDegeri(CmbTedarikci);

        CmbKategori.Items.Clear();
        CmbTedarikci.Items.Clear();

        CmbKategori.Items.Add(new ComboBoxItem { Content = "Tümü" });
        CmbTedarikci.Items.Add(new ComboBoxItem { Content = "Tümü" });

        foreach (var k in MalzemeKategoriDeposu.TumListe())
            CmbKategori.Items.Add(new ComboBoxItem { Content = k });

        foreach (var t in Kayitlar.Select(k => k.Tedarikci).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().OrderBy(s => s))
            CmbTedarikci.Items.Add(new ComboBoxItem { Content = t });

        CmbKategori.SelectedIndex = IndexBul(CmbKategori, seciliKategori);
        CmbTedarikci.SelectedIndex = IndexBul(CmbTedarikci, seciliTedarikci);
    }

    private static int IndexBul(ComboBox combo, string? deger)
    {
        if (string.IsNullOrEmpty(deger))
            return 0;

        for (var i = 0; i < combo.Items.Count; i++)
        {
            if ((combo.Items[i] as ComboBoxItem)?.Content?.ToString() == deger)
                return i;
        }

        return 0;
    }

    #endregion

    #region Tablo işlemleri

    private void MalzemeGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        KayitButonlariniGuncelle();

    private void KayitDuzenle_Click(object sender, RoutedEventArgs e) => MenuDuzenle_Click(sender, e);

    private void KayitSil_Click(object sender, RoutedEventArgs e) => MenuSil_Click(sender, e);

    private void KayitButonlariniGuncelle()
    {
        var secili = SeciliKayit() is not null;
        BtnKayitDuzenle.IsEnabled = secili;
        BtnKayitSil.IsEnabled = secili;
    }

    private void MalzemeGrid_LoadingRow(object sender, DataGridRowEventArgs e)
    {
        if (e.Row.Item is AlinanMalzemeKaydi kayit)
            e.Row.Background = KategoriRenkleri.GetFirca(kayit.Kategori);
    }

    private void MalzemeGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (SeciliKayit() is { } kayit)
            KaydiDuzenle(kayit);
    }

    private void MenuDuzenle_Click(object sender, RoutedEventArgs e)
    {
        if (SeciliKayit() is { } kayit)
            KaydiDuzenle(kayit);
    }

    private void MenuSil_Click(object sender, RoutedEventArgs e)
    {
        if (SeciliKayit() is not { } kayit)
            return;

        if (MessageBox.Show($"{kayit.FaturaNo} numaralı kaydı silmek istiyor musunuz?", "Kayıt Sil",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        Kayitlar.Remove(kayit);
        VeriGuncellendi();
    }

    private AlinanMalzemeKaydi? SeciliKayit() =>
        MalzemeGrid.SelectedItem as AlinanMalzemeKaydi;

    private void KaydiDuzenle(AlinanMalzemeKaydi kayit)
    {
        var pencere = new AlinanMalzemeDuzenleWindow(kayit) { Owner = Window.GetWindow(this) };
        if (pencere.ShowDialog() != true)
            return;

        VeriGuncellendi();
    }

    #endregion

    private List<AlinanMalzemeKaydi> GorunenKayitlar() =>
        _gorunum.Cast<AlinanMalzemeKaydi>().ToList();

    private string FiltreOzetiMetni()
    {
        var parcalar = new List<string>();

        if (DpBaslangic.SelectedDate is DateTime baslangic)
            parcalar.Add($"Başlangıç: {baslangic:dd.MM.yyyy}");
        if (DpBitis.SelectedDate is DateTime bitis)
            parcalar.Add($"Bitiş: {bitis:dd.MM.yyyy}");

        var malzeme = (MalzemeFiltre.Metin ?? "").Trim();
        if (!string.IsNullOrEmpty(malzeme))
            parcalar.Add($"Malzeme: {malzeme}");

        var kategori = ComboDegeri(CmbKategori);
        if (!string.IsNullOrEmpty(kategori) && kategori != "Tümü")
            parcalar.Add($"Kategori: {kategori}");

        var tedarikci = ComboDegeri(CmbTedarikci);
        if (!string.IsNullOrEmpty(tedarikci) && tedarikci != "Tümü")
            parcalar.Add($"Tedarikçi: {tedarikci}");

        var arama = TxtArama.Text.Trim();
        if (!string.IsNullOrEmpty(arama))
            parcalar.Add($"Arama: {arama}");

        return parcalar.Count == 0 ? "Tüm kayıtlar" : string.Join(" · ", parcalar);
    }

    private void OzetGuncelle()
    {
        var gorunenSayi = 0;
        double toplamMiktar = 0;
        decimal toplamTutar = 0;

        foreach (var oge in _gorunum)
        {
            if (oge is not AlinanMalzemeKaydi kayit)
                continue;

            gorunenSayi++;
            toplamMiktar += kayit.Miktar;
            toplamTutar += kayit.ToplamTutar;
        }

        TxtToplamKayit.Text = Kayitlar.Count.ToString();
        TxtToplamMiktar.Text = $"{toplamMiktar:N1} Ton";
        TxtToplamTutar.Text = $"₺{toplamTutar:N0}";
        TxtFiltrelenen.Text = gorunenSayi == Kayitlar.Count
            ? "Tümü"
            : $"{gorunenSayi} / {Kayitlar.Count} kayıt";
    }
}

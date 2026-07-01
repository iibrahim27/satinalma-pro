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

public partial class CimentoView : UserControl, IModulKlavyeKisayollari
{
    private readonly ICollectionView _gorunum;
    private readonly FiltreZamanlayici _filtreZamanlayici;
    private readonly ModulSayfalamaYoneticisi<CimentoKaydi> _sayfalama = new();
    private readonly FiltreZamanlayici _tarihZamanlayici;
    private List<string> _seciliCimentoCinsleri = [];

    public ObservableCollection<CimentoKaydi> Kayitlar => ModulVeriDeposu.Cimento;

    public CimentoView()
    {
        InitializeComponent();
        DataContext = this;
        _filtreZamanlayici = new FiltreZamanlayici(FiltreYenile);
        _tarihZamanlayici = new FiltreZamanlayici(TarihFiltresiUygula);

        _gorunum = CollectionViewSource.GetDefaultView(Kayitlar);
        _gorunum.Filter = KayitFiltresi;
        CimentoGrid.ItemsSource = _sayfalama.SayfaKayitlari;
        ModulSayfalamaYardimcisi.CubukBagla(_sayfalama, SayfalamaBar);

        VerileriHesapla();
        FiltreSecenekleriniGuncelle();
        SayfalamayiYenile();
        OzetGuncelle();

        Loaded += (_, _) => KullaniciYetkileri.ModulErisiminiUygula(this, "Çimento");
    }

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
            var yeniKayitlar = CimentoExcelService.DosyadanOku(dialog.FileName);
            if (yeniKayitlar.Count == 0)
            {
                MessageBox.Show("Dosyada aktarılacak kayıt bulunamadı.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ModulVeriDeposu.BeginBatch();
            try
            {
                foreach (var kayit in yeniKayitlar)
                    Kayitlar.Add(kayit);
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
        CimentoExcelService.SablonKaydet();

    private void PdfYazdir_Click(object sender, RoutedEventArgs e) =>
        CimentoPdfOlusturucu.Yazdir(GorunenKayitlar(), "Çimento Giriş Raporu");

    private void PdfIndir_Click(object sender, RoutedEventArgs e) =>
        CimentoPdfOlusturucu.Indir(GorunenKayitlar(), "Çimento Giriş Raporu");

    private void YeniKayit_Click(object sender, RoutedEventArgs e)
    {
        var yeni = new CimentoKaydi
        {
            Tarih = DateTime.Now.ToString("dd.MM.yyyy"),
            Birim = "Ton",
            CimentoSinifi = "CEM I"
        };

        var pencere = new CimentoDuzenleWindow(yeni) { Owner = Window.GetWindow(this) };
        if (pencere.ShowDialog() != true)
            return;

        Kayitlar.Add(yeni);
        VeriGuncellendi();
    }

    private void DisaAktar_Click(object sender, RoutedEventArgs e) =>
        CimentoExcelService.ListeyiKaydet(GorunenKayitlar(), "Cimento.xlsx");

    private void Yenile_Click(object sender, RoutedEventArgs e) => KisayolYenile();

    private void FiltreDegisti(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox)
            _filtreZamanlayici.Tetikle();
        else if (sender is DatePicker)
            _tarihZamanlayici.Tetikle();
        else
            FiltreYenile();
    }

    private void TarihFiltresiUygula()
    {
        FiltreSecenekleriniGuncelle();
        FiltreYenile();
    }

    private void TedarikciDegisti(object sender, RoutedEventArgs e)
    {
        CinsleriSenkronize();
        SecimMetniniGuncelle();
        FiltreYenile();
    }

    private void CimentoCinsiSec_Click(object sender, RoutedEventArgs e)
    {
        var pencere = new RaporCokluSecWindow(
            "Çimento Cinsi Seç",
            "Seçili tarih aralığı ve tedarikçiye göre listelenir. Birden fazla çimento cinsi seçebilirsiniz.",
            MevcutCimentoCinsleri(),
            _seciliCimentoCinsleri)
        {
            Owner = Window.GetWindow(this)
        };

        if (pencere.ShowDialog() == true)
        {
            _seciliCimentoCinsleri = pencere.Secilenler.ToList();
            SecimMetniniGuncelle();
            FiltreYenile();
        }
    }

    private void FiltreleriTemizle_Click(object sender, RoutedEventArgs e)
    {
        TxtArama.Text = "";
        DpBaslangic.SelectedDate = null;
        DpBitis.SelectedDate = null;
        CmbTedarikci.SelectedIndex = 0;
        _seciliCimentoCinsleri = [];
        SecimMetniniGuncelle();
        FiltreSecenekleriniGuncelle();
        FiltreYenile();
    }

    private void FiltrelenenleriFaturala_Click(object sender, RoutedEventArgs e)
    {
        var gorunen = GorunenKayitlar();
        if (gorunen.Count == 0)
        {
            MessageBox.Show("Filtreye uygun kayıt bulunamadı.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var kesilmemis = gorunen.Count(k => !k.FaturasiKesildi);
        if (kesilmemis == 0)
        {
            MessageBox.Show("Filtrelenen kayıtların tamamı zaten faturası kesilmiş olarak işaretli.",
                UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var mesaj = gorunen.Count == kesilmemis
            ? $"{gorunen.Count} kaydın faturası kesildi olarak işaretlenecek. Onaylıyor musunuz?"
            : $"Filtrelenen {gorunen.Count} kayıttan {kesilmemis} tanesi faturası kesildi olarak işaretlenecek. Onaylıyor musunuz?";

        if (MessageBox.Show(mesaj, "Toplu Fatura İşaretle", MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        foreach (var kayit in gorunen)
            kayit.FaturasiKesildi = true;

        ModulVeriDeposu.KaydetCimento();
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
        VerileriHesapla();
        FiltreSecenekleriniGuncelle();
        FiltreYenile();
    }

    private void VeriGuncellendi()
    {
        VerileriHesapla();
        FiltreSecenekleriniGuncelle();
        FiltreYenile();
    }

    private void VerileriHesapla()
    {
        foreach (var kayit in Kayitlar)
            kayit.ToplamTutariHesapla();

        CimentoArtisHesaplayici.Hesapla(Kayitlar);
    }

    private bool KayitFiltresi(object item)
    {
        if (item is not CimentoKaydi kayit)
            return false;

        var arama = TxtArama.Text.Trim();
        if (!string.IsNullOrEmpty(arama))
        {
            var metin = string.Join(" ",
                kayit.Tarih, kayit.IrsaliyeNo, kayit.CimentoSinifi, kayit.CimentoCinsi,
                kayit.Tedarikci, kayit.IndirildigiSaha, kayit.TeslimAlan, kayit.Aciklama);

            if (!metin.Contains(arama, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        if (!TarihUygunMu(kayit.Tarih))
            return false;

        var tedarikci = ComboDegeri(CmbTedarikci);
        if (!string.IsNullOrEmpty(tedarikci) && tedarikci != "Tümü" &&
            !kayit.Tedarikci.Equals(tedarikci, StringComparison.OrdinalIgnoreCase))
            return false;

        if (_seciliCimentoCinsleri.Count > 0 &&
            !_seciliCimentoCinsleri.Any(c => c.Equals(kayit.CimentoCinsi, StringComparison.CurrentCultureIgnoreCase)))
            return false;

        return true;
    }

    private bool TarihUygunMu(string tarih)
    {
        if (DpBaslangic.SelectedDate is not DateTime && DpBitis.SelectedDate is not DateTime)
            return true;

        if (!DateTime.TryParseExact(tarih, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return false;

        if (DpBaslangic.SelectedDate is DateTime b && dt.Date < b.Date)
            return false;

        if (DpBitis.SelectedDate is DateTime bit && dt.Date > bit.Date)
            return false;

        return true;
    }

    private IEnumerable<CimentoKaydi> TarihFiltreliKayitlar() =>
        Kayitlar.Where(k => TarihUygunMu(k.Tarih));

    private IEnumerable<CimentoKaydi> TedarikciFiltreliKayitlar()
    {
        var kayitlar = TarihFiltreliKayitlar();
        var tedarikci = ComboDegeri(CmbTedarikci);

        if (!string.IsNullOrEmpty(tedarikci) && tedarikci != "Tümü")
            kayitlar = kayitlar.Where(k => k.Tedarikci.Equals(tedarikci, StringComparison.OrdinalIgnoreCase));

        return kayitlar;
    }

    private List<string> MevcutCimentoCinsleri() =>
        TedarikciFiltreliKayitlar()
            .Select(k => k.CimentoCinsi)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(c => c, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    private void CinsleriSenkronize()
    {
        if (_seciliCimentoCinsleri.Count == 0)
            return;

        var gecerli = new HashSet<string>(MevcutCimentoCinsleri(), StringComparer.CurrentCultureIgnoreCase);
        _seciliCimentoCinsleri = _seciliCimentoCinsleri.Where(c => gecerli.Contains(c)).ToList();
    }

    private void SecimMetniniGuncelle()
    {
        TxtCimentoCinsiSecim.Text = _seciliCimentoCinsleri.Count switch
        {
            0 => "Tümü",
            1 => KisaMetin(_seciliCimentoCinsleri[0], 22),
            _ => $"{_seciliCimentoCinsleri.Count} seçili"
        };
    }

    private static string KisaMetin(string metin, int max) =>
        metin.Length <= max ? metin : metin[..(max - 1)] + "…";

    private static string? ComboDegeri(ComboBox combo) =>
        (combo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? combo.Text;

    private void FiltreSecenekleriniGuncelle()
    {
        var seciliTedarikci = ComboDegeri(CmbTedarikci);

        CmbTedarikci.Items.Clear();
        CmbTedarikci.Items.Add(new ComboBoxItem { Content = "Tümü" });

        foreach (var t in TarihFiltreliKayitlar()
                     .Select(k => k.Tedarikci)
                     .Where(s => !string.IsNullOrWhiteSpace(s))
                     .Distinct(StringComparer.CurrentCultureIgnoreCase)
                     .OrderBy(s => s, StringComparer.CurrentCultureIgnoreCase))
            CmbTedarikci.Items.Add(new ComboBoxItem { Content = t });

        CmbTedarikci.SelectedIndex = IndexBul(CmbTedarikci, seciliTedarikci);
        CinsleriSenkronize();
        SecimMetniniGuncelle();
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

    private void CimentoGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        KayitButonlariniGuncelle();

    private void KayitDuzenle_Click(object sender, RoutedEventArgs e) => MenuDuzenle_Click(sender, e);

    private void KayitSil_Click(object sender, RoutedEventArgs e) => MenuSil_Click(sender, e);

    private void KayitButonlariniGuncelle()
    {
        var secili = SeciliKayit() is not null;
        BtnKayitDuzenle.IsEnabled = secili;
        BtnKayitSil.IsEnabled = secili;
    }

    private void CimentoGrid_LoadingRow(object sender, DataGridRowEventArgs e)
    {
        if (e.Row.Item is CimentoKaydi kayit)
            e.Row.Background = CimentoRenkleri.GetFirca(kayit.CimentoSinifi);
    }

    private void CimentoGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
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

        if (MessageBox.Show($"{kayit.IrsaliyeNo} irsaliye numaralı kaydı silmek istiyor musunuz?", "Kayıt Sil",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        Kayitlar.Remove(kayit);
        VeriGuncellendi();
    }

    private void FaturaDurumu_Tikla(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { Tag: CimentoKaydi kayit })
        {
            kayit.FaturasiKesildi = !kayit.FaturasiKesildi;
            CimentoGrid.Items.Refresh();
            ModulVeriDeposu.KaydetCimento();
            e.Handled = true;
        }
    }

    private CimentoKaydi? SeciliKayit() =>
        CimentoGrid.SelectedItem as CimentoKaydi;

    private void KaydiDuzenle(CimentoKaydi kayit)
    {
        var pencere = new CimentoDuzenleWindow(kayit) { Owner = Window.GetWindow(this) };
        if (pencere.ShowDialog() != true)
            return;

        VeriGuncellendi();
    }

    private List<CimentoKaydi> GorunenKayitlar() =>
        _gorunum.Cast<CimentoKaydi>().ToList();

    private void OzetGuncelle()
    {
        var gorunen = GorunenKayitlar();

        TxtToplamKayit.Text = Kayitlar.Count.ToString();
        TxtToplamMiktar.Text = $"{gorunen.Sum(k => k.Miktar):N1} Ton";
        TxtToplamTutar.Text = $"₺{gorunen.Sum(k => k.ToplamTutar):N0}";
        TxtFiltrelenen.Text = gorunen.Count == Kayitlar.Count
            ? "Tümü"
            : $"{gorunen.Count} / {Kayitlar.Count} kayıt";
    }
}

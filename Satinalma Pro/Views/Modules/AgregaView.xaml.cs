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

public partial class AgregaView : UserControl, IModulKlavyeKisayollari
{
    private readonly ICollectionView _gorunum;
    private readonly FiltreZamanlayici _filtreZamanlayici;
    private readonly ModulSayfalamaYoneticisi<AgregaKaydi> _sayfalama = new();
    private List<string> _seciliAgregaCinsleri = [];
    private DateTime? _filtreBaslangic;
    private DateTime? _filtreBitis;

    public ObservableCollection<AgregaKaydi> Kayitlar => ModulVeriDeposu.Agrega;

    public AgregaView()
    {
        InitializeComponent();
        DataContext = this;
        _filtreZamanlayici = new FiltreZamanlayici(FiltreYenile);

        _gorunum = CollectionViewSource.GetDefaultView(Kayitlar);
        _gorunum.Filter = KayitFiltresi;
        AgregaGrid.ItemsSource = _sayfalama.SayfaKayitlari;
        ModulSayfalamaYardimcisi.CubukBagla(_sayfalama, SayfalamaBar);

        VerileriHesapla();
        FiltreSecenekleriniGuncelle();
        SayfalamayiYenile();
        OzetGuncelle();

        Loaded += (_, _) => KullaniciYetkileri.ModulErisiminiUygula(this, "Agrega");
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
            var yeniKayitlar = AgregaExcelService.DosyadanOku(dialog.FileName);
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
        AgregaExcelService.SablonKaydet();

    private void PdfIndir_Click(object sender, RoutedEventArgs e) =>
        AgregaPdfOlusturucu.Indir(GorunenKayitlar(), FiltreOzetiMetni());

    private void PdfYazdir_Click(object sender, RoutedEventArgs e) =>
        AgregaPdfOlusturucu.Yazdir(GorunenKayitlar(), FiltreOzetiMetni());

    private void YeniKayit_Click(object sender, RoutedEventArgs e)
    {
        var yeni = new AgregaKaydi
        {
            Tarih = DateTime.Now.ToString("dd.MM.yyyy"),
            Birim = "Ton",
            AgregaTuru = "Mıcır"
        };

        var pencere = new AgregaDuzenleWindow(yeni) { Owner = Window.GetWindow(this) };
        if (pencere.ShowDialog() != true)
            return;

        Kayitlar.Add(yeni);
        VeriGuncellendi();
    }

    private void DisaAktar_Click(object sender, RoutedEventArgs e) =>
        AgregaExcelService.ListeyiKaydet(GorunenKayitlar(), "Agrega.xlsx");

    private void Yenile_Click(object sender, RoutedEventArgs e) => KisayolYenile();

    #endregion

    #region Filtreler

    private void AramaDegisti(object sender, RoutedEventArgs e) => _filtreZamanlayici.Tetikle();

    private void Filtrele_Click(object sender, RoutedEventArgs e)
    {
        _filtreBaslangic = DpBaslangic.SelectedDate;
        _filtreBitis = DpBitis.SelectedDate;
        FiltreSecenekleriniGuncelle();
        FiltreYenile();
    }

    private void TedarikciDegisti(object sender, RoutedEventArgs e)
    {
        CinsleriSenkronize();
        SecimMetniniGuncelle();
        FiltreYenile();
    }

    private void AgregaCinsiSec_Click(object sender, RoutedEventArgs e)
    {
        var pencere = new RaporCokluSecWindow(
            "Agrega Cinsi Seç",
            "Seçili tarih aralığı ve tedarikçiye göre listelenir. Birden fazla agrega cinsi seçebilirsiniz.",
            MevcutAgregaCinsleri(),
            _seciliAgregaCinsleri)
        {
            Owner = Window.GetWindow(this)
        };

        if (pencere.ShowDialog() == true)
        {
            _seciliAgregaCinsleri = pencere.Secilenler.ToList();
            SecimMetniniGuncelle();
            FiltreYenile();
        }
    }

    private void FiltreleriTemizle_Click(object sender, RoutedEventArgs e)
    {
        TxtArama.Text = "";
        DpBaslangic.SelectedDate = null;
        DpBitis.SelectedDate = null;
        _filtreBaslangic = null;
        _filtreBitis = null;
        CmbTedarikci.SelectedIndex = 0;
        _seciliAgregaCinsleri = [];
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

        ModulVeriDeposu.KaydetAgrega();
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

        AgregaArtisHesaplayici.Hesapla(Kayitlar);
    }

    private bool KayitFiltresi(object item)
    {
        if (item is not AgregaKaydi kayit)
            return false;

        var arama = TxtArama.Text.Trim();
        if (!string.IsNullOrEmpty(arama))
        {
            var metin = string.Join(" ",
                kayit.Tarih, kayit.IrsaliyeNo, kayit.AgregaTuru, kayit.AgregaCinsi,
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

        if (_seciliAgregaCinsleri.Count > 0 &&
            !_seciliAgregaCinsleri.Any(c => c.Equals(kayit.AgregaCinsi, StringComparison.CurrentCultureIgnoreCase)))
            return false;

        return true;
    }

    private bool TarihUygunMu(string tarih) =>
        TarihYardimcisi.Aralikta(tarih, _filtreBaslangic, _filtreBitis);

    private IEnumerable<AgregaKaydi> TarihFiltreliKayitlar() =>
        Kayitlar.Where(k => TarihUygunMu(k.Tarih));

    private IEnumerable<AgregaKaydi> TedarikciFiltreliKayitlar()
    {
        var kayitlar = TarihFiltreliKayitlar();
        var tedarikci = ComboDegeri(CmbTedarikci);

        if (!string.IsNullOrEmpty(tedarikci) && tedarikci != "Tümü")
            kayitlar = kayitlar.Where(k => k.Tedarikci.Equals(tedarikci, StringComparison.OrdinalIgnoreCase));

        return kayitlar;
    }

    private List<string> MevcutAgregaCinsleri() =>
        TedarikciFiltreliKayitlar()
            .Select(k => k.AgregaCinsi)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(c => c, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    private void CinsleriSenkronize()
    {
        if (_seciliAgregaCinsleri.Count == 0)
            return;

        var gecerli = new HashSet<string>(MevcutAgregaCinsleri(), StringComparer.CurrentCultureIgnoreCase);
        _seciliAgregaCinsleri = _seciliAgregaCinsleri.Where(c => gecerli.Contains(c)).ToList();
    }

    private void SecimMetniniGuncelle()
    {
        TxtAgregaCinsiSecim.Text = _seciliAgregaCinsleri.Count switch
        {
            0 => "Tümü",
            1 => KisaMetin(_seciliAgregaCinsleri[0], 22),
            _ => $"{_seciliAgregaCinsleri.Count} seçili"
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

    private string FiltreOzetiMetni()
    {
        var parcalar = new List<string>();

        if (_filtreBaslangic is DateTime bas)
            parcalar.Add($"Başlangıç: {bas:dd.MM.yyyy}");
        if (_filtreBitis is DateTime bit)
            parcalar.Add($"Bitiş: {bit:dd.MM.yyyy}");

        var tedarikci = ComboDegeri(CmbTedarikci);
        if (!string.IsNullOrEmpty(tedarikci) && tedarikci != "Tümü")
            parcalar.Add($"Tedarikçi: {tedarikci}");

        if (_seciliAgregaCinsleri.Count > 0)
            parcalar.Add($"Agrega cinsi: {string.Join(", ", _seciliAgregaCinsleri)}");

        var arama = TxtArama.Text.Trim();
        if (!string.IsNullOrEmpty(arama))
            parcalar.Add($"Arama: {arama}");

        return parcalar.Count == 0 ? "Tüm kayıtlar" : string.Join(" · ", parcalar);
    }

    #endregion

    #region Tablo işlemleri

    private void AgregaGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        KayitButonlariniGuncelle();

    private void KayitDuzenle_Click(object sender, RoutedEventArgs e) => MenuDuzenle_Click(sender, e);

    private void KayitSil_Click(object sender, RoutedEventArgs e) => MenuSil_Click(sender, e);

    private void KayitButonlariniGuncelle()
    {
        var secili = SeciliKayit() is not null;
        BtnKayitDuzenle.IsEnabled = secili;
        BtnKayitSil.IsEnabled = secili;
    }

    private void AgregaGrid_LoadingRow(object sender, DataGridRowEventArgs e)
    {
        if (e.Row.Item is AgregaKaydi kayit)
            e.Row.Background = AgregaRenkleri.GetFirca(kayit.AgregaTuru);
    }

    private void AgregaGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
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
        if (sender is FrameworkElement { Tag: AgregaKaydi kayit })
        {
            kayit.FaturasiKesildi = !kayit.FaturasiKesildi;
            ModulVeriDeposu.KaydetAgrega();
            OzetGuncelle();
            e.Handled = true;
        }
    }

    private AgregaKaydi? SeciliKayit() =>
        AgregaGrid.SelectedItem as AgregaKaydi;

    private void KaydiDuzenle(AgregaKaydi kayit)
    {
        var pencere = new AgregaDuzenleWindow(kayit) { Owner = Window.GetWindow(this) };
        if (pencere.ShowDialog() != true)
            return;

        kayit.Tarih = TarihYardimcisi.Normalize(kayit.Tarih);
        VeriGuncellendi();
    }

    #endregion

    private List<AgregaKaydi> GorunenKayitlar() =>
        _gorunum.Cast<AgregaKaydi>().ToList();

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

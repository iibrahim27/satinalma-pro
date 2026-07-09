using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using SatinalmaPro.Helpers;
using SatinalmaPro.Services;

namespace SatinalmaPro.Views;

public partial class AcilisEkrani : Window
{
    private const int ToplamSureMs = 7000;

    private double _prgGenislik;
    private double _sonYuzde;

    public AcilisEkrani()
    {
        InitializeComponent();
        TxtVersiyon.Text = $"Versiyon {UygulamaBilgisi.Versiyon}";
        Loaded += (_, _) => GuncellePrgGenisligi();
        PrgTrack.SizeChanged += (_, _) => GuncellePrgGenisligi();
    }

    public async Task<bool> OturumAcAsync()
    {
        if (!OturumYoneticisi.BulutAktif)
        {
            MessageBox.Show(
                "Firebase yapılandırılmamış — yerel mod aktif.\n\n" +
                "• Veriler yalnızca bu bilgisayarda saklanır\n" +
                "• Mobil uygulama ile senkron olmaz\n" +
                "• Tüm modül yetkileri açıktır\n\n" +
                "Bulut kurulumu için: Ayarlar → Genel → Kurulum Kılavuzunu Aç",
                UygulamaBilgisi.Ad,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return true;
        }

        if (await OturumYoneticisi.OtomatikGirisDeneAsync())
            return true;

        // İlk açılış ve çıkış sonrası aynı split-pane giriş ekranı.
        var ok = false;
        await Dispatcher.InvokeAsync(() =>
        {
            Hide();
            ok = GirisPenceresi.OturumAc(null);
            if (ok)
            {
                YuklemePaneliniGoster();
                Show();
                Activate();
            }
        });
        return ok;
    }

    private void YuklemePaneliniGoster()
    {
        PanelYukleme.Visibility = Visibility.Visible;
        Title = UygulamaBilgisi.Ad;
        Height = 720;
    }

    private void GuncellePrgGenisligi()
    {
        if (PrgTrack.ActualWidth <= 0) return;
        _prgGenislik = PrgTrack.ActualWidth;
        PrgDolgu.Width = _prgGenislik * (_sonYuzde / 100.0);
    }

    public async Task<bool> YukleVeBekleAsync()
    {
        var baslangic = Environment.TickCount64;
        var guncellemeUygulandi = false;

        void Ilerle(double yuzde, string baslik, string durum)
        {
            _sonYuzde = Math.Clamp(yuzde, 0, 100);
            if (_prgGenislik <= 0)
                GuncellePrgGenisligi();
            PrgDolgu.Width = _prgGenislik > 0 ? _prgGenislik * (_sonYuzde / 100.0) : 0;
            TxtYuzde.Text = $"{(int)_sonYuzde}%";
            TxtBaslik.Text = baslik;
            TxtDurum.Text = durum;
        }

        await Dispatcher.InvokeAsync(() => Ilerle(0, "Uygulama yükleniyor...", "Başlatılıyor..."),
            DispatcherPriority.Loaded);
        await Task.Delay(100);

        // Firebase ayarları + güncelleme kontrolü (tarayıcı açılmaz)
        await Dispatcher.InvokeAsync(() => Ilerle(5, "Uygulama yükleniyor...", "Bulut ayarları kontrol ediliyor..."));
        OturumYoneticisi.Baslat();

        guncellemeUygulandi = await GuncellemeServisi.KontrolEtVeUygulaAsync(
            (durum, yuzde) => Dispatcher.Invoke(() =>
                Ilerle(yuzde, yuzde >= 90 ? "Güncelleniyor..." : "Güncelleme kontrol ediliyor...", durum)));

        if (guncellemeUygulandi)
            return true;

        if (OturumYoneticisi.BulutAktif)
        {
            while (Environment.TickCount64 - baslangic < ToplamSureMs)
            {
                var gecen = Environment.TickCount64 - baslangic;
                var yuzde = Math.Clamp(10 + gecen * 90.0 / ToplamSureMs, 10, 99);
                await Dispatcher.InvokeAsync(() =>
                    Ilerle(yuzde, "Uygulama yükleniyor...", "Hazırlanıyor..."));
                await Task.Delay(40);
            }

            await Dispatcher.InvokeAsync(() => Ilerle(100, "Hazır", "Giriş ekranı açılıyor..."));
            await Task.Delay(300);
            return false;
        }

        var veriYukleme = Task.Run(() =>
        {
            Dispatcher.Invoke(() => Ilerle(12, "Uygulama yükleniyor...", "Uygulama ayarları alınıyor..."));
            UygulamaAyarDeposu.Yukle();

            Dispatcher.Invoke(() => Ilerle(38, "Uygulama yükleniyor...", "Satınalma verileri alınıyor..."));
            SatinalmaDepo.Yukle();

            Dispatcher.Invoke(() => Ilerle(62, "Uygulama yükleniyor...", "Modül verileri alınıyor..."));
            ModulVeriDeposu.Yukle();

            Dispatcher.Invoke(() => Ilerle(75, "Uygulama yükleniyor...", "Finansman verileri alınıyor..."));
            FinansmanVeriDeposu.Yukle();

            Dispatcher.Invoke(() => Ilerle(88, "Uygulama yükleniyor...", "Veriler işleniyor..."));
        });

        while (Environment.TickCount64 - baslangic < ToplamSureMs)
        {
            var gecen = Environment.TickCount64 - baslangic;
            var zamanYuzde = gecen * 100.0 / ToplamSureMs;
            await Dispatcher.InvokeAsync(() =>
            {
                var mevcut = double.TryParse(TxtYuzde.Text.TrimEnd('%'), out var p) ? p : 0;
                if (zamanYuzde > mevcut)
                    Ilerle(zamanYuzde, TxtBaslik.Text, TxtDurum.Text);
            });
            await Task.Delay(40);
        }

        await veriYukleme;
        await Dispatcher.InvokeAsync(() => Ilerle(100, "Yükleme tamamlandı", "Satınalma Pro hazır"));
        await Task.Delay(350);
        return false;
    }

    public void DurumGuncelle(string durum) =>
        Dispatcher.Invoke(() => TxtDurum.Text = durum);

    public void SenkronBaslat()
    {
        _sonYuzde = 0;
        Dispatcher.Invoke(() =>
        {
            YuklemePaneliniGoster();
            TxtBaslik.Text = "Bulut verileri senkronize ediliyor...";
            TxtDurum.Text = "Bağlantı kuruluyor...";
            TxtYuzde.Text = "0%";
            GuncellePrgGenisligi();
            PrgDolgu.Width = 0;
        });
    }

    public void SenkronIlerle(int tamamlanan, int toplam, string adim)
    {
        var yuzde = toplam > 0 ? Math.Clamp(tamamlanan * 100.0 / toplam, 0, 100) : 0;
        Dispatcher.Invoke(() =>
        {
            _sonYuzde = yuzde;
            if (_prgGenislik <= 0)
                GuncellePrgGenisligi();
            PrgDolgu.Width = _prgGenislik > 0 ? _prgGenislik * (yuzde / 100.0) : 0;
            TxtYuzde.Text = $"{(int)yuzde}%";
            TxtBaslik.Text = "Bulut verileri senkronize ediliyor...";
            TxtDurum.Text = adim;
        });
    }

    public void Kapat()
    {
        if (Dispatcher.CheckAccess())
            Close();
        else
            Dispatcher.Invoke(Close);
    }

    private void BtnKapat_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void PencereSurukle_Preview(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
            return;

        if (e.Source is DependencyObject kaynak && SuruklenemezMi(kaynak))
            return;

        try { DragMove(); }
        catch { /* sürükleme sırasında ikinci tıklama yoksay */ }
    }

    private static bool SuruklenemezMi(DependencyObject? o)
    {
        while (o is not null)
        {
            if (o is Button or TextBox or PasswordBox or CheckBox or ScrollBar)
                return true;
            o = VisualTreeHelper.GetParent(o);
        }

        return false;
    }
}

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using SatinalmaPro.Helpers;
using SatinalmaPro.Services;
using SatinalmaPro.Shared.Helpers;

namespace SatinalmaPro.Views;

public partial class GirisView : UserControl
{
    public static readonly DependencyProperty KartModuProperty =
        DependencyProperty.Register(nameof(KartModu), typeof(bool), typeof(GirisView),
            new PropertyMetadata(false, (d, _) => ((GirisView)d).KartModuGuncelle()));

    public bool KartModu
    {
        get => (bool)GetValue(KartModuProperty);
        set => SetValue(KartModuProperty, value);
    }

    public event Action? GirisBasarili;

    private bool _tercihYukleniyor;
    private bool _sifreGosteriliyor;

    private void BtnSifreGoster_Click(object sender, RoutedEventArgs e)
    {
        _sifreGosteriliyor = !_sifreGosteriliyor;
        if (_sifreGosteriliyor)
        {
            TxtSifreGoster.Text = TxtSifre.Password;
            TxtSifre.Visibility = Visibility.Collapsed;
            TxtSifreGoster.Visibility = Visibility.Visible;
            BtnSifreGoster.Content = "🙈";
            TxtSifreGoster.Focus();
        }
        else
        {
            TxtSifre.Password = TxtSifreGoster.Text;
            TxtSifreGoster.Visibility = Visibility.Collapsed;
            TxtSifre.Visibility = Visibility.Visible;
            BtnSifreGoster.Content = "👁";
            TxtSifre.Focus();
        }
    }

    private string AktifSifre => _sifreGosteriliyor ? TxtSifreGoster.Text : TxtSifre.Password;

    private void SifreAyarla(string sifre)
    {
        TxtSifre.Password = sifre;
        TxtSifreGoster.Text = sifre;
    }

    public GirisView()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            KartModuGuncelle();
            TercihleriYukle();
            SunucuDurumunuGuncelle();
            TxtVersiyon.Text = $"Versiyon {Helpers.UygulamaBilgisi.Versiyon}";
        };
    }

    private void KartModuGuncelle()
    {
        if (PanelKartBaslik is null || PanelTamBaslik is null)
            return;

        PanelKartBaslik.Visibility = KartModu ? Visibility.Visible : Visibility.Collapsed;
        PanelTamBaslik.Visibility = KartModu ? Visibility.Collapsed : Visibility.Visible;
    }

    private void SunucuDurumunuGuncelle()
    {
        var aktif = OturumYoneticisi.BulutAktif;
        TxtSunucuDurum.Text = aktif ? "Sunucu bağlantısı aktif" : "Yerel mod — sunucu yapılandırılmamış";
        SunucuDurumNokta.Fill = new SolidColorBrush(aktif
            ? Color.FromRgb(0x16, 0xA3, 0x4A)
            : Color.FromRgb(0xF5, 0x9E, 0x0B));
    }

    public void TercihleriYukle()
    {
        _tercihYukleniyor = true;
        try
        {
            var tercih = OturumYoneticisi.TercihleriOku();
            ChkBeniHatirla.IsChecked = tercih.BeniHatirla;
            ChkSifremiHatirla.IsChecked = tercih.SifremiHatirla;

            if (tercih.BeniHatirla && !string.IsNullOrWhiteSpace(tercih.Eposta))
                TxtEposta.Text = tercih.Eposta;
            else
                TxtEposta.Text = "";

            if (tercih.SifremiHatirla)
                SifreAyarla(GirisSifreDeposu.Oku() ?? "");
            else
                SifreAyarla("");
        }
        finally
        {
            _tercihYukleniyor = false;
        }

        TxtSifre.Focus();
    }

    private void TercihKutusu_Degisti(object sender, RoutedEventArgs e)
    {
        if (_tercihYukleniyor)
            return;

        var beniHatirla = ChkBeniHatirla.IsChecked == true;
        var sifremiHatirla = ChkSifremiHatirla.IsChecked == true;

        if (!beniHatirla)
            TxtEposta.Text = "";

        if (!sifremiHatirla)
            SifreAyarla("");

        OturumYoneticisi.TercihKutulariniKaydet(
            beniHatirla ? TxtEposta.Text : "",
            beniHatirla,
            sifremiHatirla);
    }

    private async void Giris_Click(object sender, RoutedEventArgs e)
    {
        try { await GirisYapAsync(); }
        catch (Exception ex)
        {
            HataGunlugu.Kaydet(ex, "GirisView.Giris");
            HataGoster(ex.Message);
        }
    }

    private async Task GirisYapAsync()
    {
        MesajlariTemizle();
        BtnGiris.IsEnabled = false;
        BtnGiris.Content = "Giriş yapılıyor...";

        try
        {
            var beniHatirla = ChkBeniHatirla.IsChecked == true;
            var sifremiHatirla = ChkSifremiHatirla.IsChecked == true;
            await OturumYoneticisi.GirisYapAsync(
                TxtEposta.Text, AktifSifre, beniHatirla, sifremiHatirla);

            BtnGiris.Content = "✓";
            BtnGiris.Background = new SolidColorBrush(Color.FromRgb(0x16, 0xA3, 0x4A));
            await Task.Delay(450);
            GirisBasarili?.Invoke();
        }
        catch (Exception ex)
        {
            HataGoster(ex.Message);
            SifreHataVurgula();
        }
        finally
        {
            BtnGiris.IsEnabled = true;
            if (BtnGiris.Content?.ToString() != "✓")
                BtnGiris.Content = "Giriş Yap";
        }
    }

    private void SifreHataVurgula()
    {
        TxtEposta.BorderBrush = new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26));
        TxtSifre.BorderBrush = new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26));
        if (FindResource("KartSallaAnimasyonu") is Storyboard sb)
            sb.Begin();
    }

    private void SifremiUnuttum_Click(object sender, RoutedEventArgs e)
    {
        MesajlariTemizle();
        TxtSifirEposta.Text = TxtEposta.Text.Trim();
        PanelGiris.Visibility = Visibility.Collapsed;
        PanelSifreSifir.Visibility = Visibility.Visible;
        TxtSifirEposta.Focus();
    }

    private void GeriGiris_Click(object sender, RoutedEventArgs e)
    {
        MesajlariTemizle();
        PanelSifreSifir.Visibility = Visibility.Collapsed;
        PanelGiris.Visibility = Visibility.Visible;
        TxtSifre.Focus();
    }

    private async void SifirGonder_Click(object sender, RoutedEventArgs e)
    {
        try { await SifirGonderAsync(); }
        catch (Exception ex)
        {
            HataGunlugu.Kaydet(ex, "GirisView.SifirGonder");
            SifirHataGoster(ex.Message);
        }
    }

    private async Task SifirGonderAsync()
    {
        SifirMesajlariTemizle();
        var eposta = TxtSifirEposta.Text.Trim();

        if (string.IsNullOrWhiteSpace(eposta))
        {
            SifirHataGoster("E-posta adresi girin.");
            return;
        }

        BtnSifirGonder.IsEnabled = false;
        BtnSifirGonder.Content = "Gönderiliyor...";

        try
        {
            await OturumYoneticisi.SifreSifirlamaEpostasiGonderAsync(eposta);
            SifirBilgiGoster(
                "Sıfırlama bağlantısı e-posta adresinize gönderildi.\nGelen kutunuzu ve spam klasörünü kontrol edin.");
        }
        catch (Exception ex)
        {
            SifirHataGoster(ex.Message);
        }
        finally
        {
            BtnSifirGonder.IsEnabled = true;
            BtnSifirGonder.Content = "Sıfırlama Bağlantısı Gönder";
        }
    }

    private void GirisAlani_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            _ = GirisYapAsync();
    }

    private void SifirAlani_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            _ = SifirGonderAsync();
    }

    private void MesajlariTemizle()
    {
        HataKutusu.Visibility = Visibility.Collapsed;
        BilgiKutusu.Visibility = Visibility.Collapsed;
        TxtEposta.ClearValue(Border.BorderBrushProperty);
        TxtSifre.ClearValue(Border.BorderBrushProperty);
    }

    private void SifirMesajlariTemizle()
    {
        SifirHataKutusu.Visibility = Visibility.Collapsed;
        SifirBilgiKutusu.Visibility = Visibility.Collapsed;
    }

    private void HataGoster(string mesaj)
    {
        TxtHata.Text = AgHataMesaji.Turkcele(mesaj);
        HataKutusu.Visibility = Visibility.Visible;
    }

    private void SifirHataGoster(string mesaj)
    {
        TxtSifirHata.Text = AgHataMesaji.Turkcele(mesaj);
        SifirHataKutusu.Visibility = Visibility.Visible;
    }

    private void SifirBilgiGoster(string mesaj)
    {
        TxtSifirBilgi.Text = mesaj;
        SifirBilgiKutusu.Visibility = Visibility.Visible;
    }
}

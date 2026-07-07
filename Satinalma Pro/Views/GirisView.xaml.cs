using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SatinalmaPro.Services;
using SatinalmaPro.Shared.Helpers;

namespace SatinalmaPro.Views;

public partial class GirisView : UserControl
{
    public event Action? GirisBasarili;

    private bool _tercihYukleniyor;

    public GirisView()
    {
        InitializeComponent();
        Loaded += (_, _) => TercihleriYukle();
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

            if (tercih.SifremiHatirla)
            {
                var kayitliSifre = GirisSifreDeposu.Oku();
                TxtSifre.Password = kayitliSifre ?? "";
            }
            else
            {
                TxtSifre.Password = "";
            }
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

        OturumYoneticisi.TercihKutulariniKaydet(
            TxtEposta.Text,
            ChkBeniHatirla.IsChecked == true,
            ChkSifremiHatirla.IsChecked == true);
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
                TxtEposta.Text, TxtSifre.Password, beniHatirla, sifremiHatirla);
            GirisBasarili?.Invoke();
        }
        catch (Exception ex)
        {
            HataGoster(ex.Message);
        }
        finally
        {
            BtnGiris.IsEnabled = true;
            BtnGiris.Content = "Giriş Yap";
        }
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

using System.Windows;
using System.Windows.Input;
using SatinalmaPro.Shared.Models;
using SatinalmaPro.Shared.SaaS;
using SatinalmaYonetici.Services;

namespace SatinalmaYonetici;

public partial class MainWindow : Window
{
    private readonly PlatformOturum _oturum = new();
    private KiracıKaydi? _seciliFirma;

    public MainWindow()
    {
        InitializeComponent();
        RolKutusu.ItemsSource = KullaniciRolleri.Tum;
        RolKutusu.SelectedItem = KullaniciRolleri.Saha;

        if (!_oturum.Yapilandirildi)
        {
            DurumMetni.Text = "firebase_ayarlar.json bulunamadı.";
            YonetimPanel.Visibility = Visibility.Collapsed;
            GirisHata.Text = "firebase_ayarlar.json bulunamadı. Exe yanında yapılandırma dosyası olmalı.";
            GirisHata.Visibility = Visibility.Visible;
            return;
        }

        YonetimPanel.Visibility = Visibility.Collapsed;
        if (_oturum.OtomatikGirisDene())
            PanelleriGoster(yonetim: true);
    }

    private void PanelleriGoster(bool yonetim)
    {
        GirisPaneli.Visibility = yonetim ? Visibility.Collapsed : Visibility.Visible;
        YonetimPanel.Visibility = yonetim ? Visibility.Visible : Visibility.Collapsed;
        if (!yonetim)
            GirisHata.Visibility = Visibility.Collapsed;
    }

    private void GirisAlani_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            Giris_Click(sender, e);
    }

    private async void Giris_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            GirisHata.Visibility = Visibility.Collapsed;
            await _oturum.GirisYapAsync(GirisEposta.Text, GirisSifre.Password);

            // İlk kurulum: platform admin yoksa otomatik tanımla
            try
            {
                await FirmalariYukleAsync(gosterHata: false);
            }
            catch (Exception listeEx) when (YetkiHatasiMi(listeEx))
            {
                await _oturum.Platform.BootstrapAdminAsync();
                await FirmalariYukleAsync(gosterHata: true);
            }

            PanelleriGoster(yonetim: true);
            DurumMetni.Text = "Giriş başarılı.";
        }
        catch (Exception ex)
        {
            if (YetkiHatasiMi(ex))
            {
                GirisHata.Text =
                    "Bu hesap platform yöneticisi değil. Altındaki 'İlk kurulum' butonuna tıklayın " +
                    "veya Firestore → platform_admins kaydını kontrol edin.";
            }
            else
            {
                GirisHata.Text = ex.Message;
            }

            GirisHata.Visibility = Visibility.Visible;
            PanelleriGoster(yonetim: false);
        }
    }

    private static bool YetkiHatasiMi(Exception ex) =>
        ex.Message.Contains("Platform yöneticisi yetkisi", StringComparison.OrdinalIgnoreCase) ||
        ex.Message.Contains("permission-denied", StringComparison.OrdinalIgnoreCase);

    private void Cikis_Click(object sender, RoutedEventArgs e)
    {
        _oturum.CikisYap();
        FirmaGrid.ItemsSource = null;
        KullaniciGrid.ItemsSource = null;
        KullaniciBaslik.Text = "Kullanıcılar";
        DurumMetni.Text = "Oturum kapatıldı.";
        PanelleriGoster(yonetim: false);
    }

    private async void FirmalariYukle_Click(object sender, RoutedEventArgs e) => await FirmalariYukleAsync();

    private async Task FirmalariYukleAsync(bool gosterHata = true)
    {
        try
        {
            FirmaGrid.ItemsSource = await _oturum.Platform.FirmalariListeleAsync();
            DurumMetni.Text = "Firmalar yüklendi.";
            if (BtnKurulum != null)
                BtnKurulum.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            if (!gosterHata)
                throw;

            if (YetkiHatasiMi(ex))
            {
                DurumMetni.Text = "Platform admin yetkisi yok.";
                if (BtnKurulum != null)
                    BtnKurulum.Visibility = Visibility.Visible;
                MessageBox.Show(
                    "Platform yöneticisi yetkisi gerekli.\n\n" +
                    "Çözüm: üst bardaki 'Platform admin yap' butonuna tıklayın.\n" +
                    "Çalışmazsa Firestore → platform_admins koleksiyonunu kontrol edin.",
                    "Firmalar",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show(ex.Message, "Firmalar", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void FirmaGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        _seciliFirma = FirmaGrid.SelectedItem as KiracıKaydi;
        if (_seciliFirma is null)
            return;

        FirmaKod.Text = _seciliFirma.Kod;
        FirmaAd.Text = _seciliFirma.Ad;
        FirmaAktif.IsChecked = _seciliFirma.Aktif;
        KullaniciBaslik.Text = $"Kullanıcılar — {_seciliFirma.Ad}";
        _ = KullanicilariYukleAsync();
    }

    private void YeniFirma_Click(object sender, RoutedEventArgs e)
    {
        _seciliFirma = null;
        FirmaGrid.SelectedItem = null;
        FirmaKod.Text = "";
        FirmaAd.Text = "";
        FirmaAktif.IsChecked = true;
        KullaniciGrid.ItemsSource = null;
        KullaniciBaslik.Text = "Kullanıcılar";
        DurumMetni.Text = "Yeni firma formu hazır.";
    }

    private async void FirmaKaydet_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var kayit = await _oturum.Platform.FirmaKaydetAsync(new KiracıKaydi
            {
                Id = _seciliFirma?.Id ?? "",
                Kod = FirmaKod.Text.Trim(),
                Ad = FirmaAd.Text.Trim(),
                Aktif = FirmaAktif.IsChecked == true
            });
            await FirmalariYukleAsync();
            DurumMetni.Text = $"Firma kaydedildi: {kayit.Ad}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Firma", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task KullanicilariYukleAsync()
    {
        if (_seciliFirma is null || string.IsNullOrWhiteSpace(_seciliFirma.Id))
            return;

        try
        {
            KullaniciGrid.ItemsSource = await _oturum.Platform.KullanicilariListeleAsync(_seciliFirma.Id);
            DurumMetni.Text = $"{_seciliFirma.Ad} kullanıcıları yüklendi.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Kullanıcılar", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void KullaniciGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (KullaniciGrid.SelectedItem is not PlatformKullaniciKaydi k)
            return;

        KullaniciAdi.Text = k.KullaniciAdi;
        KullaniciEposta.Text = k.Eposta;
        KullaniciAdSoyad.Text = k.AdSoyad;
        KullaniciSaha.Text = k.Saha;
        KullaniciAktif.IsChecked = k.Aktif;
        KullaniciSifre.Password = "";
        RolKutusu.SelectedItem = k.Rol;
    }

    private void YeniKullanici_Click(object sender, RoutedEventArgs e)
    {
        KullaniciGrid.SelectedItem = null;
        KullaniciAdi.Text = "";
        KullaniciEposta.Text = "";
        KullaniciAdSoyad.Text = "";
        KullaniciSaha.Text = "";
        KullaniciSifre.Password = "";
        KullaniciAktif.IsChecked = true;
        RolKutusu.SelectedItem = KullaniciRolleri.Saha;
        DurumMetni.Text = "Yeni kullanıcı formu hazır.";
    }

    private async void LegacyAktar_Click(object sender, RoutedEventArgs e)
    {
        if (_seciliFirma is null || string.IsNullOrWhiteSpace(_seciliFirma.Id))
        {
            MessageBox.Show("Önce bir firma seçin (veya kaydedin).", "Aktarım",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var onay = MessageBox.Show(
            $"Eski sistemdeki (/users) aktif kullanıcılar «{_seciliFirma.Ad}» firmasına aktarılacak.\n\n" +
            "Kullanıcı adı yoksa e-posta adresinden üretilecek.\nDevam edilsin mi?",
            "Eski kullanıcıları aktar",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (onay != MessageBoxResult.Yes)
            return;

        try
        {
            var (imported, skipped, total) =
                await _oturum.Platform.LegacyKullanicilariAktarAsync(_seciliFirma.Id);
            await KullanicilariYukleAsync();
            DurumMetni.Text = $"Aktarım: {imported} eklendi, {skipped} atlandı (toplam {total}).";
            MessageBox.Show(
                $"Tamamlandı.\n\nAktarılan: {imported}\nAtlanan: {skipped}\nKaynak toplam: {total}",
                "Aktarım",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Aktarım", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void KullaniciKaydet_Click(object sender, RoutedEventArgs e)
    {
        if (_seciliFirma is null || string.IsNullOrWhiteSpace(_seciliFirma.Id))
        {
            MessageBox.Show("Önce bir firma seçin.", "Kullanıcı", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var secili = KullaniciGrid.SelectedItem as PlatformKullaniciKaydi;
            var sifre = string.IsNullOrWhiteSpace(KullaniciSifre.Password) ? null : KullaniciSifre.Password;
            if (secili is null && string.IsNullOrWhiteSpace(sifre))
            {
                MessageBox.Show("Yeni kullanıcı için şifre girin.", "Kullanıcı", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            await _oturum.Platform.KullaniciKaydetAsync(
                _seciliFirma.Id,
                new PlatformKullaniciKaydi
                {
                    Uid = secili?.Uid ?? "",
                    KullaniciAdi = KullaniciAdi.Text.Trim(),
                    Eposta = KullaniciEposta.Text.Trim(),
                    AdSoyad = KullaniciAdSoyad.Text.Trim(),
                    Rol = RolKutusu.SelectedItem?.ToString() ?? KullaniciRolleri.Saha,
                    Saha = KullaniciSaha.Text.Trim(),
                    Aktif = KullaniciAktif.IsChecked == true
                },
                sifre);

            await KullanicilariYukleAsync();
            DurumMetni.Text = "Kullanıcı kaydedildi.";
            KullaniciSifre.Password = "";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Kullanıcı", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void Bootstrap_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            GirisHata.Visibility = Visibility.Collapsed;

            // Giriş paneli görünürse önce kimlik doğrula; yönetim panelindeyse oturum zaten var
            if (GirisPaneli.Visibility == Visibility.Visible)
            {
                if (string.IsNullOrWhiteSpace(GirisEposta.Text) || string.IsNullOrWhiteSpace(GirisSifre.Password))
                {
                    GirisHata.Text = "Önce e-posta ve şifre girin.";
                    GirisHata.Visibility = Visibility.Visible;
                    return;
                }

                await _oturum.GirisYapAsync(GirisEposta.Text, GirisSifre.Password);
            }

            await _oturum.Platform.BootstrapAdminAsync();
            MessageBox.Show(
                "Platform yöneticisi tanımlandı. Artık firma ve kullanıcı ekleyebilirsiniz.",
                "Kurulum",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            PanelleriGoster(yonetim: true);
            await FirmalariYukleAsync();
        }
        catch (Exception ex)
        {
            if (GirisPaneli.Visibility == Visibility.Visible)
            {
                GirisHata.Text = ex.Message;
                GirisHata.Visibility = Visibility.Visible;
            }
            else
            {
                MessageBox.Show(ex.Message, "Kurulum", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SatinalmaPro.Shared.Models;
using SatinalmaPro.Shared.SaaS;
using SatinalmaYonetici.Services;
using YerelBilgi = SatinalmaYonetici.Helpers.UygulamaBilgisi;

namespace SatinalmaYonetici;

public partial class MainWindow : Window
{
    private readonly PlatformOturum _oturum = new();
    private KiracıKaydi? _seciliFirma;

    public MainWindow()
    {
        InitializeComponent();
        SurumMetinleriniGuncelle();
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

            // İlk kurulum: platform admin yoksa otomatik tanımla; varsa firmalardan ayır
            try
            {
                await FirmalariYukleAsync(gosterHata: false);
                // Mevcut admin: yanlışlıkla firmaya eklenmiş kaydı temizle
                await _oturum.Platform.PlatformHesabiniFirmalardanAyirAsync();
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
        LisansTipiniSec(_seciliFirma.LisansTipi);
        FirmaLisansDurum.Text = LisansDurumMetni(_seciliFirma);
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
        LisansTipiniSec(LisansTipleri.Deneme);
        FirmaLisansDurum.Text = "Yeni firmaya otomatik 30 gün deneme verilir.";
        KullaniciGrid.ItemsSource = null;
        KullaniciBaslik.Text = "Kullanıcılar";
        DurumMetni.Text = "Yeni firma formu hazır.";
    }

    private async void FirmaKaydet_Click(object sender, RoutedEventArgs e)
    {
        await FirmaKaydetInternalAsync(lisansYenile: false);
    }

    private async void LisansYenile_Click(object sender, RoutedEventArgs e)
    {
        if (_seciliFirma is null || string.IsNullOrWhiteSpace(_seciliFirma.Id))
        {
            MessageBox.Show("Önce bir firma seçin.", "Lisans", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var tip = SeciliLisansTipi();
        var gun = tip == LisansTipleri.Yillik ? 365 : 30;
        var onay = MessageBox.Show(
            $"Lisans bugünden itibaren {gun} gün yenilenecek ({LisansTipleri.GorunenAd(tip)}).\n"
            + "Süre dolmuşsa firma ve kullanıcılar yeniden aktifleştirilebilir.\n\nDevam edilsin mi?",
            "Lisansı yenile",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (onay != MessageBoxResult.Yes)
            return;

        await FirmaKaydetInternalAsync(lisansYenile: true);
    }

    private async void FirmaSil_Click(object sender, RoutedEventArgs e)
    {
        if (_seciliFirma is null || string.IsNullOrWhiteSpace(_seciliFirma.Id))
        {
            MessageBox.Show("Önce silinecek firmayı seçin.", "Firma sil",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var kod = _seciliFirma.Kod.Trim();
        if (string.IsNullOrWhiteSpace(kod))
        {
            MessageBox.Show("Firma kodu tanımlı değil. Önce firmayı kaydedin.", "Firma sil",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var uyari = MessageBox.Show(
            $"«{_seciliFirma.Ad}» firması ve tüm verisi (kullanıcılar, talepler, siparişler) kalıcı olarak silinecek.\n\n" +
            "Bu işlem geri alınamaz.\n\nDevam etmek için bir sonraki adımda firma kodunu yazmanız istenecek.\n\nDevam edilsin mi?",
            "Firmayı sil",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (uyari != MessageBoxResult.Yes)
            return;

        var onayKodu = MetinGir(
            "Firmayı sil — onay",
            $"Kalıcı silme için firma kodunu yazın: {kod}",
            "");
        if (onayKodu is null)
            return;

        if (!string.Equals(onayKodu.Trim(), kod, StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show("Onay kodu firma kodu ile eşleşmiyor. Silme iptal edildi.", "Firma sil",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var silinenKullanici = await _oturum.Platform.FirmaSilAsync(_seciliFirma.Id, onayKodu.Trim());
            _seciliFirma = null;
            FirmaGrid.SelectedItem = null;
            YeniFirma_Click(sender, e);
            await FirmalariYukleAsync();
            DurumMetni.Text = $"Firma silindi ({silinenKullanici} kullanıcı kaldırıldı).";
            MessageBox.Show(
                $"Firma ve tüm verisi silindi.\n\nKaldırılan kullanıcı: {silinenKullanici}",
                "Firma sil",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Firma sil", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task FirmaKaydetInternalAsync(bool lisansYenile)
    {
        try
        {
            var tip = SeciliLisansTipi();
            var kayit = await _oturum.Platform.FirmaKaydetAsync(new KiracıKaydi
            {
                Id = _seciliFirma?.Id ?? "",
                Kod = FirmaKod.Text.Trim(),
                Ad = FirmaAd.Text.Trim(),
                Aktif = lisansYenile || FirmaAktif.IsChecked == true,
                LisansTipi = tip
            }, lisansYenile);

            await FirmalariYukleAsync();
            FirmaLisansDurum.Text = LisansDurumMetni(kayit);
            DurumMetni.Text = lisansYenile
                ? $"Lisans yenilendi: {kayit.Ad}"
                : $"Firma kaydedildi: {kayit.Ad}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Firma", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private string SeciliLisansTipi()
    {
        if (FirmaLisansTipi.SelectedItem is System.Windows.Controls.ComboBoxItem item &&
            item.Tag is string tag)
            return tag;
        return LisansTipleri.Deneme;
    }

    private void LisansTipiniSec(string? tip)
    {
        var hedef = tip == LisansTipleri.Yillik ? LisansTipleri.Yillik : LisansTipleri.Deneme;
        foreach (var obj in FirmaLisansTipi.Items)
        {
            if (obj is System.Windows.Controls.ComboBoxItem item &&
                string.Equals(item.Tag as string, hedef, StringComparison.OrdinalIgnoreCase))
            {
                FirmaLisansTipi.SelectedItem = item;
                return;
            }
        }
    }

    private static string LisansDurumMetni(KiracıKaydi f)
    {
        if (f.LisansSuresiDoldu || f.LisansKalanGun is <= 0)
            return "Lisans süresi dolmuş — kullanıcılar giriş yapamaz.";

        var bitis = f.LisansBitis;
        var kalan = f.LisansKalanGun is null ? "?" : f.LisansKalanGun.Value.ToString();
        return $"{LisansTipleri.GorunenAd(f.LisansTipi)} · {kalan} gün kaldı"
               + (string.IsNullOrWhiteSpace(bitis) ? "" : $" · bitiş: {bitis[..Math.Min(10, bitis.Length)]}");
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
            var ad = KullaniciAdi.Text.Trim();
            var hata = KullaniciAdiYardimcisi.DogrulaVeyaHata(ad);
            if (hata is not null)
            {
                MessageBox.Show(hata, "Kullanıcı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

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

    private async void KullaniciSil_Click(object sender, RoutedEventArgs e)
    {
        if (_seciliFirma is null || string.IsNullOrWhiteSpace(_seciliFirma.Id))
        {
            MessageBox.Show("Önce bir firma seçin.", "Kullanıcı sil",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (KullaniciGrid.SelectedItem is not PlatformKullaniciKaydi secili ||
            string.IsNullOrWhiteSpace(secili.Uid))
        {
            MessageBox.Show("Önce silinecek kullanıcıyı listeden seçin.", "Kullanıcı sil",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var etiket = string.IsNullOrWhiteSpace(secili.KullaniciAdi)
            ? secili.AdSoyad
            : secili.KullaniciAdi;

        var onay = MessageBox.Show(
            $"«{etiket}» kullanıcısı kalıcı olarak silinecek.\n\n" +
            "Firebase Auth hesabı ve kullanıcı adı eşlemesi de kaldırılır.\n\nDevam edilsin mi?",
            "Kullanıcıyı sil",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (onay != MessageBoxResult.Yes)
            return;

        try
        {
            await _oturum.Platform.KullaniciSilAsync(_seciliFirma.Id, secili.Uid);
            YeniKullanici_Click(sender, e);
            await KullanicilariYukleAsync();
            DurumMetni.Text = $"Kullanıcı silindi: {etiket}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Kullanıcı sil", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                "Platform yöneticisi tanımlandı.\n\n" +
                "Bu hesap hiçbir firmaya bağlı değildir.\n" +
                "Firmalara giriş için ayrı kullanıcı adları oluşturun.\n" +
                "Rezerve isimler: platform, yonetici, owner…",
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

    private async void PlatformAyir_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var (users, names) = await _oturum.Platform.PlatformHesabiniFirmalardanAyirAsync();
            MessageBox.Show(
                $"Platform hesabı firmalardan ayrıldı.\n\n" +
                $"Kaldırılan firma kullanıcı kaydı: {users}\n" +
                $"Kaldırılan kullanıcı adı eşlemesi: {names}\n\n" +
                "Bundan sonra bu e-posta ile yalnızca Satınalma Yönetici kullanılır.",
                "Platform hesabı",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            DurumMetni.Text = "Platform hesabı firmalardan ayrıldı.";
            if (_seciliFirma is not null)
                await KullanicilariYukleAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Platform hesabı", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void SurumMetinleriniGuncelle()
    {
        var metin = YerelBilgi.AltBilgiMetni;
        if (GirisSurum != null) GirisSurum.Text = metin;
        if (AltSurumMetni != null) AltSurumMetni.Text = metin;
    }

    private async void GuncellemeKontrol_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            DurumMetni.Text = "Güncelleme kontrol ediliyor...";
            var guncellendi = await GuncellemeServisi.KontrolEtVeUygulaAsync(
                sessiz: false,
                ilerle: (mesaj, _) => Dispatcher.Invoke(() => DurumMetni.Text = mesaj));

            if (!guncellendi)
            {
                DurumMetni.Text = "Uygulama güncel.";
                MessageBox.Show(
                    $"Satınalma Yönetici güncel.\n\nSürüm: {YerelBilgi.Versiyon}",
                    "Güncelleme",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            DurumMetni.Text = "Güncelleme kontrolü başarısız.";
            MessageBox.Show(ex.Message, "Güncelleme", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private static string? MetinGir(string baslik, string aciklama, string varsayilan)
    {
        var dlg = new Window
        {
            Title = baslik,
            Width = 440,
            Height = 210,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            ResizeMode = ResizeMode.NoResize,
            Background = System.Windows.Media.Brushes.White
        };

        var kutu = new TextBox
        {
            Text = varsayilan,
            Height = 36,
            Padding = new Thickness(10, 6, 10, 6),
            FontSize = 14,
            VerticalContentAlignment = VerticalAlignment.Center
        };

        var tamam = false;
        string? sonuc = null;

        var panel = new StackPanel { Margin = new Thickness(20) };
        panel.Children.Add(new TextBlock
        {
            Text = aciklama,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 12)
        });
        panel.Children.Add(kutu);

        var btnRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 16, 0, 0)
        };

        var iptalBtn = new Button { Content = "İptal", MinWidth = 88, Margin = new Thickness(0, 0, 8, 0), Padding = new Thickness(12, 6, 12, 6) };
        iptalBtn.Click += (_, _) => dlg.Close();

        var okBtn = new Button { Content = "Sil", MinWidth = 88, Padding = new Thickness(12, 6, 12, 6), IsDefault = true };
        okBtn.Click += (_, _) =>
        {
            tamam = true;
            sonuc = kutu.Text;
            dlg.Close();
        };

        btnRow.Children.Add(iptalBtn);
        btnRow.Children.Add(okBtn);
        panel.Children.Add(btnRow);
        dlg.Content = panel;

        kutu.KeyDown += (_, ke) =>
        {
            if (ke.Key == Key.Enter)
            {
                tamam = true;
                sonuc = kutu.Text;
                dlg.Close();
            }
        };

        dlg.ShowDialog();
        return tamam ? sonuc : null;
    }
}

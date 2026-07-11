using System.Windows;
using System.Windows.Controls;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;
using SatinalmaPro.Shared.SaaS;

namespace SatinalmaPro.Views;

public partial class KullaniciYonetimWindow : Window
{
    private sealed class ModulYetkiSatir
    {
        public required string ModulAdi { get; init; }
        public required CheckBox Okuma { get; init; }
        public required CheckBox Yazma { get; init; }
        public List<CheckBox> SekmeKutulari { get; init; } = [];
    }

    private readonly List<ModulYetkiSatir> _modulSatirlari = [];
    private KullaniciProfili? _seciliKullanici;
    private bool _formDolduruluyor;
    private readonly bool _saasModu;

    public KullaniciYonetimWindow()
    {
        InitializeComponent();
        _saasModu = !string.IsNullOrWhiteSpace(KiracıOturumu.TenantId);
        ModulYetkiSatirlariniOlustur();

        foreach (var rol in KullaniciRolleri.Tum)
            CmbYeniRol.Items.Add(rol);
        CmbYeniRol.SelectedItem = KullaniciRolleri.Satinalma;
        RolVarsayilaniniSec();

        if (_saasModu)
            SaaSModunuUygula();

        _ = YukleAsync();
    }

    private void SaaSModunuUygula()
    {
        var uyari =
            "SaaS kiracı modunda kullanıcı oluşturma, düzenleme ve silme " +
            "Satınalma Yönetici uygulamasından yapılır.\n\n" +
            "Bu ekran yalnızca mevcut kullanıcıları görüntülemek içindir.";

        MessageBox.Show(uyari, UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);

        TxtFormBaslik.Text = "Kullanıcı yönetimi (Satınalma Yönetici)";
        TxtFormIpucu.Text =
            "Firma kullanıcıları Satınalma Yönetici uygulamasından eklenir ve düzenlenir. " +
            "Pro içinden Auth/usernames yazımı kapalıdır.";

        TxtYeniAd.IsEnabled = false;
        TxtYeniEposta.IsEnabled = false;
        TxtYeniSifre.IsEnabled = false;
        CmbYeniRol.IsEnabled = false;
        ChkAktif.IsEnabled = false;
        BtnEkle.IsEnabled = false;
        BtnGuncelle.IsEnabled = false;
        BtnSil.IsEnabled = false;
        foreach (var satir in _modulSatirlari)
        {
            satir.Okuma.IsEnabled = false;
            satir.Yazma.IsEnabled = false;
            foreach (var sekme in satir.SekmeKutulari)
                sekme.IsEnabled = false;
        }
    }

    private bool SaaSYazmaEngelliMi()
    {
        if (!_saasModu)
            return false;

        MessageBox.Show(
            "SaaS kiracı modunda kullanıcı işlemleri Satınalma Yönetici uygulamasından yapılır.\n" +
            "Pro içinden kullanıcı oluşturma/güncelleme/silme kapalıdır.",
            UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
        return true;
    }

    private async Task YukleAsync()
    {
        await EpostaSablonDeposu.YukleAsync();
        await ListeyiYenileAsync();
    }

    private void ModulYetkiSatirlariniOlustur()
    {
        foreach (var modul in ModuleCatalog.All)
        {
            var panel = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
            var ust = new Grid();
            ust.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            ust.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            ust.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var baslik = new TextBlock
            {
                Text = modul.Title,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(baslik, 0);

            var okuma = new CheckBox { Content = "Okuma", Margin = new Thickness(12, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(okuma, 1);
            var yazma = new CheckBox { Content = "Yazma", Margin = new Thickness(0, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(yazma, 2);

            okuma.Checked += (_, _) => { if (yazma.IsChecked == true && okuma.IsChecked != true) okuma.IsChecked = true; };
            yazma.Checked += (_, _) => { if (yazma.IsChecked == true) okuma.IsChecked = true; };
            okuma.Unchecked += (_, _) => { if (okuma.IsChecked != true) yazma.IsChecked = false; };

            ust.Children.Add(baslik);
            ust.Children.Add(okuma);
            ust.Children.Add(yazma);
            panel.Children.Add(ust);

            var sekmeKutulari = new List<CheckBox>();
            var sekmeler = ModulSekmeKatalogu.SekmeleriAl(modul.Title);
            if (sekmeler.Count > 0)
            {
                var sekmePanel = new WrapPanel { Margin = new Thickness(0, 6, 0, 0) };
                foreach (var sekme in sekmeler)
                {
                    var kutu = new CheckBox
                    {
                        Content = sekme,
                        Margin = new Thickness(0, 0, 14, 6),
                        FontSize = 11,
                        Foreground = (System.Windows.Media.Brush)FindResource("InkMutedBrush")
                    };
                    sekmeKutulari.Add(kutu);
                    sekmePanel.Children.Add(kutu);
                }
                panel.Children.Add(sekmePanel);
            }

            _modulSatirlari.Add(new ModulYetkiSatir
            {
                ModulAdi = modul.Title,
                Okuma = okuma,
                Yazma = yazma,
                SekmeKutulari = sekmeKutulari
            });

            ModulYetkiPanel.Children.Add(new Border
            {
                BorderBrush = (System.Windows.Media.Brush)FindResource("LineBrush"),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(0, 0, 0, 8),
                Child = panel
            });
        }
    }

    private async Task ListeyiYenileAsync()
    {
        if (OturumYoneticisi.Firestore is null)
        {
            MessageBox.Show("Bulut bağlantısı yok.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            KullaniciGrid.ItemsSource = await OturumYoneticisi.Firestore.TumKullanicilariOkuAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void Yenile_Click(object sender, RoutedEventArgs e)
    {
        try { await ListeyiYenileAsync(); }
        catch (Exception ex)
        {
            HataGunlugu.Kaydet(ex, "KullaniciYonetim.Yenile");
            MessageBox.Show(ex.Message, UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void SecimiTemizle_Click(object sender, RoutedEventArgs e)
    {
        _seciliKullanici = null;
        KullaniciGrid.SelectedItem = null;
        FormuSeciliKullaniciyaGoreDoldur();
    }

    private void KullaniciGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_formDolduruluyor)
            return;

        _seciliKullanici = KullaniciGrid.SelectedItem as KullaniciProfili;
        FormuSeciliKullaniciyaGoreDoldur();
    }

    private void KullaniciGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) =>
        KullaniciyiDuzenleModunaAl();

    private void KullaniciSatirDuzenle_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: KullaniciProfili profil })
            return;

        KullaniciGrid.SelectedItem = profil;
        KullaniciyiDuzenleModunaAl();
    }

    private void KullaniciyiDuzenleModunaAl()
    {
        if (KullaniciGrid.SelectedItem is not KullaniciProfili)
            return;

        _seciliKullanici = (KullaniciProfili)KullaniciGrid.SelectedItem;
        FormuSeciliKullaniciyaGoreDoldur();
        TxtYeniAd.Focus();
    }

    private void FormuSeciliKullaniciyaGoreDoldur()
    {
        var duzenleme = _seciliKullanici is not null;

        TxtFormBaslik.Text = duzenleme
            ? $"Düzenleniyor: {_seciliKullanici!.AdSoyad}"
            : "Yeni kullanıcı ekle";
        TxtFormIpucu.Text = duzenleme
            ? "Rol ve yetkileri değiştirin, ardından «Değişiklikleri Kaydet (Güncelle)» butonuna basın."
            : "Yeni kayıt için alanları doldurup «Kullanıcı Ekle»ye basın.";

        BtnGuncelle.IsEnabled = duzenleme && !_saasModu;
        BtnSil.IsEnabled = duzenleme && !_saasModu;
        BtnSifreSifir.IsEnabled = duzenleme && !_saasModu;
        BtnEkle.IsEnabled = !duzenleme && !_saasModu;
        TxtYeniSifre.IsEnabled = !duzenleme && !_saasModu;
        TxtYeniEposta.IsReadOnly = duzenleme || _saasModu;
        TxtYeniAd.IsEnabled = !_saasModu;
        CmbYeniRol.IsEnabled = !_saasModu;
        ChkAktif.IsEnabled = !_saasModu;

        if (!duzenleme)
        {
            FormuTemizle();
            return;
        }

        _formDolduruluyor = true;
        try
        {
            TxtYeniAd.Text = _seciliKullanici!.AdSoyad;
            TxtYeniEposta.Text = _seciliKullanici.Eposta;
            TxtYeniSifre.Password = "";
            ChkAktif.IsChecked = _seciliKullanici.Aktif;
            RolSec(_seciliKullanici.Rol);

            if (_seciliKullanici.ModulYetkileri.Count > 0)
                YetkileriYukle(_seciliKullanici.ModulYetkileri);
            else
            {
                var moduller = _seciliKullanici.Moduller.Count > 0
                    ? _seciliKullanici.Moduller
                    : KullaniciRolleri.VarsayilanModuller(_seciliKullanici.Rol).ToList();
                YetkileriRoldenYukle(_seciliKullanici.Rol, moduller);
            }
        }
        finally
        {
            _formDolduruluyor = false;
        }
    }

    private void FormuTemizle()
    {
        _formDolduruluyor = true;
        try
        {
            TxtYeniAd.Text = "";
            TxtYeniEposta.Text = "";
            TxtYeniSifre.Password = "";
            ChkAktif.IsChecked = true;
            CmbYeniRol.SelectedItem = KullaniciRolleri.Satinalma;
            TxtYeniEposta.IsReadOnly = false;
            RolVarsayilaniniSec();
        }
        finally
        {
            _formDolduruluyor = false;
        }
    }

    private void YeniKullaniciModunaGec()
    {
        _seciliKullanici = null;
        KullaniciGrid.SelectedItem = null;
        FormuTemizle();
        FormuSeciliKullaniciyaGoreDoldur();
    }

    private void RolSec(string rol)
    {
        rol = KullaniciRolleri.Normalize(rol);
        for (var i = 0; i < CmbYeniRol.Items.Count; i++)
        {
            if (string.Equals(CmbYeniRol.Items[i]?.ToString(), rol, StringComparison.OrdinalIgnoreCase))
            {
                CmbYeniRol.SelectedIndex = i;
                return;
            }
        }
        CmbYeniRol.SelectedItem = rol;
    }

    private void CmbYeniRol_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_seciliKullanici is not null)
        {
            foreach (var satir in _modulSatirlari)
                YazmaKutusunuRolIleSinirla(satir.Yazma, satir.ModulAdi, AktifFormRolu());
            return;
        }

        RolVarsayilaniniSec();
    }

    private void RolVarsayilaniniSec_Click(object sender, RoutedEventArgs e) => RolVarsayilaniniSec();

    private void RolVarsayilaniniSec()
    {
        var rol = CmbYeniRol.SelectedItem?.ToString() ?? KullaniciRolleri.Saha;
        YetkileriRoldenYukle(rol, KullaniciRolleri.VarsayilanModuller(rol));
    }

    private void TumunuTemizle_Click(object sender, RoutedEventArgs e)
    {
        foreach (var satir in _modulSatirlari)
        {
            satir.Okuma.IsChecked = false;
            satir.Yazma.IsChecked = false;
            foreach (var sekme in satir.SekmeKutulari)
                sekme.IsChecked = false;
        }
    }

    private void YetkileriRoldenYukle(string rol, IEnumerable<string> moduller)
    {
        var set = moduller.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var yazmaAtanabilir = KullaniciYetkileri.RolYazmaAtanabilir(rol);

        foreach (var satir in _modulSatirlari)
        {
            var okuma = set.Contains(satir.ModulAdi);
            satir.Okuma.IsChecked = okuma;

            if (satir.ModulAdi.Equals("Satınalma", StringComparison.OrdinalIgnoreCase))
            {
                var r = KullaniciRolleri.Normalize(rol);
                satir.Yazma.IsChecked = okuma && (yazmaAtanabilir || r is KullaniciRolleri.Yonetim or KullaniciRolleri.Sef or KullaniciRolleri.Saha);
            }
            else
                satir.Yazma.IsChecked = okuma && yazmaAtanabilir;

            YazmaKutusunuRolIleSinirla(satir.Yazma, satir.ModulAdi, rol);
            foreach (var sekme in satir.SekmeKutulari)
            {
                var ad = sekme.Content?.ToString() ?? "";
                if (satir.ModulAdi.Equals("Satınalma", StringComparison.OrdinalIgnoreCase))
                {
                    sekme.IsChecked = okuma && SatinalmaPro.Shared.Models.KullaniciRolleri
                        .VarsayilanSatinalmaSekmeler(rol)
                        .Contains(ad, StringComparer.OrdinalIgnoreCase);
                }
                else
                    sekme.IsChecked = okuma;
            }
        }
    }

    private void YetkileriYukle(IEnumerable<ModulYetkiKaydi> yetkiler)
    {
        var sozluk = yetkiler.ToDictionary(y => y.Modul, StringComparer.OrdinalIgnoreCase);
        foreach (var satir in _modulSatirlari)
        {
            if (!sozluk.TryGetValue(satir.ModulAdi, out var yetki))
            {
                satir.Okuma.IsChecked = false;
                satir.Yazma.IsChecked = false;
                foreach (var sekme in satir.SekmeKutulari)
                    sekme.IsChecked = false;
                continue;
            }

            satir.Okuma.IsChecked = yetki.Okuma;
            satir.Yazma.IsChecked = yetki.Yazma && YazmaAtanabilirMi(AktifFormRolu(), satir.ModulAdi);
            YazmaKutusunuRolIleSinirla(satir.Yazma, satir.ModulAdi, AktifFormRolu());
            foreach (var sekmeKutu in satir.SekmeKutulari)
            {
                var ad = sekmeKutu.Content?.ToString() ?? "";
                sekmeKutu.IsChecked = yetki.Sekmeler.Count == 0 || yetki.Sekmeler.Contains(ad, StringComparer.OrdinalIgnoreCase);
            }
        }
    }

    private List<ModulYetkiKaydi> YetkileriTopla()
    {
        var rol = AktifFormRolu();
        var liste = new List<ModulYetkiKaydi>();
        foreach (var satir in _modulSatirlari)
        {
            if (satir.Okuma.IsChecked != true && satir.Yazma.IsChecked != true)
                continue;

            var sekmeler = satir.SekmeKutulari
                .Where(k => k.IsChecked == true)
                .Select(k => k.Content?.ToString() ?? "")
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            var yazma = satir.Yazma.IsChecked == true && YazmaAtanabilirMi(rol, satir.ModulAdi);

            liste.Add(new ModulYetkiKaydi
            {
                Modul = satir.ModulAdi,
                Okuma = satir.Okuma.IsChecked == true,
                Yazma = yazma,
                Sekmeler = sekmeler.Count == satir.SekmeKutulari.Count ? [] : sekmeler
            });
        }

        return liste;
    }

    private string AktifFormRolu() =>
        KullaniciRolleri.Normalize(CmbYeniRol.SelectedItem?.ToString());

    private static bool YazmaAtanabilirMi(string rol, string modulAdi)
    {
        if (modulAdi.Equals("Satınalma", StringComparison.OrdinalIgnoreCase))
        {
            var r = KullaniciRolleri.Normalize(rol);
            return KullaniciYetkileri.RolYazmaAtanabilir(rol)
                   || r is KullaniciRolleri.Yonetim or KullaniciRolleri.Sef or KullaniciRolleri.Saha;
        }

        return KullaniciYetkileri.RolYazmaAtanabilir(rol);
    }

    private static void YazmaKutusunuRolIleSinirla(CheckBox yazma, string modulAdi, string? rol)
    {
        rol ??= KullaniciRolleri.Saha;
        yazma.IsEnabled = YazmaAtanabilirMi(rol, modulAdi);
        if (!yazma.IsEnabled)
            yazma.IsChecked = false;
    }

    private KullaniciProfili FormdanProfilOlustur(string? uid = null)
    {
        var yetkiler = YetkileriTopla();
        return new KullaniciProfili
        {
            Uid = uid ?? "",
            AdSoyad = TxtYeniAd.Text.Trim(),
            Eposta = TxtYeniEposta.Text.Trim(),
            Rol = KullaniciRolleri.Normalize(CmbYeniRol.SelectedItem?.ToString()),
            Aktif = ChkAktif.IsChecked == true,
            ModulYetkileri = yetkiler,
            Moduller = yetkiler.Where(y => y.Okuma).Select(y => y.Modul).ToList()
        };
    }

    private async void KullaniciEkle_Click(object sender, RoutedEventArgs e)
    {
        if (SaaSYazmaEngelliMi())
            return;

        if (OturumYoneticisi.Auth is null || OturumYoneticisi.Firestore is null)
            return;

        var eposta = TxtYeniEposta.Text.Trim();
        var sifre = TxtYeniSifre.Password;

        if (string.IsNullOrWhiteSpace(eposta) || string.IsNullOrWhiteSpace(sifre))
        {
            MessageBox.Show("E-posta ve şifre zorunludur.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var uid = await OturumYoneticisi.Auth.KullaniciOlusturAsync(eposta, sifre);
            var profil = FormdanProfilOlustur(uid);
            await OturumYoneticisi.Firestore.KullaniciKaydetAsync(profil);

            YeniKullaniciModunaGec();
            await ListeyiYenileAsync();
            MessageBox.Show("Kullanıcı oluşturuldu.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void Guncelle_Click(object sender, RoutedEventArgs e)
    {
        try { await GuncelleAsync(); }
        catch (Exception ex)
        {
            HataGunlugu.Kaydet(ex, "KullaniciYonetim.Guncelle");
            MessageBox.Show(ex.Message, UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task GuncelleAsync()
    {
        if (SaaSYazmaEngelliMi())
            return;

        if (_seciliKullanici is null || OturumYoneticisi.Firestore is null)
        {
            MessageBox.Show("Düzenlemek için listeden bir kullanıcı seçin.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (string.IsNullOrWhiteSpace(TxtYeniAd.Text))
        {
            MessageBox.Show("Ad Soyad alanı zorunludur.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!KullaniciYetkileri.AdminMi)
        {
            MessageBox.Show(
                "Kullanıcı düzenlemek için Admin olarak giriş yapmalısınız.\n\n" +
                "Firestore'daki kullanıcı kaydınızda rol alanı tam olarak \"Admin\" olmalıdır.",
                UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var uid = _seciliKullanici.Uid;
        BtnGuncelle.IsEnabled = false;

        try
        {
            var profil = FormdanProfilOlustur(uid);
            profil.Eposta = _seciliKullanici.Eposta;
            await OturumYoneticisi.Firestore.KullaniciKaydetAsync(profil);

            if (OturumYoneticisi.AktifKullanici?.Uid == profil.Uid)
                await OturumYoneticisi.ProfiliYukleAsync();

            await ListeyiYenileAsync();

            if (KullaniciGrid.ItemsSource is IEnumerable<KullaniciProfili> liste)
            {
                var bulunan = liste.FirstOrDefault(k => k.Uid == uid);
                if (bulunan is not null)
                {
                    _formDolduruluyor = true;
                    KullaniciGrid.SelectedItem = bulunan;
                    _seciliKullanici = bulunan;
                    _formDolduruluyor = false;
                    FormuSeciliKullaniciyaGoreDoldur();
                }
            }

            MessageBox.Show("Kullanıcı güncellendi.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            var mesaj = ex.Message.Contains("PERMISSION_DENIED", StringComparison.OrdinalIgnoreCase)
                ? "Yetki hatası: Firestore'da kullanıcı düzenleme yalnızca Admin rolüne açıktır.\n" +
                  "Firebase Console → Firestore → users → kendi kaydınızda rol = Admin olmalı."
                : ex.Message;
            MessageBox.Show(mesaj, UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            BtnGuncelle.IsEnabled = !_saasModu && _seciliKullanici is not null;
        }
    }

    private async void Sil_Click(object sender, RoutedEventArgs e)
    {
        if (SaaSYazmaEngelliMi())
            return;

        if (_seciliKullanici is null || OturumYoneticisi.Firestore is null)
            return;

        if (_seciliKullanici.Uid == OturumYoneticisi.Auth?.Uid)
        {
            MessageBox.Show("Kendi hesabınızı pasifleştiremezsiniz.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (MessageBox.Show(
                $"{_seciliKullanici.AdSoyad} kullanıcısı pasifleştirilecek. Devam edilsin mi?",
                UygulamaBilgisi.Ad, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        try
        {
            var profil = FormdanProfilOlustur(_seciliKullanici.Uid);
            profil.Aktif = false;
            await OturumYoneticisi.Firestore.KullaniciKaydetAsync(profil);
            await ListeyiYenileAsync();
            MessageBox.Show("Kullanıcı pasifleştirildi.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void SifreSifir_Click(object sender, RoutedEventArgs e)
    {
        if (SaaSYazmaEngelliMi())
            return;

        if (_seciliKullanici is null || OturumYoneticisi.Auth is null)
            return;

        try
        {
            await EpostaSablonDeposu.YukleAsync();
            await OturumYoneticisi.Auth.AdminSifreSifirMailiGonderAsync(_seciliKullanici.Eposta);

            var onizleme = EpostaSablonDeposu.SablonuDoldur(EpostaSablonDeposu.Ayarlar.SifreSifirGovde, _seciliKullanici);
            MessageBox.Show(
                $"Şifre sıfırlama bağlantısı gönderildi:\n{_seciliKullanici.Eposta}\n\nKayıtlı şablon metni:\n{onizleme}",
                UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void EpostaTab_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await EpostaSablonDeposu.YukleAsync();
            TxtEpostaKonu.Text = EpostaSablonDeposu.Ayarlar.SifreSifirKonu;
            TxtEpostaGovde.Text = EpostaSablonDeposu.Ayarlar.SifreSifirGovde;
        }
        catch (Exception ex)
        {
            HataGunlugu.Kaydet(ex, "KullaniciYonetim.EpostaTab");
            MessageBox.Show(ex.Message, UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void EpostaSablonKaydet_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            EpostaSablonDeposu.Ayarlar.SifreSifirKonu = TxtEpostaKonu.Text.Trim();
            EpostaSablonDeposu.Ayarlar.SifreSifirGovde = TxtEpostaGovde.Text;
            await EpostaSablonDeposu.KaydetAsync();
            MessageBox.Show("E-posta şablonu buluta kaydedildi.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}

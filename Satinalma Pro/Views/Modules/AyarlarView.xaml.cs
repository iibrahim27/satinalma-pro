using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;
using SatinalmaPro.Views;
using Microsoft.Win32;

namespace SatinalmaPro.Views.Modules;

public partial class AyarlarView : UserControl
{
    private readonly ObservableCollection<VeriKaydiDurumu> _veriDurumlari = [];
    private readonly ObservableCollection<string> _malzemeKategorileri = [];
    private readonly ObservableCollection<string> _malzemeBirimleri = [];
    private bool _sartnameYukleniyor;
    private bool _teklifSartnameYukleniyor;
    private bool _genelYukleniyor;
    private bool _filoZimmetYukleniyor;
    private bool _dovizYukleniyor;

    public AyarlarView()
    {
        InitializeComponent();
        VeriDurumGrid.ItemsSource = _veriDurumlari;
        KategoriListesi.ItemsSource = _malzemeKategorileri;
        BirimListesi.ItemsSource = _malzemeBirimleri;
        TxtVeriKlasoru.Text = SatinalmaProKlasor.Yol;
        BulutPaneliniGuncelle();
        AyarlariYukle();
        VeriDurumlariniYenile();
        Loaded += (_, _) =>
        {
            KullaniciYetkileri.SekmeleriUygula(AyarTab, "Ayarlar");
            KullaniciYetkileri.ModulErisiminiUygula(this, "Ayarlar");
        };
    }

    private void AyarlariYukle()
    {
        _genelYukleniyor = true;
        TxtFirmaAdi.Text = UygulamaAyarDeposu.Ayarlar.FirmaAdi;
        TxtLogoYolu.Text = SatinalmaProLogoDeposu.GorunenAd(UygulamaAyarDeposu.Ayarlar.LogoDosyaYolu);
        TxtAnasayfaLogoYolu.Text = SatinalmaProLogoDeposu.GorunenAd(UygulamaAyarDeposu.Ayarlar.AnasayfaLogoDosyaYolu);
        _genelYukleniyor = false;
        LogoOnizlemeGuncelle();
        AnasayfaLogoOnizlemeGuncelle();

        var satinalma = SatinalmaDepo.Ayarlar;
        _sartnameYukleniyor = true;
        TxtSartnameMetni.Text = satinalma.SartnameMetni;
        _sartnameYukleniyor = false;
        _teklifSartnameYukleniyor = true;
        TxtTeklifIstemeSartnameleri.Text = satinalma.TeklifIstemeSartnameleri;
        _teklifSartnameYukleniyor = false;
        _dovizYukleniyor = true;
        TxtVarsayilanUsdKuru.Text = satinalma.VarsayilanUsdKuru > 0
            ? satinalma.VarsayilanUsdKuru.ToString(CultureInfo.CurrentCulture)
            : "";
        TxtVarsayilanEurKuru.Text = satinalma.VarsayilanEurKuru > 0
            ? satinalma.VarsayilanEurKuru.ToString(CultureInfo.CurrentCulture)
            : "";
        _dovizYukleniyor = false;
        ImzaGridleriYenile();
        KategoriListesiniYenile();
        BirimListesiniYenile();
        FiloZimmetMaddeleriniYukle();
    }

    private void FiloZimmetMaddeleriniYukle()
    {
        _filoZimmetYukleniyor = true;
        var liste = UygulamaAyarDeposu.Ayarlar.FiloZimmetFormMaddeleri;
        TxtFiloZimmetMaddeleri.Text = liste.Count == 0
            ? ""
            : string.Join(Environment.NewLine, liste);
        _filoZimmetYukleniyor = false;
    }

    private void FiloZimmetMaddeleriDegisti(object sender, TextChangedEventArgs e)
    {
        if (_filoZimmetYukleniyor) return;
        FiloZimmetMaddeleriniKaydet(sessiz: true);
    }

    private void FiloZimmetKaydet_Click(object sender, RoutedEventArgs e) =>
        FiloZimmetMaddeleriniKaydet(sessiz: false);

    private void FiloZimmetMaddeleriniKaydet(bool sessiz)
    {
        UygulamaAyarDeposu.Ayarlar.FiloZimmetFormMaddeleri =
            ZimmetMaddeYardimcisi.Ayikla(TxtFiloZimmetMaddeleri.Text);
        UygulamaAyarDeposu.Kaydet();

        if (!sessiz)
        {
            MessageBox.Show("Zimmet formu maddeleri kaydedildi.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void AyarlarKaydet_Click(object sender, RoutedEventArgs e)
    {
        UygulamaAyarDeposu.Ayarlar.FirmaAdi = TxtFirmaAdi.Text.Trim();
        FiloZimmetMaddeleriniKaydet(sessiz: true);
        SatinalmaDepo.Ayarlar.SartnameMetni = TxtSartnameMetni.Text;
        SatinalmaDepo.Ayarlar.TeklifIstemeSartnameleri = TxtTeklifIstemeSartnameleri.Text;
        DovizKurlariniKaydet(sessiz: true);
        UygulamaAyarDeposu.Kaydet();
        SatinalmaDepo.Kaydet();

        MessageBox.Show("Tüm ayarlar kaydedildi.", UygulamaBilgisi.Ad,
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void KategoriListesiniYenile()
    {
        _malzemeKategorileri.Clear();
        foreach (var kategori in MalzemeKategoriDeposu.Liste)
            _malzemeKategorileri.Add(kategori);
    }

    private void KategoriEkle_Click(object sender, RoutedEventArgs e)
    {
        var ad = TxtYeniKategori.Text.Trim();
        if (string.IsNullOrWhiteSpace(ad))
        {
            MessageBox.Show("Kategori adı girin.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!MalzemeKategoriDeposu.Ekle(ad))
        {
            MessageBox.Show("Bu kategori zaten listede veya geçersiz.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        TxtYeniKategori.Clear();
        KategoriListesiniYenile();
    }

    private void KategoriSil_Click(object sender, RoutedEventArgs e)
    {
        if (KategoriListesi.SelectedItem is not string secili)
        {
            MessageBox.Show("Silmek için listeden bir kategori seçin.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (MalzemeKategoriDeposu.Liste.Count <= 1)
        {
            MessageBox.Show("En az bir kategori bulunmalıdır.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!MalzemeKategoriDeposu.Sil(secili))
        {
            MessageBox.Show("Kategori silinemedi.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        KategoriListesiniYenile();
    }

    private void BirimListesiniYenile()
    {
        _malzemeBirimleri.Clear();
        foreach (var birim in MalzemeBirimDeposu.Liste)
            _malzemeBirimleri.Add(birim);
    }

    private void BirimEkle_Click(object sender, RoutedEventArgs e)
    {
        var ad = TxtYeniBirim.Text.Trim();
        if (string.IsNullOrWhiteSpace(ad))
        {
            MessageBox.Show("Birim terimi girin.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!MalzemeBirimDeposu.Ekle(ad))
        {
            MessageBox.Show("Bu birim zaten listede veya geçersiz.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        TxtYeniBirim.Clear();
        BirimListesiniYenile();
    }

    private void BirimSil_Click(object sender, RoutedEventArgs e)
    {
        if (BirimListesi.SelectedItem is not string secili)
        {
            MessageBox.Show("Silmek için listeden bir birim seçin.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (MalzemeBirimDeposu.Liste.Count <= 1)
        {
            MessageBox.Show("En az bir birim terimi bulunmalıdır.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!MalzemeBirimDeposu.Sil(secili))
        {
            MessageBox.Show("Birim silinemedi.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        BirimListesiniYenile();
    }

    private void VeriDurumlariniYenile()
    {
        _veriDurumlari.Clear();
        foreach (var durum in SatinalmaProVeriKatalogu.DurumlariOlustur())
            _veriDurumlari.Add(durum);
    }

    #region Genel

    private void GenelAyarDegisti(object sender, TextChangedEventArgs e)
    {
        if (_genelYukleniyor) return;
        UygulamaAyarDeposu.Ayarlar.FirmaAdi = TxtFirmaAdi.Text.Trim();
        UygulamaAyarDeposu.Kaydet();
    }

    private void LogoSec_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Firma Logosu Seç",
            Filter = "Görsel Dosyaları|*.png;*.jpg;*.jpeg;*.bmp;*.webp"
        };

        if (dialog.ShowDialog() != true) return;

        UygulamaAyarDeposu.Ayarlar.LogoDosyaYolu = SatinalmaProLogoDeposu.Kaydet(dialog.FileName, "firma");
        TxtLogoYolu.Text = SatinalmaProLogoDeposu.GorunenAd(UygulamaAyarDeposu.Ayarlar.LogoDosyaYolu);
        LogoOnizlemeGuncelle();
        UygulamaAyarDeposu.Kaydet();
    }

    private void AnasayfaLogoSec_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Anasayfa Logosu Seç",
            Filter = "Görsel Dosyaları|*.png;*.jpg;*.jpeg;*.bmp;*.webp"
        };

        if (dialog.ShowDialog() != true) return;

        UygulamaAyarDeposu.Ayarlar.AnasayfaLogoDosyaYolu = SatinalmaProLogoDeposu.Kaydet(dialog.FileName, "anasayfa");
        TxtAnasayfaLogoYolu.Text = SatinalmaProLogoDeposu.GorunenAd(UygulamaAyarDeposu.Ayarlar.AnasayfaLogoDosyaYolu);
        AnasayfaLogoOnizlemeGuncelle();
        UygulamaAyarDeposu.Kaydet();
    }

    private void LogoOnizlemeGuncelle() =>
        LogoGorselYardimcisi.GorselAyarla(ImgLogoOnizleme, UygulamaAyarDeposu.Ayarlar.LogoDosyaYolu);

    private void AnasayfaLogoOnizlemeGuncelle() =>
        LogoGorselYardimcisi.GorselAyarla(ImgAnasayfaLogoOnizleme, UygulamaAyarDeposu.Ayarlar.AnasayfaLogoDosyaYolu);

    #endregion

    #region Satınalma

    private void ImzaGridleriYenile()
    {
        SatinalmaDepo.ImzalariHazirla(SatinalmaDepo.Ayarlar);
        SefImzaGrid.ItemsSource = null;
        YonetimImzaGrid.ItemsSource = null;
        SefImzaGrid.ItemsSource = SatinalmaDepo.Ayarlar.SefImzalari;
        YonetimImzaGrid.ItemsSource = SatinalmaDepo.Ayarlar.YonetimImzalari;
    }

    private void SartnameMetniDegisti(object sender, TextChangedEventArgs e)
    {
        if (_sartnameYukleniyor) return;
        SatinalmaDepo.Ayarlar.SartnameMetni = TxtSartnameMetni.Text;
        SatinalmaDepo.Kaydet();
    }

    private void TeklifIstemeSartnameleriDegisti(object sender, TextChangedEventArgs e)
    {
        if (_teklifSartnameYukleniyor) return;
        SatinalmaDepo.Ayarlar.TeklifIstemeSartnameleri = TxtTeklifIstemeSartnameleri.Text;
        SatinalmaDepo.Kaydet();
    }

    private void DovizKuruDegisti(object sender, TextChangedEventArgs e)
    {
        if (_dovizYukleniyor) return;
        DovizKurlariniKaydet(sessiz: true);
    }

    private void DovizKurlariniKaydet(bool sessiz)
    {
        SatinalmaDepo.Ayarlar.VarsayilanUsdKuru = OndalikOku(TxtVarsayilanUsdKuru.Text);
        SatinalmaDepo.Ayarlar.VarsayilanEurKuru = OndalikOku(TxtVarsayilanEurKuru.Text);
        SatinalmaDepo.Kaydet();
    }

    private static decimal OndalikOku(string? metin)
    {
        if (string.IsNullOrWhiteSpace(metin))
            return 0;

        var temiz = metin.Trim();
        if (decimal.TryParse(temiz, NumberStyles.Any, CultureInfo.CurrentCulture, out var sonuc))
            return sonuc;

        return decimal.TryParse(temiz.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out sonuc)
            ? sonuc
            : 0;
    }

    private void SefImzaEkle_Click(object sender, RoutedEventArgs e)
    {
        SatinalmaDepo.Ayarlar.SefImzalari.Add(new ImzaAyari { Unvan = "Yeni Şef", Aktif = true });
        ImzaGridleriYenile();
        SatinalmaDepo.Kaydet();
    }

    private void SefImzaSil_Click(object sender, RoutedEventArgs e)
    {
        if (SefImzaGrid.SelectedItem is not ImzaAyari imza) return;
        if (SatinalmaDepo.Ayarlar.SefImzalari.Count <= 1)
        {
            MessageBox.Show("En az bir şef imza alanı bulunmalıdır.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        SatinalmaDepo.Ayarlar.SefImzalari.Remove(imza);
        ImzaGridleriYenile();
        SatinalmaDepo.Kaydet();
    }

    private void YonetimImzaEkle_Click(object sender, RoutedEventArgs e)
    {
        SatinalmaDepo.Ayarlar.YonetimImzalari.Add(new ImzaAyari
        {
            Unvan = "Yönetim / Proje Müdürü",
            Aktif = true
        });
        ImzaGridleriYenile();
        SatinalmaDepo.Kaydet();
    }

    private void YonetimImzaSil_Click(object sender, RoutedEventArgs e)
    {
        if (YonetimImzaGrid.SelectedItem is not ImzaAyari imza) return;
        if (SatinalmaDepo.Ayarlar.YonetimImzalari.Count <= 1)
        {
            MessageBox.Show("En az bir yönetim imza alanı bulunmalıdır.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        SatinalmaDepo.Ayarlar.YonetimImzalari.Remove(imza);
        ImzaGridleriYenile();
        SatinalmaDepo.Kaydet();
    }

    private void ImzaGrid_CellEditEnding(object? sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.EditAction == DataGridEditAction.Cancel)
            return;

        if (sender is DataGrid grid)
            grid.CommitEdit(DataGridEditingUnit.Row, true);

        if (e.EditingElement is TextBox textBox)
        {
            var binding = textBox.GetBindingExpression(TextBox.TextProperty);
            binding?.UpdateSource();
        }

        SatinalmaDepo.KaydetAyarlar();
        _ = BulutVeriSenkronu.AyarlariHemenGonderAsync();
    }

    #endregion

    #region Veri yönetimi

    private void VeriDurumYenile_Click(object sender, RoutedEventArgs e) => VeriDurumlariniYenile();

    private void VeriKlasoruAc_Click(object sender, RoutedEventArgs e)
    {
        SatinalmaProKlasor.Olustur();
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = SatinalmaProKlasor.Yol,
            UseShellExecute = true
        });
    }

    private async void ModulSifirla_Click(object sender, RoutedEventArgs e)
    {
        try
        {
        if (sender is not Button { Tag: string dosyaAdi }) return;
        if (VeriDurumGrid.SelectedItem is not VeriKaydiDurumu secili &&
            string.IsNullOrEmpty(dosyaAdi))
            return;

        dosyaAdi = string.IsNullOrEmpty(dosyaAdi)
            ? (VeriDurumGrid.SelectedItem as VeriKaydiDurumu)?.DosyaAdi ?? ""
            : dosyaAdi;

        if (string.IsNullOrEmpty(dosyaAdi)) return;

        var tanim = SatinalmaProVeriKatalogu.TumKayitlar.FirstOrDefault(t => t.DosyaAdi == dosyaAdi);
        var ad = tanim?.ModulAdi ?? dosyaAdi;

        var sonuc = MessageBox.Show(
            $"{ad} verileri sıfırlanacak. Bu işlem geri alınamaz.\nDevam etmek istiyor musunuz?",
            "Modül Sıfırla",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (sonuc != MessageBoxResult.Yes) return;

        SatinalmaProYedeklemeServisi.ModulSifirla(dosyaAdi);
        AyarlariYukle();
        VeriDurumlariniYenile();

        if (OturumYoneticisi.GirisYapildi && OturumYoneticisi.Firestore is not null)
        {
            var anahtar = BulutVeriSenkronu.DosyaAdindanAnahtar(dosyaAdi);
            if (anahtar is not null)
            {
                try
                {
                    await BulutVeriSenkronu.AnahtarBulutaGonderAsync(anahtar);
                    MessageBox.Show(
                        $"{ad} sıfırlandı ve buluta kaydedildi.\nDiğer bilgisayarlar giriş yaptığında veya en geç ~25 sn içinde güncellenecek.",
                        UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"{ad} yerelde sıfırlandı ancak buluta yazılamadı:\n{ex.Message}",
                        UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
        }

        MessageBox.Show($"{ad} sıfırlandı.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            HataGunlugu.Kaydet(ex, "Ayarlar.ModulSifirla");
            MessageBox.Show(ex.Message, UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void TumVerileriSifirla_Click(object sender, RoutedEventArgs e)
    {
        try
        {
        var sonuc = MessageBox.Show(
            "TÜM modül verileri ve ayarlar sıfırlanacak.\nÖnce yedek almanız önerilir.\n\nDevam etmek istiyor musunuz?",
            "Tüm Verileri Sıfırla",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (sonuc != MessageBoxResult.Yes) return;

        var onay = MessageBox.Show(
            "Son onay: Tüm Satınalma Pro verileri kalıcı olarak silinecek ve varsayılanlara dönecek.",
            "Emin misiniz?",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (onay != MessageBoxResult.Yes) return;

        SatinalmaProYedeklemeServisi.TumVerileriSifirla();
        AyarlariYukle();
        VeriDurumlariniYenile();

        if (OturumYoneticisi.GirisYapildi && OturumYoneticisi.Firestore is not null)
        {
            try
            {
                await BulutVeriSenkronu.TumVerileriBulutaGonderAsync();
                MessageBox.Show(
                    "Tüm veriler sıfırlandı ve buluta kaydedildi.\nDiğer bilgisayarlar giriş yaptığında güncellenecek.",
                    UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Veriler yerelde sıfırlandı ancak buluta yazılamadı:\n{ex.Message}",
                    UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        MessageBox.Show("Tüm veriler sıfırlandı.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            HataGunlugu.Kaydet(ex, "Ayarlar.TumVerileriSifirla");
            MessageBox.Show(ex.Message, UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void YedekAl_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Yedek Kaydet",
            Filter = "ZIP Dosyası (*.zip)|*.zip",
            FileName = $"SatinalmaPro_Yedek_{DateTime.Now:yyyyMMdd_HHmm}.zip"
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            SatinalmaProYedeklemeServisi.Yedekle(dialog.FileName);
            MessageBox.Show($"Yedek oluşturuldu:\n{dialog.FileName}", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Yedek alınamadı:\n{ex.Message}", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void GeriYukle_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Yedek Seç",
            Filter = "ZIP Dosyası (*.zip)|*.zip"
        };

        if (dialog.ShowDialog() != true) return;

        var sonuc = MessageBox.Show(
            "Mevcut veriler yedekteki dosyalarla değiştirilecek.\nDevam etmek istiyor musunuz?",
            "Geri Yükle",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (sonuc != MessageBoxResult.Yes) return;

        try
        {
            SatinalmaProYedeklemeServisi.GeriYukle(dialog.FileName);
            AyarlariYukle();
            VeriDurumlariniYenile();
            MessageBox.Show("Yedek başarıyla geri yüklendi.\nDeğişikliklerin tam yansıması için modülleri yeniden açın.",
                UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Geri yükleme başarısız:\n{ex.Message}", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion

    private void BulutPaneliniGuncelle()
    {
        TxtFirebaseYolu.Text = FirebaseAyarDeposu.UygulamaDosyaYolu;
        TxtGoogleServicesDurum.Text = FirebaseAyarDeposu.GoogleServicesMevcut
            ? "Android google-services.json: yüklü ✓ (APK derlemesi / yedek)"
            : "Android google-services.json: isteğe bağlı (v1.9.4 APK içinde zaten var)";
        TxtFcmServiceAccountDurum.Text = FirebaseAyarDeposu.FcmServiceAccountMevcut
            ? "FCM Service Account: yüklü ✓ (push gönderimi için gerekli)"
            : "FCM Service Account: henüz yüklenmedi — push çalışmaz";

        if (!FirebaseAyarDeposu.Ayarlar.Yapilandirildi)
        {
            TxtFirebaseUyari.Visibility = Visibility.Visible;
            TxtFirebaseUyari.Text =
                "⚠ Firebase yapılandırılmamış — uygulama yerel modda çalışır. Mobil senkron ve push bildirimleri devre dışı kalır.";
        }
        else
            TxtFirebaseUyari.Visibility = Visibility.Collapsed;

        var manifest = FirebaseAyarDeposu.Ayarlar.GuncellemeManifestUrl;
        TxtGuncellemeDurum.Text = string.IsNullOrWhiteSpace(manifest)
            ? $"Otomatik güncelleme: yapılandırılmamış · Mevcut sürüm: {UygulamaBilgisi.Versiyon}"
            : $"Otomatik güncelleme: aktif · Sürüm: {UygulamaBilgisi.Versiyon} · Manifest: {manifest}";

        BtnKullaniciYonetimi.Visibility = KullaniciYetkileri.AdminMi && OturumYoneticisi.BulutAktif
            ? Visibility.Visible
            : Visibility.Collapsed;
        BtnBulutaYukle.Visibility = KullaniciYetkileri.AdminMi && OturumYoneticisi.BulutAktif
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void FcmServiceAccountYukle_Click(object sender, RoutedEventArgs e)
    {
        if (!KullaniciYetkileri.AdminMi)
            return;

        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Firebase Service Account JSON seçin",
            Filter = "JSON (*.json)|*.json"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            FirebaseAyarDeposu.FcmServiceAccountKaydet(dialog.FileName);
            FirebaseAyarDeposu.Kaydet();
            MessageBox.Show(
                "Service Account JSON kaydedildi.\nPush bildirimleri artık çalışabilir.",
                UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
            BulutPaneliniGuncelle();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void GoogleServicesYukle_Click(object sender, RoutedEventArgs e)
    {
        if (!KullaniciYetkileri.AdminMi)
            return;

        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "google-services.json seçin",
            Filter = "Firebase Android Config (google-services.json)|google-services.json|JSON (*.json)|*.json"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            FirebaseAyarDeposu.GoogleServicesJsonKaydet(dialog.FileName);
            MessageBox.Show(
                "google-services.json kaydedildi.\nAndroid uygulaması bir sonraki derlemede kullanacak.",
                UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
            BulutPaneliniGuncelle();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BulutaYukle_Click(object sender, RoutedEventArgs e)
    {
        BtnBulutaYukle.IsEnabled = false;
        try
        {
            await BulutVeriSenkronu.TumVerileriBulutaGonderAsync();
            MessageBox.Show(
                "Tüm veriler ve logolar Firebase bulutuna yüklendi.\nDiğer bilgisayarlar giriş yaptığında aynı verileri görecek.",
                UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            HataGunlugu.Kaydet(ex, "Ayarlar.BulutaYukle");
            MessageBox.Show(ex.Message, UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            BtnBulutaYukle.IsEnabled = true;
        }
    }

    private void KullaniciYonetimi_Click(object sender, RoutedEventArgs e)
    {
        var pencere = new KullaniciYonetimWindow { Owner = Window.GetWindow(this) };
        pencere.ShowDialog();
    }

    private void FirebaseKurulum_Click(object sender, RoutedEventArgs e) =>
        KurulumDosyasiAc("FIREBASE_KURULUM.txt");

    private void FirebaseAndroidKurulum_Click(object sender, RoutedEventArgs e) =>
        KurulumDosyasiAc("FIREBASE_ANDROID_CONSOLE.txt");

    private void GithubKurulum_Click(object sender, RoutedEventArgs e) =>
        KurulumDosyasiAc("GITHUB_RELEASES_KURULUM.txt");

    private void GithubSurumGuncelleme_Click(object sender, RoutedEventArgs e) =>
        KurulumDosyasiAc("GITHUB_SURUM_GUNCELLEME.txt");

    private static void KurulumDosyasiAc(string dosyaAdi)
    {
        var yol = Path.Combine(AppContext.BaseDirectory, dosyaAdi);
        if (!File.Exists(yol))
        {
            MessageBox.Show("Kurulum kılavuzu bulunamadı.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = yol,
            UseShellExecute = true
        });
    }
}

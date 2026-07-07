using System.Windows;
using System.Windows.Controls;
using SatinalmaPro.Models;
using SatinalmaPro.Services;

namespace SatinalmaPro.Helpers;

public static class SatinalmaTeklifDegerlendirmeYardimcisi
{
    public static SatinalmaTalep? GuncelTalep(SatinalmaTalep? talep) =>
        talep is null ? null : SatinalmaDepo.Talepler.FirstOrDefault(t => t.Id == talep.Id) ?? talep;

    public static bool YonetimeTeklifGonderilebilir(SatinalmaTalep talep) =>
        SatinalmaTalepYardimcisi.GercekTeklifVar(talep)
        && !SatinalmaTalepYardimcisi.TeklifYonetimOnayiBekliyor(talep)
        && !talep.YonetimOnayKilitli
        && (SatinalmaTalepKuyrugu.SatinalmaKarsilastirma(talep)
            || talep.Durum is SatinalmaTalepDurumlari.TeklifGirisi
                or SatinalmaTalepDurumlari.Karsilastirma
                or SatinalmaTalepDurumlari.ImzaSurecinde
                or SatinalmaTalepDurumlari.Hazirlaniyor);

    public static string? YonetimeGonderEngelMesaji(SatinalmaTalep talep)
    {
        if (!SatinalmaTalepYardimcisi.GercekTeklifVar(talep))
            return "Yönetime göndermek için en az bir geçerli teklif girin (firma adı ve fiyat).";

        if (SatinalmaTalepYardimcisi.TeklifYonetimOnayiBekliyor(talep))
            return "Bu talep zaten yönetime gönderildi. «Teklif Girilen» sekmesinden takip edebilirsiniz.";

        if (talep.YonetimOnayKilitli)
            return "Bu talep yönetim tarafından onaylanmış; tekrar gönderilemez.";

        if (!YonetimeTeklifGonderilebilir(talep))
            return "Bu aşamada yönetime gönderilemez. Önce teklifleri kaydedin.";

        if (talep.SatinalmaKalemOnerisiElleSecildi && !SatinalmaOneriYardimcisi.TumKalemlerOnerili(talep))
            return "Kalem bazlı öneri başlattınız — her kalem için firma seçin veya seçimleri temizleyin.";

        return null;
    }

    public static SatinalmaTeklif YonetimeTeklifGonderiminiDogrula(SatinalmaTalep talep)
    {
        talep.Teklifler ??= [];
        talep.Kalemler ??= [];
        foreach (var teklif in talep.Teklifler)
            teklif.FiyatlariHesapla(talep.Kalemler);

        if (talep.Teklifler.Count == 0)
            throw new InvalidOperationException("Yönetime göndermek için en az bir teklif girilmelidir.");

        foreach (var teklif in talep.Teklifler)
        {
            if (teklif.GenelToplam <= 0)
                throw new InvalidOperationException($"'{teklif.FirmaAdi}' teklifinde geçerli fiyat bulunamadı.");
        }

        if (talep.SatinalmaKalemOnerisiElleSecildi)
        {
            if (!SatinalmaOneriYardimcisi.TumKalemlerOnerili(talep))
                throw new InvalidOperationException("Kalem bazlı öneri için her kalemde firma seçin veya kalem seçimlerini temizleyin.");

            foreach (var kalem in talep.Kalemler)
            {
                var fiyat = SatinalmaOneriYardimcisi.KalemOneriFiyati(talep, kalem);
                if (fiyat is null || fiyat.ToplamTutar <= 0)
                    throw new InvalidOperationException($"'{kalem.Malzeme}' kalemi için geçerli birim fiyat seçin.");
            }

            return talep.Teklifler.First();
        }

        // Kısmi kalem seçimi varsa gönderimi engelleme — yalnızca tam kalem önerisi modunda doğrula
        if (SatinalmaOneriYardimcisi.HerhangiKalemOnerili(talep))
        {
            foreach (var kalem in talep.Kalemler.Where(k => k.OnerilenTeklifId != null))
                kalem.OnerilenTeklifId = null;
            talep.SatinalmaKalemOnerisiElleSecildi = false;
        }

        return talep.OnerilenTeklif()
            ?? throw new InvalidOperationException("Geçerli bir satınalma önerisi oluşturulamadı. Teklif fiyatlarını kontrol edin.");
    }

    public static void YonetimeTeklifGonderiminiHazirla(SatinalmaTalep talep, SatinalmaTeklif oneri)
    {
        if (!talep.SatinalmaOnerisiElleSecildi && !talep.SatinalmaKalemOnerisiElleSecildi)
            talep.YonetimOnerilenTeklifId = oneri.Id;

        talep.Durum = SatinalmaTalepDurumlari.YonetimOnayinda;
        talep.TeklifDuzeltmeNotu = "";
        SatinalmaTalepYardimcisi.Dokun(talep);
    }

    public static async Task<bool> YonetimeGonderAsync(SatinalmaTalep? talep)
    {
        talep = GuncelTalep(talep);
        if (talep is null)
            return false;

        if (talep.Durum is SatinalmaTalepDurumlari.TeklifGirisi
                or SatinalmaTalepDurumlari.Hazirlaniyor
                or SatinalmaTalepDurumlari.ImzaSurecinde
            && SatinalmaTalepYardimcisi.GercekTeklifVar(talep))
            talep.Durum = SatinalmaTalepDurumlari.Karsilastirma;

        SatinalmaDepo.TeklifDegisikligiIsle(talep);
        SatinalmaTeklif oneri;
        try
        {
            oneri = YonetimeTeklifGonderiminiDogrula(talep);
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(ex.Message, UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        var onay = MessageBox.Show(
            $"Teklifler yönetim onayına gönderilsin mi?\n\n{SatinalmaOneriYardimcisi.OneriMetni(talep)}",
            UygulamaBilgisi.Ad, MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (onay != MessageBoxResult.Yes)
            return false;

        YonetimeTeklifGonderiminiHazirla(talep, oneri);
        await SatinalmaKayitYardimcisi.KaydetVeBulutaGonderAsync(talep);

        try
        {
            await BildirimYoneticisi.GecersizleriOkunduYapAsync();
            await SatinalmaBildirimleri.TeklifOnaydaAsync(talep);
        }
        catch
        {
            // bildirim hatası kaydı engellemez
        }

        return true;
    }

    public static string? RedGerekcesiIste(
        string baslik = "Talep Red",
        string etiket = "Red gerekçesini girin:",
        string onayMetni = "Reddet",
        bool zorunlu = true)
    {
        var dialog = new Window
        {
            Title = baslik,
            Width = 440,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = Application.Current?.MainWindow,
            ResizeMode = ResizeMode.NoResize
        };

        var kutu = new TextBox
        {
            Margin = new Thickness(16, 12, 16, 0),
            Height = 72,
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = true,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        var panel = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
        panel.Children.Add(new TextBlock
        {
            Text = etiket,
            Margin = new Thickness(16, 16, 16, 0),
            FontWeight = FontWeights.SemiBold
        });
        panel.Children.Add(kutu);

        var butonlar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(16, 8, 16, 16)
        };
        var iptal = new Button { Content = "İptal", Width = 88, Margin = new Thickness(0, 0, 8, 0) };
        var tamam = new Button { Content = onayMetni, Width = 100, IsDefault = true };
        iptal.Click += (_, _) => { dialog.DialogResult = false; dialog.Close(); };
        tamam.Click += (_, _) => { dialog.DialogResult = true; dialog.Close(); };
        butonlar.Children.Add(iptal);
        butonlar.Children.Add(tamam);
        panel.Children.Add(butonlar);

        dialog.Content = panel;
        kutu.Focus();

        if (dialog.ShowDialog() != true)
            return null;

        var metin = kutu.Text.Trim();
        if (zorunlu && string.IsNullOrWhiteSpace(metin))
        {
            MessageBox.Show("Gerekçe zorunludur.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return null;
        }

        return metin;
    }
}

using System.Windows;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;
using SatinalmaPro.Views.Modules;

namespace SatinalmaPro.Views.Modules.Satinalma.Part1;

public static class SatinalmaPart1Servisi
{
    public static SatinalmaTalep YeniTalepOlustur()
    {
        var talep = SatinalmaDepo.YeniTalepOlustur(talepNoVer: true);
        talep.TalepEden = KullaniciYetkileri.AktifKullaniciAdi() ?? "";
        talep.OlusturanUid = OturumYoneticisi.AktifKullanici?.Uid ?? "";
        talep.OlusturanRol = OturumYoneticisi.AktifKullanici?.Rol ?? "";
        talep.TalepTuru = TalepTurleri.Normal;
        return talep;
    }

    public static void TalepEkle(SatinalmaTalep talep)
    {
        if (SatinalmaDepo.Talepler.All(t => t.Id != talep.Id))
            SatinalmaDepo.Talepler.Insert(0, talep);
    }

    public static bool GecerliMi(SatinalmaTalep talep, out string hata)
    {
        hata = "";
        if (!SatinalmaTalepYardimcisi.MalzemeGirildi(talep))
        {
            hata = "En az bir malzeme satırı girin.";
            return false;
        }

        foreach (var kalem in talep.Kalemler)
        {
            if (string.IsNullOrWhiteSpace(kalem.Malzeme))
                continue;

            if (kalem.Miktar <= 0)
            {
                hata = $"'{kalem.Malzeme}' için geçerli miktar girin.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(kalem.Birim))
            {
                hata = $"'{kalem.Malzeme}' için birim seçin.";
                return false;
            }
        }

        return true;
    }

    public static void KalemleriTemizle(SatinalmaTalep talep)
    {
        talep.Kalemler = new System.Collections.ObjectModel.ObservableCollection<SatinalmaTalepKalemi>(
            talep.Kalemler.Where(k => !string.IsNullOrWhiteSpace(k.Malzeme)));
        SatinalmaTalepYardimcisi.TalepKalemleriniTekliflerleSenkronla(talep);
    }

    public static async Task KaydetAsync(SatinalmaTalep talep)
    {
        var kullanici = OturumYoneticisi.AktifKullanici;
        var yenidenGonder = kullanici is not null
            && SatinalmaTalepYetkileri.DuzenlemeSonrasiYenidenGonder(
                kullanici.Rol, talep, kullanici.Uid, KullaniciYetkileri.AktifKullaniciAdi())
            && !SatinalmaTalepYetkileri.TalepKilitli(talep);

        if (yenidenGonder)
            SatinalmaTalepYardimcisi.SahipDuzenlemeSonrasiHazirla(talep);

        SatinalmaTalepYardimcisi.KayitOncesiHazirla(talep);
        SatinalmaDepo.TalepNoAtaIfNeeded(talep);
        KalemleriTemizle(talep);
        SatinalmaTalepYardimcisi.Dokun(talep);
        TalepEkle(talep);
        await SatinalmaKayitYardimcisi.KaydetVeBulutaGonderAsync(talep);

        if (!yenidenGonder)
            return;

        try
        {
            BildirimDeposu.Sil(b => b.TalepId == talep.Id);
            await SatinalmaBildirimleri.YonetimeGonderildiAsync(talep);
            if (OturumYoneticisi.BulutAktif && OturumYoneticisi.GirisYapildi)
                await BildirimDeposu.KaydetAsync();
        }
        catch (Exception ex)
        {
            HataGunlugu.Kaydet(ex, "SatinalmaPart1.KaydetYenidenGonder");
        }
    }

    public static async Task OnayaGonderAsync(SatinalmaTalep talep)
    {
        SatinalmaTalepYardimcisi.KayitOncesiHazirla(talep);
        SatinalmaDepo.TalepNoAtaIfNeeded(talep);
        KalemleriTemizle(talep);
        talep.Durum = SatinalmaTalepDurumlari.ImzaSurecinde;
        talep.Status = SatinalmaPro.Shared.Procurement.ProcurementStatus.Submitted;
        SatinalmaTalepYardimcisi.Dokun(talep);
        TalepEkle(talep);
        await SatinalmaKayitYardimcisi.KaydetVeBulutaGonderAsync(talep);

        try
        {
            await SatinalmaBildirimleri.YonetimeGonderildiAsync(talep);
        }
        catch (Exception ex)
        {
            HataGunlugu.Kaydet(ex, "SatinalmaPart1.OnayaGonder");
        }
    }

    public static async Task SilAsync(SatinalmaTalep talep) =>
        await SatinalmaTalepSilmeYardimcisi.SilAsync(talep);

    public static void KalemEkle(SatinalmaTalep talep)
    {
        talep.Kalemler ??= [];
        var sira = talep.Kalemler.Count + 1;
        talep.Kalemler.Add(new SatinalmaTalepKalemi { SiraNo = sira, Birim = "Adet" });
    }

    public static void KalemSil(SatinalmaTalep talep, SatinalmaTalepKalemi? kalem)
    {
        if (kalem is null)
            return;

        talep.Kalemler ??= [];
        if (talep.Kalemler.Count <= 1)
            return;

        talep.Kalemler.Remove(kalem);
        var sira = 1;
        foreach (var k in talep.Kalemler.OrderBy(x => x.SiraNo))
            k.SiraNo = sira++;
    }

    public static bool Duzenlenebilir(SatinalmaTalep talep) =>
        KullaniciYetkileri.SatinalmaTalepDuzenleyebilir(talep);

    public static bool TeklifDuzenlePenceresiAc(
        Window owner,
        SatinalmaTeklif teklif,
        IEnumerable<SatinalmaTalepKalemi> kalemler)
    {
        var pencere = new SatinalmaPro.Views.Modules.SatinalmaTeklifDuzenleWindow(
            teklif, kalemler, SatinalmaDepo.Ayarlar)
        {
            Owner = owner
        };

        if (pencere.ShowDialog() != true)
            return false;

        teklif.FiyatlariHesapla(kalemler.ToList());
        return true;
    }

    public static async Task SiparisVerAsync(SatinalmaTalep talep)
    {
        var guncel = SatinalmaDepo.Talepler.FirstOrDefault(t => t.Id == talep.Id) ?? talep;

        if (guncel.TeklifsizFirmaFiyatBekliyor)
        {
            MessageBox.Show(
                "Teklifsiz onay için önce firma ve birim fiyatı girin.",
                UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var onay = MessageBox.Show(
            $"{guncel.TalepNo} için firmaya sipariş verilsin mi?",
            UygulamaBilgisi.Ad, MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (onay != MessageBoxResult.Yes)
            return;

        try
        {
            await SatinalmaSiparisIslemleri.SiparisVerAsync(guncel);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        MessageBox.Show(
            $"{guncel.TalepNo} sipariş verildi. «Sipariş Verilen Talep ve Teklifler» sekmesinden takip edebilirsiniz.",
            UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public static bool MalKabulGoster(Window? owner, OnaylananMalzemeSatiri satir)
    {
        if (!KullaniciYetkileri.MalKabulVeStokAktarYapabilir())
        {
            MessageBox.Show(
                "Mal kabul işlemi yalnızca Satınalma veya Depo rolü tarafından yapılabilir.",
                UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (satir.SiparisTamamlandi)
            return false;

        var varsayilanMiktar = satir.KalanMiktar > 0.0001 ? satir.KalanMiktar : 0;
        var pencere = new AlinanMalzemeAktarWindow(satir, varsayilanMiktar, miktarDuzenlenebilir: true)
        {
            Owner = owner
        };

        if (pencere.ShowDialog() != true)
            return false;

        try
        {
            SatinalmaSiparisIslemleri.MalKabulVeDepoyaKaydet(
                satir,
                pencere.GirilenMiktar,
                pencere.SecilenKategori,
                pencere.GirilenTarih,
                pencere.GirilenFisNo,
                pencere.GirilenTeslimAlan,
                pencere.GirilenDepo,
                pencere.GirilenAciklama,
                pencere.GirilenFirma,
                pencere.GirilenBirimFiyat,
                pencere.SahayaDirekt,
                pencere.GirilenSahaHedef);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        var mesaj = pencere.SahayaDirekt
            ? "Malzeme depoya giriş ve sahaya çıkış olarak kaydedildi. Alınan Malzemeler modülüne de işlendi."
            : "Malzeme Alınan Malzemeler modülüne ve depo stoğuna kaydedildi.";

        MessageBox.Show(mesaj, UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
        return true;
    }

    public static bool SevkiyatiTamamlaGoster(Window? owner, OnaylananMalzemeSatiri satir)
    {
        if (!KullaniciYetkileri.MalKabulVeStokAktarYapabilir())
            return false;

        if (satir.SiparisTamamlandi)
            return false;

        if (satir.KabulEdilenMiktar <= 0)
        {
            MessageBox.Show(
                "Sevkiyatı tamamlamak için önce en az bir mal kabul kaydı yapın.",
                UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        var onay = MessageBox.Show(
            $"{satir.Malzeme}\n\nSipariş: {satir.SiparisMiktari:N2} {satir.Birim}\nKabul edilen: {satir.KabulEdilenMiktar:N2} {satir.Birim}\n\nSevkiyat tamamlansın mı? Talep ve teklif miktarları kabul edilen miktara ({satir.KabulEdilenMiktar:N2}) göre güncellenecek.",
            UygulamaBilgisi.Ad,
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (onay != MessageBoxResult.Yes)
            return false;

        try
        {
            SatinalmaSiparisIslemleri.SevkiyatiTamamla(satir);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        MessageBox.Show(
            "Sevkiyat tamamlandı. Talep ve teklif miktarları güncellendi.",
            UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
        return true;
    }
}

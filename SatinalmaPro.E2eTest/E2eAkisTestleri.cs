using SatinalmaPro.Shared.Helpers;
using SatinalmaPro.Shared.Models;
using SatinalmaPro.Shared.Procurement;
using SatinalmaPro.Shared.Services;

namespace SatinalmaPro.E2eTest;

public sealed class E2eTestSonuc
{
    public List<string> Adimlar { get; } = [];
    public List<string> Basarilar { get; } = [];
    public List<string> Eksikler { get; } = [];
    public List<string> Uyarilar { get; } = [];

    public void Adim(string mesaj) => Adimlar.Add(mesaj);
    public void Ok(string mesaj) => Basarilar.Add(mesaj);
    public void Eksik(string mesaj) => Eksikler.Add(mesaj);
    public void Uyar(string mesaj) => Uyarilar.Add(mesaj);

    public void Bekle(bool kosul, string basari, string eksik)
    {
        if (kosul) Ok(basari);
        else Eksik(eksik);
    }
}

public static class E2eAkisTestleri
{
    public static E2eTestSonuc TamTeklifliAkis(BellekTestOrtami ortam)
    {
        var sonuc = new E2eTestSonuc();
        sonuc.Adim("=== SENARYO 1: Saha → Yönetim → Teklif → Onay → Sipariş → Mal Kabul ===");

        // 1. Saha talep aç + yönetime gönder
        var talep = ortam.SahaTalepOlustur(BellekTestOrtami.Saha);
        sonuc.Adim($"1. Saha talep oluşturdu: {talep.TalepNo} → {talep.Durum}");
        sonuc.Bekle(talep.Durum == SatinalmaTalepDurumlari.ImzaSurecinde,
            "Durum: İmza Sürecinde", "Saha talep durumu IMZA olmalı");

        sonuc.Bekle(ortam.KullaniciBildirimleri(BellekTestOrtami.Yonetim).Any(b => b.Tip == BildirimTipleri.YonetimeGonderildi && b.TalepId == talep.Id),
            "Yönetime YonetimeGonderildi bildirimi oluştu",
            "Yönetime bildirim gitmedi");

        sonuc.Bekle(ortam.KullaniciBildirimleri(BellekTestOrtami.Satinalma).Any(b => b.Tip == BildirimTipleri.YonetimeGonderildi),
            "Satınalma gelen talep bildirimini aldı (ortak karar yetkisi)",
            "Satınalma gelen talep bildirimi almadı");

        var yonetimRoute = BildirimRotaServisi.HedefRoute(
            ortam.Bildirimler.First(b => b.Tip == BildirimTipleri.YonetimeGonderildi && b.HedefRol == KullaniciRolleri.Yonetim),
            KullaniciRolleri.Yonetim);
        var androidYonetimRoute = AndroidAyna.AndroidBildirimRoute(BildirimTipleri.YonetimeGonderildi, talep.Id, KullaniciRolleri.Yonetim);
        sonuc.Bekle(yonetimRoute == "gelen-talepler" && androidYonetimRoute == "gelen-talepler",
            $"Bildirim rotası gelen-talepler (M:{yonetimRoute}, A:{androidYonetimRoute})",
            $"YonetimeGonderildi rota uyumsuz M={yonetimRoute} A={androidYonetimRoute}");

        sonuc.Bekle(MasaustuPart1Ayna.TalepListede(talep, "gelen-talepler", null, null, KullaniciRolleri.Yonetim),
            "Talep yönetim gelen listesinde",
            "Talep yönetim gelen listesinde DEĞİL");

        // 2. Yönetim teklif iste
        ortam.YonetimTeklifIste(talep);
        talep = ortam.GuncelTalep(talep.Id);
        sonuc.Adim($"2. Yönetim teklif istedi → {talep.Durum}");
        sonuc.Bekle(talep.Durum == SatinalmaTalepDurumlari.TeklifGirisi,
            "Durum: Teklif Girişi", "Teklif iste sonrası durum yanlış");

        var satinalmaTeklifRoute = AndroidAyna.AndroidBildirimRoute(BildirimTipleri.TeklifIstendi, talep.Id, KullaniciRolleri.Satinalma);
        var masaustuTeklifRoute = BildirimRotaServisi.HedefRoute(
            new BildirimKaydi { Tip = BildirimTipleri.TeklifIstendi, TalepId = talep.Id },
            KullaniciRolleri.Satinalma);
        sonuc.Bekle(satinalmaTeklifRoute == $"teklif-gir?id={talep.Id}",
            $"Android TeklifIstendi → teklif-gir?id= ({satinalmaTeklifRoute})",
            $"Android TeklifIstendi rota hatalı: {satinalmaTeklifRoute}");
        sonuc.Bekle(masaustuTeklifRoute == satinalmaTeklifRoute,
            $"TeklifIstendi rotası hizalı (M/A): {masaustuTeklifRoute}",
            $"TeklifIstendi rota uyumsuz M={masaustuTeklifRoute} A={satinalmaTeklifRoute}");

        sonuc.Bekle(AndroidAyna.AndroidCanAccess(satinalmaTeklifRoute, KullaniciRolleri.Satinalma),
            "Satınalma teklif-gir?id rotasına erişebiliyor",
            "KRİTİK: Satınalma teklif-gir ekranına erişemiyor (canAccess)");

        sonuc.Bekle(MasaustuPart1Ayna.TalepListede(talep, "satinalma-teklif-istenen", null, null, KullaniciRolleri.Satinalma),
            "Talep satınalma teklif istenen listesinde",
            "Talep satinalma-teklif-istenen listesinde DEĞİL");
        sonuc.Bekle(ortam.KullaniciBildirimleri(BellekTestOrtami.Satinalma)
                .Any(b => b.Tip == BildirimTipleri.TeklifIstendi && b.TalepId == talep.Id)
            && !ortam.KullaniciBildirimleri(BellekTestOrtami.Saha)
                .Any(b => b.Tip == BildirimTipleri.TeklifIstendi && b.TalepId == talep.Id),
            "Teklif istemi yalnızca satınalmaya gitti",
            "KRİTİK: Teklif istemi talep sahibine veya yanlış role gitti");

        // 3. Satınalma 2 teklif gir
        var teklifA = ortam.TeklifEkle(talep, "E2E Firma A", 100);
        var teklifB = ortam.TeklifEkle(talep, "E2E Firma B", 95);
        talep = ortam.GuncelTalep(talep.Id);
        sonuc.Adim($"3. Satınalma 2 teklif girdi → {talep.Durum}, öneri: {talep.OnerilenTeklif()?.FirmaAdi}");
        sonuc.Bekle(talep.Durum == SatinalmaTalepDurumlari.Karsilastirma && talep.Teklifler.Count == 2,
            "Karşılaştırma aşamasında 2 teklif", "Teklif girişi/karşılaştırma hatalı");
        sonuc.Bekle(talep.OnerilenTeklif()?.FirmaAdi == "E2E Firma B",
            "Satınalma önerisi en düşük fiyat (Firma B)", "Otomatik öneri yanlış firma");

        sonuc.Bekle(MasaustuPart1Ayna.TalepListede(talep, "satinalma-karsilastirma", null, null, KullaniciRolleri.Satinalma),
            "Talep karşılaştırma listesinde",
            "Talep karşılaştırma listesinde DEĞİL");

        // 4. Yönetime gönder
        ortam.YonetimeTeklifGonder(talep);
        talep = ortam.GuncelTalep(talep.Id);
        sonuc.Adim($"4. Yönetime teklif gönderildi → {talep.Durum}");
        sonuc.Bekle(talep.Durum == SatinalmaTalepDurumlari.YonetimOnayinda,
            "Durum: Yönetim Onayında", "Yönetime gönder sonrası durum yanlış");

        var teklifOnayRoute = AndroidAyna.AndroidBildirimRoute(BildirimTipleri.TeklifOnayda, talep.Id, KullaniciRolleri.Yonetim);
        sonuc.Bekle(teklifOnayRoute == $"teklif-onay-detay?id={talep.Id}",
            $"TeklifOnayda yönetim rotası: {teklifOnayRoute}",
            "TeklifOnayda bildirim rotası hatalı");
        sonuc.Bekle(AndroidAyna.AndroidCanAccess(teklifOnayRoute, KullaniciRolleri.Yonetim),
            "Yönetim teklif-onay-detay erişimi OK",
            "Yönetim teklif-onay-detay erişemiyor");
        sonuc.Bekle(ortam.KullaniciBildirimleri(BellekTestOrtami.Yonetim)
                .Any(b => b.Tip == BildirimTipleri.TeklifOnayda && b.TalepId == talep.Id)
            && !ortam.KullaniciBildirimleri(BellekTestOrtami.Saha)
                .Any(b => b.Tip == BildirimTipleri.TeklifOnayda && b.TalepId == talep.Id)
            && !ortam.KullaniciBildirimleri(BellekTestOrtami.Satinalma)
                .Any(b => b.Tip == BildirimTipleri.TeklifOnayda && b.TalepId == talep.Id),
            "Teklif girişi yöneticiye gitti; işlemi yapan satınalmaya gitmedi",
            "KRİTİK: Teklif girişi bildirimi talep sahibine veya işlemi yapan kullanıcıya gitti");

        var satTeklifGirilenRoute = $"teklif-onay-detay?id={talep.Id}";
        sonuc.Bekle(AndroidAyna.AndroidCanAccess(satTeklifGirilenRoute, KullaniciRolleri.Satinalma),
            "Satınalma teklif-onay-detay (gönderilen) erişimi OK",
            "KRİTİK: Satınalma yönetime gönderilen teklif detayına erişemiyor");

        // 5. Yönetim onayla
        ortam.YonetimTeklifOnayla(talep, teklifB.Id);
        talep = ortam.GuncelTalep(talep.Id);
        sonuc.Adim($"5. Yönetim onayladı → {talep.Durum}, sipariş no: {talep.SiparisNo}");
        sonuc.Bekle(talep.Durum == SatinalmaTalepDurumlari.Onaylandi && talep.YonetimOnayKilitli,
            "Durum: Onaylandı + kilitli", "Onay sonrası durum hatalı");

        sonuc.Bekle(ortam.KullaniciBildirimleri(BellekTestOrtami.Saha)
                .Any(b => b.Tip == BildirimTipleri.Onaylandi && b.TalepId == talep.Id)
            && !ortam.KullaniciBildirimleri(BellekTestOrtami.Satinalma)
                .Any(b => b.Tip == BildirimTipleri.Onaylandi && b.TalepId == talep.Id),
            "Onay bildirimi yalnızca talep sahibine gitti",
            "KRİTİK: Onay bildirimi talep sahibi dışındaki kullanıcıya da gitti");

        var onayRouteSat = AndroidAyna.AndroidBildirimRoute(BildirimTipleri.Onaylandi, talep.Id, KullaniciRolleri.Saha);
        var onayRouteMasa = BildirimRotaServisi.HedefRoute(
            new BildirimKaydi { Tip = BildirimTipleri.Onaylandi, TalepId = talep.Id },
            KullaniciRolleri.Saha);
        sonuc.Bekle(onayRouteSat == onayRouteMasa,
            $"Onaylandi rotası hizalı (M/A): {onayRouteMasa}",
            $"Onaylandi rota uyumsuz M={onayRouteMasa} A={onayRouteSat}");

        sonuc.Bekle(MasaustuPart1Ayna.TalepListede(talep, "satinalma-onaylanan", null, null, KullaniciRolleri.Satinalma),
            "Talep satinalma-onaylanan listesinde",
            "Onaylanan talep satinalma-onaylanan listesinde DEĞİL");

        // 6. Sipariş ver
        ortam.SiparisVer(talep);
        talep = ortam.GuncelTalep(talep.Id);
        sonuc.Adim($"6. Sipariş verildi → {talep.Durum}");
        sonuc.Bekle(talep.Durum == SatinalmaTalepDurumlari.SiparisOlusturuldu,
            "Durum: Sipariş Oluşturuldu", "Sipariş ver sonrası durum hatalı");

        var malzemeler = OnaylananMalzemeOlusturucu.Olustur(ortam.Talepler);
        sonuc.Bekle(AndroidAyna.SatinalmaSiparisListede(talep, malzemeler),
            "Talep Android sipariş listesinde (mal kabul bekleyen)",
            "Sipariş sonrası Android satinalma-siparis listesinde DEĞİL");
        sonuc.Bekle(AndroidAyna.MasaustuSiparisListede(talep, malzemeler),
            "Talep masaüstü sipariş listesinde (mal kabul bekleyen)",
            "Sipariş sonrası masaüstü sipariş listesinde DEĞİL");

        sonuc.Bekle(ortam.KullaniciBildirimleri(BellekTestOrtami.Saha)
                .Any(b => b.Tip == BildirimTipleri.SiparisOlusturuldu && b.TalepId == talep.Id)
            && !ortam.KullaniciBildirimleri(BellekTestOrtami.Satinalma)
                .Any(b => b.Tip == BildirimTipleri.SiparisOlusturuldu && b.TalepId == talep.Id),
            "Sipariş bildirimi yalnızca talep sahibine gitti",
            "KRİTİK: Sipariş bildirimi talep sahibi dışındaki kullanıcıya da gitti");

        var siparisRoute = AndroidAyna.AndroidBildirimRoute(BildirimTipleri.SiparisOlusturuldu, talep.Id, KullaniciRolleri.Saha);
        var siparisRouteM = BildirimRotaServisi.HedefRoute(
            new BildirimKaydi { Tip = BildirimTipleri.SiparisOlusturuldu, TalepId = talep.Id },
            KullaniciRolleri.Saha);
        sonuc.Bekle(siparisRoute == siparisRouteM,
            $"SiparisOlusturuldu rotası hizalı (M/A): {siparisRouteM}",
            $"SiparisOlusturuldu rota uyumsuz M={siparisRouteM} A={siparisRoute}");

        // 7. Mal kabul
        var kalemId = talep.Kalemler[0].Id;
        ortam.MalKabul(talep, kalemId, talep.Kalemler[0].Miktar);
        talep = ortam.GuncelTalep(talep.Id);
        malzemeler = OnaylananMalzemeOlusturucu.Olustur(ortam.Talepler);
        sonuc.Adim($"7. Mal kabul tamamlandı → kabul: {talep.Kalemler[0].KabulEdilenMiktar}/{talep.Kalemler[0].Miktar}");
        sonuc.Bekle(talep.Kalemler[0].SiparisTamamlandi,
            "Kalem sipariş tamamlandı", "Mal kabul sonrası kalem tamamlanmadı");
        sonuc.Bekle(!AndroidAyna.SatinalmaSiparisListede(talep, malzemeler),
            "Tamamlanan talep sipariş listesinden düştü",
            "Mal kabul sonrası hâlâ sipariş listesinde görünüyor");
        sonuc.Bekle(AndroidAyna.SatinalmaMalKabulListede(talep, malzemeler),
            "Talep mal kabul tamamlanan listesinde",
            "Mal kabul tamamlanan listesinde DEĞİL");

        sonuc.Bekle(ortam.KullaniciBildirimleri(BellekTestOrtami.Saha).Any(b => b.Tip == BildirimTipleri.MalKabulEdildi),
            "Talep sahibi MalKabulEdildi bildirimini aldı",
            "Talep sahibine mal kabul bildirimi yok");

        var mkRoute = AndroidAyna.AndroidBildirimRoute(BildirimTipleri.MalKabulEdildi, talep.Id, KullaniciRolleri.Saha);
        var mkRouteM = BildirimRotaServisi.HedefRoute(
            new BildirimKaydi { Tip = BildirimTipleri.MalKabulEdildi, TalepId = talep.Id },
            KullaniciRolleri.Saha);
        sonuc.Bekle(mkRoute == mkRouteM,
            $"Talep sahibi MalKabulEdildi rotası hizalı (M/A): {mkRouteM}",
            $"Talep sahibi mal kabul rotası uyumsuz M={mkRouteM} A={mkRoute}");

        var redHedefleri = BildirimRolPolitikasi.ReddedildiHedefleri(
                BellekTestOrtami.Saha.Uid, BellekTestOrtami.Yonetim.Uid)
            .ToList();
        sonuc.Bekle(redHedefleri.Count == 1 && redHedefleri[0].HedefUid == BellekTestOrtami.Saha.Uid
            && !BildirimRolPolitikasi.KayitGonderilmeli(null, BellekTestOrtami.Yonetim.Uid, BellekTestOrtami.Yonetim.Uid),
            "Red bildirimi yalnızca talep sahibine gider; işlemi yapana gönderilmez",
            "KRİTİK: Red bildirimi için alıcı veya işlem yapan filtresi hatalı");

        return sonuc;
    }

    public static E2eTestSonuc SahaTalepSahiplik(BellekTestOrtami ortam)
    {
        var sonuc = new E2eTestSonuc();
        sonuc.Adim("=== SENARYO 2: Şef/Saha görünürlük ve talep sahipliği ===");

        var sahaTalep = ortam.SahaTalepOlustur(BellekTestOrtami.Saha, "E2E Saha Malzeme");
        var sefTalep = ortam.SahaTalepOlustur(BellekTestOrtami.Sef, "E2E Şef Malzeme");

        sahaTalep = ortam.GuncelTalep(sahaTalep.Id);
        sahaTalep.Durum = SatinalmaTalepDurumlari.Onaylandi;
        sahaTalep.TeklifsizYonetimOnayi = true;
        sahaTalep.YonetimOnayKilitli = true;
        ortam.Kaydet(sahaTalep);

        sefTalep = ortam.GuncelTalep(sefTalep.Id);
        sefTalep.Durum = SatinalmaTalepDurumlari.Onaylandi;
        sefTalep.TeklifsizYonetimOnayi = true;
        sefTalep.YonetimOnayKilitli = true;
        ortam.Kaydet(sefTalep);

        var sahaGorur = TabFilterManager.FilterForTab(
            ProcurementTab.ApprovedOrders,
            ortam.Talepler,
            ProcurementRequestMapper.FromTalep,
            BellekTestOrtami.Saha.Rol,
            BellekTestOrtami.Saha.Uid);

        sonuc.Bekle(sahaGorur.Any(t => t.Id == sahaTalep.Id) && sahaGorur.Any(t => t.Id == sefTalep.Id),
            "Saha kendi ve diğer kullanıcıların onaylanan taleplerini görüyor",
            "KRİTİK: Saha liste görünürlüğü istek sahibiyle sınırlandırılmış");

        var yetkiTalep = ortam.SahaTalepOlustur(BellekTestOrtami.Saha, "E2E Sahiplik Yetki Testi");
        var sahaKendiTalebiniDuzenler = SatinalmaTalepYetkileri.TalepDuzenleyebilir(
            BellekTestOrtami.Saha.Rol, yetkiTalep, BellekTestOrtami.Saha.Uid, BellekTestOrtami.Saha.AdSoyad);
        var sahaKendiTalebiniSiler = SatinalmaTalepYetkileri.TalepSilebilir(
            BellekTestOrtami.Saha.Rol, yetkiTalep, BellekTestOrtami.Saha.Uid, BellekTestOrtami.Saha.AdSoyad);
        var sefBaskaTalebiDuzenleyemez = !SatinalmaTalepYetkileri.TalepDuzenleyebilir(
            BellekTestOrtami.Sef.Rol, yetkiTalep, BellekTestOrtami.Sef.Uid, BellekTestOrtami.Sef.AdSoyad);
        var sefBaskaTalebiSilemez = !SatinalmaTalepYetkileri.TalepSilebilir(
            BellekTestOrtami.Sef.Rol, yetkiTalep, BellekTestOrtami.Sef.Uid, BellekTestOrtami.Sef.AdSoyad);
        sonuc.Bekle(
            sahaKendiTalebiniDuzenler && sahaKendiTalebiniSiler
            && sefBaskaTalebiDuzenleyemez && sefBaskaTalebiSilemez,
            "Şef/Saha yalnızca kendi talebini düzenleyip silebiliyor",
            "KRİTİK: Şef/Saha başka kullanıcının talebinde işlem yapabiliyor veya kendi talebini yönetemiyor");

        var satinalmaDuzenler = SatinalmaTalepYetkileri.TalepDuzenleyebilir(
            BellekTestOrtami.Satinalma.Rol, yetkiTalep, BellekTestOrtami.Satinalma.Uid, BellekTestOrtami.Satinalma.AdSoyad);
        var satinalmaSiler = SatinalmaTalepYetkileri.TalepSilebilir(
            BellekTestOrtami.Satinalma.Rol, yetkiTalep, BellekTestOrtami.Satinalma.Uid, BellekTestOrtami.Satinalma.AdSoyad);
        sonuc.Bekle(satinalmaDuzenler && satinalmaSiler,
            "Satınalma tüm kullanıcıların taleplerini düzenleyip silebiliyor",
            "KRİTİK: Satınalma başka kullanıcının talebini yönetemiyor");

        return sonuc;
    }

    public static E2eTestSonuc TeklifsizOnay(BellekTestOrtami ortam)
    {
        var sonuc = new E2eTestSonuc();
        sonuc.Adim("=== SENARYO 3: Teklifsiz yönetim onayı ===");

        ortam.SetUser(BellekTestOrtami.Saha);
        var talep = new SatinalmaTalep
        {
            Id = Guid.NewGuid(),
            TalepNo = ortam.YeniTalepNo(),
            Tarih = DateTime.Now.ToString("dd.MM.yyyy"),
            TalepEden = BellekTestOrtami.Saha.AdSoyad,
            OlusturanUid = BellekTestOrtami.Saha.Uid,
            OlusturanRol = BellekTestOrtami.Saha.Rol,
            SantiyeAdi = "Test",
            TalepAciklamasi = BellekTestOrtami.TestEtiketi,
            Durum = SatinalmaTalepDurumlari.ImzaSurecinde,
            Kalemler = [new SatinalmaTalepKalemi { Id = Guid.NewGuid(), Malzeme = "E2E Demir", Miktar = 5, Birim = "Ton" }]
        };
        ortam.Kaydet(talep);

        ortam.SetUser(BellekTestOrtami.Yonetim);
        talep.Durum = SatinalmaTalepDurumlari.Onaylandi;
        talep.TeklifsizYonetimOnayi = true;
        talep.YonetimOnayKilitli = true;
        ortam.Kaydet(talep);
        ortam.BildirimEkle(BildirimTipleri.Onaylandi, talep, hedefUid: talep.OlusturanUid);

        sonuc.Adim("Yönetim teklifsiz onayladı");
        sonuc.Bekle(talep.TeklifsizFirmaFiyatBekliyor,
            "Teklifsiz firma/fiyat bekliyor durumu", "Teklifsiz onay sonrası firma/fiyat beklenmiyor");

        // Firma fiyat gir
        ortam.SetUser(BellekTestOrtami.Satinalma);
        var kalem = talep.Kalemler[0];
        var teklifId = Guid.NewGuid();
        talep.Teklifler.Add(new SatinalmaTeklif
        {
            Id = teklifId,
            FirmaAdi = "E2E Teklifsiz Firma",
            Onaylandi = true,
            Fiyatlar =
            [
                new SatinalmaTeklifFiyati
                {
                    KalemId = kalem.Id,
                    BirimFiyat = 5000,
                    KdvOrani = 20
                }
            ]
        });
        talep.Teklifler[0].FiyatlariHesapla(talep.Kalemler);
        kalem.OnaylananTeklifId = teklifId;
        ortam.Kaydet(talep);

        sonuc.Bekle(!ortam.GuncelTalep(talep.Id).TeklifsizFirmaFiyatBekliyor,
            "Firma/fiyat girildi", "Teklifsiz firma/fiyat girişi tamamlanmadı");

        return sonuc;
    }

    public static E2eTestSonuc BildirimDuzeltmeAkisi(BellekTestOrtami ortam)
    {
        var sonuc = new E2eTestSonuc();
        sonuc.Adim("=== SENARYO 4: Teklif düzeltme bildirim rotası ===");

        var talep = ortam.SahaTalepOlustur(BellekTestOrtami.Saha);
        ortam.YonetimTeklifIste(talep);
        ortam.TeklifEkle(talep, "Firma X", 50);
        ortam.YonetimeTeklifGonder(talep);

        talep = ortam.GuncelTalep(talep.Id);
        talep.Durum = SatinalmaTalepDurumlari.Karsilastirma;
        talep.TeklifDuzeltmeNotu = "Fiyatları güncelleyin";
        ortam.Kaydet(talep);
        ortam.BildirimEkle(BildirimTipleri.TeklifDuzeltmeIstendi, talep, hedefRol: KullaniciRolleri.Satinalma);

        var androidRoute = AndroidAyna.AndroidBildirimRoute(BildirimTipleri.TeklifDuzeltmeIstendi, talep.Id, KullaniciRolleri.Satinalma);
        var masaRoute = BildirimRotaServisi.HedefRoute(
            new BildirimKaydi { Tip = BildirimTipleri.TeklifDuzeltmeIstendi, TalepId = talep.Id },
            KullaniciRolleri.Satinalma);

        sonuc.Bekle(androidRoute == masaRoute,
            $"TeklifDuzeltmeIstendi rotası hizalı: {masaRoute}",
            $"TeklifDuzeltmeIstendi rota uyumsuz M={masaRoute} A={androidRoute}");

        sonuc.Bekle(AndroidAyna.AndroidCanAccess(androidRoute, KullaniciRolleri.Satinalma),
            "Satınalma düzeltme bildirim rotasına erişebiliyor",
            "Düzeltme bildirimi rotası erişilemiyor");

        return sonuc;
    }
}

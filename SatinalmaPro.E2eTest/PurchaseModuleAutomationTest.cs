using SatinalmaPro.Shared.Helpers;
using SatinalmaPro.Shared.Models;
using SatinalmaPro.Shared.Procurement;
using SatinalmaPro.Shared.Procurement.Detail;

namespace SatinalmaPro.E2eTest;

/// <summary>
/// Satınalma + Stok modülü uçtan uca otomasyon simülatörü.
/// Rol oturumları, dinamik sekmeler, buton görünürlüğü, FCM topic ve Firestore güvenlik kuralları.
/// </summary>
public static class PurchaseModuleAutomationTest
{
    public static IReadOnlyList<E2eTestSonuc> TumSenaryolariCalistir(AutomasyonTestOrtami ortam)
    {
        var sonuclar = new List<E2eTestSonuc>
        {
            Senaryo1NormalTalepAkisi(ortam)
        };
        ortam.Temizle();

        sonuclar.Add(Senaryo2AcilTalepAkisi(ortam));
        ortam.Temizle();

        sonuclar.Add(GuvenlikVeRolKisitlamalari(ortam));
        ortam.Temizle();

        return sonuclar;
    }

    public static E2eTestSonuc Senaryo1NormalTalepAkisi(AutomasyonTestOrtami ortam)
    {
        var sonuc = new E2eTestSonuc();
        sonuc.Adim("=== SENARYO 1: NORMAL TALEP AKIŞI (Teklif ve Revize Döngüsü) ===");

        // 1 — Şef
        ortam.OturumAc(AutomasyonTestOrtami.Sef);
        var sefTopic = FcmTopicKayitcisi.TopicForRole(KullaniciRolleri.Sef);
        sonuc.Bekle(ortam.Fcm.AboneMi(sefTopic),
            $"Şef oturumu {sefTopic} konusuna abone",
            $"Şef FCM aboneliği başarısız: {sefTopic}");

        var talep = ortam.TalepOlustur(AutomasyonTestOrtami.Sef, ProcurementPriority.Normal);
        sonuc.Adim($"1. Şef talep oluşturdu: {talep.TalepNo} → status={talep.Status}");
        sonuc.Bekle(talep.Status == ProcurementStatus.Submitted,
            "Durum submitted", $"Beklenen submitted, gelen: {talep.Status}");

        var baskaTalep = ortam.TalepOlustur(AutomasyonTestOrtami.Saha, ProcurementPriority.Normal, "Başka Saha Malzeme");
        var sefGorur = ortam.RouteTalepleri(SatinalmaRoutes.Taleplerim, AutomasyonTestOrtami.Sef).ToList();
        sonuc.Bekle(sefGorur.Any(t => t.Id == talep.Id) && !sefGorur.Any(t => t.Id == baskaTalep.Id),
            "Şef yalnızca kendi talebini görüyor (requesterUid filtresi)",
            "KRİTİK: Şef sahiplik filtresi çalışmıyor");

        // 2 — Yönetim
        ortam.OturumAc(AutomasyonTestOrtami.Yonetim);
        sonuc.Bekle(ortam.RouteTalepleri(SatinalmaRoutes.YonetimGelenTalepler, AutomasyonTestOrtami.Yonetim)
                .Any(t => t.Id == talep.Id),
            "Yönetim Gelen Talepler sekmesinde talebi buldu",
            "Talep yönetim-gelen-talepler listesinde DEĞİL");

        var uiYonetim = ortam.UiDurumu(talep, AutomasyonTestOrtami.Yonetim);
        sonuc.Bekle(uiYonetim.VisibleActions.Contains(PurchaseRequestDetailAction.StartQuoteProcess),
            "Teklif Sürecini Başlat butonu görünür (normal öncelik)",
            "StartQuoteProcess butonu görünmüyor");
        sonuc.Bekle(!uiYonetim.VisibleActions.Contains(PurchaseRequestDetailAction.DirectApprove),
            "Direkt Onay gizli (normal öncelik)",
            "Normal talepte Direkt Onay görünmemeli");

        ortam.DetayAksiyonUygula(talep, PurchaseRequestDetailAction.StartQuoteProcess, AutomasyonTestOrtami.Yonetim);
        talep = ortam.GuncelTalep(talep.Id);
        sonuc.Adim($"2. Yönetim teklif sürecini başlattı → {talep.Status}");
        sonuc.Bekle(talep.Status == ProcurementStatus.QuoteRequested,
            "Durum quote_requested", $"Durum hatalı: {talep.Status}");

        var satTopic = FcmTopicKayitcisi.TopicForRole(KullaniciRolleri.Satinalma);
        sonuc.Bekle(ortam.Fcm.PushGittiMi(satTopic, BildirimTipleri.TeklifIstendi, talep.Id),
            $"FCM {satTopic} kanalına TeklifIstendi bildirimi gitti",
            $"Satınalma FCM bildirimi yok ({satTopic})");

        // 3 — Satınalma teklif girişi
        ortam.OturumAc(AutomasyonTestOrtami.Satinalma);
        sonuc.Bekle(ortam.SekmeGorunur(SatinalmaRoutes.SatinalmaTeklifIstenen, AutomasyonTestOrtami.Satinalma),
            "Satınalma Teklif İstemi sekmesi görünür",
            "satinalma-teklif-istenen sekmesi gizli");

        sonuc.Bekle(ortam.RouteTalepleri(SatinalmaRoutes.SatinalmaTeklifIstenen, AutomasyonTestOrtami.Satinalma)
                .Any(t => t.Id == talep.Id),
            "Talep Teklif İstemi Yapılanlar listesinde",
            "Talep satinalma-teklif-istenen listesinde DEĞİL");

        var teklifA = ortam.EnterpriseTeklifEkle(talep, "E2E Tedarikçi A", 120, AutomasyonTestOrtami.Satinalma);
        talep = ortam.GuncelTalep(talep.Id);
        sonuc.Bekle(ortam.RouteTalepleri(SatinalmaRoutes.SatinalmaTeklifGirilen, AutomasyonTestOrtami.Satinalma)
                .Any(t => t.Id == talep.Id),
            "Talep Teklif Girişi Bekleyenler sekmesinde (quote_entry)",
            "Talep satinalma-teklif-girilen listesinde DEĞİL");

        var teklifB = ortam.EnterpriseTeklifEkle(talep, "E2E Tedarikçi B", 95, AutomasyonTestOrtami.Satinalma);
        talep = ortam.GuncelTalep(talep.Id);

        sonuc.Adim($"3. Satınalma 2 teklif ekledi (quotes/line_prices) → {talep.Status}");
        sonuc.Bekle(ortam.Quotes.TryGetValue(talep.Id.ToString(), out var quotes) && quotes.Count == 2,
            "Firestore quotes alt koleksiyonunda 2 kayıt",
            "quotes alt koleksiyonu eksik");
        sonuc.Bekle(quotes!.All(q => q.LinePrices.Count > 0),
            "Her quote için line_prices verisi yazıldı",
            "line_prices eksik");

        ortam.YonetimeTeklifGonder(talep, AutomasyonTestOrtami.Satinalma);
        talep = ortam.GuncelTalep(talep.Id);
        sonuc.Bekle(talep.Status == ProcurementStatus.ManagementQuoteReview,
            "Durum management_quote_review", $"Durum hatalı: {talep.Status}");

        var yonTopic = FcmTopicKayitcisi.TopicForRole(KullaniciRolleri.Yonetim);
        sonuc.Bekle(ortam.Fcm.PushGittiMi(yonTopic, BildirimTipleri.TeklifOnayda, talep.Id),
            $"Yönetim kanalına ({yonTopic}) TeklifOnayda bildirimi gitti",
            "Yönetim teklif inceleme bildirimi yok");

        sonuc.Bekle(ortam.RouteTalepleri(SatinalmaRoutes.YonetimTeklifGirilen, AutomasyonTestOrtami.Yonetim)
                .Any(t => t.Id == talep.Id),
            "Talep Yönetim Teklif İnceleme sekmesinde",
            "yonetim-teklif-girilen listesinde DEĞİL");

        // 4 — Yönetim revize
        ortam.OturumAc(AutomasyonTestOrtami.Yonetim);
        var uiRevize = ortam.UiDurumu(talep, AutomasyonTestOrtami.Yonetim);
        sonuc.Bekle(uiRevize.VisibleActions.Contains(PurchaseRequestDetailAction.SendQuotesForRevision),
            "Teklifleri Revizeye Gönder butonu görünür",
            "Revize butonu görünmüyor");

        ortam.DetayAksiyonUygula(
            talep,
            PurchaseRequestDetailAction.SendQuotesForRevision,
            AutomasyonTestOrtami.Yonetim,
            not: "Birim fiyatları güncelleyin");
        talep = ortam.GuncelTalep(talep.Id);
        sonuc.Adim($"4. Yönetim revizeye gönderdi → {talep.Status}");
        sonuc.Bekle(talep.Status == ProcurementStatus.Comparison,
            "Durum comparison", $"Durum hatalı: {talep.Status}");
        sonuc.Bekle(ortam.Fcm.PushGittiMi(satTopic, BildirimTipleri.TeklifDuzeltmeIstendi, talep.Id),
            "Satınalma revize bildirimi aldı",
            "TeklifDuzeltmeIstendi FCM yok");

        // 5 — Satınalma güncelle ve yeniden gönder
        ortam.OturumAc(AutomasyonTestOrtami.Satinalma);
        talep = ortam.GuncelTalep(talep.Id);
        var guncelTeklif = talep.Teklifler.First(t => t.Id == teklifB.Id);
        foreach (var f in guncelTeklif.Fiyatlar)
            f.BirimFiyat = 90;
        guncelTeklif.FiyatlariHesapla(talep.Kalemler);
        ortam.Kaydet(talep);
        ortam.YonetimeTeklifGonder(talep, AutomasyonTestOrtami.Satinalma);
        talep = ortam.GuncelTalep(talep.Id);
        sonuc.Adim($"5. Satınalma teklifi güncelleyip yönetime yolladı → {talep.Status}");
        sonuc.Bekle(talep.Status == ProcurementStatus.ManagementQuoteReview,
            "Tekrar management_quote_review", "Yeniden gönderim durumu hatalı");

        // 6 — Yönetim onay + sipariş
        ortam.OturumAc(AutomasyonTestOrtami.Yonetim);
        ortam.DetayAksiyonUygula(
            talep,
            PurchaseRequestDetailAction.ApproveQuote,
            AutomasyonTestOrtami.Yonetim,
            quoteId: teklifB.Id.ToString());
        talep = ortam.GuncelTalep(talep.Id);
        sonuc.Adim($"6a. Yönetim teklif onayladı → {talep.Status}");
        sonuc.Bekle(talep.Status == ProcurementStatus.Approved,
            "Durum approved", $"Onay sonrası durum: {talep.Status}");

        ortam.SiparisOlustur(talep, AutomasyonTestOrtami.Satinalma);
        talep = ortam.GuncelTalep(talep.Id);
        sonuc.Adim($"6b. Sipariş oluşturuldu → {talep.Status}, sipariş: {talep.SiparisNo}");
        sonuc.Bekle(talep.Status == ProcurementStatus.Ordered,
            "Durum ordered", $"Sipariş durumu hatalı: {talep.Status}");

        var depoTopic = FcmTopicKayitcisi.TopicForRole(KullaniciRolleri.Depo);
        var sefTopicPush = FcmTopicKayitcisi.TopicForRole(KullaniciRolleri.Sef);
        sonuc.Bekle(ortam.Fcm.PushGittiMi(depoTopic, BildirimTipleri.SiparisOlusturuldu, talep.Id),
            $"Depo kanalına ({depoTopic}) SiparisOlusturuldu bildirimi",
            "Depo FCM bildirimi yok");
        sonuc.Bekle(
            ortam.Fcm.Pushlar.Any(p => p.Tip == BildirimTipleri.SiparisOlusturuldu && p.TalepId == talep.Id
                && (p.Topic.Contains("sef") || p.Topic.Contains("saha") || p.Topic.Contains("user-"))),
            "Saha/Şef kanalına sipariş bildirimi gitti",
            "Talep sahibine sipariş bildirimi yok");

        // 7 — Depo mal kabul
        ortam.OturumAc(AutomasyonTestOrtami.Depo);
        sonuc.Bekle(ortam.SekmeGorunur(SatinalmaRoutes.SatinalmaSiparis, AutomasyonTestOrtami.Depo),
            "Depo Yoldaki Malzemeler sekmesi görünür",
            "Depo satinalma-siparis sekmesi gizli");
        sonuc.Bekle(ortam.RouteTalepleri(SatinalmaRoutes.SatinalmaSiparis, AutomasyonTestOrtami.Depo)
                .Any(t => t.Id == talep.Id),
            "Sipariş Yoldaki Malzemeler listesinde",
            "Depo sipariş listesinde talep yok");

        var malzemeAdi = talep.Kalemler[0].Malzeme;
        var oncekiStok = ortam.Stocks.GetValueOrDefault(malzemeAdi);
        ortam.DepoMalKabulTamamla(talep);
        talep = ortam.GuncelTalep(talep.Id);

        sonuc.Adim($"7. Depo mal kabul tamamladı → status={talep.Status}");
        sonuc.Bekle(ortam.StockMovements.Any(m => m.Type == "IN" && m.RequestId == talep.Id.ToString()),
            "stock_movements koleksiyonuna IN hareketi yazıldı",
            "IN stock_movement kaydı yok");
        sonuc.Bekle(ortam.Stocks.GetValueOrDefault(malzemeAdi) > oncekiStok,
            $"stocks miktarı arttı ({oncekiStok} → {ortam.Stocks.GetValueOrDefault(malzemeAdi)})",
            "Stok miktarı artmadı");
        sonuc.Bekle(talep.Status == ProcurementStatus.Completed,
            "Ana talep completed durumunda",
            $"Tamamlanma durumu hatalı: {talep.Status}");

        return sonuc;
    }

    public static E2eTestSonuc Senaryo2AcilTalepAkisi(AutomasyonTestOrtami ortam)
    {
        var sonuc = new E2eTestSonuc();
        sonuc.Adim("=== SENARYO 2: ACİL TALEP AKIŞI (Doğrudan Karar / Teklif Yasaklama) ===");

        ortam.OturumAc(AutomasyonTestOrtami.Saha);
        var talep = ortam.TalepOlustur(AutomasyonTestOrtami.Saha, ProcurementPriority.Urgent, "E2E Acil Demir", 5);
        sonuc.Adim($"1. Saha acil talep açtı → priority={talep.Priority}, status={talep.Status}");
        sonuc.Bekle(talep.Priority == ProcurementPriority.Urgent,
            "Öncelik urgent", $"Öncelik hatalı: {talep.Priority}");

        ortam.OturumAc(AutomasyonTestOrtami.Yonetim);
        sonuc.Bekle(ortam.RouteTalepleri(SatinalmaRoutes.YonetimGelenTalepler, AutomasyonTestOrtami.Yonetim)
                .Any(t => t.Id == talep.Id),
            "Acil talep Gelen Talepler sekmesinde",
            "Acil talep gelen listesinde DEĞİL");

        var ui = ortam.UiDurumu(talep, AutomasyonTestOrtami.Yonetim);
        sonuc.Bekle(!ui.VisibleActions.Contains(PurchaseRequestDetailAction.StartQuoteProcess),
            "ASSERT: Teklif İste / Teklif Sürecini Başlat KESİNLİKLE GİZLİ",
            "KRİTİK: Acil talepte Teklif Sürecini Başlat görünüyor!");
        sonuc.Bekle(ui.VisibleActions.Contains(PurchaseRequestDetailAction.DirectApprove),
            "Direkt Onay Ver butonu aktif",
            "Direkt Onay butonu görünmüyor");
        sonuc.Bekle(ui.VisibleActions.Contains(PurchaseRequestDetailAction.RejectRequest),
            "Talebi Reddet butonu aktif",
            "Reddet butonu görünmüyor");

        ortam.DetayAksiyonUygula(talep, PurchaseRequestDetailAction.DirectApprove, AutomasyonTestOrtami.Yonetim);
        talep = ortam.GuncelTalep(talep.Id);
        sonuc.Adim($"2. Yönetim Direkt Onay verdi → {talep.Status}");
        sonuc.Bekle(talep.Status == ProcurementStatus.Approved,
            "Durum direkt approved", $"Acil onay sonrası durum: {talep.Status}");
        sonuc.Bekle(talep.TeklifsizYonetimOnayi,
            "Teklifsiz yönetim onayı işaretlendi",
            "TeklifsizYonetimOnayi false");

        // Satınalma rolüyle de aynı UI kuralı (yönetim kararı)
        ortam.OturumAc(AutomasyonTestOrtami.Satinalma);
        var uiSat = PurchaseRequestDetailPresenter.BuildUiState(talep, AutomasyonTestOrtami.Satinalma.Rol);
        sonuc.Bekle(!uiSat.VisibleActions.Contains(PurchaseRequestDetailAction.StartQuoteProcess),
            "Satınalma rolü acil submitted ekranında teklif isteyemez (yönetim kararı değil)",
            "Satınalma acil talepte teklif butonu görüyor");

        return sonuc;
    }

    public static E2eTestSonuc GuvenlikVeRolKisitlamalari(AutomasyonTestOrtami ortam)
    {
        var sonuc = new E2eTestSonuc();
        sonuc.Adim("=== GÜVENLİK VE ROL KISITLAMASI TESTLERİ (Negatif) ===");

        var atolyeSonuc = ortam.StockMovementYazmayiDene(AutomasyonTestOrtami.Atolye);
        sonuc.Bekle(atolyeSonuc == FirestoreGuvenlikSimulasyonu.IslemSonucu.PermissionDenied,
            "Atölye stock_movements yazma Permission Denied",
            "KRİTİK: Atölye stock_movements yazabildi!");

        var talep = ortam.TalepOlustur(AutomasyonTestOrtami.Sef, ProcurementPriority.Normal);
        ortam.EnterpriseTeklifEkle(talep, "Gizli Firma", 50, AutomasyonTestOrtami.Satinalma);

        var depoSonuc = ortam.QuotesOkumayiDene(AutomasyonTestOrtami.Depo, talep.Id.ToString());
        sonuc.Bekle(depoSonuc == FirestoreGuvenlikSimulasyonu.IslemSonucu.PermissionDenied,
            "Depo procurement quotes okuma engellendi",
            "KRİTİK: Depo teklifleri okuyabildi!");

        var satSonuc = ortam.QuotesOkumayiDene(AutomasyonTestOrtami.Satinalma, talep.Id.ToString());
        sonuc.Bekle(satSonuc == FirestoreGuvenlikSimulasyonu.IslemSonucu.IzinVerildi,
            "Satınalma quotes okuma izinli (pozitif kontrol)",
            "Satınalma quotes okuyamadı");

        sonuc.Bekle(!ortam.SekmeGorunur(SatinalmaRoutes.YonetimGelenTalepler, AutomasyonTestOrtami.Atolye),
            "Atölye yönetim sekmelerine erişemez",
            "Atölye yönetim sekmesi görünür");
        sonuc.Bekle(ortam.SekmeGorunur(SatinalmaRoutes.SatinalmaSiparis, AutomasyonTestOrtami.Atolye),
            "Atölye Yoldaki Malzemeler (read-only) sekmesi görünür",
            "Atölye sipariş sekmesi gizli");

        return sonuc;
    }
}

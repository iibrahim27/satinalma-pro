# Satınalma E2E Test Raporu
Tarih: 2026-07-08 09:17

Test verileri bellek içi simülasyon ile oluşturuldu; Firebase'e yazılmadı.
Akış: PurchaseModuleAutomationTest (enterprise status, FCM topic, Firestore güvenlik) + legacy E2E.
Android karşılığı: `PurchaseModuleAutomationTest` (JVM) + `PurchaseModuleAutomationInstrumentedTest` (Logcat).

## === SENARYO 1: NORMAL TALEP AKIŞI (Teklif ve Revize Döngüsü) ===

- 1. Şef talep oluşturdu: TLP-2026-0001 → status=submitted
- 2. Yönetim teklif sürecini başlattı → quote_requested
- 3. Satınalma 2 teklif ekledi (quotes/line_prices) → quote_entry
- 4. Yönetim revizeye gönderdi → comparison
- 5. Satınalma teklifi güncelleyip yönetime yolladı → management_quote_review
- 6a. Yönetim teklif onayladı → approved
- 6b. Sipariş oluşturuldu → ordered, sipariş: SIP-2026-0001
- 7. Depo mal kabul tamamladı → status=completed

### Geçen kontroller
- ✅ Şef oturumu /topics/sef konusuna abone
- ✅ Durum submitted
- ✅ Şef yalnızca kendi talebini görüyor (requesterUid filtresi)
- ✅ Yönetim Gelen Talepler sekmesinde talebi buldu
- ✅ Teklif Sürecini Başlat butonu görünür (normal öncelik)
- ✅ Direkt Onay gizli (normal öncelik)
- ✅ Durum quote_requested
- ✅ FCM /topics/satinalma kanalına TeklifIstendi bildirimi gitti
- ✅ Satınalma Teklif İstemi sekmesi görünür
- ✅ Talep Teklif İstemi Yapılanlar listesinde
- ✅ Talep Teklif Girişi Bekleyenler sekmesinde (quote_entry)
- ✅ Firestore quotes alt koleksiyonunda 2 kayıt
- ✅ Her quote için line_prices verisi yazıldı
- ✅ Durum management_quote_review
- ✅ Yönetim kanalına (/topics/yonetim) TeklifOnayda bildirimi gitti
- ✅ Talep Yönetim Teklif İnceleme sekmesinde
- ✅ Teklifleri Revizeye Gönder butonu görünür
- ✅ Durum comparison
- ✅ Satınalma revize bildirimi aldı
- ✅ Tekrar management_quote_review
- ✅ Durum approved
- ✅ Durum ordered
- ✅ Depo kanalına (/topics/depo) SiparisOlusturuldu bildirimi
- ✅ Saha/Şef kanalına sipariş bildirimi gitti
- ✅ Depo Yoldaki Malzemeler sekmesi görünür
- ✅ Sipariş Yoldaki Malzemeler listesinde
- ✅ stock_movements koleksiyonuna IN hareketi yazıldı
- ✅ stocks miktarı arttı (0 → 10)
- ✅ Ana talep completed durumunda

## === SENARYO 2: ACİL TALEP AKIŞI (Doğrudan Karar / Teklif Yasaklama) ===

- 1. Saha acil talep açtı → priority=urgent, status=submitted
- 2. Yönetim Direkt Onay verdi → approved

### Geçen kontroller
- ✅ Öncelik urgent
- ✅ Acil talep Gelen Talepler sekmesinde
- ✅ ASSERT: Teklif İste / Teklif Sürecini Başlat KESİNLİKLE GİZLİ
- ✅ Direkt Onay Ver butonu aktif
- ✅ Talebi Reddet butonu aktif
- ✅ Durum direkt approved
- ✅ Teklifsiz yönetim onayı işaretlendi
- ✅ Satınalma rolü acil submitted ekranında teklif isteyemez (yönetim kararı değil)

## === GÜVENLİK VE ROL KISITLAMASI TESTLERİ (Negatif) ===


### Geçen kontroller
- ✅ Atölye stock_movements yazma Permission Denied
- ✅ Depo procurement quotes okuma engellendi
- ✅ Satınalma quotes okuma izinli (pozitif kontrol)
- ✅ Atölye yönetim sekmelerine erişemez
- ✅ Atölye Yoldaki Malzemeler (read-only) sekmesi görünür

## === KAPSAM DENETİMİ: Status × Rol × Sekme matrisi ===

- Durum: draft
- Durum: submitted
- Durum: quote_requested
- Durum: quote_entry
- Durum: comparison
- Durum: management_quote_review
- Durum: approved
- Durum: ordered
- Durum: rejected
- Durum: completed
- === Geçiş zinciri doğrulaması (normal akış) ===

### Geçen kontroller
- ✅ Şef: en az 1 sekmede görünür (satinalma-talepler)
- ✅ Saha: en az 1 sekmede görünür (satinalma-talepler)
- ✅ Yönetim: en az 1 sekmede görünür (yonetim-gelen-talepler)
- ✅ Şef: en az 1 sekmede görünür (satinalma-talepler)
- ✅ Saha: en az 1 sekmede görünür (satinalma-talepler)
- ✅ Yönetim: en az 1 sekmede görünür (yonetim-teklif-bekleyen)
- ✅ Satınalma: en az 1 sekmede görünür (satinalma-teklif-istenen)
- ✅ Satınalma: en az 1 sekmede görünür (satinalma-teklif-girilen)
- ✅ Satınalma: en az 1 sekmede görünür (satinalma-karsilastirma)
- ✅ Yönetim: en az 1 sekmede görünür (yonetim-teklif-girilen)
- ✅ Yönetim: en az 1 sekmede görünür (yonetim-onaylanan-teklifler)
- ✅ Satınalma: en az 1 sekmede görünür (satinalma-onaylanan)
- ✅ Şef: en az 1 sekmede görünür (satinalma-onaylanan-talepler)
- ✅ Saha: en az 1 sekmede görünür (satinalma-onaylanan-talepler)
- ✅ Yönetim: en az 1 sekmede görünür (yonetim-onaylanan-teklifler)
- ✅ Satınalma: en az 1 sekmede görünür (satinalma-siparis)
- ✅ Şef: en az 1 sekmede görünür (satinalma-siparis)
- ✅ Saha: en az 1 sekmede görünür (satinalma-siparis)
- ✅ Depo: en az 1 sekmede görünür (satinalma-siparis)
- ✅ Atölye: en az 1 sekmede görünür (satinalma-siparis)
- ✅ Yönetim: en az 1 sekmede görünür (yonetim-red-verilen)
- ✅ Satınalma: en az 1 sekmede görünür (yonetim-red-verilen)
- ✅ Şef: en az 1 sekmede görünür (yonetim-red-verilen)
- ✅ Saha: en az 1 sekmede görünür (yonetim-red-verilen)
- ✅ Admin: en az 1 sekmede görünür (yonetim-onaylanan-teklifler, yonetim-gecmis, satinalma-mal-kabul)
- ✅ Yönetim: en az 1 sekmede görünür (yonetim-onaylanan-teklifler)
- ✅ Satınalma: en az 1 sekmede görünür (satinalma-mal-kabul)
- ✅ submitted → yönetim Gelen Talepler
- ✅ quote_requested → satınalma Teklif İstemi
- ✅ quote_entry → satınalma Teklif Girişi Bekleyenler
- ✅ management_quote_review → yönetim Teklif İnceleme

## === Platform menü uyumu (Desktop vs TabFilterManager) ===


### Geçen kontroller
- ✅ Şef: desktop Taleplerim=True, Android ManagementApproval=True
- ✅ Saha: desktop Taleplerim=True, Android ManagementApproval=True

### Bilinen platform farkları (uyarı)
- ⚠️ Şef: Android TabFilterManager yönetim sekmeleri gösteriyor, masaüstü göstermiyor (bilinen fark — Android shell ayrı menü kullanabilir)
- ⚠️ Saha: Android TabFilterManager yönetim sekmeleri gösteriyor, masaüstü göstermiyor (bilinen fark — Android shell ayrı menü kullanabilir)
- ⚠️ Atölye: masaüstünde Yoldaki Malzemeler var, Android TabFilterManager'da ApprovedOrders yok — Android AtolyeShell kontrol edilmeli

## === SENARYO 1: Saha → Yönetim → Teklif → Onay → Sipariş → Mal Kabul ===

- 1. Saha talep oluşturdu: TLP-2026-0001 → İmza Sürecinde
- 2. Yönetim teklif istedi → Teklif Girişi
- 3. Satınalma 2 teklif girdi → Karşılaştırma, öneri: E2E Firma B
- 4. Yönetime teklif gönderildi → Yönetim Onayında
- 5. Yönetim onayladı → Onaylandı, sipariş no: SIP-2026-0001
- 6. Sipariş verildi → Sipariş Oluşturuldu
- 7. Mal kabul tamamlandı → kabul: 10/10

### Geçen kontroller
- ✅ Durum: İmza Sürecinde
- ✅ Yönetime YonetimeGonderildi bildirimi oluştu
- ✅ Satınalmaya YonetimeGonderildi bildirimi oluştu
- ✅ Bildirim rotası gelen-talepler (M:gelen-talepler, A:gelen-talepler)
- ✅ Talep yönetim gelen listesinde
- ✅ Durum: Teklif Girişi
- ✅ Android TeklifIstendi → teklif-gir?id= (teklif-gir?id=fbba675e-6e0e-4233-9bd8-fed4ad029102)
- ✅ Satınalma teklif-gir?id rotasına erişebiliyor
- ✅ Talep satınalma teklif istenen listesinde
- ✅ Karşılaştırma aşamasında 2 teklif
- ✅ Satınalma önerisi en düşük fiyat (Firma B)
- ✅ Talep karşılaştırma listesinde
- ✅ Durum: Yönetim Onayında
- ✅ TeklifOnayda yönetim rotası: teklif-onay-detay?id=fbba675e-6e0e-4233-9bd8-fed4ad029102
- ✅ Yönetim teklif-onay-detay erişimi OK
- ✅ Satınalma teklif-onay-detay (gönderilen) erişimi OK
- ✅ Durum: Onaylandı + kilitli
- ✅ Onaylandi rotası hizalı (M/A): talep-detay?id=fbba675e-6e0e-4233-9bd8-fed4ad029102&view=onaylanan
- ✅ Talep satinalma-onaylanan listesinde
- ✅ Durum: Sipariş Oluşturuldu
- ✅ Talep Android sipariş listesinde (mal kabul bekleyen)
- ✅ Talep masaüstü sipariş listesinde (mal kabul bekleyen)
- ✅ SiparisOlusturuldu rotası hizalı (M/A): talep-detay?id=fbba675e-6e0e-4233-9bd8-fed4ad029102&view=siparis
- ✅ Kalem sipariş tamamlandı
- ✅ Tamamlanan talep sipariş listesinden düştü
- ✅ Talep mal kabul tamamlanan listesinde
- ✅ Depo MalKabulEdildi bildirimi aldı
- ✅ Depo MalKabulEdildi → stok-durum (A:stok-durum, M:stok-durum)

## === SENARYO 2: Şef/Saha talep sahipliği filtresi ===


### Geçen kontroller
- ✅ Saha yalnızca kendi onaylanan talebini görüyor

## === SENARYO 3: Teklifsiz yönetim onayı ===

- Yönetim teklifsiz onayladı

### Geçen kontroller
- ✅ Teklifsiz firma/fiyat bekliyor durumu
- ✅ Firma/fiyat girildi

## === SENARYO 4: Teklif düzeltme bildirim rotası ===


### Geçen kontroller
- ✅ TeklifDuzeltmeIstendi rotası hizalı: satinalma-teklif-duzeltme?id=256af5cd-d969-4718-a8f7-5762ad030098
- ✅ Satınalma düzeltme bildirim rotasına erişebiliyor

---
## Genel Eksiklik Özeti

Kritik eksik bulunamadı — simülasyon akışı tamamlandı.

## Masaüstü vs Android — Bilinen Kalan Farklar (PDF/UX)

- Şartname editörü + sipariş PDF birleştirme (masaüstünde var, Android basit PDF)
- İmza blokları / ayarlardan imza (masaüstünde var)
- Sipariş Onay Formu çift imza PDF (masaüstünde var)
- Malzeme katalog penceresi browse-all (masaüstünde var, Android autocomplete)
- Yönetim geçmişi tek menü vs Android iki liste
- Masaüstü bildirim tıklaması `MasaustuHedef` (liste ekranı), FCM/push `HedefRoute` (detay ekranı) — bilinçli UX ayrımı

## Test Verisi

Tüm test verileri bellek içi çalıştırıldı ve `Temizle()` ile silindi. Firebase/local dosyaya yazılmadı.

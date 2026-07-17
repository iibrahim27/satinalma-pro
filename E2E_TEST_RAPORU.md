# Satınalma E2E Test Raporu
Tarih: 2026-07-17 16:10

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
- 7. Satınalma mal kabul tamamladı → status=completed

### Geçen kontroller
- ✅ Şef oturumu /topics/sef konusuna abone
- ✅ Durum submitted
- ✅ Şef tüm talepleri görüyor (liste görünürlüğü)
- ✅ Yönetim Gelen Talepler sekmesinde talebi buldu
- ✅ Talebi Onayla butonu görünür (normal öncelik)
- ✅ Teklif İste butonu görünür (normal öncelik)
- ✅ Talebi Reddet butonu görünür (normal öncelik)
- ✅ Satınalma: Talebi Onayla görünür
- ✅ Satınalma: Teklif İste görünür
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
- ✅ Talep Satınalma Teklif İnceleme & Onay sekmesinde
- ✅ Satınalma kanalına (/topics/satinalma) TeklifOnayda bildirimi gitti
- ✅ Teklifleri Revizeye Gönder butonu görünür
- ✅ Durum comparison
- ✅ Satınalma revize bildirimi aldı
- ✅ Tekrar management_quote_review
- ✅ Durum approved
- ✅ Durum ordered
- ✅ Saha/Şef kanalına sipariş bildirimi gitti
- ✅ Depo satınalma sipariş listesine erişemez
- ✅ Satınalma sipariş ve mal kabul listesinde talebi görür
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
- ✅ Atölye satınalma sipariş sekmesine erişemez

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
- ✅ Satınalma: en az 1 sekmede görünür (satinalma-talepler, yonetim-gelen-talepler)
- ✅ Şef: en az 1 sekmede görünür (satinalma-talepler, satinalma-onay-bekleyen)
- ✅ Saha: en az 1 sekmede görünür (satinalma-talepler, satinalma-onay-bekleyen)
- ✅ Yönetim: en az 1 sekmede görünür (yonetim-teklif-bekleyen)
- ✅ Satınalma: en az 1 sekmede görünür (satinalma-teklif-istenen)
- ✅ Şef: en az 1 sekmede görünür (satinalma-onay-bekleyen)
- ✅ Saha: en az 1 sekmede görünür (satinalma-onay-bekleyen)
- ✅ Satınalma: en az 1 sekmede görünür (satinalma-teklif-girilen)
- ✅ Şef: en az 1 sekmede görünür (satinalma-onay-bekleyen)
- ✅ Saha: en az 1 sekmede görünür (satinalma-onay-bekleyen)
- ✅ Satınalma: en az 1 sekmede görünür (satinalma-karsilastirma)
- ✅ Şef: en az 1 sekmede görünür (satinalma-onay-bekleyen)
- ✅ Saha: en az 1 sekmede görünür (satinalma-onay-bekleyen)
- ✅ Yönetim: en az 1 sekmede görünür (yonetim-teklif-girilen)
- ✅ Satınalma: en az 1 sekmede görünür (yonetim-teklif-girilen)
- ✅ Şef: en az 1 sekmede görünür (satinalma-onay-bekleyen)
- ✅ Saha: en az 1 sekmede görünür (satinalma-onay-bekleyen)
- ✅ Yönetim: en az 1 sekmede görünür (yonetim-onaylanan-teklifler)
- ✅ Satınalma: en az 1 sekmede görünür (satinalma-onaylanan, satinalma-onay-gecmisi)
- ✅ Şef: en az 1 sekmede görünür (satinalma-onaylanan-talepler)
- ✅ Saha: en az 1 sekmede görünür (satinalma-onaylanan-talepler)
- ✅ Yönetim: en az 1 sekmede görünür (yonetim-onaylanan-teklifler)
- ✅ Satınalma: en az 1 sekmede görünür (satinalma-onay-gecmisi, satinalma-siparis)
- ✅ Şef: en az 1 sekmede görünür (satinalma-siparis)
- ✅ Saha: en az 1 sekmede görünür (satinalma-siparis)
- ✅ Yönetim: en az 1 sekmede görünür (yonetim-red-verilen)
- ✅ Satınalma: en az 1 sekmede görünür (yonetim-red-verilen)
- ✅ Şef: en az 1 sekmede görünür (yonetim-red-verilen)
- ✅ Saha: en az 1 sekmede görünür (yonetim-red-verilen)
- ✅ Yönetim: en az 1 sekmede görünür (yonetim-onaylanan-teklifler)
- ✅ Satınalma: en az 1 sekmede görünür (satinalma-mal-kabul)
- ✅ Şef: en az 1 sekmede görünür (satinalma-mal-kabul)
- ✅ Saha: en az 1 sekmede görünür (satinalma-mal-kabul)
- ✅ submitted → yönetim Gelen Talepler
- ✅ quote_requested → satınalma Teklif İstemi
- ✅ quote_entry → satınalma Teklif Girişi Bekleyenler
- ✅ management_quote_review → yönetim Teklif İnceleme
- ✅ management_quote_review → satınalma Teklif İnceleme & Onay

## === Platform menü uyumu (Desktop vs TabFilterManager) ===


### Geçen kontroller
- ✅ Şef: tüm talepleri izleme kapsamı masaüstü ve Android'de uyumlu
- ✅ Saha: tüm talepleri izleme kapsamı masaüstü ve Android'de uyumlu
- ✅ Depo: satınalma listeleri kapalı, yalnız stok sekmeleri açık
- ✅ Atölye: yalnız stok durumu erişimi açık

## === Masaüstü rol / modül matrisi ===


### Geçen kontroller
- ✅ Yönetim: masaüstü modül kapsamı doğru
- ✅ Şef: masaüstü modül kapsamı doğru
- ✅ Satınalma: masaüstü modül kapsamı doğru
- ✅ Saha: masaüstü modül kapsamı doğru
- ✅ Depo: masaüstü modül kapsamı doğru
- ✅ Atölye: masaüstü modül kapsamı doğru
- ✅ Saha: stok durumu ve stok hareketleri açık
- ✅ Şef: stok durumu ve stok hareketleri açık
- ✅ Atölye: yalnız stok durumu açık

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
- ✅ Satınalma gelen talep bildirimini aldı (ortak karar yetkisi)
- ✅ Bildirim rotası gelen-talepler (M:gelen-talepler, A:gelen-talepler)
- ✅ Talep yönetim gelen listesinde
- ✅ Durum: Teklif Girişi
- ✅ Android TeklifIstendi → teklif-gir?id= (teklif-gir?id=0c6fe8f2-729a-41c0-8bcf-cc0cdfefa1c2)
- ✅ TeklifIstendi rotası hizalı (M/A): teklif-gir?id=0c6fe8f2-729a-41c0-8bcf-cc0cdfefa1c2
- ✅ Satınalma teklif-gir?id rotasına erişebiliyor
- ✅ Talep satınalma teklif istenen listesinde
- ✅ Teklif istemi yalnızca satınalmaya gitti
- ✅ Karşılaştırma aşamasında 2 teklif
- ✅ Satınalma önerisi en düşük fiyat (Firma B)
- ✅ Talep karşılaştırma listesinde
- ✅ Durum: Yönetim Onayında
- ✅ TeklifOnayda yönetim rotası: teklif-onay-detay?id=0c6fe8f2-729a-41c0-8bcf-cc0cdfefa1c2
- ✅ Yönetim teklif-onay-detay erişimi OK
- ✅ Teklif girişi yöneticiye gitti; işlemi yapan satınalmaya gitmedi
- ✅ Satınalma teklif-onay-detay (gönderilen) erişimi OK
- ✅ Durum: Onaylandı + kilitli
- ✅ Onay bildirimi yalnızca talep sahibine gitti
- ✅ Onaylandi rotası hizalı (M/A): talep-detay?id=0c6fe8f2-729a-41c0-8bcf-cc0cdfefa1c2
- ✅ Talep satinalma-onaylanan listesinde
- ✅ Durum: Sipariş Oluşturuldu
- ✅ Talep Android sipariş listesinde (mal kabul bekleyen)
- ✅ Talep masaüstü sipariş listesinde (mal kabul bekleyen)
- ✅ Sipariş bildirimi yalnızca talep sahibine gitti
- ✅ SiparisOlusturuldu rotası hizalı (M/A): talep-detay?id=0c6fe8f2-729a-41c0-8bcf-cc0cdfefa1c2&view=siparis
- ✅ Kalem sipariş tamamlandı
- ✅ Tamamlanan talep sipariş listesinden düştü
- ✅ Talep mal kabul tamamlanan listesinde
- ✅ Talep sahibi MalKabulEdildi bildirimini aldı
- ✅ Talep sahibi MalKabulEdildi rotası hizalı (M/A): talep-detay?id=0c6fe8f2-729a-41c0-8bcf-cc0cdfefa1c2&view=malkabul
- ✅ Red bildirimi yalnızca talep sahibine gider; işlemi yapana gönderilmez

## === SENARYO 2: Şef/Saha görünürlük ve talep sahipliği ===


### Geçen kontroller
- ✅ Saha kendi ve diğer kullanıcıların onaylanan taleplerini görüyor
- ✅ Şef/Saha yalnızca kendi talebini düzenleyip silebiliyor
- ✅ Satınalma tüm kullanıcıların taleplerini düzenleyip silebiliyor

## === SENARYO 3: Teklifsiz yönetim onayı ===

- Yönetim teklifsiz onayladı

### Geçen kontroller
- ✅ Teklifsiz firma/fiyat bekliyor durumu
- ✅ Firma/fiyat girildi

## === SENARYO 4: Teklif düzeltme bildirim rotası ===


### Geçen kontroller
- ✅ TeklifDuzeltmeIstendi rotası hizalı: satinalma-teklif-duzeltme?id=47e8c97e-891a-417a-b0db-c4121edd88d3
- ✅ Satınalma düzeltme bildirim rotasına erişebiliyor

## === SENARYO: Kalem miktarını firmalara bölerek onay (80/20) ===

- 1. Yönetim 80 Firma A + 20 Firma B onayladı
- 2. Mal kabul satırları: 2

### Geçen kontroller
- ✅ Eksik atama toplamı (70+20) reddedildi
- ✅ İki firma ataması kaydedildi
- ✅ OnaylananTeklifId = en büyük miktarlı firma (A)
- ✅ Her iki teklif Onaylandi=true
- ✅ Her firma için sipariş no üretildi
- ✅ Atama başına bir mal kabul satırı
- ✅ Firma A satırı SiparisMiktari=80
- ✅ Firma B satırı SiparisMiktari=20
- ✅ PDF/sipariş toplamı A = 80×100 = 8000
- ✅ PDF/sipariş toplamı B = 20×110 = 2200
- ✅ Eski tek OnaylananTeklifId → tam miktar ataması

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

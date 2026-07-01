# Satınalma Pro Mobil (Android)

.NET MAUI Android uygulaması — masaüstü ile aynı Firebase kullanıcıları ve Firestore verilerini kullanır.

## Projeler

| Proje | Açıklama |
|-------|----------|
| `SatinalmaPro.Shared` | Modeller, Firebase REST servisleri, iş kuralları |
| `SatinalmaPro.Mobile` | Android MAUI arayüzü |

## Rol bazlı ekranlar

- **Admin** — Talep, teklif, onay, stok (tüm modüller)
- **Yönetim** — Gelen talepler, teklif onay, stok okuma, bildirimler
- **Satınalma** — Teklif girişi, bildirimler
- **Saha / Şantiye** — Talep açma, taleplerim, stok okuma
- **Depo** — Stok giriş/çıkış, durum, hareketler

## Satınalma akışı

1. Saha/Şantiye talep oluşturur (Acil veya Normal) → **Yönetime gönder**
2. Yönetim: Acil → Onayla/Reddet | Normal → Onayla/Reddet/Teklif iste
3. Satınalma teklif girer → Yönetim teklif seçer → Onaylandı
4. Bildirimler Firestore `veri/bildirimler` belgesinde tutulur

## Kurulum (henüz yapılacak)

1. Android SDK + emulator veya fiziksel cihaz
2. `Resources/Raw/firebase_ayarlar.json` dosyasına masaüstündeki `apiKey` ve `projectId` değerlerini yazın
3. Firestore güvenlik kuralları ve FCM push (son adım)

```powershell
cd SatinalmaPro.Mobile
dotnet build -f net9.0-android
dotnet build -f net9.0-android -t:Run
```

## Firestore yolları (masaüstü ile ortak)

- `veri/satinalma_talepler`
- `veri/satinalma_ayarlar`
- `veri/stok`
- `veri/stok_hareketleri`
- `veri/bildirimler`
- `users/{uid}`

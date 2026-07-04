# METRİK Cloud Functions

TypeScript (Node 20) — bildirim fan-out, FCM dispatch, SLA, migrasyon.

## Kurulum

```powershell
cd functions
npm install
npm run build
```

## Fonksiyonlar

| Fonksiyon | Tetik | Açıklama |
|-----------|-------|----------|
| `onLegacyTalepWrite` | `veri/satinalma_talepler` | Durum değişiminde fan-out + dual-write |
| `onNotificationDispatchCreate` | `notification_dispatch_queue` | FCM gönderimi |
| `checkApprovalSla` | Her 60 dk | Onay SLA uyarı/kritik |
| `cleanupTempStorage` | Günlük | `temp/` dosya temizliği |
| `dailyDigest` | 08:00 | Okunmamış özet |
| `seedNotificationTemplates` | Callable | Şablon seed |
| `migrateLegacyBatch` | Callable | Legacy → enterprise |
| `manualFanOut` | Callable | Test fan-out |
| `markInboxRead` | Callable | Inbox okundu |

Deploy: `docs/FIREBASE_DEPLOY.md`

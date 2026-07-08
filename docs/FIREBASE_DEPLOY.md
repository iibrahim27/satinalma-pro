# Firebase Deploy Rehberi

## Hızlı deploy (Windows)

Proje kökünde PowerShell:

```powershell
.\deploy-firebase.ps1
```

Betik önce mevcut Firebase oturumunu dener; yoksa `Satinalma Pro\fcm-service-account.json` ile service account kullanır (Cursor/CI gibi etkileşimsiz ortamlar için). Service account yoksa ve terminal etkileşimliyse `firebase login` açılır.

Alternatif: Service account ile yalnızca kurallar (IAM'de Firebase Rules Admin gerekir):

```powershell
$env:GOOGLE_APPLICATION_CREDENTIALS = "Satinalma Pro\fcm-service-account.json"
cd functions
node scripts/deploy-rules.mjs
```

## Ön koşullar

1. [Firebase CLI](https://firebase.google.com/docs/cli): `npm install -g firebase-tools`
2. Kimlik: `Satinalma Pro\fcm-service-account.json` (önerilen) veya `firebase login`
3. Proje ID: `Satinalma Pro/firebase_ayarlar.json` → `projectId`

## İlk kurulum

```powershell
cd "c:\Users\pekba\OneDrive\Desktop\METRİK"
copy .firebaserc.example .firebaserc
# .firebaserc içinde BURAYA_PROJE_ID → gerçek projectId yazın

cd functions
npm install
cd ..
```

## Deploy komutları

```powershell
# Yalnızca kurallar ve indeksler (üretim öncesi test önerilir)
firebase deploy --only firestore:rules,firestore:indexes,storage

# Cloud Functions (Node 24)
firebase deploy --only functions

# Tam deploy
firebase deploy --only firestore:rules,firestore:indexes,storage,functions
```

## Emulator (geliştirme)

```powershell
firebase emulators:start --only firestore,functions,storage
```

## Cloud Functions ortam değişkenleri

Firebase Console → Functions → Environment variables:

| Değişken | Varsayılan | Açıklama |
|----------|------------|----------|
| `MIGRATION_DUAL_WRITE` | `true` | Enterprise ↔ legacy çift yazma (aktif) |
| `SLA_ACIL_HOURS` | `4` | Acil talep SLA |
| `SLA_APPROVAL_WARN_HOURS` | `24` | Onay uyarı |
| `SLA_APPROVAL_CRIT_HOURS` | `48` | Onay kritik |

## Service Account (FCM v1)

1. Firebase Console → Project Settings → Service accounts → Generate key
2. WPF: `firebase-service-account.json` (gitignore'da)
3. Functions: otomatik `admin.initializeApp()` kullanır

## Doğrulama

Deploy sonrası:

1. Firestore Rules Playground — Saha kullanıcısı `quotes` okuyamaz
2. Functions log: `firebase functions:log`
3. WPF giriş → bildirim inbox yüklenir

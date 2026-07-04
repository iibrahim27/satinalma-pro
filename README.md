# Satınalma Pro — METRİK

Kurumsal satınalma, stok ve saha yönetim platformu.

## Projeler

| Klasör | Açıklama |
|--------|----------|
| `Satinalma Pro/` | Windows masaüstü (WPF) |
| `SatinalmaPro.Mobile/` | Tek Android APK (.NET MAUI, rol bazlı) |
| `SatinalmaPro.Shared/` | Ortak modeller ve Firebase servisleri |
| `functions/` | Firebase Cloud Functions |
| `docs/` | Mimari dokümantasyon |

## Android — Tek APK

Tüm roller tek uygulamada; giriş sonrası role göre menü açılır.

```powershell
.\APK-Olustur.bat
```

## Masaüstü

```powershell
.\DerleVeCalistir.ps1
```

## Firebase

```powershell
.\deploy-firebase.ps1
```

# METRİK ERP — Firebase Storage Security Rules

> **Sürüm:** 1.0 · 2026-07-03  
> **Kaynak dosya:** [`storage.rules`](../storage.rules)

---

## 1. Storage Mimarisi

```
gs://{project-id}.appspot.com/
├── procurement/{requestId}/          ← Talep dosyaları, fotoğraflar, teklif PDF
├── orders/{orderId}/                 ← Sipariş PDF, irsaliye, fatura
├── returns/{returnId}/               ← İade fotoğrafları
├── stock/receipts/{receiptId}/       ← Mal kabul belgeleri
├── temp/{uid}/{uploadId}/            ← Geçici (24h TTL — CF temizler)
├── system/                           ← Logo, imza, duyuru (Admin only write)
├── legacy/                           ← Mevcut medya migrasyonu
└── users/{uid}/avatar/               ← Profil fotoğrafı
```

---

## 2. Rol × Path Erişim Matrisi

| Path | Read | Create | Update | Delete |
|------|:----:|:------:|:------:|:------:|
| `procurement/**` | Tüm aktif roller* | Şef, Saha, Atölye, Satınalma, Admin | Satınalma, Admin | Satınalma, Admin |
| `orders/**` | Satınalma, Depo, Yönetim, Admin | Depo, Satınalma, Admin | Depo, Satınalma, Admin | Satınalma, Admin |
| `returns/**` | Satınalma, Depo, Yönetim, Admin | Depo, Satınalma, Admin | — | Satınalma, Admin |
| `stock/receipts/**` | Satınalma, Depo, Yönetim, Admin | Depo, Satınalma, Admin | — | Admin |
| `temp/{uid}/**` | Owner | Owner | — | Owner, Admin |
| `system/**` | Tüm aktif | — | — | Admin |
| `legacy/**` | Tüm aktif | Satınalma, Admin | — | Admin |
| `users/{uid}/avatar/**` | Tüm aktif | Owner | Owner | Owner, Admin |

\* Teklif PDF'leri: Storage Rules path bazlı; teklif fiyat gizliliği uygulama katmanında `quotes/` alt path ile ayrılabilir.

---

## 3. Upload Kısıtlamaları

| Kural | Değer |
|-------|-------|
| Max dosya boyutu (belge) | 25 MB |
| Max dosya boyutu (resim) | 10 MB |
| İzin verilen MIME | PDF, XLSX, XLS, DOC, DOCX, JPEG, PNG, WebP |
| Metadata zorunlu | `uploadedBy` = request.auth.uid |
| Metadata önerilen | `entityType`, `entityId`, `fileCategory` |

### 3.1 Örnek Upload Metadata (Client)

```json
{
  "uploadedBy": "firebase-uid",
  "entityType": "talep",
  "entityId": "uuid",
  "fileCategory": "Teklif"
}
```

---

## 4. Güvenlik Kuralları Özeti

### 4.1 Talep Dosyası Yükleme

- **Kim:** Talep sahibi (Şef/Saha/Atölye) veya Satınalma
- **Ne:** PDF, Excel, resim (teklif, şartname, saha fotoğrafı)
- **Path:** `procurement/{requestId}/attachments/{fileId}_{name}`

### 4.2 Sipariş Belgesi

- **Kim:** Depo (irsaliye/fatura), Satınalma (sipariş PDF)
- **Path:** `orders/{orderId}/pdf/`, `orders/{orderId}/irsaliye/`

### 4.3 Geçici Yükleme Akışı

```
1. Client → temp/{uid}/{uploadId}/file.pdf (upload)
2. Client → Firestore attachment metadata CREATE
3. Cloud Function → temp/ → procurement/ MOVE (copy + delete)
4. Scheduled cleanup → 24h+ temp/ DELETE
```

---

## 5. Teklif Gizliliği (Path Ayrımı)

Önerilen path yapısı:

```
procurement/{requestId}/
├── attachments/          ← Herkes (talep sahibi) okuyabilir
├── photos/               ← Saha fotoğrafları
└── quotes/{quoteId}/     ← YALNIZCA Satınalma, Yönetim, Admin
```

`storage.rules` genişletmesi:

```
match /procurement/{requestId}/quotes/{quoteId}/{fileName} {
  allow read: if isProcurement() || isManagement();
  allow write: if isProcurement() || isAdmin();
}
```

---

## 6. Deploy

```bash
firebase deploy --only storage
```

Tam deploy (Firestore + Storage + Functions):

```bash
firebase deploy --only firestore:rules,firestore:indexes,storage,functions
```

---

## 7. Test Senaryoları

| # | Senaryo | Beklenen |
|---|---------|----------|
| 1 | Saha kullanıcısı talep PDF yükler | ALLOW |
| 2 | Saha kullanıcısı quotes/ alt path okur | DENY (path ayrımı ile) |
| 3 | Depo irsaliye yükler | ALLOW |
| 4 | Atölye sipariş PDF yükler | DENY |
| 5 | Kullanıcı başka uid temp/ yükler | DENY |
| 6 | 30 MB dosya yükleme | DENY (size) |
| 7 | .exe dosya yükleme | DENY (contentType) |
| 8 | Admin system/logo değiştirir | ALLOW |
| 9 | metadata.uploadedBy ≠ auth.uid | DENY |
| 10 | Pasif kullanıcı upload | DENY (isActiveUser) |

---

*İlgili: [FIRESTORE_SECURITY_RULES.md](./FIRESTORE_SECURITY_RULES.md) · [FIRESTORE_DATABASE_DESIGN.md §10](./FIRESTORE_DATABASE_DESIGN.md)*

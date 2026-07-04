# METRİK ERP — Enterprise Firebase Security Rules

> **Sürüm:** 1.0 · 2026-07-03  
> **Kaynak dosya:** [`firestore.rules`](../firestore.rules)  
> **İndeksler:** [`firestore.indexes.json`](../firestore.indexes.json)

---

## 1. Güvenlik Mimarisi

```
┌─────────────────────────────────────────────────────────────┐
│                    Firebase Authentication                   │
│              JWT token → request.auth.uid                  │
└────────────────────────────┬────────────────────────────────┘
                             │
┌────────────────────────────▼────────────────────────────────┐
│                  Firestore Security Rules                    │
│  • isAuthenticated()                                         │
│  • users/{uid}.rol → RBAC helper functions                   │
│  • Resource-level ownership (requesterUid, assignedToUid)     │
│  • Field-level update restrictions (onlyChangedFields)       │
└────────────────────────────┬────────────────────────────────┘
                             │
         ┌───────────────────┼───────────────────┐
         ▼                   ▼                   ▼
   Client Direct         Cloud Functions      Admin SDK
   (Android/WPF)        (notification fan-out)  (batch jobs)
```

---

## 2. Rol Hiyerarşisi

| Rol | Kod | Güvenlik Grubu |
|-----|-----|----------------|
| Admin | `Admin` | Full access |
| Yönetim | `Yönetim` | Approval read; limited write |
| Satınalma | `Satınalma` | Procurement full ops |
| Depo | `Depo` | Stock + delivery write |
| Şef | `Şef` | Own requests + read stock |
| Saha | `Saha` | Own requests + read stock |
| Atölye | `Atölye` | Read stock only; create request |

---

## 3. Koleksiyon Bazlı Erişim Matrisi

| Koleksiyon | Admin | Yönetim | Satınalma | Depo | Şef/Saha/Atölye |
|------------|:-----:|:-------:|:---------:|:----:|:---------------:|
| `users` | CRUD | R (field) | R (self) | R (self) | R (self) |
| `users/.../notification_inbox` | R | R (own) | R/U (own) | R/U (own) | R/U (own) |
| `procurement_requests` | CRUD | R | CRUD | R | CRU (own, pre-approval) |
| `.../quotes` | CRUD | R | CRUD | — | — |
| `orders` | CRUD | R | CRUD | R | — |
| `.../deliveries` | CRUD | — | CRU | CRU | — |
| `returns` | CRUD | R | CRUD | R | — |
| `tasks` | CRUD | — | CRUD | — | R/U (assigned) |
| `suppliers` | CRUD | R | CRUD | — | — |
| `sites`, `materials`, `categories`, `units` | CRUD | R | R/W (materials) | R | R |
| `stock_items` | CRUD | R | CRUD | CRUD | R |
| `stock_movements` | CRUD | R | C | C | R |
| `goods_receipts` | CRUD | R | CRU | CRU | — |
| `notifications` | R | R* | R* | R* | R* |
| `notification_queue` | — | — | — | — | — (CF only) |
| `device_tokens` | R/D | CRUD (own) | CRUD (own) | CRUD (own) | CRUD (own) |
| `announcements` | CRUD | R | R | R | R |
| `system_audit_log` | R | — | C | C | C |
| `system_config` | CRUD | R | R | R | R |
| `veri/*` (legacy) | RW | RW | RW | RW | RW |

\* Yalnızca `targetUids` veya `targetRoles` eşleşmesi

---

## 4. Kritik Güvenlik Kuralları

### 4.1 Bildirim İzolasyonu

```javascript
// Kullanıcı YALNIZCA kendi inbox'ını görür
match /users/{uid}/notification_inbox/{notificationId} {
  allow read: if isOwner(uid);
}
```

- Başka kullanıcının bildirimi **asla** okunamaz
- Master `notifications` koleksiyonu: yalnızca hedef UID veya rol eşleşmesi
- Fan-out yazma: **yalnızca Cloud Function** (`isCloudFunction()`)

### 4.2 Teklif Gizliliği

```javascript
match /procurement_requests/{requestId}/quotes/{quoteId} {
  allow read: if isAdmin() || isManagement() || isProcurement();
}
```

- Şef, Saha, Atölye, Depo teklif fiyatlarını **göremez**
- FCM payload'da fiyat bilgisi Saha rollerine gönderilmemeli (Cloud Function sorumluluğu)

### 4.3 Talep Sahipliği

```javascript
// Sahiplik: yalnızca Taslak/Hazırlanıyor + onay kilitli değil
allow update: if resource.data.requesterUid == request.auth.uid
  && resource.data.status in ['Taslak', 'Hazırlanıyor']
  && !resource.data.managementApprovalLocked;
```

### 4.4 Immutable Audit

```javascript
match /audit_trail/{auditId} {
  allow create: if isAuthenticated();
  allow update, delete: if false;
}
```

### 4.5 Field-Level Update Kısıtı

Kullanıcı profili güncellemesi yalnızca:
- `fcmToken`
- `adSoyad`

Inbox güncellemesi yalnızca:
- `isRead`, `readAt`, `isArchived`, `archivedAt`, `isStarred`, `actedAt`, `dismissedAt`, `localState`

---

## 5. Cloud Function Custom Claim

Cloud Function service account için önerilen custom claim:

```javascript
// Admin SDK ile function identity
admin.auth().setCustomUserClaims(serviceAccountUid, { admin: true });
```

Rules'da:
```javascript
function isCloudFunction() {
  return request.auth.token.admin == true;
}
```

---

## 6. Firebase Storage Security Rules

Tam dokümantasyon: **[STORAGE_SECURITY_RULES.md](./STORAGE_SECURITY_RULES.md)**  
Kaynak dosya: [`storage.rules`](../storage.rules)

Özet kurallar aşağıda; teklif gizliliği için `procurement/{id}/quotes/` path ayrımı uygulanır.

```
rules_version = '2';
service firebase.storage {
  match /b/{bucket}/o {
    function isAuth() { return request.auth != null; }
    function isAdmin() {
      return firestore.get(/databases/(default)/documents/users/$(request.auth.uid)).data.rol == 'Admin';
    }
    function canProcurement() {
      let rol = firestore.get(/databases/(default)/documents/users/$(request.auth.uid)).data.rol;
      return rol in ['Admin', 'Satınalma'];
    }

    match /procurement/{requestId}/{allPaths=**} {
      allow read: if isAuth();
      allow write: if isAuth() && (canProcurement() || isAdmin());
    }
    match /orders/{orderId}/{allPaths=**} {
      allow read: if isAuth();
      allow write: if isAuth() && canProcurement();
    }
    match /temp/{uid}/{allPaths=**} {
      allow read, write: if isAuth() && request.auth.uid == uid;
    }
    match /system/{allPaths=**} {
      allow read: if isAuth();
      allow write: if isAdmin();
    }
  }
}
```

---

## 7. Deploy

```bash
firebase deploy --only firestore:rules,firestore:indexes
```

---

## 8. Test Senaryoları (Rules Unit Test)

| # | Senaryo | Beklenen |
|---|---------|----------|
| 1 | Saha kullanıcısı başka kullanıcının inbox'ını okur | DENY |
| 2 | Satınalma teklif ekler | ALLOW |
| 3 | Saha teklif okur | DENY |
| 4 | Depo mal kabul delivery oluşturur | ALLOW |
| 5 | Saha stok çıkışı yapar | DENY |
| 6 | Yönetim talep onaylar (update) | DENY (client; CF veya özel endpoint) |
| 7 | Admin system_config yazar | ALLOW |
| 8 | Client notifications master yazar | DENY |
| 9 | Cloud Function inbox fan-out yazar | ALLOW |
| 10 | Kullanıcı başka UID device_token yazar | DENY |

---

*İlgili: [FIRESTORE_DATABASE_DESIGN.md](./FIRESTORE_DATABASE_DESIGN.md)*

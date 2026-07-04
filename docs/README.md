# METRİK ERP — Enterprise Architecture Documentation

> SAP · Oracle · Dynamics · Logo Tiger · Netsis seviyesinde kurumsal ERP mimari dokümantasyonu

---

## Doküman Seti

| # | Doküman | İçerik |
|---|---------|--------|
| 1 | **[FIRESTORE_DATABASE_DESIGN.md](./FIRESTORE_DATABASE_DESIGN.md)** | Tüm koleksiyonlar, alt koleksiyonlar, alan şemaları, Storage, indeksler, migrasyon |
| 2 | **[FIRESTORE_SECURITY_RULES.md](./FIRESTORE_SECURITY_RULES.md)** | Rol bazlı güvenlik kuralları, erişim matrisi, test senaryoları |
| 3 | **[ERP_WORKFLOW_COMPLETE.md](./ERP_WORKFLOW_COMPLETE.md)** | BPMN 2.0, RBAC, State Machine, ERD, Notification Matrix, Navigation Matrix, API/Data Flow |
| 4 | [BILDIRIM_NAVIGASYON_MIMARISI.md](./BILDIRIM_NAVIGASYON_MIMARISI.md) | Bildirim & navigasyon mimarisi (60+ olay, FCM, lifecycle) |
| 5 | [SISTEM_DOKUMANTASYONU.md](./SISTEM_DOKUMANTASYONU.md) | Sistem özeti v1 (legacy şema) |
| 6 | **[CLOUD_FUNCTIONS_SPEC.md](./CLOUD_FUNCTIONS_SPEC.md)** | Cloud Functions pseudo-spec (fan-out, SLA, migrasyon) |
| 7 | **[STORAGE_SECURITY_RULES.md](./STORAGE_SECURITY_RULES.md)** | Firebase Storage güvenlik kuralları |

---

## Deploy Dosyaları

| Dosya | Açıklama |
|-------|----------|
| [`../firestore.rules`](../firestore.rules) | Enterprise Firebase Security Rules |
| [`../firestore.indexes.json`](../firestore.indexes.json) | 26 composite index tanımı |
| [`../storage.rules`](../storage.rules) | Firebase Storage Security Rules |
| [`../firebase.json`](../firebase.json) | Firebase deploy yapılandırması |

```bash
firebase deploy --only firestore:rules,firestore:indexes,storage
# Functions (implementasyon sonrası):
firebase deploy --only functions
```

---

## Mimari Özet

```
Legacy (Mevcut)                    Enterprise (Hedef)
─────────────────                  ──────────────────
veri/satinalma_talepler (JSON)  →  procurement_requests/{id}
                                   ├── line_items/
                                   ├── quotes/
                                   ├── comments/
                                   ├── attachments/
                                   └── audit_trail/
veri/bildirimler (JSON)         →  notifications/ + users/{uid}/notification_inbox/
veri/stok (JSON)                →  stock_items/ + stock_movements/
users/{uid}                     →  users/{uid} (genişletilmiş)
```

---

## Hızlı Referans

| Konu | Bölüm |
|------|-------|
| Talep durumları | ERP_WORKFLOW §4 |
| Rol yetkileri | ERP_WORKFLOW §3 |
| Bildirim olayları | BILDIRIM_NAVIGASYON §2 |
| Deep link formatı | ERP_WORKFLOW §7 |
| Firestore koleksiyonları | FIRESTORE_DATABASE §2 |
| Security Rules (Firestore) | firestore.rules |
| Security Rules (Storage) | storage.rules |
| Cloud Functions | CLOUD_FUNCTIONS_SPEC |
| SLA & escalation | CLOUD_FUNCTIONS_SPEC §4 |

---

*Son güncelleme: 2026-07-03*

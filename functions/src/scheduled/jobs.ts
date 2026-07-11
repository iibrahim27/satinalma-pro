import { onSchedule } from "firebase-functions/v2/scheduler";
import * as admin from "firebase-admin";
import { tenantUsersPath } from "../lib/saas";

function toDate(value: unknown): Date | null {
  if (!value) return null;
  if (value instanceof Date) return value;
  if (value instanceof admin.firestore.Timestamp) return value.toDate();
  if (typeof value === "string" || typeof value === "number") {
    const d = new Date(value);
    return Number.isNaN(d.getTime()) ? null : d;
  }
  return null;
}

function kalanGun(bitis: Date | null): number | null {
  if (!bitis) return null;
  return Math.ceil((bitis.getTime() - Date.now()) / 86400000);
}

/**
 * Tüm kiracıları tarar; süresi dolmuşları pasife alır.
 * Hatırlatma / SLA / günlük özet push YOK — yalnızca lisans.
 */
export const checkTenantLicenses = onSchedule("every 6 hours", async () => {
  const snap = await admin.firestore().collection("tenants").get();
  let expired = 0;

  for (const doc of snap.docs) {
    const data = doc.data() as {
      aktif?: boolean;
      lisansBitis?: admin.firestore.Timestamp | string | Date | null;
      lisansSuresiDoldu?: boolean;
    };
    if (data.aktif === false) continue;

    const bitis = toDate(data.lisansBitis);
    const kalan = kalanGun(bitis);
    if (bitis === null || kalan === null || kalan > 0) continue;

    await doc.ref.set(
      {
        aktif: false,
        lisansSuresiDoldu: true,
        guncelleme: admin.firestore.FieldValue.serverTimestamp(),
      },
      { merge: true }
    );

    const users = await admin
      .firestore()
      .collection(tenantUsersPath(doc.id))
      .where("aktif", "==", true)
      .get();
    if (!users.empty) {
      const batch = admin.firestore().batch();
      for (const u of users.docs) {
        batch.set(u.ref, { aktif: false, lisansPasifeAlindi: true }, { merge: true });
      }
      await batch.commit();
    }
    expired++;
  }

  console.log(`checkTenantLicenses: ${expired} kiracı pasife alındı`);
});

/** Depolama temizliği — bildirim göndermez. */
export const cleanupTempStorage = onSchedule("every 24 hours", async () => {
  const bucket = admin.storage().bucket();
  const [files] = await bucket.getFiles({ prefix: "temp/" });
  const cutoff = Date.now() - 7 * 24 * 3600000;

  for (const file of files) {
    const meta = file.metadata;
    const created = meta.timeCreated ? new Date(meta.timeCreated).getTime() : 0;
    if (created > 0 && created < cutoff) {
      await file.delete().catch(() => undefined);
    }
  }
});

/**
 * Eski SLA / günlük özet hatırlatmaları kapatıldı.
 * Gece/sabah telefona "hatırlatma" push gitmesin.
 * Export isimleri deploy uyumluluğu için korunur; gövde no-op.
 */
export const checkApprovalSla = onSchedule("every 60 minutes", async () => {
  console.log("checkApprovalSla: disabled (no reminder pushes)");
});

export const dailyDigest = onSchedule("0 8 * * *", async () => {
  console.log("dailyDigest: disabled (no reminder pushes)");
});

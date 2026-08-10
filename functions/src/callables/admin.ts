import { onCall, HttpsError } from "firebase-functions/v2/https";
import * as admin from "firebase-admin";
import { fanOutNotification } from "../lib/fanOut";
import { dualWriteToEnterprise, parseLegacyTalepler } from "../lib/legacyTalep";
import { tenantUsersPath } from "../lib/saas";
import { NOTIFICATION_TEMPLATES } from "../lib/templates";

export const seedNotificationTemplates = onCall(async (request) => {
  if (!request.auth) throw new HttpsError("unauthenticated", "Giriş gerekli");
  const batch = admin.firestore().batch();
  for (const [code, tpl] of Object.entries(NOTIFICATION_TEMPLATES)) {
    batch.set(admin.firestore().collection("notification_templates").doc(code), tpl);
  }
  await batch.commit();
  return { count: Object.keys(NOTIFICATION_TEMPLATES).length };
});

/**
 * Legacy → enterprise dual-write.
 * tenantId verilirse o kiracı; yoksa tüm kiracıların satinalma_talepler belgeleri.
 */
export const migrateLegacyBatch = onCall(async (request) => {
  if (!request.auth) throw new HttpsError("unauthenticated", "Giriş gerekli");
  const limit = (request.data?.limit as number) ?? 50;
  const tenantId = ((request.data?.tenantId as string) ?? "").trim();
  const db = admin.firestore();

  const tenantIds: string[] = [];
  if (tenantId) {
    tenantIds.push(tenantId);
  } else {
    const snap = await db.collection("tenants").select().get();
    for (const d of snap.docs) tenantIds.push(d.id);
    // Eski kök belge (SaaS öncesi) — varsa ekle
    const root = await db.doc("veri/satinalma_talepler").get();
    if (root.exists) tenantIds.push("");
  }

  let migrated = 0;
  for (const tid of tenantIds) {
    if (migrated >= limit) break;
    const path = tid
      ? `tenants/${tid}/veri/satinalma_talepler`
      : "veri/satinalma_talepler";
    const doc = await db.doc(path).get();
    if (!doc.exists) continue;
    const talepler = parseLegacyTalepler(doc.data()?.json as string | undefined);
    for (const t of talepler) {
      if (migrated >= limit) break;
      await dualWriteToEnterprise(t, tid || undefined);
      migrated++;
    }
  }
  return { migrated, tenantsScanned: tenantIds.length };
});

export const manualFanOut = onCall(async (request) => {
  if (!request.auth) throw new HttpsError("unauthenticated", "Giriş gerekli");
  const { eventCode, entityId, talepNo, talepEdenUid, saha } = request.data ?? {};
  const tenantId =
    (request.auth.token.tenantId as string | undefined)?.trim() ||
    (request.data?.tenantId as string | undefined)?.trim();
  if (!eventCode || !entityId) {
    throw new HttpsError("invalid-argument", "eventCode ve entityId gerekli");
  }
  if (!tenantId) {
    throw new HttpsError("failed-precondition", "tenantId gerekli");
  }

  const count = await fanOutNotification({
    tenantId,
    eventCode,
    entityType: "procurement_request",
    entityId,
    talepNo,
    talepEdenUid,
    saha,
  });
  return { recipients: count };
});

export const markInboxRead = onCall(async (request) => {
  if (!request.auth) throw new HttpsError("unauthenticated", "Giriş gerekli");
  const inboxId = request.data?.inboxId as string;
  const tenantId = (request.auth.token.tenantId as string | undefined)?.trim();
  if (!inboxId) throw new HttpsError("invalid-argument", "inboxId gerekli");
  if (!tenantId) throw new HttpsError("failed-precondition", "tenantId gerekli");

  const ref = admin
    .firestore()
    .collection(tenantUsersPath(tenantId))
    .doc(request.auth.uid)
    .collection("notification_inbox")
    .doc(inboxId);

  await ref.update({ isRead: true, readAt: admin.firestore.FieldValue.serverTimestamp() });
  return { ok: true };
});

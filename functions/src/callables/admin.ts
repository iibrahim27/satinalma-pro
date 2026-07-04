import { onCall, HttpsError } from "firebase-functions/v2/https";
import * as admin from "firebase-admin";
import { fanOutNotification } from "../lib/fanOut";
import { dualWriteToEnterprise, parseLegacyTalepler } from "../lib/legacyTalep";
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

export const migrateLegacyBatch = onCall(async (request) => {
  if (!request.auth) throw new HttpsError("unauthenticated", "Giriş gerekli");
  const limit = (request.data?.limit as number) ?? 50;

  const doc = await admin.firestore().doc("veri/satinalma_talepler").get();
  const talepler = parseLegacyTalepler(doc.data()?.json as string | undefined).slice(0, limit);
  let migrated = 0;

  for (const t of talepler) {
    await dualWriteToEnterprise(t);
    migrated++;
  }
  return { migrated };
});

export const manualFanOut = onCall(async (request) => {
  if (!request.auth) throw new HttpsError("unauthenticated", "Giriş gerekli");
  const { eventCode, entityId, talepNo, talepEdenUid, saha } = request.data ?? {};
  if (!eventCode || !entityId) {
    throw new HttpsError("invalid-argument", "eventCode ve entityId gerekli");
  }

  const count = await fanOutNotification({
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
  if (!inboxId) throw new HttpsError("invalid-argument", "inboxId gerekli");

  const ref = admin
    .firestore()
    .collection("users")
    .doc(request.auth.uid)
    .collection("notification_inbox")
    .doc(inboxId);

  await ref.update({ isRead: true, readAt: admin.firestore.FieldValue.serverTimestamp() });
  return { ok: true };
});

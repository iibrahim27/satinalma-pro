import { onSchedule } from "firebase-functions/v2/scheduler";
import * as admin from "firebase-admin";
import { fanOutNotification } from "../lib/fanOut";
import { parseLegacyTalepler } from "../lib/legacyTalep";

const WARN_H = Number(process.env.SLA_APPROVAL_WARN_HOURS ?? "24");
const CRIT_H = Number(process.env.SLA_APPROVAL_CRIT_HOURS ?? "48");

export const checkApprovalSla = onSchedule("every 60 minutes", async () => {
  const doc = await admin.firestore().doc("veri/satinalma_talepler").get();
  const talepler = parseLegacyTalepler(doc.data()?.json as string | undefined);
  const now = Date.now();

  for (const t of talepler) {
    if (!t.id || t.durum !== "Yönetim Onayında") continue;

    const refDate = t.guncellemeTarihi ?? t.olusturmaTarihi;
    if (!refDate) continue;
    const hours = (now - new Date(refDate).getTime()) / 3600000;

    const slaDoc = await admin
      .firestore()
      .collection("sla_tracking")
      .doc(`approval_${t.id}`)
      .get();
    const lastLevel = slaDoc.data()?.lastLevel as string | undefined;

    if (hours >= CRIT_H && lastLevel !== "critical") {
      await fanOutNotification({
        eventCode: "talep.sla_asildi",
        entityType: "procurement_request",
        entityId: t.id,
        talepNo: t.talepNo,
        saha: t.saha,
      });
      await slaDoc.ref.set({ lastLevel: "critical", checkedAt: new Date() }, { merge: true });
    } else if (hours >= WARN_H && lastLevel !== "warn" && lastLevel !== "critical") {
      await fanOutNotification({
        eventCode: "talep.sla_yaklasiyor",
        entityType: "procurement_request",
        entityId: t.id,
        talepNo: t.talepNo,
        saha: t.saha,
      });
      await slaDoc.ref.set({ lastLevel: "warn", checkedAt: new Date() }, { merge: true });
    }
  }
});

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

export const dailyDigest = onSchedule("0 8 * * *", async () => {
  const users = await admin.firestore().collection("users").where("aktif", "==", true).get();
  const yesterday = new Date(Date.now() - 86400000);

  for (const user of users.docs) {
    const inbox = await user.ref
      .collection("notification_inbox")
      .where("createdAt", ">=", yesterday)
      .where("isRead", "==", false)
      .limit(20)
      .get();

    if (inbox.empty) continue;

    const count = inbox.size;
    await admin.firestore().collection("notification_dispatch_queue").add({
      uid: user.id,
      title: "Günlük Özet",
      body: `${count} okunmamış bildiriminiz var`,
      data: {
        module: "system",
        screen: "inbox",
        action: "open",
        entityType: "inbox",
        entityId: user.id,
        deepLink: "metrik://system/inbox",
        eventCode: "system.daily_digest",
      },
      status: "pending",
      createdAt: new Date(),
    });
  }
});

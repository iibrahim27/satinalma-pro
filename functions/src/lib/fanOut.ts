import * as admin from "firebase-admin";
import { buildDeepLinkUri, buildDesktopRoute, DeepLinkParams } from "./deepLink";
import {
  interpolate,
  LEGACY_TIP_TO_EVENT,
  NOTIFICATION_TEMPLATES,
  NotificationTemplate,
} from "./templates";
import { tenantUsersPath } from "./saas";

function eventCodeToLegacyTip(eventCode: string): string {
  const found = Object.entries(LEGACY_TIP_TO_EVENT).find(([, event]) => event === eventCode);
  return found?.[0] ?? eventCode;
}

export interface FanOutContext {
  tenantId: string;
  eventCode: string;
  entityType: string;
  entityId: string;
  talepNo?: string;
  talepEden?: string;
  talepEdenUid?: string;
  /** İşlemi yapan — kendi bildiriminin alıcısı olmaz. */
  createdBy?: string;
  siparisNo?: string;
  saha?: string;
  extraVars?: Record<string, string>;
}

const db = () => admin.firestore();

export async function resolveTargetUids(
  template: NotificationTemplate,
  ctx: FanOutContext
): Promise<string[]> {
  const uids = new Set<string>();
  const actor = (ctx.createdBy ?? "").trim();

  if (ctx.talepEdenUid && template.eventCode === "talep.reddedildi") {
    if (!actor || ctx.talepEdenUid !== actor) uids.add(ctx.talepEdenUid);
  }

  const roles = template.targetRoles;
  if (roles.length === 0 && ctx.talepEdenUid) {
    if (!actor || ctx.talepEdenUid !== actor) uids.add(ctx.talepEdenUid);
    return [...uids].filter((u) => u !== actor);
  }

  let q: admin.firestore.Query = db()
    .collection(tenantUsersPath(ctx.tenantId))
    .where("aktif", "==", true);
  if (roles.length > 0) {
    q = q.where("rol", "in", roles.slice(0, 10));
  }
  const snap = await q.get();
  for (const doc of snap.docs) {
    if (actor && doc.id === actor) continue;
    const data = doc.data();
    if (ctx.saha && data.saha && data.saha !== ctx.saha && data.rol === "Saha") {
      continue;
    }
    uids.add(doc.id);
  }

  return [...uids];
}

/**
 * Stabil inbox id: aynı işlem aynı kişiye bir kez.
 * Okunmuşsa yeniden yazılmaz / FCM kuyruğuna eklenmez.
 */
export async function fanOutNotification(ctx: FanOutContext): Promise<number> {
  if (!ctx.tenantId?.trim()) {
    console.warn("fanOutNotification: tenantId eksik, atlandı");
    return 0;
  }

  const template = NOTIFICATION_TEMPLATES[ctx.eventCode];
  if (!template || !template.enabled) return 0;

  const vars: Record<string, string> = {
    talepNo: ctx.talepNo ?? "",
    talepEden: ctx.talepEden ?? "",
    siparisNo: ctx.siparisNo ?? "",
    ...ctx.extraVars,
  };

  const title = interpolate(template.titleTemplate, vars);
  const message = interpolate(template.messageTemplate, vars);

  const linkParams: DeepLinkParams = {
    module: template.module,
    screen: template.screen,
    action: template.action,
    entityType: ctx.entityType,
    entityId: ctx.entityId,
    eventCode: ctx.eventCode,
  };

  const deepLink = buildDeepLinkUri(linkParams);
  const desktopRoute = buildDesktopRoute(linkParams);
  const uids = await resolveTargetUids(template, ctx);
  if (uids.length === 0) return 0;

  const now = admin.firestore.FieldValue.serverTimestamp();
  const legacyTip = eventCodeToLegacyTip(ctx.eventCode);
  let written = 0;

  for (const uid of uids) {
    const inboxId = `${ctx.eventCode}_${ctx.entityId}`.replace(/[^\w.-]+/g, "_").slice(0, 700);
    const inboxCol = db()
      .collection(tenantUsersPath(ctx.tenantId))
      .doc(uid)
      .collection("notification_inbox");
    const inboxRef = inboxCol.doc(inboxId);

    const existing = await inboxRef.get();
    // Aynı işlem aynı kişiye bir kez; okunmuş/mevcut → tekrar push yok.
    if (existing.exists) continue;

    // İstemci farklı docId ile yazmış olabilir — tip+talep ile de tekilleştir.
    try {
      const dupSnap = await inboxCol
        .where("talepId", "==", ctx.entityId)
        .where("tip", "==", legacyTip)
        .limit(1)
        .get();
      if (!dupSnap.empty) continue;
    } catch (err) {
      // Index yoksa yalnızca docId dedupe ile devam et.
      console.warn("fanOut tip+talepId dedupe sorgusu atlandı", err);
    }

    await inboxRef.set(
      {
        eventCode: ctx.eventCode,
        category: template.category,
        tip: legacyTip,
        type: template.type,
        priority: template.defaultPriority,
        title,
        baslik: title,
        message,
        mesaj: message,
        entityType: ctx.entityType,
        entityId: ctx.entityId,
        talepId: ctx.entityId,
        deepLink,
        desktopRoute,
        module: template.module,
        screen: template.screen,
        action: template.action,
        isRead: false,
        isArchived: false,
        createdAt: now,
        createdBy: ctx.createdBy ?? "",
        olusturanUid: ctx.createdBy ?? "",
        dedupeKey: inboxId,
        tenantId: ctx.tenantId,
      },
      { merge: true }
    );

    await db().collection("notification_dispatch_queue").add({
      uid,
      tenantId: ctx.tenantId,
      title,
      body: message,
      data: {
        tip: legacyTip,
        type: legacyTip,
        eventCode: ctx.eventCode,
        module: template.module,
        screen: template.screen,
        action: template.action,
        entityType: ctx.entityType,
        entityId: ctx.entityId,
        talepId: ctx.entityId,
        deepLink,
        desktopRoute,
        tenantId: ctx.tenantId,
      },
      status: "pending",
      createdAt: now,
    });
    written++;
  }

  return written;
}

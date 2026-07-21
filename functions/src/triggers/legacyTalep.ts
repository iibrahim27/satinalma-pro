import * as admin from "firebase-admin";
import { onDocumentWritten } from "firebase-functions/v2/firestore";
import { fanOutNotification } from "../lib/fanOut";
import {
  diffTalepChanges,
  dualWriteToEnterprise,
  parseLegacyTalepler,
  type LegacyTalep,
} from "../lib/legacyTalep";
import { statusTransitionEvent } from "../lib/templates";

function talepGuncellemeUtc(t: LegacyTalep): number {
  if (typeof t.guncellemeUtc === "number" && t.guncellemeUtc > 0) {
    return t.guncellemeUtc;
  }
  return 0;
}

async function readResetUtc(
  db: admin.firestore.Firestore,
  tenantId: string
): Promise<number> {
  const snap = await db.doc(`tenants/${tenantId}/veri/satinalma_ayarlar`).get();
  const data = snap.data();
  if (!data) return 0;
  if (typeof data.veriSifirlamaUtc === "number" && data.veriSifirlamaUtc > 0) {
    return data.veriSifirlamaUtc;
  }
  try {
    const parsed = JSON.parse(String(data.json ?? "{}")) as {
      veriSifirlamaUtc?: number;
    };
    return typeof parsed.veriSifirlamaUtc === "number"
      ? parsed.veriSifirlamaUtc
      : 0;
  } catch {
    return 0;
  }
}

/**
 * Sıfırlama sonrası eski istemcilerin dolu listeyi geri yazmasını engeller.
 * Tüm kayıtlar resetUtc öncesine aitse belgeyi tekrar [] yapar.
 */
async function rejectStaleResurrection(
  db: admin.firestore.Firestore,
  tenantId: string,
  afterRef: admin.firestore.DocumentReference,
  afterList: LegacyTalep[],
  resetUtc: number
): Promise<boolean> {
  if (resetUtc <= 0 || afterList.length === 0) return false;

  const allStale = afterList.every((t) => {
    const g = talepGuncellemeUtc(t);
    return g <= 0 || g < resetUtc;
  });
  if (!allStale) return false;

  console.warn(
    `stale talep write rejected tenant=${tenantId} count=${afterList.length} resetUtc=${resetUtc}`
  );
  await afterRef.set(
    {
      json: "[]",
      updatedAt: new Date().toISOString(),
      updatedBy: "system-reset-guard",
      veriSifirlamaUtc: resetUtc,
    },
    { merge: true }
  );

  for (const t of afterList) {
    if (!t.id) continue;
    try {
      await db.recursiveDelete(
        db.doc(`tenants/${tenantId}/procurement_requests/${t.id}`)
      );
    } catch {
      /* ignore */
    }
  }
  return true;
}

export const onLegacyTalepWrite = onDocumentWritten(
  "tenants/{tenantId}/veri/satinalma_talepler",
  async (event) => {
    const tenantId = event.params.tenantId as string;
    const beforeJson = event.data?.before?.data()?.json as string | undefined;
    const afterJson = event.data?.after?.data()?.json as string | undefined;
    if (!afterJson || !event.data?.after) return;

    // Guard'ın kendi [] yazısı — döngüye girme.
    const updatedBy = String(event.data.after.data()?.updatedBy ?? "");
    if (updatedBy === "system-reset-guard") return;

    const beforeList = parseLegacyTalepler(beforeJson);
    const afterList = parseLegacyTalepler(afterJson);
    const db = admin.firestore();

    const resetUtc = await readResetUtc(db, tenantId);
    if (
      await rejectStaleResurrection(
        db,
        tenantId,
        event.data.after.ref,
        afterList,
        resetUtc
      )
    ) {
      return;
    }

    // Sıfırlama / silinen talepler: enterprise kopyalarını da kaldır.
    const afterIds = new Set(
      afterList.map((t) => t.id).filter((id): id is string => !!id)
    );
    for (const prev of beforeList) {
      if (!prev.id || afterIds.has(prev.id)) continue;
      try {
        await db.recursiveDelete(
          db.doc(`tenants/${tenantId}/procurement_requests/${prev.id}`)
        );
      } catch (err) {
        console.error(
          `enterprise delete failed tenant=${tenantId} talep=${prev.id}`,
          err
        );
      }
    }

    // Boş liste (tam sıfırlama): fan-out / dual-write yapma.
    if (afterList.length === 0) return;

    const changes = diffTalepChanges(beforeList, afterList);

    for (const { before, after } of changes) {
      try {
        await dualWriteToEnterprise(after, tenantId);
      } catch (err) {
        console.error(
          `dualWriteToEnterprise failed tenant=${tenantId} talep=${after?.id ?? "?"}`,
          err
        );
      }

      const eventCode = statusTransitionEvent(
        before?.durum ?? null,
        after.durum ?? ""
      );
      if (!eventCode || !after.id) continue;

      try {
        await fanOutNotification({
          tenantId,
          eventCode,
          entityType: "procurement_request",
          entityId: after.id,
          talepNo: after.talepNo,
          talepEden: after.talepEden,
          talepEdenUid: after.talepEdenUid || after.olusturanUid,
          createdBy: undefined,
          siparisNo: after.siparisNo,
          saha: after.saha,
        });
      } catch (err) {
        console.error(
          `fanOutNotification failed tenant=${tenantId} event=${eventCode} talep=${after.id}`,
          err
        );
      }
    }
  }
);

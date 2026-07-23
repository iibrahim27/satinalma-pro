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

/** İstemci saati sunucu reset damgasından biraz geride olabilir. */
const STALE_GRACE_MS = 5 * 60 * 1000;

function readUtcMs(value: unknown): number {
  if (typeof value === "number" && Number.isFinite(value) && value > 0) {
    return value;
  }
  if (typeof value === "string" && value.trim()) {
    const n = Number(value);
    if (Number.isFinite(n) && n > 0) return n;
  }
  return 0;
}

function talepGuncellemeUtc(t: LegacyTalep): number {
  const anyT = t as LegacyTalep & { GuncellemeUtc?: unknown };
  return Math.max(readUtcMs(t.guncellemeUtc), readUtcMs(anyT.GuncellemeUtc));
}

/**
 * Gerçek pre-reset kayıtları ayıklar.
 * g<=0: damgasız eski blob (stale).
 * Saat kayması: resetUtc - grace altı stale.
 */
function isStaleTalep(t: LegacyTalep, resetUtc: number): boolean {
  const g = talepGuncellemeUtc(t);
  if (g <= 0) return true;
  return g < resetUtc - STALE_GRACE_MS;
}

async function readResetUtc(
  db: admin.firestore.Firestore,
  tenantId: string
): Promise<number> {
  const snap = await db.doc(`tenants/${tenantId}/veri/satinalma_ayarlar`).get();
  const data = snap.data();
  if (!data) return 0;
  const docUtc = readUtcMs(data.veriSifirlamaUtc);
  let jsonUtc = 0;
  try {
    const parsed = JSON.parse(String(data.json ?? "{}")) as {
      veriSifirlamaUtc?: unknown;
      VeriSifirlamaUtc?: unknown;
    };
    jsonUtc = Math.max(
      readUtcMs(parsed.veriSifirlamaUtc),
      readUtcMs(parsed.VeriSifirlamaUtc)
    );
  } catch {
    /* ignore */
  }
  return Math.max(docUtc, jsonUtc);
}

/**
 * Sıfırlama sonrası eski istemcilerin dolu listeyi geri yazmasını engeller.
 * Karışık yazılarda (eski + 1 yeni) eski kayıtları ayıklar; yalnızca post-reset kalır.
 * @returns "rewrote" | "emptied" | "clean"
 */
async function stripStaleResurrection(
  db: admin.firestore.Firestore,
  tenantId: string,
  afterRef: admin.firestore.DocumentReference,
  afterList: LegacyTalep[],
  beforeList: LegacyTalep[],
  resetUtc: number
): Promise<"rewrote" | "emptied" | "clean"> {
  if (resetUtc <= 0 || afterList.length === 0) return "clean";

  const kept = afterList.filter((t) => !isStaleTalep(t, resetUtc));
  const stale = afterList.filter((t) => isStaleTalep(t, resetUtc));
  if (stale.length === 0) return "clean";

  console.warn(
    `stale talep strip tenant=${tenantId} stale=${stale.length} kept=${kept.length} resetUtc=${resetUtc} before=${beforeList.length}`
  );

  for (const t of stale) {
    if (!t.id) continue;
    try {
      await db.recursiveDelete(
        db.doc(`tenants/${tenantId}/procurement_requests/${t.id}`)
      );
    } catch {
      /* ignore */
    }
  }

  if (kept.length === 0) {
    await afterRef.set(
      {
        json: "[]",
        updatedAt: new Date().toISOString(),
        updatedBy: "system-reset-guard",
        veriSifirlamaUtc: resetUtc,
      },
      { merge: true }
    );
    return "emptied";
  }

  await afterRef.set(
    {
      json: JSON.stringify(kept),
      updatedAt: new Date().toISOString(),
      updatedBy: "system-stale-strip",
      veriSifirlamaUtc: resetUtc,
    },
    { merge: true }
  );
  return "rewrote";
}

/** Ayarlar belgesinde veriSifirlamaUtc'nin düşürülmesini engeller. */
export const onLegacyAyarlarWrite = onDocumentWritten(
  "tenants/{tenantId}/veri/satinalma_ayarlar",
  async (event) => {
    const before = event.data?.before?.data();
    const after = event.data?.after?.data();
    if (!after || !event.data?.after) return;

    const updatedBy = String(after.updatedBy ?? "");
    if (updatedBy === "system-reset-stamp-guard") return;

    const beforeDoc = readUtcMs(before?.veriSifirlamaUtc);
    const afterDoc = readUtcMs(after.veriSifirlamaUtc);

    let beforeJson = 0;
    try {
      const p = JSON.parse(String(before?.json ?? "{}")) as {
        veriSifirlamaUtc?: unknown;
        VeriSifirlamaUtc?: unknown;
      };
      beforeJson = Math.max(
        readUtcMs(p.veriSifirlamaUtc),
        readUtcMs(p.VeriSifirlamaUtc)
      );
    } catch {
      /* ignore */
    }

    let afterJson = 0;
    let afterParsed: Record<string, unknown> = {};
    try {
      const p = JSON.parse(String(after.json ?? "{}"));
      if (p && typeof p === "object" && !Array.isArray(p)) {
        afterParsed = p as Record<string, unknown>;
        afterJson = Math.max(
          readUtcMs(afterParsed.veriSifirlamaUtc),
          readUtcMs(afterParsed.VeriSifirlamaUtc)
        );
      }
    } catch {
      /* ignore */
    }

    const floor = Math.max(beforeDoc, beforeJson, afterDoc, afterJson);
    if (floor <= 0) return;

    const needsDocFix = afterDoc < floor;
    const needsJsonFix = afterJson < floor;
    if (!needsDocFix && !needsJsonFix) return;

    if (needsJsonFix) {
      afterParsed.veriSifirlamaUtc = floor;
    }

    console.warn(
      `ayarlar stamp guard tenant=${event.params.tenantId} floor=${floor} afterDoc=${afterDoc} afterJson=${afterJson}`
    );

    await event.data.after.ref.set(
      {
        ...(needsJsonFix
          ? { json: JSON.stringify(afterParsed) }
          : {}),
        veriSifirlamaUtc: floor,
        updatedAt: new Date().toISOString(),
        updatedBy: "system-reset-stamp-guard",
      },
      { merge: true }
    );
  }
);

export const onLegacyTalepWrite = onDocumentWritten(
  "tenants/{tenantId}/veri/satinalma_talepler",
  async (event) => {
    const tenantId = event.params.tenantId as string;
    const beforeJson = event.data?.before?.data()?.json as string | undefined;
    const afterJson = event.data?.after?.data()?.json as string | undefined;
    if (!afterJson || !event.data?.after) return;

    const updatedBy = String(event.data.after.data()?.updatedBy ?? "");
    // Boşaltma guard'ı — döngüye girme.
    if (updatedBy === "system-reset-guard") return;

    const beforeList = parseLegacyTalepler(beforeJson);
    const afterList = parseLegacyTalepler(afterJson);
    const db = admin.firestore();

    const resetUtc = await readResetUtc(db, tenantId);
    // system-stale-strip: zaten ayıklanmış liste — tekrar strip etme, dual-write'a devam.
    if (updatedBy !== "system-stale-strip") {
      const strip = await stripStaleResurrection(
        db,
        tenantId,
        event.data.after.ref,
        afterList,
        beforeList,
        resetUtc
      );
      if (strip === "emptied" || strip === "rewrote") return;
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

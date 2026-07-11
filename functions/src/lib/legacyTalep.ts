import * as admin from "firebase-admin";

export interface LegacyTalep {
  id?: string;
  talepNo?: string;
  talepEden?: string;
  talepEdenUid?: string;
  olusturanUid?: string;
  durum?: string;
  saha?: string;
  talepTuru?: string;
  siparisNo?: string;
  olusturmaTarihi?: string;
  guncellemeTarihi?: string;
}

export function parseLegacyTalepler(jsonStr: string | undefined): LegacyTalep[] {
  if (!jsonStr) return [];
  try {
    const parsed = JSON.parse(jsonStr);
    return Array.isArray(parsed) ? parsed : [];
  } catch {
    return [];
  }
}

export async function dualWriteToEnterprise(talep: LegacyTalep, tenantId?: string): Promise<void> {
  if (process.env.MIGRATION_DUAL_WRITE !== "true") return;
  if (!talep.id) return;

  const db = admin.firestore();
  const collectionPath = tenantId?.trim()
    ? `tenants/${tenantId.trim()}/procurement_requests`
    : "procurement_requests";
  const ref = db.collection(collectionPath).doc(talep.id);
  await ref.set(
    {
      requestNo: talep.talepNo ?? "",
      requesterName: talep.talepEden ?? "",
      requesterUid: talep.talepEdenUid ?? null,
      status: talep.durum ?? "Hazırlanıyor",
      site: talep.saha ?? "",
      requestType: talep.talepTuru ?? "Normal",
      orderNo: talep.siparisNo ?? null,
      legacySyncedAt: admin.firestore.FieldValue.serverTimestamp(),
    },
    { merge: true }
  );
}

export function diffTalepChanges(
  before: LegacyTalep[],
  after: LegacyTalep[]
): Array<{ before: LegacyTalep | null; after: LegacyTalep }> {
  const beforeMap = new Map(before.filter((t) => t.id).map((t) => [t.id!, t]));
  const changes: Array<{ before: LegacyTalep | null; after: LegacyTalep }> = [];

  for (const t of after) {
    if (!t.id) continue;
    const prev = beforeMap.get(t.id);
    if (!prev || prev.durum !== t.durum) {
      changes.push({ before: prev ?? null, after: t });
    }
  }
  return changes;
}

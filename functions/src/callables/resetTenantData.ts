import { onCall, HttpsError } from "firebase-functions/v2/https";
import * as admin from "firebase-admin";
import { tenantUsersPath } from "../lib/saas";

type ResetScope = "all" | "satinalma";

const ALL_VERI_DOCS: Array<{ id: string; json: string }> = [
  { id: "satinalma_ayarlar", json: "" },
  { id: "satinalma_talepler", json: "[]" },
  { id: "alinan_malzemeler", json: "[]" },
  { id: "stok", json: "[]" },
  { id: "stok_hareketleri", json: "[]" },
  { id: "agrega", json: "[]" },
  { id: "cimento", json: "[]" },
  { id: "akaryakit", json: "[]" },
  {
    id: "filo",
    json: JSON.stringify({ araclar: [], giderler: [], zimmetler: [] }),
  },
  { id: "finansman_gelir", json: "[]" },
  {
    id: "uygulama_ayarlar",
    json: JSON.stringify({
      firmaAdi: "",
      logoDosyaYolu: "",
      anasayfaLogoDosyaYolu: "",
      malzemeKategorileri: [],
      malzemeBirimleri: [],
    }),
  },
  { id: "iade_kayitlari", json: "[]" },
  { id: "bildirimler", json: "[]" },
  {
    id: "medya",
    json: JSON.stringify({
      firmaLogoDosya: "",
      anasayfaLogoDosya: "",
      firmaLogoBase64: "",
      anasayfaLogoBase64: "",
    }),
  },
];

const SATINALMA_VERI_DOCS = [
  "satinalma_ayarlar",
  "satinalma_talepler",
  "bildirimler",
] as const;

function normalizeRole(raw: unknown): string {
  return String(raw ?? "")
    .trim()
    .toLowerCase()
    .replace(/ı/g, "i")
    .replace(/i\u0307/g, "i") // İ → i̇ → i
    .replace(/ğ/g, "g")
    .replace(/ü/g, "u")
    .replace(/ş/g, "s")
    .replace(/ö/g, "o")
    .replace(/ç/g, "c");
}

function canResetTenant(user: Record<string, unknown>): boolean {
  if (user.aktif === false) return false;
  const rol = normalizeRole(user.rol ?? user.role);
  if (
    rol === "satinalma" ||
    rol === "admin" ||
    rol === "yonetim" ||
    rol === "yonetici" ||
    rol.includes("satinalma")
  ) {
    return true;
  }
  // Modül yetkisi: satınalma yazma
  const moduller = Array.isArray(user.moduller) ? user.moduller : [];
  if (moduller.some((m) => normalizeRole(m).includes("satinalma"))) return true;
  return false;
}

export function emptyAyarlarJson(veriSifirlamaUtc: number): string {
  return JSON.stringify({
    firmaAdi: "",
    sartnameMetni: "",
    teklifIstemeSartnameleri: "",
    sefImzalari: [],
    yonetimImzalari: [],
    sartnameler: [],
    sonTalepSira: 0,
    sonSiparisSira: 0,
    sonIadeSira: 0,
    silinenTalepIdleri: [],
    varsayilanUsdKuru: 0,
    varsayilanEurKuru: 0,
    imzaAyarleriTemiz: true,
    veriSifirlamaUtc,
  });
}

async function writeVeriDoc(
  veriRoot: admin.firestore.CollectionReference,
  docId: string,
  json: string,
  uid: string,
  veriSifirlamaUtc: number,
  nowIso: string
): Promise<void> {
  await veriRoot.doc(docId).set(
    {
      json,
      updatedAt: nowIso,
      updatedBy: uid,
      veriSifirlamaUtc,
      resetInProgress: false,
    },
    { merge: true }
  );
}

export async function wipeTenantOperationalData(
  tenantId: string,
  uid: string,
  scope: ResetScope = "all"
): Promise<{
  ok: true;
  tenantId: string;
  scope: ResetScope;
  veriSifirlamaUtc: number;
  usersProcessed: number;
  inboxesCleared: number;
}> {
  const db = admin.firestore();
  const veriSifirlamaUtc = Date.now();
  const nowIso = new Date().toISOString();
  const veriRoot = db.collection(`tenants/${tenantId}/veri`);

  await writeVeriDoc(
    veriRoot,
    "satinalma_ayarlar",
    emptyAyarlarJson(veriSifirlamaUtc),
    uid,
    veriSifirlamaUtc,
    nowIso
  );
  await veriRoot.doc("satinalma_ayarlar").set(
    { resetInProgress: true },
    { merge: true }
  );

  await writeVeriDoc(
    veriRoot,
    "satinalma_talepler",
    "[]",
    uid,
    veriSifirlamaUtc,
    nowIso
  );

  try {
    await db.recursiveDelete(
      db.collection(`tenants/${tenantId}/procurement_requests`)
    );
  } catch (err) {
    console.error(`procurement_requests wipe failed tenant=${tenantId}`, err);
  }

  let inboxCleared = 0;
  let usersProcessed = 0;

  if (scope === "all") {
    const usersSnap = await db.collection(tenantUsersPath(tenantId)).get();
    usersProcessed = usersSnap.size;
    for (const userDoc of usersSnap.docs) {
      try {
        await db.recursiveDelete(userDoc.ref.collection("notification_inbox"));
        inboxCleared++;
      } catch (err) {
        console.error(
          `inbox wipe failed tenant=${tenantId} uid=${userDoc.id}`,
          err
        );
      }
    }

    for (const doc of ALL_VERI_DOCS) {
      const json =
        doc.id === "satinalma_ayarlar"
          ? emptyAyarlarJson(veriSifirlamaUtc)
          : doc.json;
      await writeVeriDoc(
        veriRoot,
        doc.id,
        json,
        uid,
        veriSifirlamaUtc,
        nowIso
      );
    }
  } else {
    for (const docId of SATINALMA_VERI_DOCS) {
      const json =
        docId === "satinalma_ayarlar"
          ? emptyAyarlarJson(veriSifirlamaUtc)
          : "[]";
      await writeVeriDoc(
        veriRoot,
        docId,
        json,
        uid,
        veriSifirlamaUtc,
        nowIso
      );
    }
    const usersSnap = await db.collection(tenantUsersPath(tenantId)).get();
    usersProcessed = usersSnap.size;
    for (const userDoc of usersSnap.docs) {
      try {
        await db.recursiveDelete(userDoc.ref.collection("notification_inbox"));
        inboxCleared++;
      } catch (err) {
        console.error(
          `inbox wipe failed tenant=${tenantId} uid=${userDoc.id}`,
          err
        );
      }
    }
  }

  await writeVeriDoc(
    veriRoot,
    "satinalma_talepler",
    "[]",
    uid,
    veriSifirlamaUtc,
    nowIso
  );
  await writeVeriDoc(
    veriRoot,
    "satinalma_ayarlar",
    emptyAyarlarJson(veriSifirlamaUtc),
    uid,
    veriSifirlamaUtc,
    nowIso
  );

  return {
    ok: true,
    tenantId,
    scope,
    veriSifirlamaUtc,
    usersProcessed,
    inboxesCleared: inboxCleared,
  };
}

/**
 * Kiracı operasyonel verisini sunucu tarafında sıfırlar.
 * scope=all → tüm modüller + inbox + medya
 * scope=satinalma → talepler/ayarlar/bildirimler + procurement_requests
 */
export const resetTenantOperationalData = onCall(
  { timeoutSeconds: 540, memory: "1GiB" },
  async (request) => {
    if (!request.auth) {
      throw new HttpsError("unauthenticated", "Giriş gerekli");
    }

    const uid = request.auth.uid;
    const tokenTenant = String(
      (request.auth.token.tenantId as string | undefined) ?? ""
    ).trim();
    const dataTenant = String(
      (request.data?.tenantId as string | undefined) ?? ""
    ).trim();
    const tenantId = tokenTenant || dataTenant;
    const scopeRaw = String(
      (request.data?.scope as string | undefined) ?? "all"
    )
      .trim()
      .toLowerCase();
    const scope: ResetScope =
      scopeRaw === "satinalma" ? "satinalma" : "all";

    if (!tenantId) {
      throw new HttpsError("failed-precondition", "tenantId gerekli");
    }
    if (dataTenant && tokenTenant && dataTenant !== tokenTenant) {
      throw new HttpsError(
        "permission-denied",
        "Başka kiracı verisi sıfırlanamaz"
      );
    }

    const db = admin.firestore();
    const userSnap = await db.doc(`${tenantUsersPath(tenantId)}/${uid}`).get();
    if (!userSnap.exists) {
      throw new HttpsError("permission-denied", "Kiracı kullanıcısı bulunamadı");
    }
    const user = (userSnap.data() ?? {}) as Record<string, unknown>;
    if (!canResetTenant(user)) {
      throw new HttpsError(
        "permission-denied",
        `Sistemi sıfırlamak için Satınalma / Yönetim yetkisi gerekli (rol=${String(user.rol ?? user.role ?? "")})`
      );
    }

    return wipeTenantOperationalData(tenantId, uid, scope);
  }
);

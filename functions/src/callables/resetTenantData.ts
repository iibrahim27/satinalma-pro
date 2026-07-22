import { onCall, HttpsError } from "firebase-functions/v2/https";
import * as admin from "firebase-admin";
import { tenantUsersPath } from "../lib/saas";

type ResetScope = "all" | "satinalma";

export type WipeOptions = {
  /** true: imza/şartname/logo/kategori vb. ayarlar korunur; sadece operasyonel veri silinir */
  preserveSettings?: boolean;
};

/** Ayar belgeleri — preserveSettings=true iken json içeriği silinmez. */
const SETTINGS_VERI_DOC_IDS = new Set([
  "satinalma_ayarlar",
  "uygulama_ayarlar",
  "medya",
  "eposta_sablonlari",
]);

/** Bilinen operasyonel veri belgeleri ve boş halleri. */
const DATA_VERI_DOCS: Array<{ id: string; json: string }> = [
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
  { id: "iade_kayitlari", json: "[]" },
  { id: "bildirimler", json: "[]" },
];

const EMPTY_SETTINGS_DOCS: Array<{ id: string; json: string }> = [
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

const SATINALMA_DATA_DOCS = ["satinalma_talepler", "bildirimler"] as const;

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

/** Mevcut ayarları koruyarak sayaçları ve sıfırlama damgasını günceller. */
export function stampAyarlarPreservingSettingsJson(
  existingJson: string | undefined,
  veriSifirlamaUtc: number
): string {
  let parsed: Record<string, unknown> = {};
  if (existingJson && existingJson.trim()) {
    try {
      const obj = JSON.parse(existingJson) as unknown;
      if (obj && typeof obj === "object" && !Array.isArray(obj)) {
        parsed = obj as Record<string, unknown>;
      }
    } catch {
      parsed = {};
    }
  }
  parsed.veriSifirlamaUtc = veriSifirlamaUtc;
  parsed.sonTalepSira = 0;
  parsed.sonSiparisSira = 0;
  parsed.sonIadeSira = 0;
  parsed.silinenTalepIdleri = [];
  return JSON.stringify(parsed);
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

async function writeAyarlarDoc(
  veriRoot: admin.firestore.CollectionReference,
  uid: string,
  veriSifirlamaUtc: number,
  nowIso: string,
  preserveSettings: boolean
): Promise<void> {
  let json: string;
  if (preserveSettings) {
    const snap = await veriRoot.doc("satinalma_ayarlar").get();
    const existing = snap.exists
      ? String((snap.data() as { json?: string } | undefined)?.json ?? "")
      : "";
    json = stampAyarlarPreservingSettingsJson(existing, veriSifirlamaUtc);
  } else {
    json = emptyAyarlarJson(veriSifirlamaUtc);
  }
  await writeVeriDoc(
    veriRoot,
    "satinalma_ayarlar",
    json,
    uid,
    veriSifirlamaUtc,
    nowIso
  );
}

async function clearUserInboxes(
  tenantId: string
): Promise<{ usersProcessed: number; inboxCleared: number }> {
  const db = admin.firestore();
  const usersSnap = await db.collection(tenantUsersPath(tenantId)).get();
  let inboxCleared = 0;
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
  return { usersProcessed: usersSnap.size, inboxCleared };
}

/**
 * Kiracı operasyonel verisini siler.
 * Kullanıcı hesapları / usernames her zaman korunur.
 * preserveSettings=true → ayar/logo/şablon belgeleri korunur.
 */
export async function wipeTenantOperationalData(
  tenantId: string,
  uid: string,
  scope: ResetScope = "all",
  options: WipeOptions = {}
): Promise<{
  ok: true;
  tenantId: string;
  scope: ResetScope;
  veriSifirlamaUtc: number;
  usersProcessed: number;
  inboxesCleared: number;
  preserveSettings: boolean;
}> {
  const preserveSettings = options.preserveSettings === true;
  const db = admin.firestore();
  const veriSifirlamaUtc = Date.now();
  const nowIso = new Date().toISOString();
  const veriRoot = db.collection(`tenants/${tenantId}/veri`);

  await writeAyarlarDoc(
    veriRoot,
    uid,
    veriSifirlamaUtc,
    nowIso,
    preserveSettings
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

  const { usersProcessed, inboxCleared } = await clearUserInboxes(tenantId);

  if (scope === "all") {
    for (const doc of DATA_VERI_DOCS) {
      await writeVeriDoc(
        veriRoot,
        doc.id,
        doc.json,
        uid,
        veriSifirlamaUtc,
        nowIso
      );
    }

    if (!preserveSettings) {
      for (const doc of EMPTY_SETTINGS_DOCS) {
        await writeVeriDoc(
          veriRoot,
          doc.id,
          doc.json,
          uid,
          veriSifirlamaUtc,
          nowIso
        );
      }
      await writeAyarlarDoc(
        veriRoot,
        uid,
        veriSifirlamaUtc,
        nowIso,
        false
      );
    } else {
      // Ayar belgelerine yalnızca damga yaz — json içeriğine dokunma.
      for (const docId of SETTINGS_VERI_DOC_IDS) {
        if (docId === "satinalma_ayarlar") continue;
        await veriRoot.doc(docId).set(
          {
            updatedAt: nowIso,
            updatedBy: uid,
            veriSifirlamaUtc,
            resetInProgress: false,
          },
          { merge: true }
        );
      }
    }

    // Bilinmeyen veri/* belgelerini de boşalt (ayarlar hariç).
    try {
      const veriSnap = await veriRoot.get();
      for (const doc of veriSnap.docs) {
        if (SETTINGS_VERI_DOC_IDS.has(doc.id)) continue;
        if (DATA_VERI_DOCS.some((d) => d.id === doc.id)) continue;
        await writeVeriDoc(
          veriRoot,
          doc.id,
          "[]",
          uid,
          veriSifirlamaUtc,
          nowIso
        );
      }
    } catch (err) {
      console.error(`veri scan wipe failed tenant=${tenantId}`, err);
    }
  } else {
    for (const docId of SATINALMA_DATA_DOCS) {
      await writeVeriDoc(
        veriRoot,
        docId,
        "[]",
        uid,
        veriSifirlamaUtc,
        nowIso
      );
    }
    if (!preserveSettings) {
      await writeAyarlarDoc(
        veriRoot,
        uid,
        veriSifirlamaUtc,
        nowIso,
        false
      );
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
  await writeAyarlarDoc(
    veriRoot,
    uid,
    veriSifirlamaUtc,
    nowIso,
    preserveSettings
  );

  return {
    ok: true,
    tenantId,
    scope,
    veriSifirlamaUtc,
    usersProcessed,
    inboxesCleared: inboxCleared,
    preserveSettings,
  };
}

/**
 * Kiracı operasyonel verisini sunucu tarafında sıfırlar.
 * scope=all → tüm modüller + inbox
 * scope=satinalma → talepler/bildirimler + procurement_requests
 * Pro Ayarlar sıfırlaması ayarları da temizleyebilir (preserveSettings yok).
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

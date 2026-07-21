import { onCall, HttpsError } from "firebase-functions/v2/https";
import * as admin from "firebase-admin";
import { tenantUsersPath } from "../lib/saas";

const VERI_DOCS: Array<{ id: string; json: string }> = [
  {
    id: "satinalma_ayarlar",
    json: "", // filled per-call with veriSifirlamaUtc
  },
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

function normalizeRole(raw: unknown): string {
  return String(raw ?? "")
    .trim()
    .toLowerCase()
    .replace(/ı/g, "i")
    .replace(/ğ/g, "g")
    .replace(/ü/g, "u")
    .replace(/ş/g, "s")
    .replace(/ö/g, "o")
    .replace(/ç/g, "c");
}

function canResetTenant(rol: string, aktif: boolean): boolean {
  if (!aktif) return false;
  return (
    rol === "satinalma" ||
    rol === "admin" ||
    rol === "yonetim" ||
    rol === "yonetici"
  );
}

function emptyAyarlarJson(veriSifirlamaUtc: number): string {
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

/**
 * Kiracı operasyonel verisini sunucu tarafında sıfırlar:
 * - veri/* belgeleri (talepler, stok, bildirimler, medya, …)
 * - tüm kullanıcı notification_inbox alt koleksiyonları
 * - procurement_requests (ve alt koleksiyonları)
 * Kullanıcı profilleri / Firebase yapılandırması korunur.
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
    const user = userSnap.data() ?? {};
    const rol = normalizeRole(user.rol);
    const aktif = user.aktif !== false;
    if (!canResetTenant(rol, aktif)) {
      throw new HttpsError(
        "permission-denied",
        "Sistemi sıfırlamak için Satınalma / Yönetim yetkisi gerekli"
      );
    }

    const veriSifirlamaUtc = Date.now();
    const nowIso = new Date().toISOString();
    const veriRoot = db.collection(`tenants/${tenantId}/veri`);

    // 1) Damga önce yazılsın — istemciler eski cache'i hemen düşürsün.
    await veriRoot.doc("satinalma_ayarlar").set(
      {
        json: emptyAyarlarJson(veriSifirlamaUtc),
        updatedAt: nowIso,
        updatedBy: uid,
        veriSifirlamaUtc,
        resetInProgress: true,
      },
      { merge: true }
    );

    // 2) Tüm kullanıcı inbox'larını sil.
    const usersSnap = await db.collection(tenantUsersPath(tenantId)).get();
    let inboxCleared = 0;
    for (const userDoc of usersSnap.docs) {
      const inboxRef = userDoc.ref.collection("notification_inbox");
      try {
        await db.recursiveDelete(inboxRef);
        inboxCleared++;
      } catch (err) {
        console.error(
          `inbox wipe failed tenant=${tenantId} uid=${userDoc.id}`,
          err
        );
      }
    }

    // 3) Enterprise talep koleksiyonunu sil.
    try {
      await db.recursiveDelete(
        db.collection(`tenants/${tenantId}/procurement_requests`)
      );
    } catch (err) {
      console.error(`procurement_requests wipe failed tenant=${tenantId}`, err);
    }

    // 4) Operasyonel veri belgelerini boşalt.
    for (const doc of VERI_DOCS) {
      const json =
        doc.id === "satinalma_ayarlar"
          ? emptyAyarlarJson(veriSifirlamaUtc)
          : doc.json;
      await veriRoot.doc(doc.id).set(
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

    return {
      ok: true,
      tenantId,
      veriSifirlamaUtc,
      usersProcessed: usersSnap.size,
      inboxesCleared: inboxCleared,
    };
  }
);

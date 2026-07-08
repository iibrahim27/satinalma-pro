import { onCall, HttpsError } from "firebase-functions/v2/https";
import * as admin from "firebase-admin";
import {
  isReservedUsername,
  isValidUsername,
  normalizeUsername,
  signInWithEmailPassword,
  tenantUserPath,
  tenantUsersPath,
  webApiKey,
} from "../lib/saas";

const db = () => admin.firestore();

type LisansTip = "deneme" | "yillik";

type TenantDoc = {
  id: string;
  kod?: string;
  ad?: string;
  aktif?: boolean;
  lisansTipi?: string;
  lisansBaslangic?: admin.firestore.Timestamp | string | Date | null;
  lisansBitis?: admin.firestore.Timestamp | string | Date | null;
};

function toDate(value: unknown): Date | null {
  if (!value) return null;
  if (value instanceof Date) return value;
  if (typeof value === "string") {
    const d = new Date(value);
    return Number.isNaN(d.getTime()) ? null : d;
  }
  if (typeof value === "object" && value !== null && "toDate" in value) {
    try {
      return (value as admin.firestore.Timestamp).toDate();
    } catch {
      return null;
    }
  }
  return null;
}

function addDays(base: Date, days: number): Date {
  const d = new Date(base.getTime());
  d.setUTCDate(d.getUTCDate() + days);
  return d;
}

function calcKalanGun(bitis: Date | null): number | null {
  if (!bitis) return null;
  const ms = bitis.getTime() - Date.now();
  return Math.ceil(ms / 86400000);
}

function lisansOzeti(tenant: TenantDoc) {
  const tip = (tenant.lisansTipi === "yillik" ? "yillik" : "deneme") as LisansTip;
  const baslangic = toDate(tenant.lisansBaslangic);
  const bitis = toDate(tenant.lisansBitis);
  const kalanGun = calcKalanGun(bitis);
  // Bitiş tarihi yoksa henüz lisans tanımlanmamış sayılır (expire etme).
  const suresiDoldu = bitis !== null && kalanGun !== null && kalanGun <= 0;
  return {
    tip,
    baslangicUtc: baslangic?.toISOString() ?? null,
    bitisUtc: bitis?.toISOString() ?? null,
    aktif: tenant.aktif !== false && !suresiDoldu,
    kalanGun,
    suresiDoldu,
  };
}

async function ensureTenantLicense(tenantId: string, tenant: TenantDoc): Promise<TenantDoc> {
  if (toDate(tenant.lisansBitis)) {
    return expireTenantIfNeeded(tenantId, tenant);
  }

  const { baslangic, bitis } = defaultTrialDates();
  await db().collection("tenants").doc(tenantId).set(
    {
      lisansTipi: tenant.lisansTipi === "yillik" ? "yillik" : "deneme",
      lisansBaslangic: admin.firestore.Timestamp.fromDate(baslangic),
      lisansBitis: admin.firestore.Timestamp.fromDate(bitis),
      lisansSuresiDoldu: false,
      guncelleme: admin.firestore.FieldValue.serverTimestamp(),
    },
    { merge: true }
  );

  return {
    ...tenant,
    lisansTipi: tenant.lisansTipi === "yillik" ? "yillik" : "deneme",
    lisansBaslangic: baslangic,
    lisansBitis: bitis,
  };
}


async function expireTenantIfNeeded(tenantId: string, tenant: TenantDoc): Promise<TenantDoc> {
  const ozet = lisansOzeti(tenant);
  if (!ozet.suresiDoldu || tenant.aktif === false) return tenant;

  await db().collection("tenants").doc(tenantId).set(
    {
      aktif: false,
      lisansSuresiDoldu: true,
      guncelleme: admin.firestore.FieldValue.serverTimestamp(),
    },
    { merge: true }
  );

  const users = await db().collection(tenantUsersPath(tenantId)).where("aktif", "==", true).get();
  const batch = db().batch();
  for (const doc of users.docs) {
    batch.set(doc.ref, { aktif: false, lisansPasifeAlindi: true }, { merge: true });
  }
  if (!users.empty) await batch.commit();

  return { ...tenant, aktif: false };
}

function defaultTrialDates() {
  const baslangic = new Date();
  const bitis = addDays(baslangic, 30);
  return { baslangic, bitis };
}

async function assertPlatformAdmin(uid: string): Promise<void> {
  const doc = await db().collection("platform_admins").doc(uid).get();
  if (!doc.exists || doc.data()?.aktif === false) {
    throw new HttpsError("permission-denied", "Platform yöneticisi yetkisi gerekli.");
  }
}

async function isPlatformAdminUid(uid: string): Promise<boolean> {
  if (!uid) return false;
  const doc = await db().collection("platform_admins").doc(uid).get();
  return doc.exists && doc.data()?.aktif !== false;
}

async function platformAdminEpostalSet(): Promise<Set<string>> {
  const snap = await db().collection("platform_admins").where("aktif", "==", true).get();
  const set = new Set<string>();
  for (const d of snap.docs) {
    const ep = String(d.data().eposta ?? "").trim().toLowerCase();
    if (ep) set.add(ep);
  }
  return set;
}

/** Platform sahibini tüm firmalardan ve usernames kaydından ayırır. */
async function detachPlatformAdminFromTenants(uid: string, eposta?: string): Promise<{ removedUsers: number; removedUsernames: number }> {
  let removedUsers = 0;
  let removedUsernames = 0;
  const email = (eposta ?? "").trim().toLowerCase();

  const tenants = await db().collection("tenants").get();
  for (const t of tenants.docs) {
    const userRef = db().doc(tenantUserPath(t.id, uid));
    const userSnap = await userRef.get();
    if (userSnap.exists) {
      await userRef.delete();
      removedUsers++;
    }
  }

  const unameSnap = await db().collection("usernames").where("uid", "==", uid).get();
  for (const d of unameSnap.docs) {
    await d.ref.delete();
    removedUsernames++;
  }

  if (email) {
    const byEmail = await db().collection("usernames").where("eposta", "==", email).get();
    for (const d of byEmail.docs) {
      if (d.data()?.uid === uid || !d.data()?.uid) {
        await d.ref.delete();
        removedUsernames++;
      }
    }
  }

  // Platform hesabının tenant claim'i olmamalı
  try {
    const user = await admin.auth().getUser(uid);
    const claims = { ...(user.customClaims ?? {}) };
    if ("tenantId" in claims) {
      delete claims.tenantId;
      await admin.auth().setCustomUserClaims(uid, claims);
    }
  } catch {
    // yoksay
  }

  return { removedUsers, removedUsernames };
}

async function readTenant(tenantId: string): Promise<TenantDoc> {
  const doc = await db().collection("tenants").doc(tenantId).get();
  if (!doc.exists) throw new HttpsError("not-found", "Firma bulunamadı.");
  return { id: doc.id, ...doc.data() } as TenantDoc;
}

function parseLisansaInput(data: Record<string, unknown> | undefined, existing?: TenantDoc) {
  const tipRaw = ((data?.lisansTipi as string) ?? existing?.lisansTipi ?? "deneme").trim().toLowerCase();
  const tip: LisansTip = tipRaw === "yillik" ? "yillik" : "deneme";

  let baslangic = toDate(data?.lisansBaslangic) ?? toDate(existing?.lisansBaslangic);
  let bitis = toDate(data?.lisansBitis) ?? toDate(existing?.lisansBitis);

  const yenile = data?.lisansYenile === true;
  if (yenile || !baslangic || !bitis) {
    baslangic = new Date();
    bitis = tip === "yillik" ? addDays(baslangic, 365) : addDays(baslangic, 30);
  }

  return { tip, baslangic, bitis };
}

async function lookupUsername(username: string) {
  const key = normalizeUsername(username);
  const doc = await db().collection("usernames").doc(key).get();
  if (!doc.exists) return null;
  return doc.data() as { tenantId: string; uid: string; eposta: string };
}

export const loginWithUsername = onCall(async (request) => {
  const username = (request.data?.username as string) ?? "";
  const password = (request.data?.password as string) ?? "";

  if (!isValidUsername(username) || !password) {
    throw new HttpsError("invalid-argument", "INVALID_LOGIN");
  }

  const lookup = await lookupUsername(username);
  if (!lookup) {
    throw new HttpsError("not-found", "USER_NOT_FOUND");
  }

  // Platform sahibi SatınalmaPro / Android ile firma girişi yapamaz.
  if (await isPlatformAdminUid(lookup.uid)) {
    throw new HttpsError("failed-precondition", "PLATFORM_ADMIN_LOGIN");
  }

  let tenant = await readTenant(lookup.tenantId);
  tenant = await ensureTenantLicense(lookup.tenantId, tenant);
  const lisans = lisansOzeti(tenant);

  if (lisans.suresiDoldu || tenant.aktif === false) {
    throw new HttpsError("failed-precondition", "LICENSE_EXPIRED");
  }

  const userSnap = await db().doc(tenantUserPath(lookup.tenantId, lookup.uid)).get();
  if (!userSnap.exists) {
    throw new HttpsError("not-found", "USER_NOT_FOUND");
  }
  const user = userSnap.data()!;
  if (user.aktif === false) {
    throw new HttpsError("failed-precondition", "USER_INACTIVE");
  }

  let authResult;
  try {
    authResult = await signInWithEmailPassword(webApiKey(), lookup.eposta, password);
  } catch (e: unknown) {
    const msg = e instanceof Error ? e.message : "";
    if (msg.includes("WEB_API_KEY") || msg.includes("AUTH_CONFIG")) {
      console.error("loginWithUsername config error:", msg);
      throw new HttpsError("failed-precondition", "AUTH_CONFIG_MISSING");
    }
    if (
      msg.includes("EMAIL_NOT_FOUND") ||
      msg.includes("INVALID_PASSWORD") ||
      msg.includes("INVALID_LOGIN_CREDENTIALS")
    ) {
      throw new HttpsError("unauthenticated", "INVALID_LOGIN");
    }
    console.error("loginWithUsername auth error:", msg);
    throw new HttpsError("unauthenticated", "INVALID_LOGIN");
  }

  if (authResult.localId !== lookup.uid) {
    throw new HttpsError("failed-precondition", "INVALID_LOGIN");
  }

  return {
    idToken: authResult.idToken,
    refreshToken: authResult.refreshToken,
    expiresIn: parseInt(authResult.expiresIn, 10) || 3600,
    uid: lookup.uid,
    tenantId: lookup.tenantId,
    tenantAd: tenant.ad ?? "",
    eposta: lookup.eposta,
    kullaniciAdi: normalizeUsername(username),
    lisans,
  };
});

export const passwordResetByUsername = onCall(async (request) => {
  const username = (request.data?.username as string) ?? "";
  if (!isValidUsername(username)) {
    throw new HttpsError("invalid-argument", "USER_NOT_FOUND");
  }

  const lookup = await lookupUsername(username);
  if (!lookup) {
    throw new HttpsError("not-found", "USER_NOT_FOUND");
  }

  await fetch(
    `https://identitytoolkit.googleapis.com/v1/accounts:sendOobCode?key=${webApiKey()}`,
    {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        requestType: "PASSWORD_RESET",
        email: lookup.eposta,
      }),
    }
  );

  return { ok: true };
});

export const platformListTenants = onCall(async (request) => {
  if (!request.auth) throw new HttpsError("unauthenticated", "Giriş gerekli");
  await assertPlatformAdmin(request.auth.uid);

  const snap = await db().collection("tenants").orderBy("ad").get();
  const out = [];
  for (const d of snap.docs) {
    let tenant = { id: d.id, ...d.data() } as TenantDoc;
    tenant = await ensureTenantLicense(d.id, tenant);
    const lisans = lisansOzeti(tenant);
    out.push({
      id: d.id,
      kod: tenant.kod ?? "",
      ad: tenant.ad ?? "",
      aktif: tenant.aktif !== false && !lisans.suresiDoldu,
      lisansTipi: lisans.tip,
      lisansBaslangic: lisans.baslangicUtc,
      lisansBitis: lisans.bitisUtc,
      lisansKalanGun: lisans.kalanGun,
      lisansSuresiDoldu: lisans.suresiDoldu,
    });
  }
  return out;
});

export const platformSaveTenant = onCall(async (request) => {
  if (!request.auth) throw new HttpsError("unauthenticated", "Giriş gerekli");
  await assertPlatformAdmin(request.auth.uid);

  const id = ((request.data?.id as string) ?? "").trim();
  const kod = ((request.data?.kod as string) ?? "").trim();
  const ad = ((request.data?.ad as string) ?? "").trim();
  const data = (request.data ?? {}) as Record<string, unknown>;

  if (!kod || !ad) {
    throw new HttpsError("invalid-argument", "Firma kodu ve adı zorunludur.");
  }

  const tenantRef = id
    ? db().collection("tenants").doc(id)
    : db().collection("tenants").doc();

  const existingSnap = await tenantRef.get();
  const existing = existingSnap.exists
    ? ({ id: tenantRef.id, ...existingSnap.data() } as TenantDoc)
    : undefined;

  const kodSnap = await db()
    .collection("tenants")
    .where("kod", "==", kod)
    .limit(1)
    .get();
  if (!kodSnap.empty && kodSnap.docs[0].id !== tenantRef.id) {
    throw new HttpsError("already-exists", "Bu firma kodu zaten kayıtlı.");
  }

  const lisans = parseLisansaInput(data, existing);
  const lisansYenile = data.lisansYenile === true;
  const lisansOzetiHesap = lisansOzeti({
    id: tenantRef.id,
    lisansTipi: lisans.tip,
    lisansBaslangic: lisans.baslangic,
    lisansBitis: lisans.bitis,
    aktif: true,
  });

  // Süre dolmuşsa aktifi zorla kapat; yenilemede yeniden aç.
  const aktif =
    data.aktif === false
      ? false
      : (!lisansOzetiHesap.suresiDoldu && (lisansYenile || data.aktif !== false));

  await tenantRef.set(
    {
      kod,
      ad,
      aktif,
      lisansTipi: lisans.tip,
      lisansBaslangic: admin.firestore.Timestamp.fromDate(lisans.baslangic),
      lisansBitis: admin.firestore.Timestamp.fromDate(lisans.bitis),
      lisansSuresiDoldu: lisansOzetiHesap.suresiDoldu,
      guncelleme: admin.firestore.FieldValue.serverTimestamp(),
      ...(id ? {} : { olusturma: admin.firestore.FieldValue.serverTimestamp() }),
    },
    { merge: true }
  );

  // Lisans yenilendiğinde süre dolumuyla pasife alınan kullanıcıları geri aç.
  if (lisansYenile && aktif && !lisansOzetiHesap.suresiDoldu) {
    const pasifler = await db()
      .collection(tenantUsersPath(tenantRef.id))
      .where("lisansPasifeAlindi", "==", true)
      .get();
    if (!pasifler.empty) {
      const batch = db().batch();
      for (const doc of pasifler.docs) {
        batch.set(doc.ref, { aktif: true, lisansPasifeAlindi: false }, { merge: true });
      }
      await batch.commit();
    }
  }

  return {
    id: tenantRef.id,
    kod,
    ad,
    aktif,
    lisansTipi: lisans.tip,
    lisansBaslangic: lisans.baslangic.toISOString(),
    lisansBitis: lisans.bitis.toISOString(),
    lisansKalanGun: lisansOzetiHesap.kalanGun,
    lisansSuresiDoldu: lisansOzetiHesap.suresiDoldu,
  };
});

export const platformListTenantUsers = onCall(async (request) => {
  if (!request.auth) throw new HttpsError("unauthenticated", "Giriş gerekli");
  await assertPlatformAdmin(request.auth.uid);

  const tenantId = ((request.data?.tenantId as string) ?? "").trim();
  if (!tenantId) throw new HttpsError("invalid-argument", "tenantId gerekli");

  const snap = await db().collection(tenantUsersPath(tenantId)).get();
  return snap.docs.map((d) => ({
    uid: d.id,
    kullaniciAdi: d.data().kullaniciAdi ?? "",
    eposta: d.data().eposta ?? "",
    adSoyad: d.data().adSoyad ?? "",
    rol: d.data().rol ?? "",
    aktif: d.data().aktif !== false,
    saha: d.data().saha ?? "",
  }));
});

export const platformSaveTenantUser = onCall(async (request) => {
  if (!request.auth) throw new HttpsError("unauthenticated", "Giriş gerekli");
  await assertPlatformAdmin(request.auth.uid);

  const tenantId = ((request.data?.tenantId as string) ?? "").trim();
  const kullaniciAdi = (request.data?.kullaniciAdi as string) ?? "";
  const eposta = ((request.data?.eposta as string) ?? "").trim();
  const adSoyad = ((request.data?.adSoyad as string) ?? "").trim();
  const rol = ((request.data?.rol as string) ?? "").trim();
  const saha = ((request.data?.saha as string) ?? "").trim();
  const aktif = request.data?.aktif !== false;
  const sifre = (request.data?.sifre as string) ?? "";
  const uid = ((request.data?.uid as string) ?? "").trim();

  if (!tenantId || !isValidUsername(kullaniciAdi) || !eposta || !adSoyad || !rol) {
    throw new HttpsError("invalid-argument", "Zorunlu alanlar eksik.");
  }

  if (isReservedUsername(kullaniciAdi)) {
    throw new HttpsError(
      "invalid-argument",
      "Bu kullanıcı adı platform için rezervedir. Firmaya atanamaz (ör. platform, yonetici)."
    );
  }

  await readTenant(tenantId);
  const normalized = normalizeUsername(kullaniciAdi);
  const emailLower = eposta.toLowerCase();

  // Platform sahibi hesabı firmaya kullanıcı olarak eklenemez.
  if (uid && (await isPlatformAdminUid(uid))) {
    throw new HttpsError(
      "failed-precondition",
      "Platform yöneticisi hiçbir firmaya bağlanamaz. Ayrı bir firma kullanıcısı oluşturun."
    );
  }

  const adminEmails = await platformAdminEpostalSet();
  if (adminEmails.has(emailLower)) {
    throw new HttpsError(
      "failed-precondition",
      "Platform yöneticisi e-postası firmaya atanamaz. Firma için ayrı e-posta kullanın."
    );
  }

  const unameRef = db().collection("usernames").doc(normalized);
  const existingUname = await unameRef.get();
  if (existingUname.exists && existingUname.data()?.uid !== uid) {
    throw new HttpsError("already-exists", "Bu kullanıcı adı başka bir hesapta kayıtlı.");
  }

  let userUid = uid;
  if (!userUid) {
    if (!sifre || sifre.length < 6) {
      throw new HttpsError("invalid-argument", "Yeni kullanıcı için en az 6 karakter şifre gerekli.");
    }
    try {
      const created = await admin.auth().createUser({ email: eposta, password: sifre, displayName: adSoyad });
      userUid = created.uid;
    } catch (e: unknown) {
      const msg = (e as { code?: string }).code;
      if (msg === "auth/email-already-exists") {
        throw new HttpsError("already-exists", "Bu e-posta zaten kayıtlı.");
      }
      throw new HttpsError("internal", "Kullanıcı oluşturulamadı.");
    }
  } else if (sifre) {
    await admin.auth().updateUser(userUid, { password: sifre });
  }

  const userPath = tenantUserPath(tenantId, userUid);
  await db().doc(userPath).set(
    {
      tenantId,
      kullaniciAdi: normalized,
      eposta,
      adSoyad,
      rol,
      saha: saha || null,
      aktif,
      guncelleme: admin.firestore.FieldValue.serverTimestamp(),
    },
    { merge: true }
  );

  await unameRef.set({ tenantId, uid: userUid, eposta });

  await admin.auth().setCustomUserClaims(userUid, { tenantId });

  return { uid: userUid, kullaniciAdi: normalized, eposta, adSoyad, rol, aktif };
});

export const platformBootstrapAdmin = onCall(async (request) => {
  if (!request.auth) throw new HttpsError("unauthenticated", "Giriş gerekli");

  const mevcut = await db().collection("platform_admins").doc(request.auth.uid).get();
  if (mevcut.exists && mevcut.data()?.aktif !== false) {
    const epostaMevcut =
      (request.auth.token.email as string | undefined) ??
      String(mevcut.data()?.eposta ?? "");
    const detachMevcut = await detachPlatformAdminFromTenants(request.auth.uid, epostaMevcut);
    return {
      ok: true,
      uid: request.auth.uid,
      already: true,
      removedUsers: detachMevcut.removedUsers,
      removedUsernames: detachMevcut.removedUsernames,
    };
  }

  const snap = await db().collection("platform_admins").where("aktif", "==", true).limit(1).get();
  const herhangi = snap.empty
    ? await db().collection("platform_admins").limit(1).get()
    : snap;

  if (!herhangi.empty && herhangi.docs[0].id !== request.auth.uid) {
    throw new HttpsError(
      "failed-precondition",
      "Platform yöneticisi zaten tanımlı. Firebase Console → Firestore → platform_admins koleksiyonundan mevcut admin kaydını silip yeniden deneyin."
    );
  }

  const eposta = request.auth.token.email as string | undefined;
  await db().collection("platform_admins").doc(request.auth.uid).set({
    eposta: eposta ?? "",
    aktif: true,
    olusturma: admin.firestore.FieldValue.serverTimestamp(),
  });

  // Platform sahibi hiçbir firmaya bağlı olmamalı — varsa ayır.
  const detach = await detachPlatformAdminFromTenants(request.auth.uid, eposta);

  return {
    ok: true,
    uid: request.auth.uid,
    already: false,
    removedUsers: detach.removedUsers,
    removedUsernames: detach.removedUsernames,
  };
});

/** Platform sahibini tüm firma kullanıcı listelerinden ayırır (temizlik). */
export const platformDetachSelf = onCall(async (request) => {
  if (!request.auth) throw new HttpsError("unauthenticated", "Giriş gerekli");
  await assertPlatformAdmin(request.auth.uid);

  const eposta = (request.auth.token.email as string | undefined) ?? "";
  const detach = await detachPlatformAdminFromTenants(request.auth.uid, eposta);
  return { ok: true, ...detach };
});

/** Eski kök /users koleksiyonundaki aktif kullanıcıları seçili firmaya taşır. */
export const platformImportLegacyUsers = onCall(async (request) => {
  if (!request.auth) throw new HttpsError("unauthenticated", "Giriş gerekli");
  await assertPlatformAdmin(request.auth.uid);

  const tenantId = ((request.data?.tenantId as string) ?? "").trim();
  if (!tenantId) throw new HttpsError("invalid-argument", "tenantId gerekli");

  await readTenant(tenantId);

  const snap = await db().collection("users").get();
  let imported = 0;
  let skipped = 0;
  const oleyler: string[] = [];

  const adminEmails = await platformAdminEpostalSet();
  const platformUids = new Set(
    (await db().collection("platform_admins").where("aktif", "==", true).get()).docs.map((d) => d.id)
  );

  for (const doc of snap.docs) {
    const data = doc.data();
    if (data.aktif === false) {
      skipped++;
      continue;
    }

    // Platform sahibi firmaya taşınmaz.
    if (platformUids.has(doc.id)) {
      skipped++;
      oleyler.push(`${doc.id}: platform yöneticisi — firma aktarımı atlandı`);
      continue;
    }

    const eposta = String(data.eposta ?? data.email ?? "").trim().toLowerCase();
    if (eposta && adminEmails.has(eposta)) {
      skipped++;
      oleyler.push(`${eposta}: platform e-postası — firma aktarımı atlandı`);
      continue;
    }

    const adSoyad = String(data.adSoyad ?? data.displayName ?? eposta).trim();
    const rol = String(data.rol ?? "Saha").trim() || "Saha";
    const saha = String(data.saha ?? "").trim();

    let usernameRaw = String(data.kullaniciAdi ?? "").trim();
    if (!usernameRaw && eposta.includes("@")) {
      usernameRaw = eposta.split("@")[0] ?? "";
    }
    if (!usernameRaw) {
      usernameRaw = `user_${doc.id.slice(0, 8)}`;
    }

    let normalized = normalizeUsername(usernameRaw);
    if (isReservedUsername(normalized)) {
      normalized = normalizeUsername(`firma_${normalized}`);
    }
    if (!isValidUsername(normalized)) {
      normalized = normalizeUsername(`u_${doc.id.slice(0, 10)}`);
    }

    // Çakışmada uid ekle
    const unameRef = db().collection("usernames").doc(normalized);
    const existing = await unameRef.get();
    if (existing.exists && existing.data()?.uid !== doc.id) {
      normalized = normalizeUsername(`${normalized}_${doc.id.slice(0, 4)}`);
    }

    if (!isValidUsername(normalized)) {
      skipped++;
      oleyler.push(`${doc.id}: kullanıcı adı üretilemedi`);
      continue;
    }

    await db()
      .doc(tenantUserPath(tenantId, doc.id))
      .set(
        {
          tenantId,
          kullaniciAdi: normalized,
          eposta: eposta || `${doc.id}@migrated.local`,
          adSoyad: adSoyad || normalized,
          rol,
          saha: saha || null,
          aktif: true,
          guncelleme: admin.firestore.FieldValue.serverTimestamp(),
          legacyImported: true,
        },
        { merge: true }
      );

    await db()
      .collection("usernames")
      .doc(normalized)
      .set({
        tenantId,
        uid: doc.id,
        eposta: eposta || `${doc.id}@migrated.local`,
      });

    await admin.auth().setCustomUserClaims(doc.id, { tenantId }).catch(() => undefined);
    imported++;
  }

  return { imported, skipped, total: snap.size, uyarilar: oleyler.slice(0, 20) };
});

import { onCall, HttpsError } from "firebase-functions/v2/https";
import * as admin from "firebase-admin";
import {
  isValidUsername,
  normalizeUsername,
  signInWithEmailPassword,
  tenantUserPath,
  tenantUsersPath,
  webApiKey,
} from "../lib/saas";

const db = () => admin.firestore();

async function assertPlatformAdmin(uid: string): Promise<void> {
  const doc = await db().collection("platform_admins").doc(uid).get();
  if (!doc.exists || doc.data()?.aktif === false) {
    throw new HttpsError("permission-denied", "Platform yöneticisi yetkisi gerekli.");
  }
}

async function readTenant(tenantId: string) {
  const doc = await db().collection("tenants").doc(tenantId).get();
  if (!doc.exists) throw new HttpsError("not-found", "Firma bulunamadı.");
  return { id: doc.id, ...doc.data() } as {
    id: string;
    kod?: string;
    ad?: string;
    aktif?: boolean;
  };
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

  const tenant = await readTenant(lookup.tenantId);
  if (tenant.aktif === false) {
    throw new HttpsError("failed-precondition", "TENANT_INACTIVE");
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
  } catch {
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
  return snap.docs.map((d) => ({
    id: d.id,
    kod: d.data().kod ?? "",
    ad: d.data().ad ?? "",
    aktif: d.data().aktif !== false,
  }));
});

export const platformSaveTenant = onCall(async (request) => {
  if (!request.auth) throw new HttpsError("unauthenticated", "Giriş gerekli");
  await assertPlatformAdmin(request.auth.uid);

  const id = ((request.data?.id as string) ?? "").trim();
  const kod = ((request.data?.kod as string) ?? "").trim();
  const ad = ((request.data?.ad as string) ?? "").trim();
  const aktif = request.data?.aktif !== false;

  if (!kod || !ad) {
    throw new HttpsError("invalid-argument", "Firma kodu ve adı zorunludur.");
  }

  const tenantRef = id
    ? db().collection("tenants").doc(id)
    : db().collection("tenants").doc();

  const kodSnap = await db()
    .collection("tenants")
    .where("kod", "==", kod)
    .limit(1)
    .get();
  if (!kodSnap.empty && kodSnap.docs[0].id !== tenantRef.id) {
    throw new HttpsError("already-exists", "Bu firma kodu zaten kayıtlı.");
  }

  await tenantRef.set(
    {
      kod,
      ad,
      aktif,
      guncelleme: admin.firestore.FieldValue.serverTimestamp(),
      ...(id ? {} : { olusturma: admin.firestore.FieldValue.serverTimestamp() }),
    },
    { merge: true }
  );

  return { id: tenantRef.id, kod, ad, aktif };
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

  await readTenant(tenantId);
  const normalized = normalizeUsername(kullaniciAdi);

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
    return { ok: true, uid: request.auth.uid, already: true };
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

  return { ok: true, uid: request.auth.uid, already: false };
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

  for (const doc of snap.docs) {
    const data = doc.data();
    if (data.aktif === false) {
      skipped++;
      continue;
    }

    const eposta = String(data.eposta ?? data.email ?? "").trim().toLowerCase();
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

import { onCall, HttpsError } from "firebase-functions/v2/https";
import * as admin from "firebase-admin";
import * as zlib from "zlib";
import { promisify } from "util";
import { tenantUsersPath } from "../lib/saas";
import { wipeTenantOperationalData } from "./resetTenantData";

const gzip = promisify(zlib.gzip);
const gunzip = promisify(zlib.gunzip);

const db = () => admin.firestore();

async function assertPlatformAdmin(uid: string): Promise<void> {
  const doc = await db().collection("platform_admins").doc(uid).get();
  if (!doc.exists || doc.data()?.aktif === false) {
    throw new HttpsError("permission-denied", "Platform yöneticisi yetkisi gerekli.");
  }
}

/** Minimal store-method ZIP (tek dosya). */
function zipSingleFile(fileName: string, data: Buffer): Buffer {
  const nameBuf = Buffer.from(fileName, "utf8");
  const crc = crc32(data);
  const local = Buffer.alloc(30 + nameBuf.length);
  local.writeUInt32LE(0x04034b50, 0);
  local.writeUInt16LE(20, 4);
  local.writeUInt16LE(0, 6);
  local.writeUInt16LE(0, 8);
  local.writeUInt16LE(0, 10);
  local.writeUInt16LE(0, 12);
  local.writeUInt32LE(crc >>> 0, 14);
  local.writeUInt32LE(data.length, 18);
  local.writeUInt32LE(data.length, 22);
  local.writeUInt16LE(nameBuf.length, 26);
  local.writeUInt16LE(0, 28);
  nameBuf.copy(local, 30);

  const central = Buffer.alloc(46 + nameBuf.length);
  central.writeUInt32LE(0x02014b50, 0);
  central.writeUInt16LE(20, 4);
  central.writeUInt16LE(20, 6);
  central.writeUInt16LE(0, 8);
  central.writeUInt16LE(0, 10);
  central.writeUInt16LE(0, 12);
  central.writeUInt16LE(0, 14);
  central.writeUInt32LE(crc >>> 0, 16);
  central.writeUInt32LE(data.length, 20);
  central.writeUInt32LE(data.length, 24);
  central.writeUInt16LE(nameBuf.length, 28);
  central.writeUInt16LE(0, 30);
  central.writeUInt16LE(0, 32);
  central.writeUInt16LE(0, 34);
  central.writeUInt16LE(0, 36);
  central.writeUInt32LE(0, 38);
  central.writeUInt32LE(0, 42);
  nameBuf.copy(central, 46);

  const end = Buffer.alloc(22);
  end.writeUInt32LE(0x06054b50, 0);
  end.writeUInt16LE(0, 4);
  end.writeUInt16LE(0, 6);
  end.writeUInt16LE(1, 8);
  end.writeUInt16LE(1, 10);
  end.writeUInt32LE(central.length, 12);
  end.writeUInt32LE(local.length + data.length, 16);
  end.writeUInt16LE(0, 20);

  return Buffer.concat([local, data, central, end]);
}

function crc32(buf: Buffer): number {
  let c = ~0;
  for (let i = 0; i < buf.length; i++) {
    c ^= buf[i];
    for (let k = 0; k < 8; k++) {
      c = c & 1 ? (0xedb88320 ^ (c >>> 1)) : c >>> 1;
    }
  }
  return ~c;
}

function unzipSingleJson(zipBuf: Buffer): Buffer {
  // PK\x03\x04 local header
  if (zipBuf.length < 30 || zipBuf.readUInt32LE(0) !== 0x04034b50) {
    // gzip fallback (eski/basit paket)
    if (zipBuf[0] === 0x1f && zipBuf[1] === 0x8b) {
      return zlib.gunzipSync(zipBuf);
    }
    throw new HttpsError("invalid-argument", "Geçersiz yedek dosyası.");
  }
  const nameLen = zipBuf.readUInt16LE(26);
  const extraLen = zipBuf.readUInt16LE(28);
  const method = zipBuf.readUInt16LE(8);
  const compSize = zipBuf.readUInt32LE(18);
  const start = 30 + nameLen + extraLen;
  const payload = zipBuf.subarray(start, start + compSize);
  if (method === 0) return Buffer.from(payload);
  if (method === 8) return zlib.inflateRawSync(payload);
  throw new HttpsError("invalid-argument", "Desteklenmeyen ZIP sıkıştırma.");
}

function serializeFirestore(value: unknown): unknown {
  if (value == null) return value;
  if (value instanceof admin.firestore.Timestamp) {
    return { __ts: value.toDate().toISOString() };
  }
  if (Array.isArray(value)) return value.map(serializeFirestore);
  if (typeof value === "object") {
    const out: Record<string, unknown> = {};
    for (const [k, v] of Object.entries(value as Record<string, unknown>)) {
      out[k] = serializeFirestore(v);
    }
    return out;
  }
  return value;
}

function reviveFirestore(value: unknown): unknown {
  if (value == null) return value;
  if (Array.isArray(value)) return value.map(reviveFirestore);
  if (typeof value === "object") {
    const obj = value as Record<string, unknown>;
    if (typeof obj.__ts === "string") {
      const d = new Date(obj.__ts);
      if (!Number.isNaN(d.getTime())) return admin.firestore.Timestamp.fromDate(d);
    }
    const out: Record<string, unknown> = {};
    for (const [k, v] of Object.entries(obj)) {
      out[k] = reviveFirestore(v);
    }
    return out;
  }
  return value;
}

export const platformBackupTenant = onCall(
  { timeoutSeconds: 540, memory: "1GiB" },
  async (request) => {
    if (!request.auth) throw new HttpsError("unauthenticated", "Giriş gerekli");
    await assertPlatformAdmin(request.auth.uid);

    const tenantId = String(request.data?.tenantId ?? "").trim();
    if (!tenantId) throw new HttpsError("invalid-argument", "tenantId gerekli");

    const tenantSnap = await db().collection("tenants").doc(tenantId).get();
    if (!tenantSnap.exists) throw new HttpsError("not-found", "Firma bulunamadı.");

    const veriSnap = await db().collection(`tenants/${tenantId}/veri`).get();
    const usersSnap = await db().collection(tenantUsersPath(tenantId)).get();

    const packageJson = {
      format: "satinalma-platform-backup-v1",
      createdAt: new Date().toISOString(),
      createdBy: request.auth.uid,
      tenantId,
      tenant: serializeFirestore({ id: tenantSnap.id, ...tenantSnap.data() }),
      veri: veriSnap.docs.map((d) => ({
        id: d.id,
        data: serializeFirestore(d.data()),
      })),
      users: usersSnap.docs.map((d) => {
        const data = { ...(d.data() as Record<string, unknown>) };
        delete data.sifre;
        delete data.password;
        delete data.passwordHash;
        return { id: d.id, data: serializeFirestore(data) };
      }),
    };

    const jsonBuf = Buffer.from(JSON.stringify(packageJson), "utf8");
    // JSON'u gzip'leyip zip içine koy (boyut + tek dosya).
    const gz = await gzip(jsonBuf);
    const zipBuf = zipSingleFile("backup.json.gz", gz);

    const stamp = new Date().toISOString().replace(/[:.]/g, "-");
    const path = `platform-backups/${tenantId}/${stamp}.zip`;
    const file = admin.storage().bucket().file(path);
    await file.save(zipBuf, {
      contentType: "application/zip",
      metadata: {
        metadata: {
          tenantId,
          createdBy: request.auth.uid,
          format: "satinalma-platform-backup-v1",
        },
      },
    });

    const [downloadUrl] = await file.getSignedUrl({
      action: "read",
      expires: Date.now() + 60 * 60 * 1000,
    });

    return {
      ok: true,
      tenantId,
      path,
      downloadUrl,
      sizeBytes: zipBuf.length,
      veriCount: veriSnap.size,
      userCount: usersSnap.size,
    };
  }
);

export const platformRestoreTenant = onCall(
  { timeoutSeconds: 540, memory: "1GiB" },
  async (request) => {
    if (!request.auth) throw new HttpsError("unauthenticated", "Giriş gerekli");
    await assertPlatformAdmin(request.auth.uid);

    const tenantId = String(request.data?.tenantId ?? "").trim();
    const storagePath = String(request.data?.storagePath ?? "").trim();
    const restoreUsers = request.data?.restoreUsers !== false;

    if (!tenantId) throw new HttpsError("invalid-argument", "tenantId gerekli");
    if (!storagePath.startsWith(`platform-backups/${tenantId}/`)) {
      throw new HttpsError(
        "invalid-argument",
        "storagePath bu firmaya ait platform-backups yolu olmalı."
      );
    }

    const tenantSnap = await db().collection("tenants").doc(tenantId).get();
    if (!tenantSnap.exists) throw new HttpsError("not-found", "Firma bulunamadı.");

    const [zipBuf] = await admin.storage().bucket().file(storagePath).download();
    const gzOrJson = unzipSingleJson(zipBuf);
    let jsonBuf: Buffer;
    try {
      jsonBuf = gzOrJson[0] === 0x1f && gzOrJson[1] === 0x8b
        ? await gunzip(gzOrJson)
        : gzOrJson;
    } catch {
      jsonBuf = gzOrJson;
    }

    let paket: {
      format?: string;
      tenantId?: string;
      veri?: Array<{ id: string; data: Record<string, unknown> }>;
      users?: Array<{ id: string; data: Record<string, unknown> }>;
    };
    try {
      paket = JSON.parse(jsonBuf.toString("utf8"));
    } catch {
      throw new HttpsError("invalid-argument", "Yedek JSON okunamadı.");
    }

    if (paket.tenantId && paket.tenantId !== tenantId) {
      throw new HttpsError(
        "failed-precondition",
        "Yedek başka bir firmaya ait."
      );
    }

    const veriSifirlamaUtc = Date.now();
    const nowIso = new Date().toISOString();
    const veriRoot = db().collection(`tenants/${tenantId}/veri`);
    let restoredDocs = 0;

    for (const item of paket.veri ?? []) {
      if (!item?.id) continue;
      const data = reviveFirestore(item.data) as Record<string, unknown>;
      await veriRoot.doc(item.id).set(
        {
          ...data,
          veriSifirlamaUtc,
          updatedAt: nowIso,
          updatedBy: request.auth.uid,
          resetInProgress: false,
        },
        { merge: true }
      );
      restoredDocs++;
    }

    // Damga her durumda güncellenir — istemciler eski cache'i düşürsün.
    await veriRoot.doc("satinalma_ayarlar").set(
      { veriSifirlamaUtc, updatedAt: nowIso, updatedBy: request.auth.uid },
      { merge: true }
    );

    let restoredUsers = 0;
    if (restoreUsers) {
      for (const item of paket.users ?? []) {
        if (!item?.id) continue;
        const data = reviveFirestore(item.data) as Record<string, unknown>;
        delete data.sifre;
        delete data.password;
        delete data.passwordHash;
        await db()
          .doc(`${tenantUsersPath(tenantId)}/${item.id}`)
          .set(data, { merge: true });
        restoredUsers++;
      }
    }

    return {
      ok: true,
      tenantId,
      restoredDocs,
      restoredUsers,
      veriSifirlamaUtc,
    };
  }
);

export const platformResetTenantData = onCall(
  { timeoutSeconds: 540, memory: "1GiB" },
  async (request) => {
    if (!request.auth) throw new HttpsError("unauthenticated", "Giriş gerekli");
    await assertPlatformAdmin(request.auth.uid);

    const tenantId = String(request.data?.tenantId ?? "").trim();
    if (!tenantId) throw new HttpsError("invalid-argument", "tenantId gerekli");

    const tenantSnap = await db().collection("tenants").doc(tenantId).get();
    if (!tenantSnap.exists) throw new HttpsError("not-found", "Firma bulunamadı.");

    const scopeRaw = String(request.data?.scope ?? "all").trim().toLowerCase();
    const scope = scopeRaw === "satinalma" ? "satinalma" : "all";

    return wipeTenantOperationalData(tenantId, request.auth.uid, scope);
  }
);

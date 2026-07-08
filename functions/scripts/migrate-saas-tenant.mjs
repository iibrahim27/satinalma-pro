/**
 * Mevcut tek-kiracı verisini tenants/{tenantId}/ altına taşır.
 * Kullanım: node functions/scripts/migrate-saas-tenant.mjs <tenantId> <firmaKodu> <firmaAd>
 */
import admin from "firebase-admin";

const projectId = "satinalmapro-8e7da";
const tenantId = process.argv[2];
const kod = process.argv[3];
const ad = process.argv[4];

if (!tenantId || !kod || !ad) {
  console.error("Kullanım: node migrate-saas-tenant.mjs <tenantId> <kod> <ad>");
  process.exit(1);
}

admin.initializeApp({ projectId });
const db = admin.firestore();

const veriDocs = [
  "alinan_malzemeler",
  "stok",
  "stok_hareketleri",
  "agrega",
  "cimento",
  "akaryakit",
  "filo",
  "satinalma_talepler",
  "satinalma_ayarlar",
  "finansman_gelir",
  "uygulama_ayarlar",
  "bildirimler",
  "eposta_sablonlari",
  "medya",
];

async function copyDoc(from, to) {
  const snap = await db.doc(from).get();
  if (!snap.exists) return false;
  await db.doc(to).set(snap.data(), { merge: true });
  return true;
}

async function main() {
  await db.collection("tenants").doc(tenantId).set(
    { kod, ad, aktif: true, migratedAt: admin.firestore.FieldValue.serverTimestamp() },
    { merge: true }
  );

  let copied = 0;
  for (const docId of veriDocs) {
    if (await copyDoc(`veri/${docId}`, `tenants/${tenantId}/veri/${docId}`)) copied++;
  }

  const users = await db.collection("users").get();
  for (const user of users.docs) {
    const data = user.data();
    await db.doc(`tenants/${tenantId}/users/${user.id}`).set({ ...data, tenantId }, { merge: true });
    if (data.kullaniciAdi && data.eposta) {
      const key = String(data.kullaniciAdi).trim().toLowerCase();
      await db.collection("usernames").doc(key).set({
        tenantId,
        uid: user.id,
        eposta: data.eposta,
      });
    }
  }

  console.log(`Tenant ${tenantId} hazır. ${copied} veri belgesi taşındı. ${users.size} kullanıcı.`);
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});

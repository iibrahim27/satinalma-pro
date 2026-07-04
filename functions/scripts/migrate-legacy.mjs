/**
 * Mevcut legacy talepleri procurement_requests koleksiyonuna yazar.
 */
import admin from "firebase-admin";
import { dualWriteToEnterprise, parseLegacyTalepler } from "../lib/lib/legacyTalep.js";

process.env.MIGRATION_DUAL_WRITE = "true";
admin.initializeApp({ projectId: "satinalmapro-8e7da" });

async function main() {
  const doc = await admin.firestore().doc("veri/satinalma_talepler").get();
  const talepler = parseLegacyTalepler(doc.data()?.json);
  let migrated = 0;

  for (const t of talepler) {
    if (!t.id) continue;
    await dualWriteToEnterprise(t);
    migrated++;
  }

  console.log(`✓ ${migrated} talep procurement_requests'e senkronlandı`);
}

main().catch((e) => {
  console.error("Migrasyon hatası:", e.message ?? e);
  process.exit(1);
});

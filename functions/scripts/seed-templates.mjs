/**
 * notification_templates koleksiyonunu Firestore'a yazar.
 * Kullanım:
 *   set GOOGLE_APPLICATION_CREDENTIALS=Satinalma Pro\fcm-service-account.json
 *   npm run seed:templates
 */
import admin from "firebase-admin";
import { NOTIFICATION_TEMPLATES } from "../lib/lib/templates.js";

const projectId = "satinalmapro-8e7da";

if (!process.env.GOOGLE_APPLICATION_CREDENTIALS) {
  console.error("GOOGLE_APPLICATION_CREDENTIALS tanımlı değil.");
  process.exit(1);
}

admin.initializeApp({ projectId });
const db = admin.firestore();

async function main() {
  const batch = db.batch();
  let count = 0;

  for (const [code, tpl] of Object.entries(NOTIFICATION_TEMPLATES)) {
    batch.set(db.collection("notification_templates").doc(code), tpl);
    count++;
  }

  await batch.commit();
  console.log(`✓ ${count} bildirim şablonu yazıldı → notification_templates`);
}

main().catch((e) => {
  console.error("Seed hatası:", e.message ?? e);
  process.exit(1);
});

/**
 * Firmalar ve kullanıcılar hariç operasyonel veriyi siler.
 *
 * Korunan:
 *   - tenants/{id} belge alanları (ad, kod, lisans…)
 *   - tenants/{id}/users/**
 *   - usernames/**
 *   - platform_admins/**
 *   - Auth kullanıcıları
 *
 * Silinen:
 *   - tenants/{id}/veri/**
 *   - tenants/{id}/procurement_requests/**
 *   - kök /veri/**
 *   - kök /procurement_requests/**
 *   - sla_tracking, notification_dispatch_queue, notification_queue,
 *     notification_delivery_log (opsiyonel kuyruklar)
 *
 * Kullanım:
 *   set GOOGLE_APPLICATION_CREDENTIALS=...\fcm-service-account.json
 *   node functions/scripts/wipe-operational-data.mjs --yes
 *   node functions/scripts/wipe-operational-data.mjs --yes --tenant <tenantId>
 */
import admin from "firebase-admin";

const projectId = "satinalmapro-8e7da";
const args = process.argv.slice(2);
const confirm = args.includes("--yes");
const tenantIdx = args.indexOf("--tenant");
const onlyTenant = tenantIdx >= 0 ? (args[tenantIdx + 1] || "").trim() : "";

if (!confirm) {
  console.error("Bu işlem geri alınamaz. Onay için --yes ekleyin.");
  console.error("Örnek: node wipe-operational-data.mjs --yes");
  console.error("Tek firma: node wipe-operational-data.mjs --yes --tenant <tenantId>");
  process.exit(1);
}

admin.initializeApp({ projectId });
const db = admin.firestore();

const ROOT_COLLECTIONS_TO_WIPE = [
  "veri",
  "procurement_requests",
  "sla_tracking",
  "notification_dispatch_queue",
  "notification_queue",
  "notification_delivery_log",
];

async function wipeCollection(path) {
  const ref = db.collection(path);
  const snap = await ref.limit(1).get();
  if (snap.empty) {
    console.log(`  (boş) ${path}`);
    return 0;
  }
  console.log(`  siliniyor: ${path}`);
  await db.recursiveDelete(ref);
  return 1;
}

async function wipeTenantOperational(tenantId) {
  console.log(`\nTenant: ${tenantId}`);
  await wipeCollection(`tenants/${tenantId}/veri`);
  await wipeCollection(`tenants/${tenantId}/procurement_requests`);
  // users / notification_inbox korunur
}

async function main() {
  console.log(`Proje: ${projectId}`);
  console.log("Korunan: tenants meta, users, usernames, platform_admins");
  console.log("Silinen: veri, procurement_requests, kuyruklar\n");

  if (onlyTenant) {
    const doc = await db.collection("tenants").doc(onlyTenant).get();
    if (!doc.exists) {
      console.error(`Tenant bulunamadı: ${onlyTenant}`);
      process.exit(1);
    }
    await wipeTenantOperational(onlyTenant);
  } else {
    const tenants = await db.collection("tenants").get();
    console.log(`${tenants.size} firma bulundu.`);
    for (const t of tenants.docs) {
      await wipeTenantOperational(t.id);
    }
  }

  console.log("\nKök koleksiyonlar:");
  for (const name of ROOT_COLLECTIONS_TO_WIPE) {
    await wipeCollection(name);
  }

  console.log("\nTamam. Firmalar ve kullanıcılar duruyor; operasyonel veri silindi.");
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});

/**
 * Service account ile Firestore + Storage kurallarını deploy eder.
 * Kullanım: GOOGLE_APPLICATION_CREDENTIALS=... node scripts/deploy-rules.mjs
 */
import fs from "fs";
import path from "path";
import { fileURLToPath } from "url";
import { GoogleAuth } from "google-auth-library";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(__dirname, "../..");
const projectId = "satinalmapro-8e7da";

const auth = new GoogleAuth({
  scopes: [
    "https://www.googleapis.com/auth/cloud-platform",
    "https://www.googleapis.com/auth/firebase",
  ],
});

async function api(method, urlPath, body) {
  const client = await auth.getClient();
  const res = await client.request({
    url: `https://firebaserules.googleapis.com/v1/${urlPath}`,
    method,
    data: body,
  });
  return res.data;
}

async function deployRules(name, rulesFile, releaseName) {
  const source = fs.readFileSync(path.join(root, rulesFile), "utf8");
  console.log(`→ ${name} kuralları yükleniyor (${rulesFile})...`);

  const ruleset = await api("POST", `projects/${projectId}/rulesets`, {
    source: { files: [{ name: "rules", content: source }] },
  });

  await api("POST", `projects/${projectId}/releases`, {
    name: `projects/${projectId}/releases/${releaseName}`,
    rulesetName: ruleset.name,
  });

  console.log(`✓ ${name} deploy edildi (${ruleset.name})`);
}

async function main() {
  if (!process.env.GOOGLE_APPLICATION_CREDENTIALS) {
    console.error("GOOGLE_APPLICATION_CREDENTIALS tanımlı değil.");
    process.exit(1);
  }

  try {
    await deployRules("Firestore", "firestore.rules", "cloud.firestore");
    await deployRules("Storage", "storage.rules", "firebase.storage");
    console.log("\nKurallar başarıyla yayınlandı.");
  } catch (e) {
    const code = e.response?.data?.error?.code ?? e.code;
    if (code === 403 || code === "PERMISSION_DENIED") {
      console.error("\nService account deploy yetkisi yok.");
      console.error("Çözüm: proje kökünde deploy-firebase.ps1 çalıştırın (firebase login gerekir).");
      console.error("Firebase Console > IAM > service account'a 'Firebase Rules Admin' rolü de eklenebilir.");
    }
    throw e;
  }
}

main().catch((e) => {
  console.error("Deploy hatası:", e.response?.data ?? e.message ?? e);
  process.exit(1);
});

export function normalizeUsername(raw: string): string {
  return raw
    .trim()
    .toLowerCase()
    .replace(/ı/g, "i")
    .replace(/ğ/g, "g")
    .replace(/ü/g, "u")
    .replace(/ş/g, "s")
    .replace(/ö/g, "o")
    .replace(/ç/g, "c");
}

/** Firma kullanıcıları için rezerve — platform sahibi bu isimleri kullanamaz / firmaya yazılamaz. */
export const REZERVE_KULLANICI_ADLARI = new Set([
  "platform",
  "platformadmin",
  "platform_admin",
  "yonetici",
  "yonetim",
  "owner",
  "satinalmayonetici",
  "satinalma_yonetici",
  "saas",
  "root",
  "system",
  "sistem",
]);

export function isReservedUsername(raw: string): boolean {
  const n = normalizeUsername(raw);
  if (REZERVE_KULLANICI_ADLARI.has(n)) return true;
  return n.startsWith("platform") || n.startsWith("yonetici_");
}

export function isValidUsername(raw: string): boolean {
  const n = normalizeUsername(raw);
  return /^[a-z0-9][a-z0-9._-]{2,31}$/.test(n);
}

export function tenantUsersPath(tenantId: string): string {
  return `tenants/${tenantId}/users`;
}

export function tenantUserPath(tenantId: string, uid: string): string {
  return `${tenantUsersPath(tenantId)}/${uid}`;
}

export async function signInWithEmailPassword(
  apiKey: string,
  email: string,
  password: string
): Promise<{
  idToken: string;
  refreshToken: string;
  localId: string;
  email?: string;
  expiresIn: string;
}> {
  const res = await fetch(
    `https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key=${apiKey}`,
    {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ email, password, returnSecureToken: true }),
    }
  );
  const json = (await res.json()) as Record<string, unknown>;
  if (!res.ok) {
    const msg = (json?.error as { message?: string })?.message ?? "INVALID_LOGIN";
    throw new Error(msg);
  }
  return json as {
    idToken: string;
    refreshToken: string;
    localId: string;
    email?: string;
    expiresIn: string;
  };
}

export function webApiKey(): string {
  const key =
    process.env.WEB_API_KEY?.trim() ||
    process.env.IDENTITY_TOOLKIT_API_KEY?.trim();
  if (!key) {
    throw new Error("WEB_API_KEY ortam değişkeni tanımlı değil.");
  }
  return key;
}

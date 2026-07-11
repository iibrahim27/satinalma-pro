import * as admin from "firebase-admin";
import { tenantUserPath } from "./saas";

export async function sendFcmToUser(
  uid: string,
  title: string,
  body: string,
  data: Record<string, string>,
  tenantId?: string
): Promise<boolean> {
  const resolvedTenantId = tenantId?.trim() || data.tenantId?.trim();
  if (!resolvedTenantId) {
    console.warn(`FCM skipped for ${uid}: tenantId yok`);
    return false;
  }

  const userDoc = await admin.firestore().doc(tenantUserPath(resolvedTenantId, uid)).get();
  const token = userDoc.data()?.fcmToken as string | undefined;
  if (!token) return false;

  try {
    await admin.messaging().send({
      token,
      // data-only: tıklanınca MainActivity extras ile ilgili işleme gider (notification payload launcher'ı açar).
      data: {
        ...Object.fromEntries(
          Object.entries(data).map(([k, v]) => [k, String(v ?? "")])
        ),
        title: String(title ?? ""),
        body: String(body ?? ""),
      },
      android: {
        priority: "high",
      },
    });
    return true;
  } catch (e) {
    console.warn(`FCM failed for ${uid}:`, e);
    return false;
  }
}

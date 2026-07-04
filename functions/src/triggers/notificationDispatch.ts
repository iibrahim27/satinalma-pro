import { onDocumentCreated } from "firebase-functions/v2/firestore";
import { sendFcmToUser } from "../lib/fcm";

export const onNotificationDispatchCreate = onDocumentCreated(
  "notification_dispatch_queue/{docId}",
  async (event) => {
    const data = event.data?.data();
    if (!data || data.status !== "pending") return;

    const uid = data.uid as string;
    const title = data.title as string;
    const body = data.body as string;
    const payload = (data.data ?? {}) as Record<string, string>;

    const sent = await sendFcmToUser(uid, title, body, payload);
    await event.data?.ref.update({
      status: sent ? "sent" : "failed",
      sentAt: new Date(),
    });
  }
);

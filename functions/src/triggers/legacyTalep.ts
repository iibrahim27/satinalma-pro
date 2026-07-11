import { onDocumentWritten } from "firebase-functions/v2/firestore";
import { fanOutNotification } from "../lib/fanOut";
import {
  diffTalepChanges,
  dualWriteToEnterprise,
  parseLegacyTalepler,
} from "../lib/legacyTalep";
import { statusTransitionEvent } from "../lib/templates";

export const onLegacyTalepWrite = onDocumentWritten(
  "tenants/{tenantId}/veri/satinalma_talepler",
  async (event) => {
    const tenantId = event.params.tenantId as string;
    const beforeJson = event.data?.before?.data()?.json as string | undefined;
    const afterJson = event.data?.after?.data()?.json as string | undefined;
    if (!afterJson) return;

    const beforeList = parseLegacyTalepler(beforeJson);
    const afterList = parseLegacyTalepler(afterJson);
    const changes = diffTalepChanges(beforeList, afterList);

    for (const { before, after } of changes) {
      try {
        await dualWriteToEnterprise(after, tenantId);
      } catch (err) {
        console.error(
          `dualWriteToEnterprise failed tenant=${tenantId} talep=${after?.id ?? "?"}`,
          err
        );
      }

      const eventCode = statusTransitionEvent(before?.durum ?? null, after.durum ?? "");
      if (!eventCode || !after.id) continue;

      try {
        await fanOutNotification({
          tenantId,
          eventCode,
          entityType: "procurement_request",
          entityId: after.id,
          talepNo: after.talepNo,
          talepEden: after.talepEden,
          talepEdenUid: after.talepEdenUid || after.olusturanUid,
          // İşlemi yapan bilinmiyorsa oluşturanı hariç tutma (yanlışlıkla alıcıyı kesmesin).
          // Red/onayda talepEdenUid hedefleme zaten actor dışı kalır.
          createdBy: undefined,
          siparisNo: after.siparisNo,
          saha: after.saha,
        });
      } catch (err) {
        console.error(
          `fanOutNotification failed tenant=${tenantId} event=${eventCode} talep=${after.id}`,
          err
        );
      }
    }
  }
);

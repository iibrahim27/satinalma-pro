import * as admin from "firebase-admin";
import { setGlobalOptions } from "firebase-functions/v2";

admin.initializeApp();
setGlobalOptions({ region: "europe-west1", maxInstances: 10 });

export { onLegacyTalepWrite } from "./triggers/legacyTalep";
export { onNotificationDispatchCreate } from "./triggers/notificationDispatch";
export { checkApprovalSla, cleanupTempStorage, dailyDigest } from "./scheduled/jobs";
export {
  seedNotificationTemplates,
  migrateLegacyBatch,
  manualFanOut,
  markInboxRead,
} from "./callables/admin";
export {
  loginWithUsername,
  passwordResetByUsername,
  platformListTenants,
  platformSaveTenant,
  platformListTenantUsers,
  platformSaveTenantUser,
  platformBootstrapAdmin,
  platformDetachSelf,
  platformImportLegacyUsers,
} from "./callables/saas";

import * as admin from "firebase-admin";
import { setGlobalOptions } from "firebase-functions/v2";

admin.initializeApp();
setGlobalOptions({ region: "europe-west1", maxInstances: 10 });

export { onLegacyTalepWrite, onLegacyAyarlarWrite } from "./triggers/legacyTalep";
export { onNotificationDispatchCreate } from "./triggers/notificationDispatch";
export { checkApprovalSla, cleanupTempStorage, dailyDigest, checkTenantLicenses } from "./scheduled/jobs";
export {
  seedNotificationTemplates,
  migrateLegacyBatch,
  manualFanOut,
  markInboxRead,
} from "./callables/admin";
export { resetTenantOperationalData } from "./callables/resetTenantData";
export {
  platformBackupTenant,
  platformRestoreTenant,
  platformResetTenantData,
} from "./callables/platformTenantOps";
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
  platformDeleteTenantUser,
  platformDeleteTenant,
} from "./callables/saas";

package com.satinalmapro.android.core.saas

import com.satinalmapro.shared.model.UserRole

/** Kiracıya özel FCM topic adları — firmalar birbirinin bildirim kanalına abone olamaz. */
object TenantFcmTopics {
    fun sanitizeTenantId(tenantId: String): String =
        tenantId.replace(Regex("[^a-zA-Z0-9]"), "_").take(40)

    fun forRole(role: UserRole, tenantId: String): String =
        "t_${sanitizeTenantId(tenantId)}_${role.fcmTopic}"

    fun allForTenant(tenantId: String): List<String> =
        UserRole.entries.map { forRole(it, tenantId) }
}

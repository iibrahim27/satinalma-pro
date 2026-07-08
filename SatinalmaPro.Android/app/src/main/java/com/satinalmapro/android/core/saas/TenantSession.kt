package com.satinalmapro.android.core.saas

object TenantSession {
    @Volatile
    private var tenantId: String? = null

    @Volatile
    private var tenantName: String? = null

    fun set(tenantId: String, tenantName: String? = null) {
        this.tenantId = tenantId.trim()
        this.tenantName = tenantName?.trim()
    }

    fun tenantId(): String? = tenantId

    fun requireTenantId(): String =
        tenantId?.takeIf { it.isNotBlank() }
            ?: throw IllegalStateException("Kiracı oturumu bulunamadı. Tekrar giriş yapın.")

    fun clear() {
        tenantId = null
        tenantName = null
    }
}

package com.satinalmapro.android.core.saas

object TenantSession {
    @Volatile
    private var tenantId: String? = null

    @Volatile
    private var tenantName: String? = null

    @Volatile
    private var license: TenantLicense? = null

    fun set(tenantId: String, tenantName: String? = null, license: TenantLicense? = null) {
        this.tenantId = tenantId.trim()
        this.tenantName = tenantName?.trim()
        this.license = license
    }

    fun setLicense(license: TenantLicense?) {
        this.license = license
    }

    fun tenantId(): String? = tenantId

    fun tenantName(): String? = tenantName

    fun license(): TenantLicense? = license

    fun requireTenantId(): String =
        tenantId?.takeIf { it.isNotBlank() }
            ?: throw IllegalStateException("Kiracı oturumu bulunamadı. Tekrar giriş yapın.")

    fun clear() {
        tenantId = null
        tenantName = null
        license = null
    }
}

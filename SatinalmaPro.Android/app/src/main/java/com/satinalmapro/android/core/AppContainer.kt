package com.satinalmapro.android.core

import android.content.Context
import com.satinalmapro.android.BuildConfig
import com.satinalmapro.android.core.model.AppNotification
import com.satinalmapro.android.core.model.BildirimRecord
import com.satinalmapro.android.core.model.BildirimTipleri
import com.satinalmapro.android.core.model.SatinalmaAyarlar
import com.satinalmapro.android.core.model.UygulamaAyarlar
import com.satinalmapro.android.core.model.ManagedUser
import com.satinalmapro.android.core.model.UserProfile
import com.satinalmapro.android.core.model.DashboardActivity
import com.satinalmapro.android.core.model.DashboardCard
import com.satinalmapro.android.core.model.StokHareket
import com.satinalmapro.android.core.model.StokHareketTipi
import com.satinalmapro.android.core.model.StokKaydi
import com.satinalmapro.android.core.model.OnaylananMalzemeSatiri
import com.satinalmapro.android.core.model.AgregaKaydi
import com.satinalmapro.android.core.model.AlinanMalzemeKaydi
import com.satinalmapro.android.core.model.CimentoKaydi
import com.satinalmapro.android.core.model.TalepItem
import com.satinalmapro.android.core.model.TalepQueue
import com.satinalmapro.android.core.model.UpdateManifest
import com.satinalmapro.android.core.saas.TenantLicense
import com.satinalmapro.android.core.saas.TenantSession
import com.satinalmapro.android.data.firebase.SaaSAuthClient
import com.satinalmapro.android.core.NetworkMonitor
import com.satinalmapro.android.core.roles.KullaniciRolleri
import com.satinalmapro.android.core.roles.OnaylananMalzemeOlusturucu
import com.satinalmapro.android.core.roles.MalzemeOneri
import com.satinalmapro.android.core.roles.TalepKuyrugu
import com.satinalmapro.android.core.roles.BildirimRota
import com.satinalmapro.android.data.repository.BildirimRepository
import com.satinalmapro.android.data.repository.StokRepository
import com.satinalmapro.android.data.repository.ModulRepository
import com.satinalmapro.android.data.repository.SettingsRepository
import kotlinx.coroutines.sync.Mutex
import kotlinx.coroutines.sync.withLock
import com.satinalmapro.android.data.repository.TalepRepository
import com.satinalmapro.android.data.detail.PurchaseRequestDetailController
import com.satinalmapro.shared.filter.detail.PurchaseRequestDetailAction
import com.satinalmapro.android.data.firebase.FirebaseAuthClient
import com.satinalmapro.android.data.firebase.FirebaseConfig
import com.satinalmapro.android.data.firebase.FirestoreClient
import com.satinalmapro.android.data.firebase.HttpClients
import com.satinalmapro.android.data.local.OfflineCache
import com.satinalmapro.android.data.local.RequestDraft
import com.satinalmapro.android.data.local.BildirimHatirlatmaDeposu
import com.satinalmapro.android.data.local.BiometricPreferences
import com.satinalmapro.android.data.local.RequestDraftStore
import com.satinalmapro.android.services.ApkUpdateInstaller
import com.satinalmapro.android.services.LocalNotificationHelper
import com.satinalmapro.android.services.SatinalmaPdfBaglam
import com.satinalmapro.android.services.StokTeslimFisiHelper
import com.satinalmapro.android.security.BiometricAuthHelper
import com.satinalmapro.android.core.helpers.BildirimLog
import com.satinalmapro.android.core.helpers.BildirimMantikAnahtari
import com.satinalmapro.android.services.FcmPushService
import com.satinalmapro.android.services.FcmSubscriptionHelper
import com.satinalmapro.android.services.FirebaseAuthBridge
import com.satinalmapro.android.services.MyFirebaseMessagingService
import kotlin.coroutines.resume
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.suspendCancellableCoroutine
import org.json.JSONArray
import org.json.JSONObject
import java.io.File
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale

class AppContainer(private val context: Context) {
    enum class UpdateInstallResult { SUCCESS, NEEDS_PERMISSION, FAILED }

    data class UpdateCheckResult(
        val available: Boolean,
        val manifest: UpdateManifest? = null,
        val error: String? = null
    )

    private val prefs = context.getSharedPreferences("satinalma_pro", Context.MODE_PRIVATE)

    init {
        ensureLoginCredentialsFresh()
    }

    val config: FirebaseConfig = loadFirebaseConfig(context)
    val auth = FirebaseAuthClient(config)
    val firestore = FirestoreClient(config, auth)
    val apkInstaller = ApkUpdateInstaller(context)
    private val offlineCache = OfflineCache(context)
    val draftStore = RequestDraftStore(context)
    private val hatirlatmaDeposu = BildirimHatirlatmaDeposu(context)
    val biometricPreferences = BiometricPreferences(context)
    private val fcmPush = FcmPushService(context, config.projectId, firestore)
    val bildirimler = BildirimRepository(firestore, auth, fcmPush)
    val talepler = TalepRepository(firestore, auth, bildirimler)
    val talepDetayController = PurchaseRequestDetailController(talepler)
    val stokRepo = StokRepository(firestore, auth)
    val modulRepo = ModulRepository(firestore, auth)
    val settingsRepo = SettingsRepository(firestore, auth)

    private val _uygulamaAyarlar = MutableStateFlow(UygulamaAyarlar())
    val uygulamaAyarlar: StateFlow<UygulamaAyarlar> = _uygulamaAyarlar.asStateFlow()

    private val _satinalmaAyarlar = MutableStateFlow(SatinalmaAyarlar())
    val satinalmaAyarlar: StateFlow<SatinalmaAyarlar> = _satinalmaAyarlar.asStateFlow()

    private val _settingsUsers = MutableStateFlow<List<ManagedUser>>(emptyList())
    val settingsUsers: StateFlow<List<ManagedUser>> = _settingsUsers.asStateFlow()

    private val _user = MutableStateFlow<UserProfile?>(null)
    val user: StateFlow<UserProfile?> = _user.asStateFlow()

    private val _talepler = MutableStateFlow<List<TalepItem>>(emptyList())
    val talepList: StateFlow<List<TalepItem>> = _talepler.asStateFlow()

    private val _stok = MutableStateFlow<List<StokKaydi>>(emptyList())
    val stokList: StateFlow<List<StokKaydi>> = _stok.asStateFlow()

    private val _stokHareketleri = MutableStateFlow<List<StokHareket>>(emptyList())
    val stokHareketleri: StateFlow<List<StokHareket>> = _stokHareketleri.asStateFlow()

    private val _agrega = MutableStateFlow<List<AgregaKaydi>>(emptyList())
    val agregaList: StateFlow<List<AgregaKaydi>> = _agrega.asStateFlow()

    private val _cimento = MutableStateFlow<List<CimentoKaydi>>(emptyList())
    val cimentoList: StateFlow<List<CimentoKaydi>> = _cimento.asStateFlow()

    private val _alinanMalzemeKayitlari = MutableStateFlow<List<AlinanMalzemeKaydi>>(emptyList())
    val alinanMalzemeKayitlari: StateFlow<List<AlinanMalzemeKaydi>> = _alinanMalzemeKayitlari.asStateFlow()

    private val _materialNames = MutableStateFlow<List<String>>(emptyList())
    val materialNames: StateFlow<List<String>> = _materialNames.asStateFlow()

    private val _notifications = MutableStateFlow<List<AppNotification>>(emptyList())
    val notifications: StateFlow<List<AppNotification>> = _notifications.asStateFlow()

    var pendingRoute: String? = null
    var pendingNotificationId: String? = null

    private val syncMutex = Mutex()
    private var lastNotifCleanupMs = 0L
    private val notifCleanupIntervalMs = 3 * 60 * 1000L

    suspend fun restoreSession(): Boolean {
        val json = prefs.getString(KEY_SESSION, null) ?: return false
        return try {
            val obj = JSONObject(json)
            val uid = obj.optString("uid")
            val tenantId = prefs.getString("saved_tenant_id", null)?.takeIf { it.isNotBlank() }
            if (tenantId.isNullOrBlank()) {
                // SaaS öncesi / bozuk oturum — giriş ekranına düş.
                clearBrokenSession()
                return false
            }

            val lisans = if (obj.has("lisansBitisUtc") || obj.has("lisansTip")) {
                TenantLicense(
                    tip = obj.optString("lisansTip", "deneme"),
                    bitisUtc = obj.optString("lisansBitisUtc").ifBlank { null },
                    kalanGun = if (obj.has("lisansKalanGun")) obj.optInt("lisansKalanGun") else null,
                    suresiDoldu = obj.optInt("lisansKalanGun", 1) <= 0
                )
            } else null
            if (lisans?.suresiDoldu == true) {
                clearBrokenSession()
                return false
            }
            TenantSession.set(tenantId, license = lisans)

            loadProfileCache(uid)?.takeIf { it.active }?.let { _user.value = it }
            auth.restoreSession(
                obj.getString("refreshToken"),
                uid.ifBlank { null },
                obj.optString("email").ifBlank { null }
            )
            loadProfile(allowCached = true)
            // FCM token/topic splash'ı bloklamasın — arka planda dene.
            CoroutineScope(Dispatchers.IO).launch {
                runCatching { registerFcmIfNeeded() }
                runCatching { syncFcmRoleSubscription(showSuccessToast = false) }
            }
            true
        } catch (_: Exception) {
            clearBrokenSession()
            false
        }
    }

    private fun clearBrokenSession() {
        runCatching { FirebaseAuthBridge.signOut() }
        auth.clear()
        TenantSession.clear()
        _user.value = null
        prefs.edit()
            .remove(KEY_SESSION)
            .remove("saved_tenant_id")
            .apply()
    }

    fun hasPersistedSession(): Boolean = prefs.contains(KEY_SESSION)

    fun rememberedLoginEmail(): String {
        if (!prefs.getBoolean(KEY_REMEMBER_ME, false)) return ""
        return prefs.getString(KEY_SAVED_EMAIL, null)?.trim().orEmpty()
    }

    fun clearRememberedLogin() {
        prefs.edit()
            .putBoolean(KEY_REMEMBER_ME, false)
            .remove(KEY_SAVED_EMAIL)
            .apply()
    }

    fun shouldRequireBiometricUnlock(): Boolean {
        if (!hasPersistedSession() || _user.value == null) return false
        if (!isBiometricAvailable()) return false
        return biometricPreferences.isEnabled()
    }

    fun isBiometricAvailable(): Boolean = BiometricAuthHelper.isHardwareAvailable(context)

    suspend fun changePassword(currentPassword: String, newPassword: String) {
        if (!NetworkMonitor.isOnline(context)) {
            throw IllegalStateException(NetworkError.translate("Unable to resolve host"))
        }
        val email = auth.email ?: _user.value?.email
            ?: throw IllegalStateException("Oturum bulunamadı")
        if (newPassword.length < 6) {
            throw IllegalStateException("Yeni şifre en az 6 karakter olmalıdır")
        }
        auth.changePassword(email, currentPassword, newPassword)
        prefs.edit().putString(KEY_SESSION, auth.sessionJson(true)).apply()
    }

    suspend fun sendPasswordResetEmail(username: String) {
        if (!NetworkMonitor.isOnline(context)) {
            throw IllegalStateException(NetworkError.translate("Unable to resolve host"))
        }
        if (!config.isConfigured) {
            throw IllegalStateException("Firebase ayarları yapılandırılmamış.")
        }
        val trimmed = username.trim()
        if (trimmed.isBlank()) {
            throw IllegalStateException("Kullanıcı adı girin")
        }
        SaaSAuthClient(config).passwordResetByUsername(trimmed)
    }

    suspend fun login(username: String, password: String, rememberMe: Boolean) {
        if (!NetworkMonitor.isOnline(context)) {
            throw IllegalStateException(NetworkError.translate("Unable to resolve host"))
        }
        if (!config.isConfigured) {
            throw IllegalStateException("Firebase ayarları yapılandırılmamış.")
        }
        try {
            val saas = SaaSAuthClient(config)
            val result = saas.loginWithUsername(username, password)
            auth.applySaaSLogin(result)
            TenantSession.set(result.tenantId, result.tenantAd, result.lisans)
            val email = result.eposta ?: ""
            runCatching { FirebaseAuthBridge.signIn(email, password) }
                .onFailure { ex -> BildirimLog.w("FCM_TOPIC", "Firebase Auth SDK senkronu atlandı: ${ex.message}") }
            loadProfile(allowCached = false)
            if (rememberMe) {
                prefs.edit()
                    .putString(KEY_SESSION, auth.sessionJson(true))
                    .putBoolean(KEY_REMEMBER_ME, true)
                    .putString(KEY_SAVED_EMAIL, username.trim())
                    .putString("saved_tenant_id", result.tenantId)
                    .apply()
            } else {
                prefs.edit()
                    .remove(KEY_SESSION)
                    .putBoolean(KEY_REMEMBER_ME, false)
                    .remove(KEY_SAVED_EMAIL)
                    .remove("saved_tenant_id")
                    .apply()
            }
            registerFcmIfNeeded()
            syncFcmRoleSubscription()
        } catch (e: Exception) {
            auth.clear()
            TenantSession.clear()
            prefs.edit().remove(KEY_SESSION).apply()
            throw if (e is IllegalStateException) e else IllegalStateException(NetworkError.translate(e.message))
        }
    }

    fun logout() {
        val uid = auth.uid
        runCatching { FcmSubscriptionHelper(context).unsubscribeAllRoleTopics() }
        runCatching { FirebaseAuthBridge.signOut() }
        auth.clear()
        TenantSession.clear()
        _user.value = null
        _materialNames.value = emptyList()
        _notifications.value = emptyList()
        _talepler.value = emptyList()
        _stok.value = emptyList()
        _stokHareketleri.value = emptyList()
        _agrega.value = emptyList()
        _cimento.value = emptyList()
        _alinanMalzemeKayitlari.value = emptyList()
        _uygulamaAyarlar.value = UygulamaAyarlar()
        _settingsUsers.value = emptyList()
        prefs.edit().remove(KEY_SESSION).remove("saved_tenant_id").apply()
        unregisterFcm(uid)
    }

    private fun unregisterFcm(uid: String?) {
        CoroutineScope(Dispatchers.IO).launch {
            if (!uid.isNullOrBlank()) {
                runCatching { firestore.updateFcmToken(uid, "") }
            }
            runCatching { com.google.firebase.messaging.FirebaseMessaging.getInstance().deleteToken() }
        }
    }

    suspend fun syncData() {
        if (!syncMutex.tryLock()) return
        try {
            val uid = auth.uid ?: return
            loadMaterialNames()
            loadTalepler()
            loadSatinalmaAyarlar()
            loadStok()
            loadModulKayitlari()
            loadNotifications(uid, cleanup = true)
            runCatching { loadUygulamaAyarlar() }
        } finally {
            syncMutex.unlock()
        }
    }

    suspend fun loadUygulamaAyarlar() {
        val ayarlar = settingsRepo.loadSettings()
        _uygulamaAyarlar.value = ayarlar
        if (KullaniciRolleri.isAdmin(_user.value?.role)) {
            _settingsUsers.value = settingsRepo.loadUsers(::parseUserFields)
        }
    }

    private suspend fun loadSatinalmaAyarlar() {
        _satinalmaAyarlar.value = runCatching { talepler.loadAyarlar() }.getOrDefault(SatinalmaAyarlar())
    }

    fun pdfBaglam(): SatinalmaPdfBaglam {
        val sat = _satinalmaAyarlar.value
        val uyg = _uygulamaAyarlar.value
        val firma = sat.firmaAdi.ifBlank { uyg.firmaAdi }.ifBlank { "Satınalma Pro" }
        return SatinalmaPdfBaglam(
            firmaAdi = firma,
            sefImzalari = sat.sefImzalari.filter { it.aktif },
            yonetimImzalari = sat.yonetimImzalari.filter { it.aktif }
        )
    }

    suspend fun saveUygulamaAyarlar(ayarlar: UygulamaAyarlar) {
        settingsRepo.saveSettings(ayarlar)
        _uygulamaAyarlar.value = ayarlar
    }

    suspend fun saveManagedUser(user: ManagedUser) {
        settingsRepo.saveUser(user)
        _settingsUsers.value = settingsRepo.loadUsers(::parseUserFields)
    }

    suspend fun createManagedUser(
        email: String,
        password: String,
        fullName: String,
        role: String,
        site: String,
        active: Boolean
    ) {
        settingsRepo.createUser(email, password, fullName, role, site, active)
        _settingsUsers.value = settingsRepo.loadUsers(::parseUserFields)
    }

    private suspend fun reloadTalepler() {
        _talepler.value = talepler.loadTalepler()
    }

    private suspend fun loadStok() {
        _stok.value = runCatching { stokRepo.loadStok() }.getOrDefault(emptyList())
        _stokHareketleri.value = runCatching { stokRepo.loadHareketler() }.getOrDefault(emptyList())
    }

    private suspend fun loadModulKayitlari() {
        _agrega.value = runCatching { modulRepo.loadAgrega() }.getOrDefault(emptyList())
        _cimento.value = runCatching { modulRepo.loadCimento() }.getOrDefault(emptyList())
        _alinanMalzemeKayitlari.value = runCatching { modulRepo.loadAlinanMalzemeler() }.getOrDefault(emptyList())
    }

    fun modulBugun(): String = modulRepo.bugun()

    suspend fun agregaKaydet(kayit: AgregaKaydi) {
        val role = _user.value?.role
        val list = _agrega.value.toMutableList()
        val index = list.indexOfFirst { it.id == kayit.id }
        if (index >= 0) list[index] = kayit else list.add(0, kayit)
        modulRepo.saveAgrega(list, role)
        _agrega.value = modulRepo.loadAgrega()
    }

    suspend fun agregaSil(id: String) {
        val role = _user.value?.role
        val list = _agrega.value.filter { it.id != id }
        modulRepo.saveAgrega(list, role)
        _agrega.value = list
    }

    suspend fun cimentoKaydet(kayit: CimentoKaydi) {
        val role = _user.value?.role
        val list = _cimento.value.toMutableList()
        val index = list.indexOfFirst { it.id == kayit.id }
        if (index >= 0) list[index] = kayit else list.add(0, kayit)
        modulRepo.saveCimento(list, role)
        _cimento.value = modulRepo.loadCimento()
    }

    suspend fun cimentoSil(id: String) {
        val role = _user.value?.role
        val list = _cimento.value.filter { it.id != id }
        modulRepo.saveCimento(list, role)
        _cimento.value = list
    }

    suspend fun alinanMalzemeKaydet(kayit: AlinanMalzemeKaydi) {
        val role = _user.value?.role
        val list = _alinanMalzemeKayitlari.value.toMutableList()
        val index = list.indexOfFirst { it.id == kayit.id }
        if (index >= 0) list[index] = kayit else list.add(0, kayit)
        modulRepo.saveAlinanMalzemeler(list, role)
        _alinanMalzemeKayitlari.value = modulRepo.loadAlinanMalzemeler()
    }

    suspend fun alinanMalzemeSil(id: String) {
        val role = _user.value?.role
        val list = _alinanMalzemeKayitlari.value.filter { it.id != id }
        modulRepo.saveAlinanMalzemeler(list, role)
        _alinanMalzemeKayitlari.value = list
    }

    suspend fun createRequest(
        site: String,
        aciklama: String,
        oncelik: String,
        kalemler: List<Triple<String, String, String>>
    ): TalepItem {
        val user = _user.value ?: throw IllegalStateException("Oturum gerekli")
        val talep = talepler.createWithKalemler(user, site, aciklama, oncelik, kalemler)
        reloadTalepler()
        refreshNotifications()
        draftStore.clear()
        return talep
    }

    fun loadDraft(): RequestDraft? = draftStore.load()

    fun saveDraft(draft: RequestDraft) = draftStore.save(draft)

    fun clearDraft() = draftStore.clear()

    private fun requireTeklifGirisYetkisi(user: UserProfile) {
        if (!KullaniciRolleri.canEnterQuotes(user.role)) {
            throw IllegalStateException("Teklif girişi yalnızca satınalma yetkisine açıktır")
        }
    }

    suspend fun addTeklif(talepId: String, firmaAdi: String, marka: String, vadeGunu: Int, teslimSuresi: String, odemeSekli: String, kalemFiyatlari: Map<String, Double>): TalepItem {
        val user = _user.value ?: throw IllegalStateException("Oturum gerekli")
        requireTeklifGirisYetkisi(user)
        val result = talepler.addTeklif(talepId, firmaAdi, marka, vadeGunu, teslimSuresi, odemeSekli, kalemFiyatlari)
        reloadTalepler()
        refreshNotifications()
        return result
    }

    suspend fun updateTeklif(
        talepId: String,
        teklifId: String,
        firmaAdi: String,
        marka: String,
        vadeGunu: Int,
        teslimSuresi: String,
        odemeSekli: String,
        kalemFiyatlari: Map<String, Double>
    ): TalepItem {
        val user = _user.value ?: throw IllegalStateException("Oturum gerekli")
        requireTeklifGirisYetkisi(user)
        val result = talepler.updateTeklif(talepId, teklifId, firmaAdi, marka, vadeGunu, teslimSuresi, odemeSekli, kalemFiyatlari)
        reloadTalepler()
        refreshNotifications()
        return result
    }

    suspend fun deleteTeklif(talepId: String, teklifId: String): TalepItem {
        val user = _user.value ?: throw IllegalStateException("Oturum gerekli")
        requireTeklifGirisYetkisi(user)
        val result = talepler.deleteTeklif(talepId, teklifId)
        reloadTalepler()
        refreshNotifications()
        return result
    }

    suspend fun satinalmaOnerisiSec(talepId: String, teklifId: String): TalepItem {
        val user = _user.value ?: throw IllegalStateException("Oturum gerekli")
        requireTeklifGirisYetkisi(user)
        val result = talepler.satinalmaOnerisiSec(talepId, teklifId)
        reloadTalepler()
        refreshNotifications()
        return result
    }

    suspend fun satinalmaOnerisiOtomatigeAl(talepId: String): TalepItem {
        val user = _user.value ?: throw IllegalStateException("Oturum gerekli")
        requireTeklifGirisYetkisi(user)
        val result = talepler.satinalmaOnerisiOtomatigeAl(talepId)
        reloadTalepler()
        refreshNotifications()
        return result
    }

    suspend fun sendQuotesToManagement(talepId: String): TalepItem {
        val user = _user.value ?: throw IllegalStateException("Oturum gerekli")
        requireTeklifGirisYetkisi(user)
        val result = talepler.sendQuotesToManagement(talepId, user)
        reloadTalepler()
        refreshNotifications()
        return result
    }

    suspend fun yonetimOnayla(talepId: String, teklifIste: Boolean): TalepItem {
        val user = _user.value ?: throw IllegalStateException("Oturum gerekli")
        val result = talepler.yonetimOnayla(talepId, user, teklifIste)
        reloadTalepler()
        refreshNotifications()
        return result
    }

    suspend fun yonetimReddet(talepId: String, gerekce: String): TalepItem {
        val user = _user.value ?: throw IllegalStateException("Oturum gerekli")
        val result = talepler.yonetimReddet(talepId, user, gerekce)
        reloadTalepler()
        refreshNotifications()
        return result
    }

    suspend fun applyTalepDetayAction(
        talepId: String,
        action: PurchaseRequestDetailAction,
        quoteId: String? = null,
        note: String? = null
    ): TalepItem {
        val user = _user.value ?: throw IllegalStateException("Oturum gerekli")
        val result = talepDetayController.apply(talepId, user, action, quoteId, note)
        reloadTalepler()
        refreshNotifications()
        return result
    }

    suspend fun yonetimTeklifOnayla(talepId: String, teklifId: String): TalepItem {
        val user = _user.value ?: throw IllegalStateException("Oturum gerekli")
        val result = talepler.yonetimTeklifOnayla(talepId, user, teklifId)
        reloadTalepler()
        refreshNotifications()
        return result
    }

    suspend fun kalemTeklifiAta(talepId: String, kalemId: String, teklifId: String?): TalepItem {
        val user = _user.value ?: throw IllegalStateException("Oturum gerekli")
        val result = talepler.kalemTeklifiAta(talepId, kalemId, teklifId)
        reloadTalepler()
        return result
    }

    suspend fun kalemBazliOnayla(talepId: String, kalemTeklifAtamalari: Map<String, String>? = null): TalepItem {
        val user = _user.value ?: throw IllegalStateException("Oturum gerekli")
        val result = talepler.kalemBazliOnayla(talepId, user, kalemTeklifAtamalari)
        reloadTalepler()
        refreshNotifications()
        return result
    }

    suspend fun teklifGeriGonder(talepId: String, gerekce: String?): TalepItem {
        val user = _user.value ?: throw IllegalStateException("Oturum gerekli")
        val result = talepler.teklifGeriGonder(talepId, user, gerekce)
        reloadTalepler()
        refreshNotifications()
        return result
    }

    suspend fun siparisVer(talepId: String): TalepItem {
        val user = _user.value ?: throw IllegalStateException("Oturum gerekli")
        val result = talepler.siparisVer(talepId, user)
        reloadTalepler()
        refreshNotifications()
        return result
    }

    suspend fun teklifsizFirmaFiyatKaydet(talepId: String, girdiler: List<Triple<String, String, Double>>): TalepItem {
        val user = _user.value ?: throw IllegalStateException("Oturum gerekli")
        requireTeklifGirisYetkisi(user)
        val result = talepler.teklifsizFirmaFiyatKaydet(talepId, girdiler)
        reloadTalepler()
        refreshNotifications()
        return result
    }

    suspend fun talepSil(talepId: String) {
        val user = _user.value ?: throw IllegalStateException("Oturum gerekli")
        talepler.talepSil(talepId, user)
        reloadTalepler()
        refreshNotifications()
    }

    suspend fun talepGuncelle(
        talepId: String,
        site: String,
        aciklama: String,
        talepTuru: String,
        kalemler: List<Triple<String, String, String>>
    ): TalepItem {
        val user = _user.value ?: throw IllegalStateException("Oturum gerekli")
        val result = talepler.talepGuncelle(talepId, user, site, aciklama, talepTuru, kalemler)
        reloadTalepler()
        refreshNotifications()
        return result
    }

    suspend fun malKabulVeDepoyaKaydet(
        talepId: String,
        kalemId: String,
        miktar: Double,
        firma: String,
        birimFiyat: Double,
        kategori: String,
        fisNo: String,
        teslimAlan: String,
        depoSaha: String,
        sahayaDirekt: Boolean = false,
        sahaHedef: String = ""
    ) {
        val user = _user.value ?: throw IllegalStateException("Oturum gerekli")
        val satir = OnaylananMalzemeOlusturucu.olustur(_talepler.value)
            .firstOrNull { it.talepId.equals(talepId, true) && it.kalemId.equals(kalemId, true) }
            ?: throw IllegalArgumentException("Kalem bulunamadı")

        val kat = kategori.trim().ifBlank { "Malzeme" }
        kategoriEkle(kat)

        talepler.malKabul(talepId, kalemId, miktar, user)

        val tarih = java.text.SimpleDateFormat("dd.MM.yyyy", java.util.Locale.getDefault()).format(java.util.Date())
        val indirildigiSaha = if (sahayaDirekt && sahaHedef.isNotBlank()) sahaHedef.trim() else depoSaha
        val kayit = AlinanMalzemeKaydi(
            tarih = tarih,
            faturaNo = fisNo,
            kategori = kat,
            malzemeHizmet = satir.malzeme,
            miktar = miktar,
            birim = satir.birim,
            birimFiyati = birimFiyat,
            tedarikci = firma,
            indirildigiSaha = indirildigiSaha,
            teslimAlan = teslimAlan,
            aciklama = "Satınalma: ${satir.talepNo}",
            satinalmaTalepId = talepId,
            satinalmaKalemId = kalemId
        ).hesaplaToplam()
        alinanMalzemeKaydet(kayit)

        val teslimEden = listOfNotNull(user.role.takeIf { it.isNotBlank() }, user.fullName.takeIf { it.isNotBlank() })
            .joinToString(" ")
        stokRepo.girisYap(
            user = user,
            malzeme = satir.malzeme,
            miktar = miktar,
            birim = satir.birim,
            kategori = kat,
            depo = depoSaha,
            birimMaliyet = birimFiyat,
            belgeNo = fisNo,
            teslimEden = teslimEden,
            teslimAlan = teslimAlan
        )
        if (sahayaDirekt && sahaHedef.isNotBlank()) {
            stokRepo.cikisYap(
                user = user,
                malzeme = satir.malzeme,
                miktar = miktar,
                depo = depoSaha,
                belgeNo = "$fisNo-Ç",
                teslimEden = teslimEden,
                teslimAlan = sahaHedef.trim()
            )
        }
        reloadTalepler()
        loadStok()
        refreshNotifications()
    }

    suspend fun sevkiyatiTamamla(talepId: String, kalemId: String) {
        val user = _user.value ?: throw IllegalStateException("Oturum gerekli")
        talepler.sevkiyatiTamamla(talepId, kalemId, user)
        reloadTalepler()
        refreshNotifications()
    }

    private suspend fun kategoriEkle(ad: String) {
        val ayarlar = _uygulamaAyarlar.value
        if (ayarlar.malzemeKategorileri.any { it.equals(ad, ignoreCase = true) }) return
        saveUygulamaAyarlar(ayarlar.copy(malzemeKategorileri = ayarlar.malzemeKategorileri + ad))
    }

    suspend fun stokGiris(malzeme: String, miktar: Double, birim: String, kategori: String, depo: String, birimMaliyet: Double, belgeNo: String, teslimEden: String, teslimAlan: String) {
        val user = _user.value ?: throw IllegalStateException("Oturum gerekli")
        stokRepo.girisYap(user, malzeme, miktar, birim, kategori, depo, birimMaliyet, belgeNo, teslimEden, teslimAlan)
        loadStok()
    }

    suspend fun stokCikis(malzeme: String, miktar: Double, depo: String, belgeNo: String, teslimEden: String, teslimAlan: String) {
        val user = _user.value ?: throw IllegalStateException("Oturum gerekli")
        stokRepo.cikisYap(user, malzeme, miktar, depo, belgeNo, teslimEden, teslimAlan)
        loadStok()
    }

    suspend fun stokSayim(malzeme: String, depo: String, sayimMiktari: Double) {
        val user = _user.value ?: throw IllegalStateException("Oturum gerekli")
        stokRepo.sayimYap(user, malzeme, depo, sayimMiktari)
        loadStok()
    }

    fun sonrakiGirisBelgeNo(): String =
        StokBelgeNoUretici.sonrakiGirisBelgeNo(_stokHareketleri.value)

    fun sonrakiCikisBelgeNo(): String =
        StokBelgeNoUretici.sonrakiCikisBelgeNo(_stokHareketleri.value)

    fun stokMalzemeOnerileri(query: String?, sadeceMevcut: Boolean = false): List<String> {
        val source = if (sadeceMevcut) {
            _stok.value.filter { it.mevcutMiktar > 0 }.map { it.malzemeAdi }
        } else {
            (_stok.value.map { it.malzemeAdi } + _materialNames.value).distinct()
        }
        return MalzemeOneri.filtrele(source, query, bosSorgudaGoster = sadeceMevcut)
    }

    suspend fun stokGirisCoklu(
        belgeNo: String,
        teslimAlan: String,
        satirlar: List<StokRepository.GirisSatir>
    ) {
        val user = _user.value ?: throw IllegalStateException("Oturum gerekli")
        stokRepo.girisYapCoklu(user, belgeNo, user.site.orEmpty(), teslimAlan, satirlar)
        loadStok()
    }

    suspend fun stokCikisCoklu(belgeNo: String, teslimAlan: String, satirlar: List<StokRepository.CikisSatir>) {
        val user = _user.value ?: throw IllegalStateException("Oturum gerekli")
        stokRepo.cikisYapCoklu(user, belgeNo, teslimAlan, satirlar)
        loadStok()
    }

    suspend fun stokHareketSil(hareketId: String) {
        val user = _user.value ?: throw IllegalStateException("Oturum gerekli")
        stokRepo.hareketSil(user, hareketId)
        loadStok()
    }

    suspend fun stokHareketGuncelle(
        hareketId: String,
        tarih: String,
        miktar: Double,
        belgeNo: String,
        islemYapan: String,
        teslimEdilen: String,
        aciklama: String
    ) {
        val user = _user.value ?: throw IllegalStateException("Oturum gerekli")
        stokRepo.hareketGuncelle(user, hareketId, tarih, miktar, belgeNo, islemYapan, teslimEdilen, aciklama)
        loadStok()
    }

    fun stokCikisFisiOlustur(
        belgeNo: String,
        teslimAlan: String,
        satirlar: List<StokRepository.CikisSatir>
    ): StokTeslimFisiHelper.Fis? {
        val user = _user.value ?: return null
        if (satirlar.isEmpty() || teslimAlan.isBlank()) return null
        val tarih = StokTeslimFisiHelper.bugunTarih()
        val teslimEden = StokTeslimFisiHelper.teslimEdenMetni(user.role, user.fullName)
        val firmaAdi = _uygulamaAyarlar.value.firmaAdi.ifBlank { "Satınalma Pro" }
        val depo = user.site.orEmpty()
        val fisSatirlar = satirlar.map { satir ->
            val stok = _stok.value.firstOrNull { it.malzemeAdi.equals(satir.malzeme, true) }
            val birim = stok?.birim?.ifBlank { "Adet" } ?: "Adet"
            StokTeslimFisiHelper.Satir(
                malzeme = satir.malzeme,
                miktar = StokTeslimFisiHelper.miktarMetni(satir.miktar, birim),
                birim = birim,
                depoSaha = depo
            )
        }
        return StokTeslimFisiHelper.Fis(
            belgeNo = belgeNo,
            tarih = tarih,
            teslimEden = teslimEden,
            teslimAlan = teslimAlan.trim(),
            satirlar = fisSatirlar,
            firmaAdi = firmaAdi
        )
    }

    suspend fun markAllNotificationsRead() {
        syncMutex.withLock {
            val user = _user.value ?: return
            val uid = auth.uid ?: return
            bildirimler.tumunuOkunduIsaretle(user)
            _notifications.value = _notifications.value.map { it.copy(read = true) }
            _notifications.value
                .filter { bildirimler.appNotificationKullaniciyaMi(it, user) }
                .forEach { hatirlatmaDeposu.temizle(bildirimler.appNotificationToRecord(it)) }
            runCatching { LocalNotificationHelper.cancelAll(context) }
            lastNotifCleanupMs = System.currentTimeMillis()
        }
    }

    suspend fun clearNotifications() {
        syncMutex.withLock {
            val user = _user.value ?: return
            val uid = auth.uid ?: return
            _notifications.value
                .filter { bildirimler.appNotificationKullaniciyaMi(it, user) }
                .forEach { hatirlatmaDeposu.temizle(bildirimler.appNotificationToRecord(it)) }
            bildirimler.temizle(user, _talepler.value)
            _notifications.value = emptyList()
            lastNotifCleanupMs = System.currentTimeMillis()
            runCatching { LocalNotificationHelper.cancelAll(context) }
        }
    }

    suspend fun markNotificationRead(id: String) {
        val uid = auth.uid ?: return
        val item = _notifications.value.firstOrNull { it.id.equals(id, true) }
        item?.let { hatirlatmaDeposu.temizle(bildirimler.appNotificationToRecord(it)) }
        val inboxId = item?.inboxDocId?.takeIf { it.isNotBlank() } ?: id
        runCatching { firestore.markInboxRead(uid, inboxId) }
        runCatching { bildirimler.okunduIsaretle(id) }
        _notifications.value = _notifications.value.map {
            if (it.id.equals(id, true)) it.copy(read = true) else it
        }
    }

    fun filteredTalepler(queue: TalepQueue): List<TalepItem> =
        talepler.filter(queue, _talepler.value, _user.value)

    fun findTalep(id: String?): TalepItem? =
        id?.let { target -> _talepler.value.firstOrNull { it.id.equals(target, true) } }

    fun dashboardData(): Pair<List<DashboardCard>, List<DashboardActivity>> {
        val user = _user.value
        val role = KullaniciRolleri.normalize(user?.role)
        val unread = _notifications.value.count { !it.read }
        val (cards, _) = talepler.dashboard(user, _talepler.value, unread)

        val finalCards = if (role == KullaniciRolleri.DEPO) {
            val hareketler = _stokHareketleri.value
            val girisSayisi = hareketler.count { it.hareketTipi.contains("Giri", ignoreCase = true) }
            listOf(
                DashboardCard("Stok Girişi", girisSayisi.toString(), "Kayıtlı giriş hareketi", "stok-giris"),
                DashboardCard("Stok Hareket", hareketler.size.toString(), "Tüm hareketler", "stok-hareket"),
                cards.firstOrNull { it.route == "bildirimler" }
                    ?: DashboardCard("Bildirim", unread.toString(), "Okunmamış", "bildirimler")
            )
        } else {
            cards
        }

        val activities = rolSonIslemler(
            user = user,
            talepler = _talepler.value,
            notifications = _notifications.value,
            hareketler = _stokHareketleri.value,
            stok = _stok.value
        )
        return finalCards to activities
    }

    private fun rolSonIslemler(
        user: UserProfile?,
        talepler: List<TalepItem>,
        notifications: List<AppNotification>,
        hareketler: List<StokHareket>,
        stok: List<StokKaydi>
    ): List<DashboardActivity> {
        val role = KullaniciRolleri.normalize(user?.role)
        val uid = user?.uid.orEmpty()
        val ad = user?.fullName.orEmpty()

        return when (role) {
            KullaniciRolleri.DEPO -> depoSonIslemler(hareketler)
            KullaniciRolleri.ATOLYE -> atolyeSonIslemler(stok, hareketler)
            KullaniciRolleri.YONETIM -> birlestir(
                bildirimSonIslemler(user, talepler, notifications, 4),
                talepSonIslemler(
                    role, uid, ad, talepler,
                    TalepQueue.GELEN_TALEPLER,
                    TalepQueue.TEKLIF_BEKLEYEN,
                    TalepQueue.TEKLIF_ONAY,
                    TalepQueue.RED_TALEPLER,
                    TalepQueue.ONAY_GECMISI
                ),
                limit = 6
            )
            KullaniciRolleri.SATINALMA -> birlestir(
                bildirimSonIslemler(user, talepler, notifications, 4),
                talepSonIslemler(
                    role, uid, ad, talepler,
                    TalepQueue.TEKLIF_GIR,
                    TalepQueue.ONAY_BEKLEYEN,
                    TalepQueue.TEKLIF_ONAY,
                    TalepQueue.GELEN_TALEPLER,
                    TalepQueue.RED_TALEPLER
                ),
                limit = 6
            )
            KullaniciRolleri.ADMIN -> birlestir(
                bildirimSonIslemler(user, talepler, notifications, 4),
                talepSonIslemler(
                    role, uid, ad, talepler,
                    TalepQueue.GELEN_TALEPLER,
                    TalepQueue.TEKLIF_GIR,
                    TalepQueue.TEKLIF_ONAY,
                    TalepQueue.ONAY_BEKLEYEN,
                    TalepQueue.RED_TALEPLER
                ),
                limit = 6
            )
            KullaniciRolleri.SEF, KullaniciRolleri.SAHA -> talepSonIslemler(
                role, uid, ad, talepler,
                TalepQueue.TALEPLERIM,
                TalepQueue.ONAY_BEKLEYEN,
                TalepQueue.ONAYLANAN_TALEPLER
            )
            else -> talepSonIslemler(
                role, uid, ad, talepler,
                TalepQueue.TALEPLERIM,
                TalepQueue.ONAY_BEKLEYEN
            )
        }
    }

    private fun birlestir(
        bildirimler: List<DashboardActivity>,
        talepler: List<DashboardActivity>,
        limit: Int
    ): List<DashboardActivity> = (bildirimler + talepler).take(limit)

    private fun bildirimSonIslemler(
        user: UserProfile?,
        talepler: List<TalepItem>,
        notifications: List<AppNotification>,
        limit: Int
    ): List<DashboardActivity> =
        notifications
            .filter {
                bildirimler.appNotificationKullaniciyaMi(it, user) &&
                    bildirimler.appNotificationGecerliMi(it, talepler)
            }
            .take(limit)
            .map { n ->
                DashboardActivity(
                    title = n.title.ifBlank { n.message },
                    subtitle = n.time.ifBlank { "Bildirim" },
                    status = if (n.read) "Okundu" else "Yeni",
                    route = n.route?.let { BildirimRota.safeRoute(it, user?.role) } ?: "bildirimler",
                    talepId = n.requestId
                )
            }

    private fun talepSonIslemler(
        role: String,
        uid: String,
        ad: String,
        talepler: List<TalepItem>,
        vararg kuyruklar: TalepQueue,
        limit: Int = 6
    ): List<DashboardActivity> =
        kuyruklar
            .flatMap { kuyruk -> TalepKuyrugu.filtre(kuyruk, talepler, uid, ad, role) }
            .filter { TalepKuyrugu.kayitli(it) }
            .distinctBy { it.id }
            .sortedByDescending { it.guncellemeUtc }
            .take(limit)
            .map { talepToActivity(it) }

    private fun talepToActivity(talep: TalepItem): DashboardActivity =
        DashboardActivity(
            title = talep.talepNo.ifBlank { "Talep" },
            subtitle = "${talep.malzemeOzeti} · ${talep.talepEden}",
            status = talep.durum,
            route = "talep-detay?id=${talep.id}",
            talepId = talep.id
        )

    private fun atolyeSonIslemler(stok: List<StokKaydi>, hareketler: List<StokHareket>): List<DashboardActivity> {
        val uyarilar = stok
            .filter { it.durumMetin != "Normal" }
            .sortedWith(
                compareBy<StokKaydi> { if (it.durumMetin == "Tükendi") 0 else 1 }
                    .thenBy { it.malzemeAdi }
            )
            .take(4)
            .map { kayit ->
                DashboardActivity(
                    title = kayit.malzemeAdi,
                    subtitle = "Mevcut: ${kayit.mevcutMiktar} ${kayit.birim}",
                    status = if (kayit.durumMetin == "Tükendi") "Kritik" else "Düşük",
                    route = "stok-durum",
                    talepId = null
                )
            }
        if (uyarilar.isNotEmpty()) return uyarilar
        return depoSonIslemler(hareketler).take(5)
    }

    private fun depoSonIslemler(hareketler: List<StokHareket>): List<DashboardActivity> =
        hareketler
            .sortedWith(
                compareByDescending<StokHareket> { it.tarih }
                    .thenByDescending { it.id }
            )
            .take(5)
            .map { h ->
                val route = when {
                    h.hareketTipi.equals(StokHareketTipi.GIRIS, ignoreCase = true) ||
                        h.hareketTipi.contains("Giri", ignoreCase = true) -> "stok-giris"
                    h.hareketTipi.equals(StokHareketTipi.CIKIS, ignoreCase = true) ||
                        h.hareketTipi.contains("Çık", ignoreCase = true) ||
                        h.hareketTipi.contains("Cik", ignoreCase = true) -> "stok-cikis"
                    else -> "stok-hareket"
                }
                val miktarMetin = if (h.birim.isBlank()) h.miktar.toString() else "${h.miktar} ${h.birim}"
                DashboardActivity(
                    title = h.belgeNo.ifBlank { h.malzemeAdi.ifBlank { "Stok hareketi" } },
                    subtitle = buildString {
                        append(h.malzemeAdi)
                        if (miktarMetin.isNotBlank()) {
                            append(" · ")
                            append(miktarMetin)
                        }
                        if (h.depoSaha.isNotBlank()) {
                            append(" · ")
                            append(h.depoSaha)
                        }
                    },
                    status = h.hareketTipi.ifBlank { "Hareket" },
                    route = route,
                    talepId = null
                )
            }

    fun approvedMaterials(): List<OnaylananMalzemeSatiri> = talepler.approvedMaterials(_talepler.value)

    fun siparisBekleyenMalzemeler(): List<OnaylananMalzemeSatiri> =
        talepler.siparisBekleyenMalzemeler(_talepler.value)

    suspend fun refreshNotifications() {
        val uid = auth.uid ?: return
        if (!syncMutex.tryLock()) return
        try {
            loadNotifications(uid, cleanup = false)
        } finally {
            syncMutex.unlock()
        }
    }

    suspend fun ensureFcmRegistered() = registerFcmIfNeeded()

    suspend fun refreshNotificationsWithCleanup() {
        val uid = auth.uid ?: return
        loadNotifications(uid, cleanup = true)
    }

    private suspend fun loadTalepler() {
        _talepler.value = runCatching {
            val loaded = talepler.loadTalepler()
            // Boş bulut sonucu geçerli offline cache'i silmesin (URL/geçici hata).
            if (loaded.isEmpty()) {
                val cached = offlineCache.loadTalepler()
                if (cached.isNotEmpty()) cached else loaded
            } else {
                offlineCache.saveTalepler(loaded)
                loaded
            }
        }.getOrElse { offlineCache.loadTalepler() }
    }

    fun materialSuggestions(query: String): List<String> =
        MalzemeOneri.filtrele(_materialNames.value, query)

    suspend fun checkForUpdate(): UpdateCheckResult {
        val urls = manifestUrls()
        if (urls.isEmpty()) {
            return UpdateCheckResult(false, error = "Güncelleme adresi yapılandırılmamış")
        }
        if (!NetworkMonitor.isOnline(context)) {
            return UpdateCheckResult(false, error = NetworkError.translate("Unable to resolve host"))
        }
        val errors = mutableListOf<String>()
        var best: UpdateManifest? = null
        for (baseUrl in urls) {
            try {
                val json = HttpClients.get(cacheBustUrl(baseUrl))
                if (json.isBlank()) {
                    errors.add("Boş yanıt: $baseUrl")
                    continue
                }
                val manifest = parseManifest(json)
                if (manifest.version.isBlank() || manifest.build <= 0) {
                    errors.add("Geçersiz manifest: $baseUrl")
                    continue
                }
                if (best == null || manifest.build > best!!.build ||
                    (manifest.build == best!!.build && versionGreater(manifest.version, best!!.version))
                ) {
                    best = manifest
                }
            } catch (e: Exception) {
                errors.add("${baseUrl}: ${e.message ?: e.javaClass.simpleName}")
            }
        }
        val manifest = best ?: return UpdateCheckResult(
            false,
            error = NetworkError.translate(errors.lastOrNull() ?: "Güncelleme sunucusuna ulaşılamadı")
        )
        val needsUpdate = manifest.build > BuildConfig.VERSION_CODE ||
            versionGreater(manifest.version, BuildConfig.VERSION_NAME)
        return UpdateCheckResult(needsUpdate, if (needsUpdate) manifest else null)
    }

    private fun cacheBustUrl(baseUrl: String): String =
        if (baseUrl.contains('?')) "$baseUrl&_=${System.currentTimeMillis()}"
        else "$baseUrl?_=${System.currentTimeMillis()}"

    private fun manifestUrls(): List<String> = buildList {
        add("https://raw.githubusercontent.com/iibrahim27/satinalma-pro/main/version.json")
        add("https://github.com/iibrahim27/satinalma-pro/raw/main/version.json")
        if (config.updateManifestUrl.isNotBlank()) add(config.updateManifestUrl.trim())
        add("https://cdn.jsdelivr.net/gh/iibrahim27/satinalma-pro@main/version.json")
    }.distinct()

    private fun apkDownloadUrls(manifest: UpdateManifest): List<String> = buildList {
        manifest.downloadUrlApk.takeIf { it.isNotBlank() }?.let(::add)
        add("https://github.com/iibrahim27/satinalma-pro/releases/download/v${manifest.version}/SatinalmaPro.apk")
    }.distinct()

    suspend fun downloadAndInstallUpdate(
        manifest: UpdateManifest,
        onProgress: (String, Int) -> Unit
    ): UpdateInstallResult {
        val urls = apkDownloadUrls(manifest)
        if (urls.isEmpty()) throw IllegalStateException("Güncelleme indirme adresi bulunamadı")
        onProgress("Güncelleme indiriliyor...", 10)
        val target = File(context.cacheDir, "SatinalmaPro_${manifest.version}_b${manifest.build}.apk")
        var lastError: Exception? = null
        for ((index, apkUrl) in urls.withIndex()) {
            try {
                HttpClients.download(apkUrl, target) { p ->
                    onProgress("İndiriliyor... %$p", 10 + (p * 0.8).toInt())
                }
                lastError = null
                break
            } catch (e: Exception) {
                lastError = e as? Exception ?: Exception(e.message ?: "İndirme başarısız")
                if (index == urls.lastIndex) throw lastError!!
            }
        }
        if (!validateDownloadedApk(target, manifest)) {
            target.delete()
            throw IllegalStateException("İndirilen APK geçersiz veya sürüm uyuşmuyor.")
        }
        prefs.edit().putString(KEY_PENDING_APK, target.absolutePath).apply()
        onProgress("Kurulum başlatılıyor...", 95)
        return installPendingApk(target)
    }

    fun retryPendingInstall(): UpdateInstallResult {
        val path = prefs.getString(KEY_PENDING_APK, null) ?: return UpdateInstallResult.FAILED
        return installPendingApk(File(path))
    }

    private fun installPendingApk(file: File): UpdateInstallResult {
        if (!file.exists()) {
            clearPendingApk()
            return UpdateInstallResult.FAILED
        }
        if (!apkInstaller.ensureInstallPermission()) return UpdateInstallResult.NEEDS_PERMISSION
        return try {
            apkInstaller.install(file)
            clearPendingApk()
            UpdateInstallResult.SUCCESS
        } catch (_: Exception) {
            UpdateInstallResult.FAILED
        }
    }

    fun clearPendingApk() {
        prefs.edit().remove(KEY_PENDING_APK).apply()
    }

    fun markUpdateSkipped(manifest: UpdateManifest) {
        prefs.edit().putInt(KEY_SKIPPED_UPDATE_BUILD, manifest.build).apply()
    }

    fun isUpdateSkipped(manifest: UpdateManifest): Boolean =
        prefs.getInt(KEY_SKIPPED_UPDATE_BUILD, 0) >= manifest.build

    private fun validateDownloadedApk(file: File, manifest: UpdateManifest): Boolean {
        val info = context.packageManager.getPackageArchiveInfo(
            file.absolutePath,
            android.content.pm.PackageManager.GET_ACTIVITIES
        ) ?: return true
        val apkBuild = if (android.os.Build.VERSION.SDK_INT >= 28) {
            info.longVersionCode
        } else {
            @Suppress("DEPRECATION")
            info.versionCode.toLong()
        }
        return apkBuild >= manifest.build
    }

    suspend fun checkAndApplyUpdate(onProgress: (String, Int) -> Unit): Boolean {
        val check = checkForUpdate()
        if (!check.available || check.manifest == null) return false
        return downloadAndInstallUpdate(check.manifest, onProgress) == UpdateInstallResult.SUCCESS
    }

    fun parseUserFields(uid: String, fields: JSONObject): UserProfile {
        fun field(name: String) = fields.optJSONObject(name)?.optString("stringValue").orEmpty()
        return UserProfile(
            uid = uid,
            email = field("eposta").ifBlank { field("email") },
            fullName = field("adSoyad").ifBlank { field("fullName") },
            role = KullaniciRolleri.normalize(field("rol").ifBlank { field("role") }),
            active = fields.optJSONObject("aktif")?.optBoolean("booleanValue")
                ?: fields.optJSONObject("active")?.optBoolean("booleanValue") ?: true,
            site = field("saha").ifBlank { field("site") }.ifBlank { null },
            phone = field("telefon").ifBlank { field("phone") }.ifBlank { null }
        )
    }

    private suspend fun loadProfile(allowCached: Boolean = false) {
        val uid = auth.uid ?: throw IllegalStateException("Oturum bulunamadı")
        try {
            val fields = firestore.readUser(uid)
                ?: throw IllegalStateException("Kullanıcı profili bulunamadı. Masaüstünden kullanıcı oluşturulmalıdır.")
            val profile = parseUserFields(uid, fields)
            if (!profile.active) throw IllegalStateException("Hesabınız pasif durumda")
            saveProfileCache(profile)
            _user.value = profile
        } catch (e: Exception) {
            if (allowCached && NetworkError.isNetworkRelated(e)) {
                loadProfileCache(uid)?.takeIf { it.active }?.let { cached ->
                    _user.value = cached
                    return
                }
            }
            throw if (e is IllegalStateException) e else IllegalStateException(NetworkError.translate(e.message))
        }
    }

    private fun saveProfileCache(profile: UserProfile) {
        val obj = JSONObject()
            .put("uid", profile.uid)
            .put("email", profile.email)
            .put("fullName", profile.fullName)
            .put("role", profile.role)
            .put("active", profile.active)
            .put("site", profile.site.orEmpty())
            .put("phone", profile.phone.orEmpty())
        prefs.edit().putString(KEY_PROFILE, obj.toString()).apply()
    }

    private fun loadProfileCache(uid: String): UserProfile? {
        val json = prefs.getString(KEY_PROFILE, null) ?: return null
        return try {
            val obj = JSONObject(json)
            if (obj.optString("uid") != uid) return null
            UserProfile(
                uid = uid,
                email = obj.optString("email"),
                fullName = obj.optString("fullName"),
                role = KullaniciRolleri.normalize(obj.optString("role")),
                active = obj.optBoolean("active", true),
                site = obj.optString("site").ifBlank { null },
                phone = obj.optString("phone").ifBlank { null }
            )
        } catch (_: Exception) {
            null
        }
    }

    private suspend fun loadMaterialNames() {
        val names = linkedSetOf<String>()
        try {
            firestore.readDocumentJson("veri/alinan_malzemeler")?.let { json ->
                val arr = JSONArray(json)
                for (i in 0 until arr.length()) {
                    val item = arr.optJSONObject(i) ?: continue
                    item.optString("MalzemeHizmet").takeIf { it.isNotBlank() }?.let(names::add)
                    item.optString("malzemeHizmet").takeIf { it.isNotBlank() }?.let(names::add)
                }
            }
        } catch (_: Exception) { }
        try {
            firestore.readDocumentJson("veri/stok")?.let { json ->
                val arr = JSONArray(json)
                for (i in 0 until arr.length()) {
                    val item = arr.optJSONObject(i) ?: continue
                    item.optString("MalzemeAdi").takeIf { it.isNotBlank() }?.let(names::add)
                    item.optString("malzemeAdi").takeIf { it.isNotBlank() }?.let(names::add)
                }
            }
        } catch (_: Exception) { }
        _materialNames.value = names.toList()
    }

    private suspend fun temizleGecersizBildirimler(uid: String) {
        val removed = runCatching { bildirimler.gecersizleriSil(_talepler.value, uid) }.getOrDefault(emptyList())
        removed.forEach { id -> LocalNotificationHelper.cancel(context, id) }
    }

    private suspend fun loadNotifications(uid: String, cleanup: Boolean = true) {
        val now = System.currentTimeMillis()
        if (cleanup || now - lastNotifCleanupMs > notifCleanupIntervalMs) {
            temizleGecersizBildirimler(uid)
            lastNotifCleanupMs = now
        }
        val user = _user.value
        val talepList = _talepler.value
        val inboxRecords: List<BildirimRecord> = runCatching {
            tumInboxKayitlari(uid).mapNotNull { doc: JSONObject -> inboxToRecord(doc, user?.role) }
        }.getOrDefault(emptyList())
        val cloud = runCatching { bildirimler.loadAll() }.getOrDefault(emptyList())
        val mergedRecords = bildirimler.inboxIleBirlestir(cloud, inboxRecords, user)
            .filter { bildirimler.kullaniciyaMi(it, user) && bildirimler.gecerliMi(it, talepList) }
        _notifications.value = bildirimler.toAppNotifications(mergedRecords, user, talepList)
        trayBildirimleriniGoster(mergedRecords, user)
    }

    /** FCM ulaşmazsa periyodik senkron ile tray bildirimi (masaüstü → Android). */
    private fun trayBildirimleriniGoster(records: List<BildirimRecord>, user: UserProfile?) {
        if (user == null) return
        for (record in records) {
            if (record.okundu) continue
            if (!bildirimler.kullaniciyaMi(record, user)) continue
            if (!hatirlatmaDeposu.gosterilebilirMi(record)) continue
            val route = BildirimRota.hedefRoute(
                BildirimRota.normalizeTip(record.tip),
                record.talepId,
                user.role
            )
            val id = record.inboxDocId?.takeIf { it.isNotBlank() }
                ?: BildirimMantikAnahtari.olustur(record)
            if (LocalNotificationHelper.show(context, record.baslik, record.mesaj, route, id)) {
                hatirlatmaDeposu.gosterildi(record)
            }
        }
    }

    private fun inboxToRecord(doc: JSONObject, @Suppress("UNUSED_PARAMETER") role: String?): BildirimRecord? {
        val fields = doc.optJSONObject("fields") ?: return null
        if (inboxArsivlenmisMi(fields)) return null
        fun s(key: String) = fields.optJSONObject(key)?.optString("stringValue").orEmpty()
        val eventCode = s("eventCode")
        val tip = s("tip").ifBlank { s("type") }.ifBlank { eventCodeToTip(eventCode) }
        val talepId = s("talepId").ifBlank { s("entityId") }.ifBlank { null }
        val hedefRol = s("hedefRol").ifBlank { s("targetRole") }.ifBlank { null }
        val hedefUid = s("hedefUid").ifBlank { s("targetUid") }.ifBlank { null }
        val olusturanUid = s("olusturanUid").ifBlank { s("createdBy") }.ifBlank { null }
        val docId = doc.optString("name").substringAfterLast('/')
        if (docId.isBlank()) return null
        val guncellemeUtc = fields.optJSONObject("guncellemeUtc")?.optString("integerValue")?.toLongOrNull()
            ?: fields.optJSONObject("updatedAt")?.optString("integerValue")?.toLongOrNull()
            ?: 0L
        return BildirimRecord(
            id = docId,
            baslik = s("baslik").ifBlank { s("title") },
            mesaj = s("mesaj").ifBlank { s("message") },
            tip = tip,
            talepId = talepId,
            hedefRol = hedefRol,
            hedefUid = hedefUid,
            olusturanUid = olusturanUid.orEmpty(),
            olusturmaTarihi = s("zaman").ifBlank { s("time") },
            okundu = fields.optJSONObject("isRead")?.optBoolean("booleanValue")
                ?: fields.optJSONObject("okundu")?.optBoolean("booleanValue") ?: false,
            guncellemeUtc = guncellemeUtc,
            inboxDocId = docId
        )
    }

    private suspend fun tumInboxKayitlari(uid: String): List<JSONObject> =
        runCatching { firestore.readInbox(uid, limit = 200) }.getOrDefault(emptyList())

    private fun inboxArsivlenmisMi(fields: JSONObject): Boolean {
        if (!fields.optJSONObject("dismissedAt")?.optString("timestampValue").isNullOrBlank()) return true
        if (!fields.optJSONObject("archivedAt")?.optString("timestampValue").isNullOrBlank()) return true
        return fields.optJSONObject("isArchived")?.optBoolean("booleanValue") == true
    }

    private fun eventCodeToTip(eventCode: String): String = when (eventCode) {
        "talep.yonetime_gonderildi" -> BildirimTipleri.YONETIME_GONDERILDI
        "teklif.istendi" -> BildirimTipleri.TEKLIF_ISTENDI
        "teklif.yonetime_gonderildi" -> BildirimTipleri.TEKLIF_ONAYDA
        "teklif.duzeltme_istendi" -> BildirimTipleri.TEKLIF_DUZELTME_ISTENDI
        "talep.onaylandi" -> BildirimTipleri.ONAYLANDI
        "talep.reddedildi" -> BildirimTipleri.REDDEDILDI
        "siparis.olusturuldu" -> BildirimTipleri.SIPARIS_OLUSTURULDU
        "depo.mal_kabul_yapildi" -> BildirimTipleri.MAL_KABUL_EDILDI
        else -> eventCode
    }

    fun syncFcmRoleSubscription(showSuccessToast: Boolean = true) {
        if (TenantSession.tenantId().isNullOrBlank()) {
            BildirimLog.w("FCM_TOPIC", "tenantId yok — topic aboneliği atlandı")
            return
        }
        if (!FirebaseAuthBridge.hasMatchingSession(auth.uid)) {
            BildirimLog.w("FCM_TOPIC", "Firebase Auth SDK oturumu yok — topic aboneliği atlandı")
            return
        }
        runCatching {
            FcmSubscriptionHelper(context).syncRoleTopicSubscription(showSuccessToast)
        }.onFailure { ex ->
            BildirimLog.e("FCM_TOPIC", "Topic aboneliği başarısız", ex)
        }
    }

    private suspend fun registerFcmIfNeeded() {
        val uid = auth.uid ?: return
        if (com.google.firebase.FirebaseApp.getApps(context).isEmpty()) {
            BildirimLog.w("FCM_TOKEN", "FirebaseApp yok — token kaydı atlandı")
            return
        }
        try {
            val pending = prefs.getString(MyFirebaseMessagingService.KEY_PENDING_FCM_TOKEN, null)
            val token = pending?.takeIf { it.isNotBlank() }
                ?: run {
                    // Token alınamazsa splash/login asla bloklanmasın.
                    kotlinx.coroutines.withTimeoutOrNull(8_000) {
                        com.google.firebase.messaging.FirebaseMessaging.getInstance().token.awaitTask()
                    }
                }
            if (token.isNullOrBlank()) {
                BildirimLog.w("FCM_TOKEN", "Token alınamadı/zaman aşımı uid=$uid")
                return
            }
            firestore.updateFcmToken(uid, token)
            prefs.edit().remove(MyFirebaseMessagingService.KEY_PENDING_FCM_TOKEN).apply()
            BildirimLog.i("FCM_TOKEN", "Kayıt tamam uid=$uid token=${token.take(12)}…")
        } catch (ex: Exception) {
            BildirimLog.e("FCM_TOKEN", "Token kaydı başarısız uid=$uid", ex)
        }
    }

    private fun parseManifest(json: String): UpdateManifest {
        val obj = JSONObject(json.trim().removePrefix("\uFEFF"))
        return UpdateManifest(
            version = obj.optString("version").trim().trim('"'),
            build = obj.optInt("build"),
            downloadUrlApk = obj.optString("downloadUrlApk").trim(),
            notes = obj.optString("notes").trim()
        )
    }

    private fun versionGreater(remote: String, local: String): Boolean {
        fun parts(v: String) = v.split('.').map { it.toIntOrNull() ?: 0 }
        val r = parts(remote)
        val l = parts(local)
        for (i in 0 until maxOf(r.size, l.size)) {
            val rv = r.getOrElse(i) { 0 }
            val lv = l.getOrElse(i) { 0 }
            if (rv != lv) return rv > lv
        }
        return false
    }

    /** Güncelleme veya yeniden kurulum sonrası oturum ve kayıtlı giriş bilgilerini temizler. */
    private fun ensureLoginCredentialsFresh() {
        runCatching {
            val packageInfo = context.packageManager.getPackageInfo(context.packageName, 0)
            val firstInstallTime = packageInfo.firstInstallTime
            val storedVersion = prefs.getInt(KEY_STORED_VERSION_CODE, -1)
            val storedFirstInstall = prefs.getLong(KEY_STORED_FIRST_INSTALL, -1L)

            val versionChanged = storedVersion != -1 && storedVersion != BuildConfig.VERSION_CODE
            val reinstallDetected = storedFirstInstall != -1L && storedFirstInstall != firstInstallTime

            if (versionChanged || reinstallDetected) {
                clearLoginPersistence()
            }

            prefs.edit()
                .putInt(KEY_STORED_VERSION_CODE, BuildConfig.VERSION_CODE)
                .putLong(KEY_STORED_FIRST_INSTALL, firstInstallTime)
                .apply()
        }.onFailure {
            BildirimLog.e("SESSION", "Versiyon kontrolü atlandı", it)
        }
    }

    private fun clearLoginPersistence() {
        runCatching { FirebaseAuthBridge.signOut() }
        auth.clear()
        TenantSession.clear()
        _user.value = null
        prefs.edit()
            .remove(KEY_SESSION)
            .remove("saved_tenant_id")
            .remove(KEY_SAVED_EMAIL)
            .putBoolean(KEY_REMEMBER_ME, false)
            .apply()
    }

    companion object {
        private const val KEY_SESSION = "session_json"
        private const val KEY_PROFILE = "profile_cache"
        private const val KEY_PENDING_APK = "pending_apk_path"
        private const val KEY_SKIPPED_UPDATE_BUILD = "skipped_update_build"
        private const val KEY_STORED_VERSION_CODE = "stored_version_code"
        private const val KEY_STORED_FIRST_INSTALL = "stored_first_install_time"
        private const val KEY_SAVED_EMAIL = "saved_login_email"
        private const val KEY_REMEMBER_ME = "remember_login"

        fun loadFirebaseConfig(context: Context): FirebaseConfig {
            var apiKey = ""
            var projectId = ""
            var updateManifestUrl = ""
            try {
                context.assets.open("firebase_ayarlar.json").bufferedReader().use { reader ->
                    val obj = JSONObject(reader.readText())
                    apiKey = obj.optString("apiKey")
                    projectId = obj.optString("projectId")
                    updateManifestUrl = obj.optString("guncellemeManifestUrl")
                }
            } catch (_: Exception) {
                // firebase_ayarlar.json yoksa google-services.json'dan doldur
            }
            if (apiKey.isBlank() || projectId.isBlank()) {
                GoogleServicesLoader.readProjectFromAssets(context)?.let { (pid, key) ->
                    if (projectId.isBlank()) projectId = pid
                    if (apiKey.isBlank()) apiKey = key
                }
            }
            return FirebaseConfig(apiKey, projectId, updateManifestUrl)
        }
    }

    private suspend fun <T> com.google.android.gms.tasks.Task<T>.awaitTask(): T =
        suspendCancellableCoroutine { cont ->
            addOnCompleteListener { task ->
                if (task.isSuccessful) cont.resume(task.result)
                else cont.cancel(task.exception ?: IllegalStateException("Task failed"))
            }
        }
}

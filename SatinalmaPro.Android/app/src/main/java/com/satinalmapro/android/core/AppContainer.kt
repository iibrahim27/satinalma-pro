package com.satinalmapro.android.core

import android.content.Context
import com.satinalmapro.android.BuildConfig
import com.satinalmapro.android.core.model.AppNotification
import com.satinalmapro.android.core.model.UpdateManifest
import com.satinalmapro.android.core.model.UserProfile
import com.satinalmapro.android.core.model.StokHareket
import com.satinalmapro.android.core.model.StokKaydi
import com.satinalmapro.android.core.model.TalepItem
import com.satinalmapro.android.core.model.TalepQueue
import com.satinalmapro.android.core.NetworkError
import com.satinalmapro.android.core.NetworkMonitor
import com.satinalmapro.android.core.roles.KullaniciRolleri
import com.satinalmapro.android.core.roles.MalzemeOneri
import com.satinalmapro.android.data.repository.BildirimRepository
import com.satinalmapro.android.data.repository.StokRepository
import com.satinalmapro.android.data.repository.TalepRepository
import com.satinalmapro.android.data.firebase.FirebaseAuthClient
import com.satinalmapro.android.data.firebase.FirebaseConfig
import com.satinalmapro.android.data.firebase.FirestoreClient
import com.satinalmapro.android.data.firebase.HttpClients
import com.satinalmapro.android.data.local.OfflineCache
import com.satinalmapro.android.data.local.RequestDraft
import com.satinalmapro.android.data.local.RequestDraftStore
import com.satinalmapro.android.services.ApkUpdateInstaller
import com.satinalmapro.android.services.LocalNotificationHelper
import com.satinalmapro.android.services.FcmPushService
import kotlin.coroutines.resume
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.suspendCancellableCoroutine
import org.json.JSONArray
import org.json.JSONObject
import java.io.File

class AppContainer(private val context: Context) {
    enum class UpdateInstallResult { SUCCESS, NEEDS_PERMISSION, FAILED }

    data class UpdateCheckResult(
        val available: Boolean,
        val manifest: UpdateManifest? = null,
        val error: String? = null
    )

    private val prefs = context.getSharedPreferences("satinalma_pro", Context.MODE_PRIVATE)
    val config: FirebaseConfig = loadFirebaseConfig(context)
    val auth = FirebaseAuthClient(config)
    val firestore = FirestoreClient(config, auth)
    val apkInstaller = ApkUpdateInstaller(context)
    private val offlineCache = OfflineCache(context)
    val draftStore = RequestDraftStore(context)
    private val fcmPush = FcmPushService(context, config.projectId, firestore)
    val bildirimler = BildirimRepository(firestore, auth, fcmPush)
    val talepler = TalepRepository(firestore, auth, bildirimler)
    val stokRepo = StokRepository(firestore, auth)

    private val _user = MutableStateFlow<UserProfile?>(null)
    val user: StateFlow<UserProfile?> = _user.asStateFlow()

    private val _talepler = MutableStateFlow<List<TalepItem>>(emptyList())
    val talepList: StateFlow<List<TalepItem>> = _talepler.asStateFlow()

    private val _stok = MutableStateFlow<List<StokKaydi>>(emptyList())
    val stokList: StateFlow<List<StokKaydi>> = _stok.asStateFlow()

    private val _stokHareketleri = MutableStateFlow<List<StokHareket>>(emptyList())
    val stokHareketleri: StateFlow<List<StokHareket>> = _stokHareketleri.asStateFlow()

    private val _materialNames = MutableStateFlow<List<String>>(emptyList())
    val materialNames: StateFlow<List<String>> = _materialNames.asStateFlow()

    private val _notifications = MutableStateFlow<List<AppNotification>>(emptyList())
    val notifications: StateFlow<List<AppNotification>> = _notifications.asStateFlow()

    var pendingRoute: String? = null
    var pendingNotificationId: String? = null

    private var knownNotificationIds = emptySet<String>()
    private var notificationsInitialized = false

    suspend fun restoreSession(): Boolean {
        val json = prefs.getString(KEY_SESSION, null) ?: return false
        return try {
            val obj = JSONObject(json)
            if (!obj.optBoolean("rememberMe", true)) return false
            auth.restoreSession(
                obj.getString("refreshToken"),
                obj.optString("uid").ifBlank { null },
                obj.optString("email").ifBlank { null }
            )
            loadProfile(allowCached = true)
            runCatching { syncData() }
            registerFcmIfNeeded()
            true
        } catch (_: Exception) {
            auth.clear()
            false
        }
    }

    suspend fun login(email: String, password: String, rememberMe: Boolean) {
        if (!NetworkMonitor.isOnline(context)) {
            throw IllegalStateException(NetworkError.translate("Unable to resolve host"))
        }
        if (!config.isConfigured) {
            throw IllegalStateException("Firebase ayarları yapılandırılmamış.")
        }
        try {
            auth.signIn(email, password)
            loadProfile(allowCached = false)
            if (rememberMe) {
                prefs.edit().putString(KEY_SESSION, auth.sessionJson(true)).apply()
            } else {
                prefs.edit().remove(KEY_SESSION).apply()
            }
            runCatching { syncData() }
            registerFcmIfNeeded()
        } catch (e: Exception) {
            auth.clear()
            prefs.edit().remove(KEY_SESSION).apply()
            throw if (e is IllegalStateException) e else IllegalStateException(NetworkError.translate(e.message))
        }
    }

    fun logout() {
        auth.clear()
        _user.value = null
        _materialNames.value = emptyList()
        _notifications.value = emptyList()
        _talepler.value = emptyList()
        _stok.value = emptyList()
        _stokHareketleri.value = emptyList()
        knownNotificationIds = emptySet()
        notificationsInitialized = false
        prefs.edit().remove(KEY_SESSION).apply()
    }

    suspend fun syncData() {
        val uid = auth.uid ?: return
        loadMaterialNames()
        loadTalepler()
        loadStok()
        loadNotifications(uid)
    }

    private suspend fun reloadTalepler() {
        _talepler.value = talepler.loadTalepler()
    }

    private suspend fun loadStok() {
        _stok.value = runCatching { stokRepo.loadStok() }.getOrDefault(emptyList())
        _stokHareketleri.value = runCatching { stokRepo.loadHareketler() }.getOrDefault(emptyList())
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
        loadNotifications(user.uid)
        draftStore.clear()
        return talep
    }

    fun loadDraft(): RequestDraft? = draftStore.load()

    fun saveDraft(draft: RequestDraft) = draftStore.save(draft)

    suspend fun addTeklif(talepId: String, firmaAdi: String, marka: String, vadeGunu: Int, teslimSuresi: String, odemeSekli: String, kalemFiyatlari: Map<String, Double>): TalepItem {
        val result = talepler.addTeklif(talepId, firmaAdi, marka, vadeGunu, teslimSuresi, odemeSekli, kalemFiyatlari)
        reloadTalepler()
        return result
    }

    suspend fun sendQuotesToManagement(talepId: String): TalepItem {
        val user = _user.value ?: throw IllegalStateException("Oturum gerekli")
        val result = talepler.sendQuotesToManagement(talepId, user)
        reloadTalepler()
        loadNotifications(user.uid)
        return result
    }

    suspend fun yonetimOnayla(talepId: String, teklifIste: Boolean): TalepItem {
        val user = _user.value ?: throw IllegalStateException("Oturum gerekli")
        val result = talepler.yonetimOnayla(talepId, user, teklifIste)
        reloadTalepler()
        loadNotifications(user.uid)
        return result
    }

    suspend fun yonetimReddet(talepId: String, gerekce: String): TalepItem {
        val user = _user.value ?: throw IllegalStateException("Oturum gerekli")
        val result = talepler.yonetimReddet(talepId, user, gerekce)
        reloadTalepler()
        loadNotifications(user.uid)
        return result
    }

    suspend fun yonetimTeklifOnayla(talepId: String, teklifId: String): TalepItem {
        val user = _user.value ?: throw IllegalStateException("Oturum gerekli")
        val result = talepler.yonetimTeklifOnayla(talepId, user, teklifId)
        reloadTalepler()
        loadNotifications(user.uid)
        return result
    }

    suspend fun teklifsizFirmaFiyatKaydet(talepId: String, girdiler: List<Triple<String, String, Double>>): TalepItem {
        val result = talepler.teklifsizFirmaFiyatKaydet(talepId, girdiler)
        reloadTalepler()
        return result
    }

    suspend fun malKabul(talepId: String, kalemId: String, miktar: Double): TalepItem {
        val user = _user.value ?: throw IllegalStateException("Oturum gerekli")
        val result = talepler.malKabul(talepId, kalemId, miktar, user)
        reloadTalepler()
        loadNotifications(user.uid)
        return result
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

    fun filteredTalepler(queue: TalepQueue): List<TalepItem> =
        talepler.filter(queue, _talepler.value, _user.value)

    fun findTalep(id: String?): TalepItem? =
        id?.let { target -> _talepler.value.firstOrNull { it.id.equals(target, true) } }

    fun dashboardData() = talepler.dashboard(
        _user.value,
        _talepler.value,
        _notifications.value.count { !it.read }
    )

    fun approvedMaterials(): List<TalepItem> = talepler.approvedMaterials(_talepler.value)

    suspend fun markNotificationRead(id: String) {
        val uid = auth.uid ?: return
        runCatching { firestore.markInboxRead(uid, id) }
        runCatching { bildirimler.okunduIsaretle(id) }
        loadNotifications(uid)
    }

    private suspend fun loadTalepler() {
        _talepler.value = runCatching {
            talepler.loadTalepler().also { offlineCache.saveTalepler(it) }
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
        prefs.edit().putString(KEY_PENDING_APK, target.absolutePath).apply()
        onProgress("Kurulum başlatılıyor...", 95)
        return installPendingApk(target)
    }

    fun retryPendingInstall(): UpdateInstallResult {
        val path = prefs.getString(KEY_PENDING_APK, null) ?: return UpdateInstallResult.FAILED
        return installPendingApk(File(path))
    }

    private fun installPendingApk(file: File): UpdateInstallResult {
        if (!file.exists()) return UpdateInstallResult.FAILED
        if (!apkInstaller.ensureInstallPermission()) return UpdateInstallResult.NEEDS_PERMISSION
        return try {
            apkInstaller.install(file)
            UpdateInstallResult.SUCCESS
        } catch (_: Exception) {
            UpdateInstallResult.FAILED
        }
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

    private suspend fun loadNotifications(uid: String) {
        val user = _user.value
        val talepList = _talepler.value
        val cloud = runCatching { bildirimler.loadAll() }.getOrDefault(emptyList())
        val cloudMapped = bildirimler.toAppNotifications(cloud, user, talepList)
        val inbox = runCatching {
            firestore.readInbox(uid).mapNotNull { doc ->
                val fields = doc.optJSONObject("fields") ?: return@mapNotNull null
                fun s(key: String) = fields.optJSONObject(key)?.optString("stringValue").orEmpty()
                AppNotification(
                    id = doc.optString("name").substringAfterLast('/'),
                    title = s("baslik").ifBlank { s("title") },
                    message = s("mesaj").ifBlank { s("message") },
                    type = s("tip").ifBlank { s("type") },
                    time = s("zaman").ifBlank { s("time") },
                    requestId = s("talepId").ifBlank { null },
                    route = s("route").ifBlank { null },
                    read = fields.optJSONObject("isRead")?.optBoolean("booleanValue")
                        ?: fields.optJSONObject("okundu")?.optBoolean("booleanValue") ?: false
                )
            }
        }.getOrDefault(emptyList())
        val merged = linkedMapOf<String, AppNotification>()
        cloudMapped.forEach { merged[it.id] = it }
        inbox.forEach { item ->
            val existing = merged[item.id]
            merged[item.id] = if (existing == null) item else item.copy(read = item.read || existing.read)
        }
        val sorted = merged.values.sortedByDescending { it.time }
        if (notificationsInitialized) {
            sorted.filter { !it.read && it.id !in knownNotificationIds }.forEach { item ->
                val route = item.route ?: "bildirimler"
                LocalNotificationHelper.show(context, item.title, item.message, route, item.id)
            }
        } else {
            notificationsInitialized = true
        }
        knownNotificationIds = sorted.map { it.id }.toSet()
        _notifications.value = sorted
    }

    private suspend fun registerFcmIfNeeded() {
        val uid = auth.uid ?: return
        try {
            val token = com.google.firebase.messaging.FirebaseMessaging.getInstance().token.awaitTask()
            firestore.updateFcmToken(uid, token)
        } catch (_: Exception) { }
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

    companion object {
        private const val KEY_SESSION = "session_json"
        private const val KEY_PROFILE = "profile_cache"
        private const val KEY_PENDING_APK = "pending_apk_path"

        fun loadFirebaseConfig(context: Context): FirebaseConfig {
            return try {
                context.assets.open("firebase_ayarlar.json").bufferedReader().use { reader ->
                    val obj = JSONObject(reader.readText())
                    FirebaseConfig(
                        apiKey = obj.optString("apiKey"),
                        projectId = obj.optString("projectId"),
                        updateManifestUrl = obj.optString("guncellemeManifestUrl")
                    )
                }
            } catch (_: Exception) {
                FirebaseConfig("", "", "")
            }
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

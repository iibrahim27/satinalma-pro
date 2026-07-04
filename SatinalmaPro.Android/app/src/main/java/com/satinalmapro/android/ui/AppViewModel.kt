package com.satinalmapro.android.ui

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.satinalmapro.android.core.AppContainer
import com.satinalmapro.android.core.model.MenuItem
import com.satinalmapro.android.core.model.TalepItem
import com.satinalmapro.android.core.model.TalepQueue
import com.satinalmapro.android.core.model.UpdateManifest
import com.satinalmapro.android.core.model.UserProfile
import com.satinalmapro.android.core.AppContainer.UpdateInstallResult
import com.satinalmapro.android.core.NetworkError
import com.satinalmapro.android.core.model.ManagedUser
import com.satinalmapro.android.core.model.UygulamaAyarlar
import com.satinalmapro.android.core.roles.KullaniciRolleri
import com.satinalmapro.android.core.roles.BildirimRota
import com.satinalmapro.android.core.roles.RolNavigasyon
import java.util.concurrent.ConcurrentHashMap
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.SharingStarted
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.map
import kotlinx.coroutines.flow.stateIn
import kotlinx.coroutines.launch

class AppViewModel(private val container: AppContainer) : ViewModel() {
    val user = container.user
    val notifications = container.notifications
    val materialNames = container.materialNames
    val talepler = container.talepList
    val stokList = container.stokList
    val stokHareketleri = container.stokHareketleri
    val settingsUsers = container.settingsUsers
    val malzemeBirimleri = container.uygulamaAyarlar.map { it.malzemeBirimleri }
        .stateIn(viewModelScope, SharingStarted.WhileSubscribed(5000), UygulamaAyarlar.varsayilanBirimler)
    val malzemeKategorileri = container.uygulamaAyarlar.map { it.malzemeKategorileri }
        .stateIn(viewModelScope, SharingStarted.WhileSubscribed(5000), UygulamaAyarlar.varsayilanKategoriler)

    private val _settingsMessage = MutableStateFlow<String?>(null)
    val settingsMessage: StateFlow<String?> = _settingsMessage.asStateFlow()

    private val _settingsError = MutableStateFlow<String?>(null)
    val settingsError: StateFlow<String?> = _settingsError.asStateFlow()

    private val _splashDone = MutableStateFlow(false)
    val splashDone: StateFlow<Boolean> = _splashDone.asStateFlow()

    private val _isLoggedIn = MutableStateFlow(false)
    val isLoggedIn: StateFlow<Boolean> = _isLoggedIn.asStateFlow()

    private val _splashMessage = MutableStateFlow("Uygulama yükleniyor...")
    val splashMessage: StateFlow<String> = _splashMessage.asStateFlow()

    private val _loginError = MutableStateFlow<String?>(null)
    val loginError: StateFlow<String?> = _loginError.asStateFlow()

    private val _currentRoute = MutableStateFlow<String?>(null)
    val currentRoute: StateFlow<String?> = _currentRoute.asStateFlow()

    private val _loading = MutableStateFlow(false)
    val loading: StateFlow<Boolean> = _loading.asStateFlow()

    private val _submitError = MutableStateFlow<String?>(null)
    val submitError: StateFlow<String?> = _submitError.asStateFlow()

    private val routeHistory = ArrayDeque<String>()

    private val _pendingUpdate = MutableStateFlow<UpdateManifest?>(null)
    val pendingUpdate: StateFlow<UpdateManifest?> = _pendingUpdate.asStateFlow()

    private val _updateProgress = MutableStateFlow<Int?>(null)
    val updateProgress: StateFlow<Int?> = _updateProgress.asStateFlow()

    private val _updateMessage = MutableStateFlow<String?>(null)
    val updateMessage: StateFlow<String?> = _updateMessage.asStateFlow()

    private val _updateError = MutableStateFlow<String?>(null)
    val updateError: StateFlow<String?> = _updateError.asStateFlow()

    private val _showUpdateDialog = MutableStateFlow(false)
    val showUpdateDialog: StateFlow<Boolean> = _showUpdateDialog.asStateFlow()

    val dashboardCards = talepler.map { container.dashboardData().first }
        .stateIn(viewModelScope, SharingStarted.WhileSubscribed(5000), emptyList())

    val dashboardActivities = talepler.map { container.dashboardData().second }
        .stateIn(viewModelScope, SharingStarted.WhileSubscribed(5000), emptyList())

    private val filteredFlows = ConcurrentHashMap<TalepQueue, StateFlow<List<TalepItem>>>()
    private val talepByIdFlows = ConcurrentHashMap<String, StateFlow<TalepItem?>>()
    private val approvedMaterialsFlow by lazy {
        talepler.map { container.approvedMaterials() }
            .stateIn(viewModelScope, SharingStarted.WhileSubscribed(5000), emptyList())
    }

    fun filteredTalepler(queue: TalepQueue): StateFlow<List<TalepItem>> =
        filteredFlows.getOrPut(queue) {
            talepler.map { container.filteredTalepler(queue) }
                .stateIn(
                    viewModelScope,
                    SharingStarted.WhileSubscribed(5000),
                    container.filteredTalepler(queue)
                )
        }

    fun talepById(id: String): StateFlow<TalepItem?> =
        talepByIdFlows.getOrPut(id) {
            talepler.map { container.findTalep(id) }
                .stateIn(viewModelScope, SharingStarted.WhileSubscribed(5000), container.findTalep(id))
        }

    fun approvedMaterials(): StateFlow<List<TalepItem>> = approvedMaterialsFlow

    fun startSplash() {
        viewModelScope.launch {
            _splashMessage.value = "Güncelleme kontrol ediliyor..."
            runCatching { container.checkForUpdate() }.getOrNull()?.let { result ->
                if (result.available && result.manifest != null) {
                    _pendingUpdate.value = result.manifest
                }
            }
            _splashMessage.value = "Oturum kontrol ediliyor..."
            val restored = runCatching { container.restoreSession() }.getOrDefault(false)
            _isLoggedIn.value = restored
            if (restored) {
                _currentRoute.value = RolNavigasyon.defaultRoute(container.user.value?.role)
                val pending = _pendingUpdate.value
                if (pending != null && !container.isUpdateSkipped(pending)) {
                    _showUpdateDialog.value = true
                }
            }
            container.pendingRoute?.let { route ->
                navigateFromNotification(route, container.pendingNotificationId)
                container.pendingRoute = null
                container.pendingNotificationId = null
            }
            _splashDone.value = true
        }
    }

    fun login(email: String, password: String, rememberMe: Boolean) {
        viewModelScope.launch {
            _loginError.value = null
            _loading.value = true
            runCatching {
                container.login(email, password, rememberMe)
            }.onSuccess {
                _isLoggedIn.value = true
                routeHistory.clear()
                _currentRoute.value = RolNavigasyon.defaultRoute(container.user.value?.role)
                consumePendingNotificationRoute()
            }.onFailure {
                _loginError.value = NetworkError.translate(it.message)
            }
            _loading.value = false
        }
    }

    fun logout() {
        container.logout()
        _isLoggedIn.value = false
        _currentRoute.value = null
        routeHistory.clear()
    }

    fun menus(): List<MenuItem> = RolNavigasyon.menus(container.user.value?.role)

    fun navigate(route: String, pushHistory: Boolean = true) {
        val safe = BildirimRota.safeRoute(route, container.user.value?.role)
        val current = _currentRoute.value
        if (pushHistory && current != null && current != safe) {
            routeHistory.addLast(current)
        }
        _currentRoute.value = safe
    }

    fun navigateFromMenu(route: String) {
        routeHistory.clear()
        navigate(route, pushHistory = false)
    }

    fun navigateBack(): Boolean {
        if (routeHistory.isNotEmpty()) {
            _currentRoute.value = routeHistory.removeLast()
            return true
        }
        val current = _currentRoute.value?.substringBefore('?') ?: "dashboard"
        if (current != "dashboard") {
            _currentRoute.value = "dashboard"
            return true
        }
        return false
    }

    fun navigateFromNotification(route: String, notificationId: String? = null) {
        if (container.user.value == null || !_splashDone.value || !_isLoggedIn.value) {
            container.pendingRoute = route
            container.pendingNotificationId = notificationId
            return
        }
        navigate(route)
        viewModelScope.launch {
            notificationId?.let { runCatching { container.markNotificationRead(it) } }
            runCatching { container.syncData() }
        }
    }

    private fun consumePendingNotificationRoute() {
        val route = container.pendingRoute ?: return
        val notificationId = container.pendingNotificationId
        container.pendingRoute = null
        container.pendingNotificationId = null
        navigateFromNotification(route, notificationId)
    }

    fun canAccess(route: String): Boolean =
        RolNavigasyon.canAccess(container.user.value?.role, route)

    fun currentUser(): UserProfile? = container.user.value

    fun refreshData() {
        viewModelScope.launch {
            _loading.value = true
            runCatching { container.syncData() }
            _loading.value = false
        }
    }

    fun startBackgroundRefresh() {
        viewModelScope.launch {
            while (true) {
                kotlinx.coroutines.delay(12_000)
                if (_isLoggedIn.value) runCatching { container.refreshNotifications() }
            }
        }
        viewModelScope.launch {
            while (true) {
                kotlinx.coroutines.delay(30_000)
                if (_isLoggedIn.value) runCatching { container.syncData() }
            }
        }
        viewModelScope.launch {
            while (true) {
                kotlinx.coroutines.delay(6 * 60 * 60 * 1000L)
                if (_isLoggedIn.value) checkForUpdates(userInitiated = false)
            }
        }
    }

    fun onAppResume() {
        viewModelScope.launch {
            if (_isLoggedIn.value) {
                runCatching { container.refreshNotifications() }
                runCatching { container.syncData() }
            }
        }
    }

    /** userInitiated=true yalnizca Profil ekranindaki manuel kontrol icin; diger durumlarda sessiz calisir. */
    fun checkForUpdates(userInitiated: Boolean = false) {
        viewModelScope.launch {
            if (userInitiated) {
                _updateError.value = null
                _updateMessage.value = "Güncelleme kontrol ediliyor..."
            }
            val result = runCatching { container.checkForUpdate() }.getOrElse {
                _updateMessage.value = null
                if (userInitiated) {
                    _updateError.value = NetworkError.translate(it)
                    _showUpdateDialog.value = true
                }
                return@launch
            }
            _updateMessage.value = null
            when {
                result.error != null -> {
                    if (userInitiated) {
                        _updateError.value = result.error
                        _showUpdateDialog.value = true
                    }
                }
                result.available && result.manifest != null -> {
                    _pendingUpdate.value = result.manifest
                    if (userInitiated) {
                        _showUpdateDialog.value = true
                    }
                }
                userInitiated -> {
                    _updateMessage.value = "Uygulama güncel."
                    _showUpdateDialog.value = true
                }
            }
        }
    }

    fun dismissUpdateDialog() {
        _pendingUpdate.value?.let { container.markUpdateSkipped(it) }
        _showUpdateDialog.value = false
        _updateError.value = null
        _updateProgress.value = null
    }

    fun startUpdateDownload() {
        val manifest = _pendingUpdate.value ?: return
        viewModelScope.launch {
            _updateError.value = null
            _updateProgress.value = 0
            _updateMessage.value = "Güncelleme indiriliyor..."
            val result = runCatching {
                container.downloadAndInstallUpdate(manifest) { msg, progress ->
                    _updateMessage.value = msg
                    _updateProgress.value = progress
                }
            }.getOrElse {
                _updateProgress.value = null
                _updateError.value = NetworkError.translate(it)
                return@launch
            }
            _updateProgress.value = null
            when (result) {
                UpdateInstallResult.SUCCESS -> {
                    _updateMessage.value = "Kurulum ekranı açıldı. Yüklemeyi tamamlayın."
                    _showUpdateDialog.value = false
                }
                UpdateInstallResult.NEEDS_PERMISSION -> {
                    _updateError.value = "Kurulum izni gerekli. Ayarlardan 'Bu kaynaktan yükle' iznini verin, sonra tekrar Güncelle'ye basın."
                }
                UpdateInstallResult.FAILED -> {
                    container.clearPendingApk()
                    _updateError.value = "Güncelleme kurulamadı. APK imzası uyuşmuyor olabilir; release dosyasını manuel yükleyin."
                }
            }
        }
    }

    fun materialSuggestions(query: String): List<String> = container.materialSuggestions(query)

    fun handleNotificationRoute(route: String?, notificationId: String? = null) {
        if (route.isNullOrBlank()) return
        navigateFromNotification(route, notificationId)
    }

    fun submitRequest(
        site: String,
        aciklama: String,
        oncelik: String,
        kalemler: List<Triple<String, String, String>>,
        onSuccess: () -> Unit
    ) {
        viewModelScope.launch {
            _submitError.value = null
            _loading.value = true
            runCatching {
                container.createRequest(site, aciklama, oncelik, kalemler)
            }.onSuccess {
                _loading.value = false
                onSuccess()
            }.onFailure { error ->
                _loading.value = false
                _submitError.value = NetworkError.translate(error.message ?: "Talep gönderilemedi")
            }
        }
    }

    fun loadDraft() = container.loadDraft()

    fun saveDraft(draft: com.satinalmapro.android.data.local.RequestDraft) = container.saveDraft(draft)

    fun openNotification(notificationId: String, route: String) {
        navigate(route)
        viewModelScope.launch {
            runCatching { container.markNotificationRead(notificationId) }
        }
    }

    fun runWorkflow(onSuccess: () -> Unit = {}, block: suspend () -> Unit) {
        viewModelScope.launch {
            _submitError.value = null
            _loading.value = true
            runCatching { block() }
                .onSuccess { _loading.value = false; onSuccess() }
                .onFailure { _loading.value = false; _submitError.value = it.message ?: "İşlem başarısız" }
        }
    }

    fun addTeklif(talepId: String, firmaAdi: String, marka: String, vadeGunu: Int, teslimSuresi: String, odemeSekli: String, kalemFiyatlari: Map<String, Double>, onSuccess: () -> Unit) =
        runWorkflow(onSuccess) { container.addTeklif(talepId, firmaAdi, marka, vadeGunu, teslimSuresi, odemeSekli, kalemFiyatlari) }

    fun sendQuotesToManagement(talepId: String, onSuccess: () -> Unit) =
        runWorkflow(onSuccess) { container.sendQuotesToManagement(talepId) }

    fun yonetimOnayla(talepId: String, teklifIste: Boolean, onSuccess: () -> Unit) =
        runWorkflow(onSuccess) { container.yonetimOnayla(talepId, teklifIste) }

    fun yonetimReddet(talepId: String, gerekce: String, onSuccess: () -> Unit) =
        runWorkflow(onSuccess) { container.yonetimReddet(talepId, gerekce) }

    fun yonetimTeklifOnayla(talepId: String, teklifId: String, onSuccess: () -> Unit) =
        runWorkflow(onSuccess) { container.yonetimTeklifOnayla(talepId, teklifId) }

    fun teklifsizFirmaFiyatKaydet(talepId: String, girdiler: List<Triple<String, String, Double>>, onSuccess: () -> Unit) =
        runWorkflow(onSuccess) { container.teklifsizFirmaFiyatKaydet(talepId, girdiler) }

    fun malKabul(talepId: String, kalemId: String, miktar: String, onSuccess: () -> Unit) {
        val m = miktar.replace(',', '.').toDoubleOrNull() ?: run { _submitError.value = "Geçerli miktar girin"; return }
        runWorkflow(onSuccess) { container.malKabul(talepId, kalemId, m) }
    }

    fun stokGiris(malzeme: String, miktar: String, birim: String, kategori: String, depo: String, birimMaliyet: String, belgeNo: String, teslimAlan: String, onSuccess: () -> Unit) {
        val m = miktar.replace(',', '.').toDoubleOrNull() ?: run { _submitError.value = "Geçerli miktar girin"; return }
        val f = birimMaliyet.replace(',', '.').toDoubleOrNull() ?: 0.0
        runWorkflow(onSuccess) { container.stokGiris(malzeme, m, birim, kategori, depo, f, belgeNo, "", teslimAlan) }
    }

    fun stokCikis(malzeme: String, miktar: String, depo: String, belgeNo: String, teslimAlan: String, onSuccess: () -> Unit) {
        val m = miktar.replace(',', '.').toDoubleOrNull() ?: run { _submitError.value = "Geçerli miktar girin"; return }
        runWorkflow(onSuccess) { container.stokCikis(malzeme, m, depo, belgeNo, "", teslimAlan) }
    }

    fun stokSayim(malzeme: String, depo: String, sayimMiktari: String, onSuccess: () -> Unit) {
        val m = sayimMiktari.replace(',', '.').toDoubleOrNull() ?: run { _submitError.value = "Geçerli miktar girin"; return }
        runWorkflow(onSuccess) { container.stokSayim(malzeme, depo, m) }
    }

    fun loadSettings() {
        if (!KullaniciRolleri.isAdmin(container.user.value?.role)) return
        viewModelScope.launch {
            _settingsError.value = null
            _loading.value = true
            runCatching { container.loadUygulamaAyarlar() }
                .onSuccess { _settingsMessage.value = null }
                .onFailure { _settingsError.value = NetworkError.translate(it.message) }
            _loading.value = false
        }
    }

    fun saveUser(user: ManagedUser, onSuccess: () -> Unit = {}) {
        viewModelScope.launch {
            _settingsError.value = null
            _settingsMessage.value = null
            _loading.value = true
            runCatching { container.saveManagedUser(user) }
                .onSuccess {
                    _settingsMessage.value = "Kullanıcı kaydedildi."
                    onSuccess()
                }
                .onFailure { _settingsError.value = NetworkError.translate(it.message) }
            _loading.value = false
        }
    }

    fun createUser(
        email: String,
        password: String,
        fullName: String,
        role: String,
        site: String,
        active: Boolean,
        onSuccess: () -> Unit = {}
    ) {
        if (email.isBlank() || password.length < 6 || fullName.isBlank()) {
            _settingsError.value = "E-posta, ad soyad ve en az 6 karakterlik şifre girin."
            return
        }
        viewModelScope.launch {
            _settingsError.value = null
            _settingsMessage.value = null
            _loading.value = true
            runCatching {
                container.createManagedUser(email, password, fullName, role, site, active)
            }.onSuccess {
                _settingsMessage.value = "Kullanıcı oluşturuldu."
                onSuccess()
            }.onFailure { _settingsError.value = NetworkError.translate(it.message) }
            _loading.value = false
        }
    }

    fun addBirim(term: String, onSuccess: () -> Unit = {}) =
        updateTermList(
            current = container.uygulamaAyarlar.value.malzemeBirimleri,
            term = term,
            onSuccess = onSuccess
        ) { list, ayarlar ->
            container.saveUygulamaAyarlar(ayarlar.copy(malzemeBirimleri = list))
        }

    fun removeBirim(term: String) = updateTermList(
        current = container.uygulamaAyarlar.value.malzemeBirimleri,
        term = term,
        remove = true
    ) { list, ayarlar ->
        container.saveUygulamaAyarlar(ayarlar.copy(malzemeBirimleri = list))
    }

    fun addKategori(term: String, onSuccess: () -> Unit = {}) =
        updateTermList(
            current = container.uygulamaAyarlar.value.malzemeKategorileri,
            term = term,
            onSuccess = onSuccess
        ) { list, ayarlar ->
            container.saveUygulamaAyarlar(ayarlar.copy(malzemeKategorileri = list))
        }

    fun removeKategori(term: String) = updateTermList(
        current = container.uygulamaAyarlar.value.malzemeKategorileri,
        term = term,
        remove = true
    ) { list, ayarlar ->
        container.saveUygulamaAyarlar(ayarlar.copy(malzemeKategorileri = list))
    }

    private fun updateTermList(
        current: List<String>,
        term: String,
        remove: Boolean = false,
        onSuccess: () -> Unit = {},
        save: suspend (List<String>, UygulamaAyarlar) -> Unit
    ) {
        val trimmed = term.trim()
        if (trimmed.isBlank()) {
            _settingsError.value = "Geçerli bir terim girin."
            return
        }
        val next = if (remove) {
            if (current.size <= 1) {
                _settingsError.value = "En az bir terim kalmalıdır."
                return
            }
            current.filterNot { it.equals(trimmed, ignoreCase = true) }
        } else {
            if (current.any { it.equals(trimmed, ignoreCase = true) }) {
                _settingsError.value = "Bu terim zaten listede."
                return
            }
            current + trimmed
        }
        viewModelScope.launch {
            _settingsError.value = null
            _loading.value = true
            runCatching { save(next, container.uygulamaAyarlar.value) }
                .onSuccess {
                    _settingsMessage.value = if (remove) "Terim silindi." else "Terim eklendi."
                    onSuccess()
                }
                .onFailure { _settingsError.value = NetworkError.translate(it.message) }
            _loading.value = false
        }
    }
}

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
                if (_pendingUpdate.value != null) _showUpdateDialog.value = true
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
                checkForUpdates(userInitiated = false)
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
        if (container.user.value == null) {
            container.pendingRoute = route
            container.pendingNotificationId = notificationId
            return
        }
        viewModelScope.launch {
            runCatching { container.syncData() }
            notificationId?.let { runCatching { container.markNotificationRead(it) } }
            navigate(route)
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
            when (container.retryPendingInstall()) {
                UpdateInstallResult.NEEDS_PERMISSION -> {
                    _updateError.value = "Kurulum izni gerekli. Ayarlardan 'Bu kaynaktan yükle' iznini verin."
                    _showUpdateDialog.value = true
                }
                UpdateInstallResult.SUCCESS -> {
                    _updateMessage.value = "Kurulum ekranı açıldı."
                }
                else -> Unit
            }
            if (_isLoggedIn.value) {
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
                    _showUpdateDialog.value = true
                }
                userInitiated -> {
                    _updateMessage.value = "Uygulama güncel."
                    _showUpdateDialog.value = true
                }
            }
        }
    }

    fun dismissUpdateDialog() {
        _showUpdateDialog.value = false
        _updateError.value = null
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
                }
                UpdateInstallResult.NEEDS_PERMISSION -> {
                    _updateError.value = "Kurulum izni gerekli. Ayarlardan izin verip tekrar 'Güncelle'ye basın."
                }
                UpdateInstallResult.FAILED -> {
                    _updateError.value = "Güncelleme kurulamadı."
                }
            }
        }
    }

    fun materialSuggestions(query: String): List<String> = container.materialSuggestions(query)

    fun handleNotificationRoute(route: String?, notificationId: String? = null) {
        if (route.isNullOrBlank()) return
        if (!_splashDone.value || !_isLoggedIn.value) {
            container.pendingRoute = route
            container.pendingNotificationId = notificationId
            return
        }
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
            }.onFailure {
                _loading.value = false
                _submitError.value = it.message ?: "Talep gönderilemedi"
            }
        }
    }

    fun loadDraft() = container.loadDraft()

    fun saveDraft(draft: com.satinalmapro.android.data.local.RequestDraft) = container.saveDraft(draft)

    fun openNotification(notificationId: String, route: String) {
        viewModelScope.launch {
            runCatching { container.markNotificationRead(notificationId) }
            val safe = BildirimRota.safeRoute(route, container.user.value?.role)
            navigate(safe)
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
}

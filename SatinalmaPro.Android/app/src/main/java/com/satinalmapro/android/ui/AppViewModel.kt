package com.satinalmapro.android.ui

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.satinalmapro.android.core.AppContainer
import com.satinalmapro.android.core.model.MenuItem
import com.satinalmapro.android.core.model.OnaylananMalzemeSatiri
import com.satinalmapro.android.core.model.TalepItem
import com.satinalmapro.android.core.model.TalepQueue
import com.satinalmapro.android.core.model.UpdateManifest
import com.satinalmapro.android.core.model.UserProfile
import com.satinalmapro.android.core.AppContainer.UpdateInstallResult
import com.satinalmapro.android.core.NetworkError
import com.satinalmapro.android.core.model.ManagedUser
import com.satinalmapro.android.core.model.UygulamaAyarlar
import com.satinalmapro.android.core.roles.KullaniciRolleri
import com.satinalmapro.android.data.repository.StokRepository
import com.satinalmapro.android.core.roles.BildirimRota
import com.satinalmapro.android.core.roles.RolNavigasyon
import java.util.concurrent.ConcurrentHashMap
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.SharingStarted
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import androidx.fragment.app.FragmentActivity
import com.satinalmapro.android.security.BiometricAuthHelper
import com.satinalmapro.android.services.BackgroundSyncScheduler
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.Job
import kotlinx.coroutines.flow.combine
import kotlinx.coroutines.flow.flowOn
import kotlinx.coroutines.flow.map
import kotlinx.coroutines.flow.stateIn
import kotlinx.coroutines.launch
import kotlinx.coroutines.sync.Mutex
import kotlinx.coroutines.withContext

class AppViewModel(private val container: AppContainer) : ViewModel() {
    /** Şifre ile giriş yapıldıysa bu oturumda biyometrik kilidi atla. */
    private var passwordSessionUnlocked = false
    private var splashJob: Job? = null
    private var loginInProgress = false
    val user = container.user
    val notifications = container.notifications
    val materialNames = container.materialNames
    val talepler = container.talepList
    val stokList = container.stokList
    val stokHareketleri = container.stokHareketleri
    val agregaList = container.agregaList
    val cimentoList = container.cimentoList
    val alinanMalzemeKayitlari = container.alinanMalzemeKayitlari
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

    private val _needsBiometricUnlock = MutableStateFlow(false)
    val needsBiometricUnlock: StateFlow<Boolean> = _needsBiometricUnlock.asStateFlow()

    private val _biometricError = MutableStateFlow<String?>(null)
    val biometricError: StateFlow<String?> = _biometricError.asStateFlow()

    private val _splashMessage = MutableStateFlow("Uygulama yükleniyor...")
    val splashMessage: StateFlow<String> = _splashMessage.asStateFlow()

    private val _loginError = MutableStateFlow<String?>(null)
    val loginError: StateFlow<String?> = _loginError.asStateFlow()

    private val _loginMessage = MutableStateFlow<String?>(null)
    val loginMessage: StateFlow<String?> = _loginMessage.asStateFlow()

    private val _currentRoute = MutableStateFlow<String?>(null)
    val currentRoute: StateFlow<String?> = _currentRoute.asStateFlow()

    private val _loading = MutableStateFlow(false)
    val loading: StateFlow<Boolean> = _loading.asStateFlow()

    /** İlk ağ senkronu bitene kadar boş listeyi «kayıt yok» sanma. */
    private val _initialSyncPending = MutableStateFlow(false)
    val initialSyncPending: StateFlow<Boolean> = _initialSyncPending.asStateFlow()
    private val workflowMutex = Mutex()
    private val syncMutex = Mutex()

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

    private val _biometricAvailable = MutableStateFlow(container.isBiometricAvailable())
    val biometricAvailable: StateFlow<Boolean> = _biometricAvailable.asStateFlow()

    private val _biometricEnabled = MutableStateFlow(container.biometricPreferences.isEnabled())
    val biometricEnabled: StateFlow<Boolean> = _biometricEnabled.asStateFlow()

    private val _profileMessage = MutableStateFlow<String?>(null)
    val profileMessage: StateFlow<String?> = _profileMessage.asStateFlow()

    private val _profileError = MutableStateFlow<String?>(null)
    val profileError: StateFlow<String?> = _profileError.asStateFlow()

    fun setBiometricEnabled(enabled: Boolean) {
        container.biometricPreferences.setEnabled(enabled)
        _biometricEnabled.value = enabled
        if (!enabled) {
            _profileMessage.value = "Biyometrik kilit kapatıldı"
            _biometricError.value = null
        }
    }

    fun setBiometricError(message: String) {
        _biometricError.value = message
        _profileError.value = message
    }

    fun enableBiometricWithVerification(activity: FragmentActivity) {
        _biometricError.value = null
        _profileError.value = null
        _profileMessage.value = null
        BiometricAuthHelper.authenticate(
            activity = activity,
            title = "Biyometrik Kilit",
            subtitle = "Etkinleştirmek için parmak izi veya ekran kilidi ile doğrulayın",
            onSuccess = {
                setBiometricEnabled(true)
                _profileMessage.value = "Biyometrik kilit etkinleştirildi"
            },
            onError = {
                _biometricError.value = it
                _profileError.value = it
            },
            onCancel = { _profileError.value = "Doğrulama iptal edildi" }
        )
    }

    fun changePassword(currentPassword: String, newPassword: String, confirmPassword: String, onSuccess: () -> Unit) {
        if (newPassword.length < 6) {
            _profileError.value = "Yeni şifre en az 6 karakter olmalıdır"
            return
        }
        if (newPassword != confirmPassword) {
            _profileError.value = "Yeni şifreler eşleşmiyor"
            return
        }
        viewModelScope.launch {
            _profileError.value = null
            _profileMessage.value = null
            _loading.value = true
            runCatching { container.changePassword(currentPassword, newPassword) }
                .onSuccess {
                    _profileMessage.value = "Şifreniz güncellendi"
                    onSuccess()
                }
                .onFailure { _profileError.value = NetworkError.translate(it.message) }
            _loading.value = false
        }
    }

    // Ölü akışlar — sync sonrası ağır dashboardData() Main'de patlamasın diye kaldırıldı.
    // UI DashboardScreen kuyruk kartlarını kullanıyor.

    val menuBadges = combine(talepler, user) { talepList, u ->
        runCatching {
            RolNavigasyon.menuBadgeCounts(u?.role, talepList, u?.uid.orEmpty(), u?.fullName.orEmpty())
        }.getOrDefault(emptyMap())
    }
        .flowOn(Dispatchers.Default)
        .stateIn(viewModelScope, SharingStarted.WhileSubscribed(5000), emptyMap())

    val queueItemCounts = combine(talepler, user) { talepList, u ->
        runCatching {
            RolNavigasyon.queueItemCounts(u?.role, talepList, u?.uid.orEmpty(), u?.fullName.orEmpty())
        }.getOrDefault(emptyMap())
    }
        .flowOn(Dispatchers.Default)
        .stateIn(viewModelScope, SharingStarted.WhileSubscribed(5000), emptyMap())

    private val filteredFlows = ConcurrentHashMap<TalepQueue, StateFlow<List<TalepItem>>>()
    private val talepByIdFlows = ConcurrentHashMap<String, StateFlow<TalepItem?>>()
    private val approvedMaterialsFlow by lazy {
        talepler.map { container.approvedMaterials() }
            .stateIn(viewModelScope, SharingStarted.WhileSubscribed(5000), emptyList())
    }
    private val siparisBekleyenFlow by lazy {
        talepler.map { container.siparisBekleyenMalzemeler() }
            .stateIn(viewModelScope, SharingStarted.WhileSubscribed(5000), emptyList())
    }

    fun filteredTalepler(queue: TalepQueue): StateFlow<List<TalepItem>> =
        filteredFlows.getOrPut(queue) {
            talepler.map { container.filteredTalepler(queue) }
                .stateIn(
                    viewModelScope,
                    // Sekme değişince 5 sn sonra upstream kesilmesin ” boş flash olmasın.
                    SharingStarted.WhileSubscribed(60_000),
                    container.filteredTalepler(queue)
                )
        }

    fun talepById(id: String): StateFlow<TalepItem?> =
        talepByIdFlows.getOrPut(id) {
            talepler.map { container.findTalep(id) }
                .stateIn(
                    viewModelScope,
                    SharingStarted.WhileSubscribed(60_000),
                    container.findTalep(id)
                )
        }

    fun approvedMaterials(): StateFlow<List<OnaylananMalzemeSatiri>> = approvedMaterialsFlow

    fun siparisBekleyenMalzemeler(): StateFlow<List<OnaylananMalzemeSatiri>> = siparisBekleyenFlow

    fun startSplash() {
        // Zaten giriş yapılmışsa veya splash bitmişse tekrar restore etme.
        if (_isLoggedIn.value || passwordSessionUnlocked || loginInProgress) {
            _splashDone.value = true
            _needsBiometricUnlock.value = false
            if (container.hasActiveSession()) _isLoggedIn.value = true
            return
        }
        if (_splashDone.value) {
            if (container.hasActiveSession()) {
                _needsBiometricUnlock.value = false
                _isLoggedIn.value = true
            }
            return
        }
        splashJob?.cancel()
        splashJob = viewModelScope.launch {
            _splashMessage.value = "Yükleniyor..."
            val restored = try {
                kotlinx.coroutines.withTimeoutOrNull(6_000) {
                    container.restoreSession()
                } ?: container.hasActiveSession()
            } catch (_: kotlinx.coroutines.CancellationException) {
                // Login splash'ı iptal etti ” oturumu düşürme.
                return@launch
            } catch (_: Exception) {
                container.hasActiveSession()
            }

            // Login yarışını kazanmış olabilir; asla geri alma.
            if (_isLoggedIn.value || passwordSessionUnlocked || loginInProgress) {
                _splashDone.value = true
                _needsBiometricUnlock.value = false
                if (container.hasActiveSession() || passwordSessionUnlocked) {
                    _isLoggedIn.value = true
                }
                return@launch
            }

            when {
                restored || container.hasActiveSession() -> {
                    applyPostLoginNavigation()
                    val lockBiometric = container.shouldRequireBiometricUnlock() &&
                        !passwordSessionUnlocked &&
                        !loginInProgress &&
                        !_isLoggedIn.value
                    if (lockBiometric) {
                        _needsBiometricUnlock.value = true
                        // Tekrar kontrol ” login arada bitmiş olabilir.
                        if (!passwordSessionUnlocked && !loginInProgress && !_isLoggedIn.value) {
                            _isLoggedIn.value = false
                        }
                    } else {
                        _needsBiometricUnlock.value = false
                        _isLoggedIn.value = true
                        biometricUnlockedAt = System.currentTimeMillis()
                    }
                }
                else -> {
                    if (!_isLoggedIn.value && !passwordSessionUnlocked && !loginInProgress) {
                        _needsBiometricUnlock.value = false
                        _isLoggedIn.value = false
                    }
                }
            }

            // Son güvenlik: login başarısını splash ezmesin.
            if (passwordSessionUnlocked || loginInProgress) {
                _needsBiometricUnlock.value = false
                _isLoggedIn.value = true
            }

            container.pendingRoute?.let { route ->
                navigateFromNotification(route, container.pendingNotificationId)
                container.pendingRoute = null
                container.pendingNotificationId = null
            }
            _splashDone.value = true
            if (_isLoggedIn.value) {
                container.hydrateFromOfflineCache()
                startPostLoginSync()
                BackgroundSyncScheduler.ensureScheduled(container.appContext)
            }
        }
    }

    private fun startPostLoginSync() {
        viewModelScope.launch(Dispatchers.IO) {
            _initialSyncPending.value = container.talepList.value.isEmpty()
            container.hydrateFromOfflineCache()
            // Gecikme yok ” cache zaten ekranda; ağ hemen yenilesin.
            runCatching { container.syncLiveData() }
                .onFailure { android.util.Log.e("PostLoginSync", "syncLiveData", it) }
            _initialSyncPending.value = false
            runCatching { container.syncData() }
                .onFailure { android.util.Log.e("PostLoginSync", "syncData", it) }
        }
    }

    fun login(email: String, password: String, rememberMe: Boolean) {
        viewModelScope.launch {
            _loginError.value = null
            _loginMessage.value = null
            _loading.value = true
            loginInProgress = true
            // Splash restore yarışını iptal et ” aksi halde giriş sonrası isLoggedIn=false yazar.
            splashJob?.cancel()
            splashJob = null
            runCatching {
                container.login(email, password, rememberMe)
            }.onSuccess {
                passwordSessionUnlocked = true
                _splashDone.value = true
                _needsBiometricUnlock.value = false
                _isLoggedIn.value = true
                biometricUnlockedAt = System.currentTimeMillis()
                appPausedAt = 0L
                applyPostLoginNavigation()
                consumePendingNotificationRoute()
                startPostLoginSync()
                BackgroundSyncScheduler.ensureScheduled(container.appContext)
            }.onFailure {
                _loginError.value = NetworkError.translate(it.message)
                // Başarısız login'de splash'ı tekrar açma; kullanıcı login ekranında kalsın.
                _splashDone.value = true
            }
            loginInProgress = false
            _loading.value = false
        }
    }

    /** Bellekte profil varken UI'nın login'e düşmesini engeller. */
    fun ensureLoggedInFromSession() {
        if (loginInProgress) return
        if ((_isLoggedIn.value || passwordSessionUnlocked || container.hasActiveSession()) &&
            !_needsBiometricUnlock.value
        ) {
            _splashDone.value = true
            _isLoggedIn.value = true
            if (_currentRoute.value == null) applyPostLoginNavigation()
        }
    }

    fun clearLoginFeedback() {
        _loginError.value = null
        _loginMessage.value = null
    }

    fun rememberedLoginEmail(): String = container.rememberedLoginEmail()

    val isServerConfigured: Boolean get() = container.config.isConfigured

    fun clearRememberedLogin() = container.clearRememberedLogin()

    fun forgotPassword(email: String) {
        viewModelScope.launch {
            _loginError.value = null
            _loginMessage.value = null
            _loading.value = true
            runCatching { container.sendPasswordResetEmail(email) }
                .onSuccess {
                    _loginMessage.value = "Sıfırlama bağlantısı kayıtlı e-posta adresinize gönderildi. Gelen kutunuzu ve spam klasörünü kontrol edin."
                }
                .onFailure { _loginError.value = NetworkError.translate(it.message) }
            _loading.value = false
        }
    }

    fun promptBiometricUnlock(activity: FragmentActivity) {
        if (!container.biometricPreferences.isEnabled()) {
            _biometricError.value = "Biyometrik kilit kapalı. Profil ayarlarından etkinleştirin."
            return
        }
        if (container.user.value == null) {
            _biometricError.value = "Kayıtlı oturum bulunamadı. Kullanıcı adı ve şifre ile giriş yapın."
            return
        }
        _biometricError.value = null
        BiometricAuthHelper.authenticate(
            activity = activity,
            onSuccess = { completeBiometricUnlock() },
            onError = { _biometricError.value = it },
            onCancel = { }
        )
    }

    fun completeBiometricUnlock() {
        if (!container.biometricPreferences.isEnabled() || container.user.value == null) {
            _biometricError.value = "Biyometrik giriş kullanılamıyor. Kullanıcı adı ve şifre ile giriş yapın."
            _needsBiometricUnlock.value = false
            _isLoggedIn.value = false
            return
        }
        _biometricError.value = null
        _needsBiometricUnlock.value = false
        _isLoggedIn.value = true
        biometricUnlockedAt = System.currentTimeMillis()
        startPostLoginSync()
    }

    private fun applyPostLoginNavigation() {
        val pending = container.pendingRoute
        _currentRoute.value = if (!pending.isNullOrBlank()) {
            BildirimRota.safeRoute(pending, container.user.value?.role)
        } else {
            RolNavigasyon.defaultRoute(container.user.value?.role)
        }
        val update = _pendingUpdate.value
        if (update != null && !container.isUpdateSkipped(update)) {
            _showUpdateDialog.value = true
        }
    }

    fun logout() {
        passwordSessionUnlocked = false
        loginInProgress = false
        container.logout()
        _isLoggedIn.value = false
        _needsBiometricUnlock.value = false
        _biometricError.value = null
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
        // Bildirim tıklaması: ana ekranı değil, doğrudan ilgili işlemi aç.
        routeHistory.clear()
        navigate(route, pushHistory = false)
        viewModelScope.launch {
            notificationId?.let { runCatching { container.markNotificationRead(it) } }
            runCatching { container.syncLiveData() }
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

    fun pdfBaglam() = container.pdfBaglam()

    /**
     * PDF paylaşmadan önce kiracı medyasını yeniler — Compose'un eski (logosuz) bağlamı
     * yakalamasını ve sync yarışını önler.
     */
    fun withPdfBaglam(block: (com.satinalmapro.android.services.SatinalmaPdfBaglam) -> Unit) {
        viewModelScope.launch {
            runCatching { container.loadMedya() }
            val baglam = container.pdfBaglam()
            withContext(Dispatchers.Main) { block(baglam) }
        }
    }

    fun refreshData() {
        viewModelScope.launch {
            if (!syncMutex.tryLock()) return@launch
            try {
                _loading.value = true
                runCatching { container.syncData() }
            } finally {
                _loading.value = false
                syncMutex.unlock()
            }
        }
    }

    private var backgroundRefreshStarted = false

    fun startBackgroundRefresh() {
        if (backgroundRefreshStarted) return
        backgroundRefreshStarted = true
        BackgroundSyncScheduler.ensureScheduled(container.appContext)
        // Canlı talep+bildirim ” uygulama açıkken sık; arka planda WorkManager tamamlar.
        viewModelScope.launch(Dispatchers.IO) {
            while (true) {
                val delayMs = if (foreground) 12_000L else 45_000L
                kotlinx.coroutines.delay(delayMs)
                if (_isLoggedIn.value) runCatching { container.syncLiveData() }
            }
        }
        // Tam senkron (stok/ayarlar) daha seyrek.
        viewModelScope.launch(Dispatchers.IO) {
            kotlinx.coroutines.delay(15_000)
            while (true) {
                kotlinx.coroutines.delay(if (foreground) 60_000 else 120_000)
                if (_isLoggedIn.value) runCatching { container.syncData() }
            }
        }
    }

    private var appPausedAt = 0L
    private var biometricUnlockedAt = 0L
    private var foreground = true

    fun onAppPause() {
        foreground = false
        appPausedAt = System.currentTimeMillis()
    }

    fun onAppResume() {
        foreground = true
        viewModelScope.launch(Dispatchers.IO) {
            // Şifre ile açılmış oturumda biyometrik kilidi tamamen atla (izin diyaloğu / recreate atmasın).
            if (passwordSessionUnlocked || loginInProgress) {
                if (container.hasActiveSession()) {
                    _needsBiometricUnlock.value = false
                    _isLoggedIn.value = true
                }
                // Hafif canlı senkron ” giriş oturumunda da telefon güncel kalsın.
                if (_isLoggedIn.value) {
                    runCatching { container.syncLiveData() }
                }
                return@launch
            }

            if (!container.hasActiveSession() && !container.hasPersistedSession()) return@launch
            if (container.user.value == null) return@launch

            val pauseDuration = if (appPausedAt > 0L) {
                System.currentTimeMillis() - appPausedAt
            } else {
                0L
            }
            val sinceUnlock = if (biometricUnlockedAt > 0L) {
                System.currentTimeMillis() - biometricUnlockedAt
            } else {
                Long.MAX_VALUE
            }
            val shouldLock = container.shouldRequireBiometricUnlock() &&
                _isLoggedIn.value &&
                !_needsBiometricUnlock.value &&
                pauseDuration >= 30_000 &&
                sinceUnlock >= 30_000
            if (shouldLock) {
                _needsBiometricUnlock.value = true
                _isLoggedIn.value = false
                return@launch
            }
            runCatching { container.syncLiveData() }
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

    fun onNotificationPermissionGranted() {
        viewModelScope.launch {
            runCatching { container.ensureFcmRegistered() }
            runCatching { container.refreshNotifications() }
        }
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

    fun clearDraft() = container.clearDraft()

    fun openNotification(notificationId: String, route: String) {
        navigate(route)
        viewModelScope.launch {
            runCatching { container.markNotificationRead(notificationId) }
        }
    }

    fun runWorkflow(onSuccess: () -> Unit = {}, block: suspend () -> Unit) {
        viewModelScope.launch {
            if (!workflowMutex.tryLock()) return@launch
            try {
                _submitError.value = null
                _loading.value = true
                runCatching { block() }
                    .onSuccess { onSuccess() }
                    .onFailure { _submitError.value = it.message ?: "İşlem başarısız" }
            } finally {
                _loading.value = false
                workflowMutex.unlock()
            }
        }
    }

    fun addTeklif(talepId: String, firmaAdi: String, marka: String, vadeGunu: Int, teslimSuresi: String, odemeSekli: String, kalemFiyatlari: Map<String, Double>, onSuccess: () -> Unit) =
        runWorkflow(onSuccess) { container.addTeklif(talepId, firmaAdi, marka, vadeGunu, teslimSuresi, odemeSekli, kalemFiyatlari) }

    fun updateTeklif(
        talepId: String,
        teklifId: String,
        firmaAdi: String,
        marka: String,
        vadeGunu: Int,
        teslimSuresi: String,
        odemeSekli: String,
        kalemFiyatlari: Map<String, Double>,
        onSuccess: () -> Unit
    ) = runWorkflow(onSuccess) {
        container.updateTeklif(talepId, teklifId, firmaAdi, marka, vadeGunu, teslimSuresi, odemeSekli, kalemFiyatlari)
    }

    fun deleteTeklif(talepId: String, teklifId: String, onSuccess: () -> Unit) =
        runWorkflow(onSuccess) { container.deleteTeklif(talepId, teklifId) }

    fun satinalmaOnerisiSec(talepId: String, teklifId: String, onSuccess: () -> Unit = {}) =
        runWorkflow(onSuccess) { container.satinalmaOnerisiSec(talepId, teklifId) }

    fun satinalmaOnerisiOtomatigeAl(talepId: String, onSuccess: () -> Unit = {}) =
        runWorkflow(onSuccess) { container.satinalmaOnerisiOtomatigeAl(talepId) }

    fun sendQuotesToManagement(talepId: String, onSuccess: () -> Unit) =
        runWorkflow(onSuccess) { container.sendQuotesToManagement(talepId) }

    fun yonetimOnayla(talepId: String, teklifIste: Boolean, onSuccess: () -> Unit) =
        runWorkflow(onSuccess) { container.yonetimOnayla(talepId, teklifIste) }

    fun yonetimReddet(talepId: String, gerekce: String, onSuccess: () -> Unit) =
        runWorkflow(onSuccess) { container.yonetimReddet(talepId, gerekce) }

    fun applyTalepDetayAction(
        talepId: String,
        action: com.satinalmapro.shared.filter.detail.PurchaseRequestDetailAction,
        quoteId: String? = null,
        note: String? = null,
        onSuccess: () -> Unit = {}
    ) = runWorkflow(onSuccess) { container.applyTalepDetayAction(talepId, action, quoteId, note) }

    fun yonetimTeklifOnayla(talepId: String, teklifId: String, onSuccess: () -> Unit) =
        runWorkflow(onSuccess) { container.yonetimTeklifOnayla(talepId, teklifId) }

    fun kalemBazliOnayla(talepId: String, kalemTeklifAtamalari: Map<String, String>, onSuccess: () -> Unit) =
        runWorkflow(onSuccess) { container.kalemBazliOnayla(talepId, kalemTeklifAtamalari) }

    fun kalemBazliOnaylaBolunmus(
        talepId: String,
        kalemFirmaAtamalari: Map<String, List<com.satinalmapro.android.core.model.KalemFirmaAtamasi>>,
        onSuccess: () -> Unit
    ) = runWorkflow(onSuccess) {
        container.kalemBazliOnayla(talepId, kalemTeklifAtamalari = null, kalemFirmaAtamalari = kalemFirmaAtamalari)
    }

    fun teklifGeriGonder(talepId: String, gerekce: String, onSuccess: () -> Unit) =
        runWorkflow(onSuccess) { container.teklifGeriGonder(talepId, gerekce.ifBlank { null }) }

    fun siparisVer(talepId: String, onSuccess: () -> Unit = {}) =
        runWorkflow(onSuccess) { container.siparisVer(talepId) }

    fun teklifsizFirmaFiyatKaydet(talepId: String, girdiler: List<Triple<String, String, Double>>, onSuccess: () -> Unit) =
        runWorkflow(onSuccess) { container.teklifsizFirmaFiyatKaydet(talepId, girdiler) }

    fun talepSil(talepId: String, onSuccess: () -> Unit) =
        runWorkflow(onSuccess) { container.talepSil(talepId) }

    fun talepGuncelle(
        talepId: String,
        site: String,
        aciklama: String,
        talepTuru: String,
        kalemler: List<Triple<String, String, String>>,
        onSuccess: () -> Unit
    ) = runWorkflow(onSuccess) { container.talepGuncelle(talepId, site, aciklama, talepTuru, kalemler) }

    fun malKabulVeDepoyaKaydet(
        talepId: String,
        kalemId: String,
        form: com.satinalmapro.android.ui.procurement.MalKabulFormSonuc,
        teklifId: String? = null,
        onSuccess: () -> Unit
    ) {
        val m = form.miktar.replace(',', '.').toDoubleOrNull()
            ?: run { _submitError.value = "Geçerli miktar girin"; return }
        val f = form.birimFiyat.replace(',', '.').toDoubleOrNull()
            ?: run { _submitError.value = "Geçerli birim fiyat girin"; return }
        if (form.firma.isBlank()) {
            _submitError.value = "Firma / tedarikçi girin"
            return
        }
        if (f <= 0) {
            _submitError.value = "Birim fiyat sıfırdan büyük olmalı"
            return
        }
        if (form.teslimAlan.isBlank()) {
            _submitError.value = "Teslim alan girin"
            return
        }
        if (form.depoSaha.isBlank()) {
            _submitError.value = if (form.sahayaDirekt) "Giriş deposu girin" else "Depo / saha girin"
            return
        }
        if (form.sahayaDirekt && form.sahaHedef.isBlank()) {
            _submitError.value = "Malzemenin indiği sahayı girin"
            return
        }
        runWorkflow(onSuccess) {
            container.malKabulVeDepoyaKaydet(
                talepId, kalemId, m, form.firma.trim(), f,
                form.kategori.ifBlank { "Malzeme" },
                form.fisNo.trim(), form.teslimAlan.trim(), form.depoSaha.trim(),
                form.sahayaDirekt, form.sahaHedef.trim(), teklifId
            )
        }
    }

    fun sevkiyatiTamamla(talepId: String, kalemId: String, teklifId: String? = null, onSuccess: () -> Unit) =
        runWorkflow(onSuccess) { container.sevkiyatiTamamla(talepId, kalemId, teklifId) }

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

    fun stokMalzemeOnerileri(query: String, sadeceMevcut: Boolean = false): List<String> =
        container.stokMalzemeOnerileri(query, sadeceMevcut)

    fun sonrakiGirisBelgeNo(): String = container.sonrakiGirisBelgeNo()

    fun sonrakiCikisBelgeNo(): String = container.sonrakiCikisBelgeNo()

    fun stokGirisCoklu(
        belgeNo: String,
        depo: String,
        teslimAlan: String,
        satirlar: List<StokRepository.GirisSatir>,
        onSuccess: () -> Unit
    ) {
        if (satirlar.isEmpty()) {
            _submitError.value = "En az bir satır girin"
            return
        }
        runWorkflow(onSuccess) { container.stokGirisCoklu(belgeNo, depo, teslimAlan, satirlar) }
    }

    fun stokCikisFisiOlustur(
        belgeNo: String,
        teslimAlan: String,
        satirlar: List<StokRepository.CikisSatir>
    ): com.satinalmapro.android.services.StokTeslimFisiHelper.Fis? =
        container.stokCikisFisiOlustur(belgeNo, teslimAlan, satirlar)

    fun stokCikisCoklu(
        belgeNo: String,
        teslimAlan: String,
        satirlar: List<StokRepository.CikisSatir>,
        onSuccess: () -> Unit
    ) {
        if (satirlar.isEmpty()) {
            _submitError.value = "En az bir satır girin"
            return
        }
        if (teslimAlan.isBlank()) {
            _submitError.value = "Teslim alan girin"
            return
        }
        runWorkflow(onSuccess) { container.stokCikisCoklu(belgeNo, teslimAlan, satirlar) }
    }

    fun stokHareketSil(hareketId: String, onSuccess: () -> Unit = {}) {
        runWorkflow(onSuccess) { container.stokHareketSil(hareketId) }
    }

    fun stokHareketGuncelle(
        hareketId: String,
        tarih: String,
        miktar: String,
        belgeNo: String,
        islemYapan: String,
        teslimEdilen: String,
        aciklama: String,
        onSuccess: () -> Unit = {}
    ) {
        val m = miktar.replace(',', '.').toDoubleOrNull()
        if (m == null || m <= 0) {
            _submitError.value = "Geçerli miktar girin"
            return
        }
        runWorkflow(onSuccess) {
            container.stokHareketGuncelle(hareketId, tarih, m, belgeNo, islemYapan, teslimEdilen, aciklama)
        }
    }

    fun markAllNotificationsRead() {
        viewModelScope.launch {
            if (!syncMutex.tryLock()) return@launch
            try {
                _loading.value = true
                runCatching { container.markAllNotificationsRead() }
                    .onFailure { _submitError.value = it.message ?: "Bildirimler güncellenemedi" }
            } finally {
                _loading.value = false
                syncMutex.unlock()
            }
        }
    }

    fun clearNotifications() {
        viewModelScope.launch {
            if (!syncMutex.tryLock()) return@launch
            try {
                _loading.value = true
                runCatching { container.clearNotifications() }
                    .onFailure { _submitError.value = it.message ?: "Bildirimler temizlenemedi" }
            } finally {
                _loading.value = false
                syncMutex.unlock()
            }
        }
    }

    fun loadSettings() {
        val role = container.user.value?.role
        if (!KullaniciRolleri.isAdmin(role) &&
            KullaniciRolleri.normalize(role) != KullaniciRolleri.SATINALMA
        ) return
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
            term = term,
            onSuccess = onSuccess,
            read = { it.malzemeBirimleri },
            write = { ayarlar, list -> ayarlar.copy(malzemeBirimleri = list) }
        )

    fun removeBirim(term: String) = updateTermList(
        term = term,
        remove = true,
        read = { it.malzemeBirimleri },
        write = { ayarlar, list -> ayarlar.copy(malzemeBirimleri = list) }
    )

    fun addKategori(term: String, onSuccess: () -> Unit = {}) =
        updateTermList(
            term = term,
            onSuccess = onSuccess,
            read = { it.malzemeKategorileri },
            write = { ayarlar, list -> ayarlar.copy(malzemeKategorileri = list) }
        )

    fun removeKategori(term: String) = updateTermList(
        term = term,
        remove = true,
        read = { it.malzemeKategorileri },
        write = { ayarlar, list -> ayarlar.copy(malzemeKategorileri = list) }
    )

    fun modulBugun(): String = container.modulBugun()

    fun agregaKaydet(kayit: com.satinalmapro.android.core.model.AgregaKaydi, onDone: () -> Unit = {}) =
        modulIslem({ container.agregaKaydet(kayit) }, onDone)

    fun agregaSil(id: String, onDone: () -> Unit = {}) =
        modulIslem({ container.agregaSil(id) }, onDone)

    fun cimentoKaydet(kayit: com.satinalmapro.android.core.model.CimentoKaydi, onDone: () -> Unit = {}) =
        modulIslem({ container.cimentoKaydet(kayit) }, onDone)

    fun cimentoSil(id: String, onDone: () -> Unit = {}) =
        modulIslem({ container.cimentoSil(id) }, onDone)

    fun alinanMalzemeKaydet(kayit: com.satinalmapro.android.core.model.AlinanMalzemeKaydi, onDone: () -> Unit = {}) =
        modulIslem({ container.alinanMalzemeKaydet(kayit) }, onDone)

    fun alinanMalzemeSil(id: String, onDone: () -> Unit = {}) =
        modulIslem({ container.alinanMalzemeSil(id) }, onDone)

    /** Fiyat Analiz için Alınan Malzemeler belgesini yeniden yükler. */
    fun refreshAlinanMalzemeler() {
        viewModelScope.launch {
            runCatching { container.reloadAlinanMalzemeler() }
        }
    }

    private fun modulIslem(block: suspend () -> Unit, onDone: () -> Unit) {
        viewModelScope.launch {
            _submitError.value = null
            _loading.value = true
            runCatching { block() }
                .onSuccess { onDone() }
                .onFailure { _submitError.value = NetworkError.translate(it.message) }
            _loading.value = false
        }
    }

    private fun updateTermList(
        term: String,
        remove: Boolean = false,
        onSuccess: () -> Unit = {},
        read: (UygulamaAyarlar) -> List<String>,
        write: (UygulamaAyarlar, List<String>) -> UygulamaAyarlar
    ) {
        val trimmed = term.trim()
        if (trimmed.isBlank()) {
            _settingsError.value = "Geçerli bir terim girin."
            return
        }
        viewModelScope.launch {
            _settingsError.value = null
            _loading.value = true
            runCatching {
                container.loadUygulamaAyarlar()
                val ayarlar = container.uygulamaAyarlar.value
                val kaynak = read(ayarlar)
                val next = if (remove) {
                    if (kaynak.size <= 1) throw IllegalStateException("En az bir terim kalmalıdır.")
                    kaynak.filterNot { it.equals(trimmed, ignoreCase = true) }
                } else {
                    if (kaynak.any { it.equals(trimmed, ignoreCase = true) })
                        throw IllegalStateException("Bu terim zaten listede.")
                    kaynak + trimmed
                }
                container.saveUygulamaAyarlar(write(ayarlar, next))
            }.onSuccess {
                _settingsMessage.value = if (remove) "Terim silindi." else "Terim eklendi."
                onSuccess()
            }.onFailure { _settingsError.value = NetworkError.translate(it.message) }
            _loading.value = false
        }
    }
}
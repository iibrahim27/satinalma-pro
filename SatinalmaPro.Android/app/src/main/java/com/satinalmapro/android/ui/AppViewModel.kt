package com.satinalmapro.android.ui

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.satinalmapro.android.core.AppContainer
import com.satinalmapro.android.core.model.MenuItem
import com.satinalmapro.android.core.model.TalepItem
import com.satinalmapro.android.core.model.TalepQueue
import com.satinalmapro.android.core.model.UserProfile
import com.satinalmapro.android.core.roles.BildirimRota
import com.satinalmapro.android.core.roles.RolNavigasyon
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

    val dashboardCards = talepler.map { container.dashboardData().first }
        .stateIn(viewModelScope, SharingStarted.WhileSubscribed(5000), emptyList())

    val dashboardActivities = talepler.map { container.dashboardData().second }
        .stateIn(viewModelScope, SharingStarted.WhileSubscribed(5000), emptyList())

    fun filteredTalepler(queue: TalepQueue): StateFlow<List<TalepItem>> =
        talepler.map { container.filteredTalepler(queue) }
            .stateIn(viewModelScope, SharingStarted.WhileSubscribed(5000), emptyList())

    fun talepById(id: String): StateFlow<TalepItem?> =
        talepler.map { container.findTalep(id) }
            .stateIn(viewModelScope, SharingStarted.WhileSubscribed(5000), null)

    fun approvedMaterials(): StateFlow<List<TalepItem>> =
        talepler.map { container.approvedMaterials() }
            .stateIn(viewModelScope, SharingStarted.WhileSubscribed(5000), emptyList())

    fun startSplash() {
        viewModelScope.launch {
            _splashMessage.value = "Güncelleme kontrol ediliyor..."
            val updating = runCatching {
                container.checkAndApplyUpdate { msg, _ -> _splashMessage.value = msg }
            }.getOrDefault(false)
            if (updating) {
                _splashMessage.value = "Kurulum ekranı açıldı. Yükleme tamamlanınca uygulamayı yeniden açın."
                return@launch
            }
            _splashMessage.value = "Oturum kontrol ediliyor..."
            val restored = runCatching { container.restoreSession() }.getOrDefault(false)
            _isLoggedIn.value = restored
            if (restored) {
                _currentRoute.value = RolNavigasyon.defaultRoute(container.user.value?.role)
            }
            container.pendingRoute?.let { route ->
                navigateFromNotification(route)
                container.pendingRoute = null
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
                _currentRoute.value = RolNavigasyon.defaultRoute(container.user.value?.role)
            }.onFailure {
                _loginError.value = it.message ?: "Giriş başarısız"
            }
            _loading.value = false
        }
    }

    fun logout() {
        container.logout()
        _isLoggedIn.value = false
        _currentRoute.value = null
    }

    fun menus(): List<MenuItem> = RolNavigasyon.menus(container.user.value?.role)

    fun navigate(route: String) {
        val safe = BildirimRota.safeRoute(route, container.user.value?.role)
        _currentRoute.value = safe
    }

    fun navigateFromNotification(route: String) {
        if (container.user.value == null) {
            container.pendingRoute = route
            return
        }
        navigate(route)
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
    }

    fun materialSuggestions(query: String): List<String> = container.materialSuggestions(query)

    fun handleNotificationRoute(route: String?) {
        if (!route.isNullOrBlank()) navigateFromNotification(route)
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
            navigate(route)
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

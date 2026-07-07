package com.satinalmapro.android.ui.screens.modul

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.rounded.Add
import androidx.compose.material.icons.rounded.Delete
import androidx.compose.material.icons.rounded.Edit
import androidx.compose.material.icons.rounded.Print
import androidx.compose.material.icons.rounded.Search
import androidx.compose.material.icons.rounded.Share
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Button
import androidx.compose.material3.Checkbox
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.FloatingActionButton
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.pulltorefresh.PullToRefreshBox
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.material3.HorizontalDivider
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import com.satinalmapro.android.core.model.AgregaKaydi
import com.satinalmapro.android.core.model.AlinanMalzemeKaydi
import com.satinalmapro.android.core.model.CimentoKaydi
import com.satinalmapro.android.core.model.ModulKayitTipi
import com.satinalmapro.android.core.roles.KullaniciRolleri
import com.satinalmapro.android.services.ModulListePaylasHelper
import com.satinalmapro.android.ui.AppViewModel
import com.satinalmapro.android.core.liste.ModulGrupSecenegi
import com.satinalmapro.android.core.liste.ModulListeAyarlari
import com.satinalmapro.android.core.liste.ModulTabloKolon
import com.satinalmapro.android.core.liste.paginateList
import com.satinalmapro.android.core.liste.totalPages
import com.satinalmapro.android.ui.components.AppSearchField
import com.satinalmapro.android.ui.components.ModulTabloToolbar
import com.satinalmapro.android.ui.theme.AppColors
import com.satinalmapro.android.ui.theme.AppSpacing

@Composable
fun AgregaScreen(viewModel: AppViewModel) = ModulKayitScreen(viewModel, ModulKayitTipi.AGREGA)

@Composable
fun CimentoScreen(viewModel: AppViewModel) = ModulKayitScreen(viewModel, ModulKayitTipi.CIMENTO)

@Composable
fun AlinanMalzemeModulScreen(viewModel: AppViewModel) = ModulKayitScreen(viewModel, ModulKayitTipi.ALINAN_MALZEME)

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun ModulKayitScreen(viewModel: AppViewModel, tip: ModulKayitTipi) {
    val context = LocalContext.current
    val user by viewModel.user.collectAsState()
    val canWrite = KullaniciRolleri.canModulKayitWrite(user?.role)
    var search by remember { mutableStateOf("") }
    var editAgrega by remember { mutableStateOf<AgregaKaydi?>(null) }
    var editCimento by remember { mutableStateOf<CimentoKaydi?>(null) }
    var editMalzeme by remember { mutableStateOf<AlinanMalzemeKaydi?>(null) }
    var deleteAgrega by remember { mutableStateOf<AgregaKaydi?>(null) }
    var deleteCimento by remember { mutableStateOf<CimentoKaydi?>(null) }
    var deleteMalzeme by remember { mutableStateOf<AlinanMalzemeKaydi?>(null) }
    var listeAyarlari by remember { mutableStateOf(ModulListeAyarlari()) }
    var listeSayfa by remember { mutableStateOf(1) }
    val error by viewModel.submitError.collectAsState()
    val refreshing by viewModel.loading.collectAsState()

    when (tip) {
        ModulKayitTipi.AGREGA -> {
            val list by viewModel.agregaList.collectAsState()
            val filtered = list.filter { matchesSearch(search, it.tarih, it.agregaCinsi, it.tedarikci, it.irsaliyeNo) }
            val kolonlar = AgregaKolonTanimlari
            val gruplar = AgregaGrupSecenekleri
            val sorted = filtered.siralaModulListe(listeAyarlari, { it.tarih }) { k, f ->
                when (f) {
                    "cinsi" -> k.agregaCinsi
                    "tedarikci" -> k.tedarikci
                    "tur" -> k.agregaTuru
                    else -> ""
                }
            }
            val toplamSayfa = totalPages(sorted.size, listeAyarlari.pageSize)
            val sayfa = listeSayfa.coerceIn(1, toplamSayfa)
            val sayfali = paginateList(sorted, sayfa, listeAyarlari.pageSize)
            ModulListeScaffold(
                baslik = tip.baslik,
                altBaslik = "Agrega giriş kayıtları — masaüstü ile senkron",
                canWrite = canWrite,
                refreshing = refreshing,
                onRefresh = { viewModel.refreshData() },
                error = error,
                search = search,
                onSearch = { search = it; listeSayfa = 1 },
                onShareCsv = {
                    val rows = mutableListOf(listOf("Tarih", "Malzeme Cinsi", "Miktar", "Birim Fiyatı"))
                    filtered.forEach { k ->
                        rows.add(listOf(k.tarih, k.agregaCinsi, formatMiktar(k.miktar), formatFiyat(k.birimFiyati)))
                    }
                    ModulListePaylasHelper.csvPaylas(context, tip.baslik, rows)
                },
                onSharePdf = {
                    val basliklar = listOf("Tarih", "Malzeme Cinsi", "Miktar", "Birim Fiyatı")
                    val satirlar = filtered.map { listOf(it.tarih, it.agregaCinsi, formatMiktar(it.miktar), formatFiyat(it.birimFiyati)) }
                    ModulListePaylasHelper.pdfTabloPaylas(context, tip.baslik, basliklar, satirlar)
                },
                onAdd = { editAgrega = AgregaKaydi(tarih = viewModel.modulBugun()) },
                itemCount = filtered.size,
                fullscreen = listeAyarlari.fullscreen,
                toolbar = {
                    ModulTabloToolbar(
                        ayarlar = listeAyarlari,
                        kolonlar = kolonlar,
                        grupSecenekleri = gruplar,
                        kayitSayisi = sorted.size,
                        guncelSayfa = sayfa,
                        toplamSayfa = toplamSayfa,
                        onAyarlarDegisti = { listeAyarlari = it; listeSayfa = 1 },
                        onSayfaDegisti = { listeSayfa = it }
                    )
                }
            ) {
                ModulKayitTablo(
                    kolonlar = kolonlar,
                    hiddenIds = listeAyarlari.hiddenColumnIds,
                    dense = listeAyarlari.dense,
                    satirlar = sayfali.map { kayit ->
                        ModulTabloSatiriData(
                            hucreler = mapOf(
                                "tarih" to kayit.tarih,
                                "cinsi" to kayit.agregaCinsi,
                                "tur" to kayit.agregaTuru,
                                "tedarikci" to kayit.tedarikci,
                                "miktar" to formatMiktar(kayit.miktar),
                                "fiyat" to formatFiyat(kayit.birimFiyati)
                            ),
                            onEdit = { editAgrega = kayit },
                            onDelete = { deleteAgrega = kayit }
                        )
                    },
                    canWrite = canWrite
                )
            }
            editAgrega?.let { k ->
                AgregaDuzenleDialog(k, canWrite, onDismiss = { editAgrega = null }) { kayit ->
                    viewModel.agregaKaydet(kayit) { editAgrega = null }
                }
            }
            deleteAgrega?.let { k ->
                SilOnayDialog("${k.agregaCinsi} silinsin mi?", onDismiss = { deleteAgrega = null }) {
                    viewModel.agregaSil(k.id) { deleteAgrega = null }
                }
            }
        }
        ModulKayitTipi.CIMENTO -> {
            val list by viewModel.cimentoList.collectAsState()
            val filtered = list.filter { matchesSearch(search, it.tarih, it.cimentoCinsi, it.tedarikci, it.irsaliyeNo) }
            val kolonlar = CimentoKolonTanimlari
            val gruplar = CimentoGrupSecenekleri
            val sorted = filtered.siralaModulListe(listeAyarlari, { it.tarih }) { k, f ->
                when (f) {
                    "cinsi" -> k.cimentoCinsi
                    "sinif" -> k.cimentoSinifi
                    "tedarikci" -> k.tedarikci
                    else -> ""
                }
            }
            val toplamSayfa = totalPages(sorted.size, listeAyarlari.pageSize)
            val sayfa = listeSayfa.coerceIn(1, toplamSayfa)
            val sayfali = paginateList(sorted, sayfa, listeAyarlari.pageSize)
            ModulListeScaffold(
                baslik = tip.baslik,
                altBaslik = "Dökme çimento giriş kayıtları",
                canWrite = canWrite,
                refreshing = refreshing,
                onRefresh = { viewModel.refreshData() },
                error = error,
                search = search,
                onSearch = { search = it; listeSayfa = 1 },
                onShareCsv = {
                    val rows = mutableListOf(listOf("Tarih", "Malzeme Cinsi", "Miktar", "Birim Fiyatı"))
                    filtered.forEach { k ->
                        rows.add(listOf(k.tarih, k.cimentoCinsi, formatMiktar(k.miktar), formatFiyat(k.birimFiyati)))
                    }
                    ModulListePaylasHelper.csvPaylas(context, tip.baslik, rows)
                },
                onSharePdf = {
                    val basliklar = listOf("Tarih", "Malzeme Cinsi", "Miktar", "Birim Fiyatı")
                    val satirlar = filtered.map { listOf(it.tarih, it.cimentoCinsi, formatMiktar(it.miktar), formatFiyat(it.birimFiyati)) }
                    ModulListePaylasHelper.pdfTabloPaylas(context, tip.baslik, basliklar, satirlar)
                },
                onAdd = { editCimento = CimentoKaydi(tarih = viewModel.modulBugun()) },
                itemCount = filtered.size,
                fullscreen = listeAyarlari.fullscreen,
                toolbar = {
                    ModulTabloToolbar(
                        ayarlar = listeAyarlari,
                        kolonlar = kolonlar,
                        grupSecenekleri = gruplar,
                        kayitSayisi = sorted.size,
                        guncelSayfa = sayfa,
                        toplamSayfa = toplamSayfa,
                        onAyarlarDegisti = { listeAyarlari = it; listeSayfa = 1 },
                        onSayfaDegisti = { listeSayfa = it }
                    )
                }
            ) {
                ModulKayitTablo(
                    kolonlar = kolonlar,
                    hiddenIds = listeAyarlari.hiddenColumnIds,
                    dense = listeAyarlari.dense,
                    satirlar = sayfali.map { kayit ->
                        ModulTabloSatiriData(
                            hucreler = mapOf(
                                "tarih" to kayit.tarih,
                                "cinsi" to kayit.cimentoCinsi,
                                "sinif" to kayit.cimentoSinifi,
                                "tedarikci" to kayit.tedarikci,
                                "miktar" to formatMiktar(kayit.miktar),
                                "fiyat" to formatFiyat(kayit.birimFiyati)
                            ),
                            onEdit = { editCimento = kayit },
                            onDelete = { deleteCimento = kayit }
                        )
                    },
                    canWrite = canWrite
                )
            }
            editCimento?.let { k ->
                CimentoDuzenleDialog(k, canWrite, onDismiss = { editCimento = null }) { kayit ->
                    viewModel.cimentoKaydet(kayit) { editCimento = null }
                }
            }
            deleteCimento?.let { k ->
                SilOnayDialog("${k.cimentoCinsi} silinsin mi?", onDismiss = { deleteCimento = null }) {
                    viewModel.cimentoSil(k.id) { deleteCimento = null }
                }
            }
        }
        ModulKayitTipi.ALINAN_MALZEME -> {
            val list by viewModel.alinanMalzemeKayitlari.collectAsState()
            val filtered = list.filter { matchesSearch(search, it.tarih, it.malzemeHizmet, it.tedarikci, it.faturaNo) }
            val kolonlar = MalzemeKolonTanimlari
            val gruplar = MalzemeGrupSecenekleri
            val sorted = filtered.siralaModulListe(listeAyarlari, { it.tarih }) { k, f ->
                when (f) {
                    "malzeme" -> k.malzemeHizmet
                    "kategori" -> k.kategori
                    "tedarikci" -> k.tedarikci
                    else -> ""
                }
            }
            val toplamSayfa = totalPages(sorted.size, listeAyarlari.pageSize)
            val sayfa = listeSayfa.coerceIn(1, toplamSayfa)
            val sayfali = paginateList(sorted, sayfa, listeAyarlari.pageSize)
            ModulListeScaffold(
                baslik = tip.baslik,
                altBaslik = "Bağımsız malzeme giriş kayıtları (Excel aktarımı masaüstünden)",
                canWrite = canWrite,
                refreshing = refreshing,
                onRefresh = { viewModel.refreshData() },
                error = error,
                search = search,
                onSearch = { search = it; listeSayfa = 1 },
                onShareCsv = {
                    val rows = mutableListOf(listOf("Tarih", "Malzeme Adı", "Miktar", "Birim", "Birim Fiyatı"))
                    filtered.forEach { k ->
                        rows.add(listOf(k.tarih, k.malzemeHizmet, formatMiktar(k.miktar), k.birim, formatFiyat(k.birimFiyati)))
                    }
                    ModulListePaylasHelper.csvPaylas(context, tip.baslik, rows)
                },
                onSharePdf = {
                    val basliklar = listOf("Tarih", "Malzeme Adı", "Miktar", "Birim", "Birim Fiyatı")
                    val satirlar = filtered.map {
                        listOf(it.tarih, it.malzemeHizmet, formatMiktar(it.miktar), it.birim, formatFiyat(it.birimFiyati))
                    }
                    ModulListePaylasHelper.pdfTabloPaylas(context, tip.baslik, basliklar, satirlar)
                },
                onAdd = { editMalzeme = AlinanMalzemeKaydi(tarih = viewModel.modulBugun()) },
                itemCount = filtered.size,
                fullscreen = listeAyarlari.fullscreen,
                toolbar = {
                    ModulTabloToolbar(
                        ayarlar = listeAyarlari,
                        kolonlar = kolonlar,
                        grupSecenekleri = gruplar,
                        kayitSayisi = sorted.size,
                        guncelSayfa = sayfa,
                        toplamSayfa = toplamSayfa,
                        onAyarlarDegisti = { listeAyarlari = it; listeSayfa = 1 },
                        onSayfaDegisti = { listeSayfa = it }
                    )
                }
            ) {
                ModulKayitTablo(
                    kolonlar = kolonlar,
                    hiddenIds = listeAyarlari.hiddenColumnIds,
                    dense = listeAyarlari.dense,
                    satirlar = sayfali.map { kayit ->
                        ModulTabloSatiriData(
                            hucreler = mapOf(
                                "tarih" to kayit.tarih,
                                "malzeme" to kayit.malzemeHizmet.ifBlank { "—" },
                                "kategori" to kayit.kategori,
                                "tedarikci" to kayit.tedarikci,
                                "miktar" to formatMiktar(kayit.miktar),
                                "birim" to kayit.birim,
                                "fiyat" to formatFiyat(kayit.birimFiyati)
                            ),
                            onEdit = { editMalzeme = kayit },
                            onDelete = { deleteMalzeme = kayit }
                        )
                    },
                    canWrite = canWrite
                )
            }
            editMalzeme?.let { k ->
                MalzemeDuzenleDialog(k, canWrite, onDismiss = { editMalzeme = null }) { kayit ->
                    viewModel.alinanMalzemeKaydet(kayit) { editMalzeme = null }
                }
            }
            deleteMalzeme?.let { k ->
                SilOnayDialog("${k.malzemeHizmet} silinsin mi?", onDismiss = { deleteMalzeme = null }) {
                    viewModel.alinanMalzemeSil(k.id) { deleteMalzeme = null }
                }
            }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun ModulListeScaffold(
    baslik: String,
    altBaslik: String,
    canWrite: Boolean,
    refreshing: Boolean,
    onRefresh: () -> Unit,
    error: String?,
    search: String,
    onSearch: (String) -> Unit,
    onShareCsv: () -> Unit,
    onSharePdf: () -> Unit,
    onAdd: () -> Unit,
    itemCount: Int,
    fullscreen: Boolean = false,
    toolbar: (@Composable () -> Unit)? = null,
    content: @Composable () -> Unit
) {
    Scaffold(
        floatingActionButton = {
            if (canWrite && !fullscreen) {
                FloatingActionButton(onClick = onAdd, containerColor = AppColors.Primary) {
                    Icon(Icons.Rounded.Add, "Yeni kayıt")
                }
            }
        }
    ) { padding ->
        Column(Modifier.fillMaxSize().padding(padding).padding(horizontal = AppSpacing.screenHorizontal, vertical = if (fullscreen) 8.dp else AppSpacing.screenVertical)) {
            if (!fullscreen) {
                Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween, verticalAlignment = Alignment.CenterVertically) {
                    Column(Modifier.weight(1f)) {
                        Text(baslik, style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.SemiBold)
                        Text(altBaslik, style = MaterialTheme.typography.bodySmall, color = AppColors.TextSecondary)
                    }
                    Row(horizontalArrangement = Arrangement.spacedBy(4.dp)) {
                        IconButton(onClick = onShareCsv) { Icon(Icons.Rounded.Share, "CSV paylaş") }
                        IconButton(onClick = onSharePdf) { Icon(Icons.Rounded.Print, "PDF paylaş") }
                    }
                }
                Text("$itemCount kayıt", style = MaterialTheme.typography.labelMedium, color = AppColors.TextSecondary, modifier = Modifier.padding(top = 4.dp))
                AppSearchField(
                    value = search,
                    onValueChange = onSearch,
                    placeholder = "Ara..."
                )
            }
            toolbar?.invoke()
            error?.let { Text(it, color = AppColors.Danger, modifier = Modifier.padding(bottom = 8.dp)) }
            PullToRefreshBox(
                isRefreshing = refreshing,
                onRefresh = onRefresh,
                modifier = Modifier.weight(1f).fillMaxWidth()
            ) {
                content()
            }
        }
    }
}

private data class ModulTabloSatiriData(
    val hucreler: Map<String, String>,
    val onEdit: () -> Unit,
    val onDelete: () -> Unit
)

@Composable
private fun ModulKayitTablo(
    kolonlar: List<ModulTabloKolon>,
    hiddenIds: Set<String>,
    dense: Boolean,
    satirlar: List<ModulTabloSatiriData>,
    canWrite: Boolean
) {
    val gorunur = kolonlar.filter { it.id !in hiddenIds }
    if (satirlar.isEmpty()) {
        Text("Kayıt bulunamadı.", color = AppColors.TextSecondary, modifier = Modifier.padding(8.dp))
        return
    }
    if (gorunur.isEmpty()) {
        Text("En az bir kolon görünür olmalıdır.", color = AppColors.Danger, modifier = Modifier.padding(8.dp))
        return
    }
    ModulTabloBaslik(gorunur, canWrite, dense)
    LazyColumn(modifier = Modifier.fillMaxSize()) {
        items(satirlar.size) { index ->
            val satir = satirlar[index]
            ModulTabloSatiri(
                kolonlar = gorunur,
                hucreler = satir.hucreler,
                dense = dense,
                canWrite = canWrite,
                onEdit = satir.onEdit,
                onDelete = satir.onDelete
            )
        }
    }
}

@Composable
private fun ModulTabloBaslik(kolonlar: List<ModulTabloKolon>, canWrite: Boolean, dense: Boolean) {
    val padV = if (dense) 4.dp else 8.dp
    Row(
        Modifier
            .fillMaxWidth()
            .padding(horizontal = 4.dp, vertical = padV),
        horizontalArrangement = Arrangement.spacedBy(6.dp),
        verticalAlignment = Alignment.CenterVertically
    ) {
        kolonlar.forEach { kolon ->
            Text(
                kolon.label,
                Modifier.weight(kolon.weight),
                style = MaterialTheme.typography.labelMedium,
                color = AppColors.TextSecondary,
                fontWeight = FontWeight.SemiBold,
                maxLines = 2
            )
        }
        if (canWrite) Spacer(Modifier.width(72.dp))
    }
    HorizontalDivider(color = AppColors.Border)
}

@Composable
private fun ModulTabloSatiri(
    kolonlar: List<ModulTabloKolon>,
    hucreler: Map<String, String>,
    dense: Boolean,
    canWrite: Boolean,
    onEdit: () -> Unit,
    onDelete: () -> Unit
) {
    val padV = if (dense) 6.dp else 10.dp
    Column {
        Row(
            Modifier
                .fillMaxWidth()
                .padding(horizontal = 4.dp, vertical = padV),
            horizontalArrangement = Arrangement.spacedBy(6.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            kolonlar.forEach { kolon ->
                Text(
                    hucreler[kolon.id].orEmpty().ifBlank { "—" },
                    Modifier.weight(kolon.weight),
                    style = if (dense) MaterialTheme.typography.labelSmall else MaterialTheme.typography.bodySmall,
                    color = AppColors.TextPrimary,
                    maxLines = 2,
                    overflow = TextOverflow.Ellipsis
                )
            }
            if (canWrite) {
                Row(Modifier.width(72.dp), horizontalArrangement = Arrangement.End) {
                    IconButton(onClick = onEdit, modifier = Modifier.size(if (dense) 24.dp else 28.dp)) {
                        Icon(Icons.Rounded.Edit, "Düzenle", modifier = Modifier.size(if (dense) 16.dp else 18.dp))
                    }
                    IconButton(onClick = onDelete, modifier = Modifier.size(if (dense) 24.dp else 28.dp)) {
                        Icon(Icons.Rounded.Delete, "Sil", tint = AppColors.Danger, modifier = Modifier.size(if (dense) 16.dp else 18.dp))
                    }
                }
            }
        }
        HorizontalDivider(color = AppColors.Border)
    }
}

@Composable
private fun SilOnayDialog(mesaj: String, onDismiss: () -> Unit, onConfirm: () -> Unit) {
    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text("Kayıt Sil") },
        text = { Text(mesaj) },
        confirmButton = { TextButton(onClick = onConfirm) { Text("Sil", color = AppColors.Danger) } },
        dismissButton = { TextButton(onClick = onDismiss) { Text("İptal") } }
    )
}

@Composable
private fun AgregaDuzenleDialog(kayit: AgregaKaydi, canWrite: Boolean, onDismiss: () -> Unit, onSave: (AgregaKaydi) -> Unit) {
    var k by remember(kayit.id) { mutableStateOf(kayit) }
    KayitFormDialog("Agrega Kaydı", onDismiss, canWrite, onSave = { onSave(k.hesaplaToplam()) }) {
        FormField("Tarih", k.tarih, canWrite) { k = k.copy(tarih = it) }
        FormField("İrsaliye No", k.irsaliyeNo, canWrite) { k = k.copy(irsaliyeNo = it) }
        FormField("Agrega Türü", k.agregaTuru, canWrite) { k = k.copy(agregaTuru = it) }
        FormField("Agrega Cinsi", k.agregaCinsi, canWrite) { k = k.copy(agregaCinsi = it) }
        FormField("Miktar", k.miktar.toString(), canWrite) { v -> k = k.copy(miktar = v.toDoubleOrNull() ?: k.miktar) }
        FormField("Birim", k.birim, canWrite) { k = k.copy(birim = it) }
        FormField("Birim Fiyat", k.birimFiyati.toString(), canWrite) { v -> k = k.copy(birimFiyati = v.toDoubleOrNull() ?: k.birimFiyati) }
        FormField("Tedarikçi", k.tedarikci, canWrite) { k = k.copy(tedarikci = it) }
        FormField("İndirildiği Saha", k.indirildigiSaha, canWrite) { k = k.copy(indirildigiSaha = it) }
        FormField("Teslim Alan", k.teslimAlan, canWrite) { k = k.copy(teslimAlan = it) }
        FormField("Açıklama", k.aciklama, canWrite) { k = k.copy(aciklama = it) }
        Row(verticalAlignment = Alignment.CenterVertically) {
            Checkbox(checked = k.faturasiKesildi, onCheckedChange = { if (canWrite) k = k.copy(faturasiKesildi = it) }, enabled = canWrite)
            Text("Faturası kesildi")
        }
    }
}

@Composable
private fun CimentoDuzenleDialog(kayit: CimentoKaydi, canWrite: Boolean, onDismiss: () -> Unit, onSave: (CimentoKaydi) -> Unit) {
    var k by remember(kayit.id) { mutableStateOf(kayit) }
    KayitFormDialog("Çimento Kaydı", onDismiss, canWrite, onSave = { onSave(k.hesaplaToplam()) }) {
        FormField("Tarih", k.tarih, canWrite) { k = k.copy(tarih = it) }
        FormField("İrsaliye No", k.irsaliyeNo, canWrite) { k = k.copy(irsaliyeNo = it) }
        FormField("Çimento Sınıfı", k.cimentoSinifi, canWrite) { k = k.copy(cimentoSinifi = it) }
        FormField("Çimento Cinsi", k.cimentoCinsi, canWrite) { k = k.copy(cimentoCinsi = it) }
        FormField("Miktar", k.miktar.toString(), canWrite) { v -> k = k.copy(miktar = v.toDoubleOrNull() ?: k.miktar) }
        FormField("Birim", k.birim, canWrite) { k = k.copy(birim = it) }
        FormField("Birim Fiyat", k.birimFiyati.toString(), canWrite) { v -> k = k.copy(birimFiyati = v.toDoubleOrNull() ?: k.birimFiyati) }
        FormField("Tedarikçi", k.tedarikci, canWrite) { k = k.copy(tedarikci = it) }
        FormField("İndirildiği Saha", k.indirildigiSaha, canWrite) { k = k.copy(indirildigiSaha = it) }
        FormField("Teslim Alan", k.teslimAlan, canWrite) { k = k.copy(teslimAlan = it) }
        FormField("Açıklama", k.aciklama, canWrite) { k = k.copy(aciklama = it) }
        Row(verticalAlignment = Alignment.CenterVertically) {
            Checkbox(checked = k.faturasiKesildi, onCheckedChange = { if (canWrite) k = k.copy(faturasiKesildi = it) }, enabled = canWrite)
            Text("Faturası kesildi")
        }
    }
}

@Composable
private fun MalzemeDuzenleDialog(kayit: AlinanMalzemeKaydi, canWrite: Boolean, onDismiss: () -> Unit, onSave: (AlinanMalzemeKaydi) -> Unit) {
    var k by remember(kayit.id) { mutableStateOf(kayit) }
    KayitFormDialog("Alınan Malzeme", onDismiss, canWrite, onSave = { onSave(k.hesaplaToplam()) }) {
        FormField("Tarih", k.tarih, canWrite) { k = k.copy(tarih = it) }
        FormField("Fatura No", k.faturaNo, canWrite) { k = k.copy(faturaNo = it) }
        FormField("Kategori", k.kategori, canWrite) { k = k.copy(kategori = it) }
        FormField("Malzeme / Hizmet", k.malzemeHizmet, canWrite) { k = k.copy(malzemeHizmet = it) }
        FormField("Miktar", k.miktar.toString(), canWrite) { v -> k = k.copy(miktar = v.toDoubleOrNull() ?: k.miktar) }
        FormField("Birim", k.birim, canWrite) { k = k.copy(birim = it) }
        FormField("Birim Fiyat", k.birimFiyati.toString(), canWrite) { v -> k = k.copy(birimFiyati = v.toDoubleOrNull() ?: k.birimFiyati) }
        FormField("Tedarikçi", k.tedarikci, canWrite) { k = k.copy(tedarikci = it) }
        FormField("İndirildiği Saha", k.indirildigiSaha, canWrite) { k = k.copy(indirildigiSaha = it) }
        FormField("Teslim Alan", k.teslimAlan, canWrite) { k = k.copy(teslimAlan = it) }
        FormField("Açıklama", k.aciklama, canWrite) { k = k.copy(aciklama = it) }
    }
}

@Composable
private fun KayitFormDialog(
    baslik: String,
    onDismiss: () -> Unit,
    canWrite: Boolean,
    onSave: () -> Unit,
    content: @Composable () -> Unit
) {
    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text(baslik) },
        text = {
            Column(Modifier.verticalScroll(rememberScrollState()), verticalArrangement = Arrangement.spacedBy(8.dp)) {
                content()
            }
        },
        confirmButton = {
            if (canWrite) Button(onClick = onSave) { Text("Kaydet") }
        },
        dismissButton = { TextButton(onClick = onDismiss) { Text(if (canWrite) "İptal" else "Kapat") } }
    )
}

@Composable
private fun FormField(label: String, value: String, enabled: Boolean, onChange: (String) -> Unit) {
    OutlinedTextField(
        value = value,
        onValueChange = onChange,
        label = { Text(label) },
        modifier = Modifier.fillMaxWidth(),
        enabled = enabled,
        singleLine = label != "Açıklama"
    )
}

private fun matchesSearch(query: String, vararg fields: String): Boolean {
    if (query.isBlank()) return true
    return fields.any { it.contains(query, ignoreCase = true) }
}

private fun formatMiktar(miktar: Double): String =
    if (miktar % 1.0 == 0.0) miktar.toLong().toString() else "%.2f".format(miktar)

private fun formatFiyat(fiyat: Double): String = "%.2f ₺".format(fiyat)

private val AgregaKolonTanimlari = listOf(
    ModulTabloKolon("tarih", "Tarih", 0.85f),
    ModulTabloKolon("cinsi", "Malzeme Cinsi", 1.35f),
    ModulTabloKolon("tur", "Tür", 0.9f),
    ModulTabloKolon("tedarikci", "Tedarikçi", 1f),
    ModulTabloKolon("miktar", "Miktar", 0.75f),
    ModulTabloKolon("fiyat", "Birim Fiyatı", 0.95f)
)

private val AgregaGrupSecenekleri = listOf(
    ModulGrupSecenegi("Malzeme Cinsi", "cinsi"),
    ModulGrupSecenegi("Tür", "tur"),
    ModulGrupSecenegi("Tedarikçi", "tedarikci")
)

private val CimentoKolonTanimlari = listOf(
    ModulTabloKolon("tarih", "Tarih", 0.85f),
    ModulTabloKolon("cinsi", "Malzeme Cinsi", 1.35f),
    ModulTabloKolon("sinif", "Sınıf", 0.85f),
    ModulTabloKolon("tedarikci", "Tedarikçi", 1f),
    ModulTabloKolon("miktar", "Miktar", 0.75f),
    ModulTabloKolon("fiyat", "Birim Fiyatı", 0.95f)
)

private val CimentoGrupSecenekleri = listOf(
    ModulGrupSecenegi("Malzeme Cinsi", "cinsi"),
    ModulGrupSecenegi("Sınıf", "sinif"),
    ModulGrupSecenegi("Tedarikçi", "tedarikci")
)

private val MalzemeKolonTanimlari = listOf(
    ModulTabloKolon("tarih", "Tarih", 0.8f),
    ModulTabloKolon("malzeme", "Malzeme Adı", 1.25f),
    ModulTabloKolon("kategori", "Kategori", 0.9f),
    ModulTabloKolon("tedarikci", "Tedarikçi", 1f),
    ModulTabloKolon("miktar", "Miktar", 0.65f),
    ModulTabloKolon("birim", "Birim", 0.55f),
    ModulTabloKolon("fiyat", "Birim Fiyatı", 0.85f)
)

private val MalzemeGrupSecenekleri = listOf(
    ModulGrupSecenegi("Malzeme", "malzeme"),
    ModulGrupSecenegi("Kategori", "kategori"),
    ModulGrupSecenegi("Tedarikçi", "tedarikci")
)

private fun <T> List<T>.siralaModulListe(
    ayarlar: ModulListeAyarlari,
    tarih: (T) -> String,
    grup: (T, String) -> String
): List<T> {
    val gf = ayarlar.groupField
    return if (gf.isNullOrBlank()) {
        sortedByDescending { tarih(it) }
    } else {
        sortedWith(
            compareBy<T> { grup(it, gf).lowercase() }
                .thenByDescending { tarih(it) }
        )
    }
}

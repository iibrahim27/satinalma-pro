package com.satinalmapro.android.ui.screens.stok

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.itemsIndexed
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.width
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.rounded.Add
import androidx.compose.material.icons.rounded.Delete
import androidx.compose.material.icons.rounded.Edit
import androidx.compose.material.icons.rounded.Print
import androidx.compose.material.icons.rounded.Search
import androidx.compose.material.icons.rounded.Share
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.ExposedDropdownMenuBox
import androidx.compose.material3.ExposedDropdownMenuDefaults
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.OutlinedButton
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.mutableStateListOf
import androidx.compose.ui.text.font.FontWeight
import com.satinalmapro.android.data.repository.StokRepository
import com.satinalmapro.android.services.ModulListePaylasHelper
import com.satinalmapro.android.services.StokTeslimFisiHelper
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.MenuAnchorType
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.OutlinedTextFieldDefaults
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
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import com.satinalmapro.android.core.model.StokHareket
import com.satinalmapro.android.core.model.StokHareketTipi
import com.satinalmapro.android.core.roles.KullaniciRolleri
import com.satinalmapro.android.core.model.StokKaydi
import com.satinalmapro.android.ui.AppViewModel
import com.satinalmapro.android.ui.components.AppCard
import com.satinalmapro.android.ui.components.AppSearchField
import com.satinalmapro.android.core.liste.ModulGrupSecenegi
import com.satinalmapro.android.core.liste.ModulListeAyarlari
import com.satinalmapro.android.core.liste.ModulTabloKolon
import com.satinalmapro.android.core.liste.paginateList
import com.satinalmapro.android.core.liste.totalPages
import com.satinalmapro.android.ui.components.ModulTabloToolbar
import com.satinalmapro.android.ui.theme.AppColors
import com.satinalmapro.android.ui.theme.AppShapes
import com.satinalmapro.android.ui.theme.AppSpacing

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun StokDurumScreen(
    viewModel: AppViewModel,
    atolyeMode: Boolean = false,
    modifier: Modifier = Modifier
) {
    var search by remember { mutableStateOf("") }
    var suggestionsExpanded by remember { mutableStateOf(false) }
    var stokAyarlar by remember { mutableStateOf(ModulListeAyarlari()) }
    var stokSayfa by remember { mutableStateOf(1) }
    val stok by viewModel.stokList.collectAsState()
    val context = LocalContext.current
    val baseList = remember(stok, atolyeMode) {
        if (atolyeMode) stok.filter { it.mevcutMiktar > 0 } else stok
    }
    val query = search.trim()
    val filtered = baseList.filter { it.matchesMalzemeSearch(query) }
    val sortedStok = if (atolyeMode) filtered else filtered.siralaStokListe(stokAyarlar)
    val stokToplamSayfa = if (atolyeMode) 1 else totalPages(sortedStok.size, stokAyarlar.pageSize)
    val stokGuncelSayfa = if (atolyeMode) 1 else stokSayfa.coerceIn(1, stokToplamSayfa)
    val gorunenStok = if (atolyeMode) filtered else paginateList(sortedStok, stokGuncelSayfa, stokAyarlar.pageSize)
    val stokKolonlar = listOf(
        ModulTabloKolon("malzeme", "Malzeme", 1.2f),
        ModulTabloKolon("depo", "Depo", 0.9f),
        ModulTabloKolon("miktar", "Miktar", 0.8f),
        ModulTabloKolon("durum", "Durum", 0.7f)
    )
    val stokGruplar = listOf(
        ModulGrupSecenegi("Depo", "depo"),
        ModulGrupSecenegi("Durum", "durum")
    )
    val gorunurKolonlar = stokKolonlar.filter { it.id !in stokAyarlar.hiddenColumnIds }
    val tamEkranMod = !atolyeMode && stokAyarlar.fullscreen
    val suggestions = remember(baseList, query) {
        if (!atolyeMode || query.isBlank()) emptyList()
        else baseList
            .map { it.malzemeAdi.trim() }
            .filter { it.isNotBlank() }
            .distinctBy { it.lowercase() }
            .filter { it.contains(query, ignoreCase = true) }
            .sortedBy { it.lowercase() }
            .take(10)
    }

    Column(
        modifier.fillMaxSize().padding(
            horizontal = AppSpacing.screenHorizontal,
            vertical = if (tamEkranMod) 8.dp else AppSpacing.screenVertical
        )
    ) {
        if (!tamEkranMod) {
        if (atolyeMode) {
            ExposedDropdownMenuBox(
                expanded = suggestionsExpanded && suggestions.isNotEmpty(),
                onExpandedChange = { suggestionsExpanded = it && suggestions.isNotEmpty() }
            ) {
                OutlinedTextField(
                    value = search,
                    onValueChange = {
                        search = it
                        suggestionsExpanded = it.isNotBlank()
                    },
                    modifier = Modifier
                        .fillMaxWidth()
                        .menuAnchor(MenuAnchorType.PrimaryEditable, enabled = true),
                    placeholder = { Text("Malzeme adı ara...") },
                    label = { Text("Malzeme Ara") },
                    leadingIcon = { Icon(Icons.Rounded.Search, contentDescription = "Ara") },
                    trailingIcon = {
                        if (suggestions.isNotEmpty()) {
                            ExposedDropdownMenuDefaults.TrailingIcon(expanded = suggestionsExpanded)
                        }
                    },
                    singleLine = true,
                    shape = AppShapes.medium,
                    colors = stokSearchColors()
                )
                ExposedDropdownMenu(
                    expanded = suggestionsExpanded && suggestions.isNotEmpty(),
                    onDismissRequest = { suggestionsExpanded = false }
                ) {
                    suggestions.forEach { option ->
                        DropdownMenuItem(
                            text = { Text(option) },
                            onClick = {
                                search = option
                                suggestionsExpanded = false
                            },
                            contentPadding = ExposedDropdownMenuDefaults.ItemContentPadding
                        )
                    }
                }
            }
        } else {
            StokSearchField(
                value = search,
                onValueChange = { search = it; stokSayfa = 1 },
                placeholder = "Malzeme veya depo ara..."
            )
        }
        }
        if (!atolyeMode) {
            ModulTabloToolbar(
                ayarlar = stokAyarlar,
                kolonlar = stokKolonlar,
                grupSecenekleri = stokGruplar,
                kayitSayisi = sortedStok.size,
                guncelSayfa = stokGuncelSayfa,
                toplamSayfa = stokToplamSayfa,
                onAyarlarDegisti = { stokAyarlar = it; stokSayfa = 1 },
                onSayfaDegisti = { stokSayfa = it }
            )
            if (!tamEkranMod && filtered.isNotEmpty()) {
            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.End) {
                IconButton(onClick = {
                    val rows = mutableListOf(listOf("Malzeme", "Depo", "Miktar", "Birim", "Durum", "Birim Maliyet", "Toplam"))
                    filtered.forEach { k ->
                        rows.add(listOf(
                            k.malzemeAdi, k.depoSaha,
                            k.mevcutMiktar.toString(), k.birim, k.durumMetin,
                            k.birimMaliyet.toString(), k.toplamDeger.toString()
                        ))
                    }
                    ModulListePaylasHelper.csvPaylas(context, "Stok Durumu", rows)
                }) {
                    Icon(Icons.Rounded.Share, contentDescription = "Stok listesini CSV paylaş")
                }
                IconButton(onClick = {
                    val basliklar = listOf("Malzeme", "Depo", "Miktar", "Durum", "Toplam")
                    val satirlar = filtered.map { k ->
                        listOf(
                            k.malzemeAdi, k.depoSaha,
                            "${formatStokMiktar(k.mevcutMiktar, k.birim)}",
                            k.durumMetin, k.toplamDeger.toString()
                        )
                    }
                    ModulListePaylasHelper.pdfTabloPaylas(context, "Stok Durumu", basliklar, satirlar)
                }) {
                    Icon(Icons.Rounded.Print, contentDescription = "Stok listesini PDF paylaş")
                }
            }
            }
        }
        Spacer(Modifier.height(8.dp))
        when {
            baseList.isEmpty() -> Text("Stok kaydı yok.", color = AppColors.TextSecondary, modifier = Modifier.padding(8.dp))
            filtered.isEmpty() -> Text("Arama sonucu bulunamadı.", color = AppColors.TextSecondary, modifier = Modifier.padding(8.dp))
            gorunurKolonlar.isEmpty() -> Text("En az bir kolon görünür olmalıdır.", color = AppColors.Danger, modifier = Modifier.padding(8.dp))
            else -> {
                StokTableHeader(kolonlar = gorunurKolonlar, dense = stokAyarlar.dense)
                LazyColumn {
                    itemsIndexed(gorunenStok, key = { index, kayit ->
                        "${kayit.malzemeAdi}-${kayit.depoSaha}-$index"
                    }) { index, kayit ->
                        StokTableRow(
                            kolonlar = gorunurKolonlar,
                            hucreler = mapOf(
                                "malzeme" to kayit.malzemeAdi,
                                "depo" to kayit.depoSaha,
                                "miktar" to formatStokMiktar(kayit.mevcutMiktar, kayit.birim),
                                "durum" to kayit.durumMetin
                            ),
                            dense = stokAyarlar.dense
                        )
                        if (index < gorunenStok.lastIndex) {
                            HorizontalDivider(color = AppColors.Border)
                        }
                    }
                }
            }
        }
    }
}

@Composable
private fun StokTableHeader(kolonlar: List<ModulTabloKolon>, dense: Boolean = false) {
    val padV = if (dense) 4.dp else 8.dp
    val textStyle = MaterialTheme.typography.labelMedium
    Row(
        Modifier
            .fillMaxWidth()
            .padding(horizontal = 4.dp, vertical = padV),
        horizontalArrangement = Arrangement.spacedBy(8.dp)
    ) {
        kolonlar.forEach { kolon ->
            Text(
                kolon.label,
                Modifier.weight(kolon.weight),
                style = textStyle,
                color = AppColors.TextSecondary,
                textAlign = if (kolon.id == "miktar") TextAlign.End else TextAlign.Start
            )
        }
    }
    HorizontalDivider(color = AppColors.Border)
}

@Composable
private fun StokTableRow(
    kolonlar: List<ModulTabloKolon>,
    hucreler: Map<String, String>,
    dense: Boolean = false
) {
    val padV = if (dense) 6.dp else 10.dp
    val textStyle = if (dense) MaterialTheme.typography.labelSmall else MaterialTheme.typography.bodyMedium
    Row(
        Modifier
            .fillMaxWidth()
            .padding(horizontal = 4.dp, vertical = padV),
        horizontalArrangement = Arrangement.spacedBy(8.dp),
        verticalAlignment = Alignment.CenterVertically
    ) {
        kolonlar.forEach { kolon ->
            Text(
                hucreler[kolon.id].orEmpty().ifBlank { "—" },
                Modifier.weight(kolon.weight),
                style = textStyle,
                color = AppColors.TextPrimary,
                textAlign = if (kolon.id == "miktar") TextAlign.End else TextAlign.Start,
                maxLines = 2,
                overflow = TextOverflow.Ellipsis
            )
        }
    }
}

private fun formatStokMiktar(miktar: Double, birim: String): String {
    val miktarMetin = if (miktar == miktar.toLong().toDouble()) {
        miktar.toLong().toString()
    } else {
        miktar.toString()
    }
    return if (birim.isBlank()) miktarMetin else "$miktarMetin $birim"
}

@Composable
fun StokHareketScreen(viewModel: AppViewModel) {
    val context = LocalContext.current
    var search by remember { mutableStateOf("") }
    val user by viewModel.user.collectAsState()
    val hareketler by viewModel.stokHareketleri.collectAsState()
    val error by viewModel.submitError.collectAsState()
    val canWrite = KullaniciRolleri.canStockWrite(user?.role)
    val exportList = hareketler.filter { it.matchesSearch(search) }
    val filtered = exportList.take(150)
    var duzenlenecek by remember { mutableStateOf<StokHareket?>(null) }
    var silinecek by remember { mutableStateOf<StokHareket?>(null) }

    duzenlenecek?.let { hareket ->
        StokHareketDuzenleDialog(
            hareket = hareket,
            onDismiss = { duzenlenecek = null },
            onKaydet = { tarih, miktar, belgeNo, islemYapan, teslimEdilen, aciklama ->
                viewModel.stokHareketGuncelle(
                    hareket.id, tarih, miktar, belgeNo, islemYapan, teslimEdilen, aciklama
                ) { duzenlenecek = null }
            }
        )
    }

    silinecek?.let { hareket ->
        AlertDialog(
            onDismissRequest = { silinecek = null },
            title = { Text("Hareket Sil") },
            text = { Text("${hareket.malzemeAdi} — ${formatStokMiktar(hareket.miktar, hareket.birim)} silinsin mi? Stok miktarı geri alınır.") },
            confirmButton = {
                TextButton(onClick = {
                    viewModel.stokHareketSil(hareket.id) { silinecek = null }
                }) { Text("Sil", color = AppColors.Danger) }
            },
            dismissButton = {
                TextButton(onClick = { silinecek = null }) { Text("İptal") }
            }
        )
    }

    Column(Modifier.fillMaxSize().padding(horizontal = AppSpacing.screenHorizontal, vertical = AppSpacing.screenVertical)) {
        Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween, verticalAlignment = Alignment.CenterVertically) {
            Text("Stok Hareketleri", style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.SemiBold, modifier = Modifier.weight(1f))
            if (exportList.isNotEmpty()) {
                Row {
                    IconButton(onClick = {
                        val rows = mutableListOf(
                            listOf("Tarih", "Tip", "Malzeme", "Miktar", "Birim", "Depo", "Belge No", "İşlem Yapan", "Teslim Edilen", "Açıklama")
                        )
                        exportList.forEach { h ->
                            rows.add(listOf(
                                h.tarih, h.hareketTipi, h.malzemeAdi,
                                h.miktar.toString(), h.birim, h.depoSaha,
                                h.belgeNo, h.islemYapan, h.teslimEdilen, h.aciklama
                            ))
                        }
                        ModulListePaylasHelper.csvPaylas(context, "Stok Hareketleri", rows)
                    }) {
                        Icon(Icons.Rounded.Share, contentDescription = "Stok hareketlerini CSV paylaş")
                    }
                    IconButton(onClick = {
                        val basliklar = listOf("Tarih", "Tip", "Malzeme", "Miktar", "Depo")
                        val satirlar = exportList.map { h ->
                            listOf(
                                h.tarih, h.hareketTipi, h.malzemeAdi,
                                formatStokMiktar(h.miktar, h.birim), h.depoSaha
                            )
                        }
                        ModulListePaylasHelper.pdfTabloPaylas(context, "Stok Hareketleri", basliklar, satirlar)
                    }) {
                        Icon(Icons.Rounded.Print, contentDescription = "Stok hareketlerini PDF paylaş")
                    }
                }
            }
        }
        StokSearchField(
            value = search,
            onValueChange = { search = it },
            placeholder = "Malzeme veya hareket tipi ara..."
        )
        Spacer(Modifier.height(8.dp))
        error?.let {
            Text(it, color = AppColors.Danger, style = MaterialTheme.typography.bodySmall, modifier = Modifier.padding(bottom = 4.dp))
        }
        when {
            hareketler.isEmpty() -> Text("Stok hareketi yok.", color = AppColors.TextSecondary, modifier = Modifier.padding(8.dp))
            filtered.isEmpty() -> Text("Arama sonucu bulunamadı.", color = AppColors.TextSecondary, modifier = Modifier.padding(8.dp))
            else -> {
                if (exportList.size > filtered.size) {
                    Text(
                        "Son ${filtered.size} hareket gösteriliyor (${exportList.size} kayıt). Paylaşım tüm sonuçları içerir.",
                        style = MaterialTheme.typography.labelSmall,
                        color = AppColors.TextSecondary,
                        modifier = Modifier.padding(bottom = 4.dp)
                    )
                }
                HareketTableHeader(canWrite = canWrite)
                LazyColumn {
                    itemsIndexed(filtered, key = { index, h ->
                        h.id.ifBlank { "hareket-$index-${h.tarih}-${h.malzemeAdi}" }
                    }) { index, h ->
                        val duzenlenebilir = canWrite && (
                            h.hareketTipi.equals(StokHareketTipi.GIRIS, true) ||
                                h.hareketTipi.equals(StokHareketTipi.CIKIS, true)
                            )
                        HareketTableRow(
                            tarih = h.tarih,
                            tip = h.hareketTipi,
                            malzeme = h.malzemeAdi,
                            miktar = formatStokMiktar(h.miktar, h.birim),
                            canEdit = duzenlenebilir,
                            onEdit = { duzenlenecek = h },
                            onDelete = { silinecek = h }
                        )
                        if (index < filtered.lastIndex) {
                            HorizontalDivider(color = AppColors.Border)
                        }
                    }
                }
            }
        }
    }
}

@Composable
private fun StokHareketDuzenleDialog(
    hareket: StokHareket,
    onDismiss: () -> Unit,
    onKaydet: (String, String, String, String, String, String) -> Unit
) {
    var tarih by remember(hareket.id) { mutableStateOf(hareket.tarih) }
    var miktar by remember(hareket.id) { mutableStateOf(hareket.miktar.toString()) }
    var belgeNo by remember(hareket.id) { mutableStateOf(hareket.belgeNo) }
    var islemYapan by remember(hareket.id) { mutableStateOf(hareket.islemYapan) }
    var teslimEdilen by remember(hareket.id) { mutableStateOf(hareket.teslimEdilen) }
    var aciklama by remember(hareket.id) { mutableStateOf(hareket.aciklama) }

    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text("${hareket.hareketTipi} Düzenle") },
        text = {
            Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                Text(hareket.malzemeAdi, fontWeight = FontWeight.SemiBold)
                Text("${hareket.depoSaha} · ${hareket.birim}", style = MaterialTheme.typography.bodySmall, color = AppColors.TextSecondary)
                OutlinedTextField(value = tarih, onValueChange = { tarih = it }, label = { Text("Tarih") }, singleLine = true, modifier = Modifier.fillMaxWidth())
                OutlinedTextField(value = miktar, onValueChange = { miktar = it }, label = { Text("Miktar") }, singleLine = true, modifier = Modifier.fillMaxWidth())
                OutlinedTextField(value = belgeNo, onValueChange = { belgeNo = it }, label = { Text("Belge No") }, singleLine = true, modifier = Modifier.fillMaxWidth())
                OutlinedTextField(value = islemYapan, onValueChange = { islemYapan = it }, label = { Text("İşlem Yapan") }, singleLine = true, modifier = Modifier.fillMaxWidth())
                OutlinedTextField(value = teslimEdilen, onValueChange = { teslimEdilen = it }, label = { Text("Teslim Edilen") }, singleLine = true, modifier = Modifier.fillMaxWidth())
                OutlinedTextField(value = aciklama, onValueChange = { aciklama = it }, label = { Text("Açıklama") }, singleLine = true, modifier = Modifier.fillMaxWidth())
            }
        },
        confirmButton = {
            TextButton(onClick = { onKaydet(tarih, miktar, belgeNo, islemYapan, teslimEdilen, aciklama) }) {
                Text("Kaydet")
            }
        },
        dismissButton = {
            TextButton(onClick = onDismiss) { Text("İptal") }
        }
    )
}

@Composable
private fun HareketTableHeader(canWrite: Boolean = false) {
    Row(
        Modifier.fillMaxWidth().padding(horizontal = 4.dp, vertical = 8.dp),
        horizontalArrangement = Arrangement.spacedBy(6.dp)
    ) {
        Text("Tarih", Modifier.weight(0.75f), style = MaterialTheme.typography.labelMedium, color = AppColors.TextSecondary)
        Text("Giriş/Çıkış", Modifier.weight(0.55f), style = MaterialTheme.typography.labelMedium, color = AppColors.TextSecondary, maxLines = 1)
        Text("Malzeme", Modifier.weight(1.25f), style = MaterialTheme.typography.labelMedium, color = AppColors.TextSecondary)
        Text("Miktar", Modifier.weight(0.7f), style = MaterialTheme.typography.labelMedium, color = AppColors.TextSecondary, textAlign = TextAlign.End)
        if (canWrite) {
            Spacer(Modifier.width(56.dp))
        }
    }
    HorizontalDivider(color = AppColors.Border)
}

@Composable
private fun HareketTableRow(
    tarih: String,
    tip: String,
    malzeme: String,
    miktar: String,
    canEdit: Boolean = false,
    onEdit: () -> Unit = {},
    onDelete: () -> Unit = {}
) {
    val tipRenk = when {
        tip.contains("Giri", true) -> AppColors.Success
        tip.contains("Çık", true) || tip.contains("Cik", true) -> AppColors.Danger
        else -> AppColors.Primary
    }
    val tipKisa = when {
        tip.contains("Giri", true) -> "Giriş"
        tip.contains("Çık", true) || tip.contains("Cik", true) -> "Çıkış"
        else -> tip
    }
    Row(
        Modifier.fillMaxWidth().padding(horizontal = 4.dp, vertical = 10.dp),
        horizontalArrangement = Arrangement.spacedBy(6.dp),
        verticalAlignment = Alignment.CenterVertically
    ) {
        Text(tarih.ifBlank { "-" }, Modifier.weight(0.75f), style = MaterialTheme.typography.bodySmall, color = AppColors.TextSecondary, maxLines = 1)
        Text(tipKisa, Modifier.weight(0.55f), style = MaterialTheme.typography.bodyMedium, color = tipRenk, maxLines = 1)
        Text(malzeme, Modifier.weight(1.25f), style = MaterialTheme.typography.bodyMedium, color = AppColors.TextPrimary, maxLines = 2, overflow = TextOverflow.Ellipsis)
        Text(miktar, Modifier.weight(0.7f), style = MaterialTheme.typography.bodyMedium, color = AppColors.TextPrimary, textAlign = TextAlign.End, maxLines = 1)
        if (canEdit) {
            Row(horizontalArrangement = Arrangement.spacedBy(0.dp)) {
                IconButton(onClick = onEdit, modifier = Modifier.size(36.dp)) {
                    Icon(Icons.Rounded.Edit, contentDescription = "Düzenle", tint = AppColors.Primary, modifier = Modifier.size(18.dp))
                }
                IconButton(onClick = onDelete, modifier = Modifier.size(36.dp)) {
                    Icon(Icons.Rounded.Delete, contentDescription = "Sil", tint = AppColors.Danger, modifier = Modifier.size(18.dp))
                }
            }
        }
    }
}

private data class StokCikisLine(var malzeme: String = "", var miktar: String = "")

private data class StokGirisLine(
    var malzeme: String = "",
    var miktar: String = "",
    var birim: String = "Adet",
    var kategori: String = "Genel",
    var birimFiyati: String = ""
)

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun StokGirisScreen(viewModel: AppViewModel) {
    val user by viewModel.user.collectAsState()
    val birimler by viewModel.malzemeBirimleri.collectAsState()
    val kategoriler by viewModel.malzemeKategorileri.collectAsState()
    val hareketler by viewModel.stokHareketleri.collectAsState()
    val lines = remember { mutableStateListOf(StokGirisLine()) }
    var belgeNo by remember { mutableStateOf("") }
    var teslimAlan by remember { mutableStateOf("") }
    val error by viewModel.submitError.collectAsState()
    val loading by viewModel.loading.collectAsState()

    LaunchedEffect(hareketler.size, user?.fullName) {
        if (belgeNo.isBlank()) belgeNo = viewModel.sonrakiGirisBelgeNo()
        if (teslimAlan.isBlank()) teslimAlan = user?.fullName.orEmpty()
    }

    Column(
        Modifier.fillMaxSize().verticalScroll(rememberScrollState()).padding(horizontal = AppSpacing.screenHorizontal, vertical = AppSpacing.screenVertical),
        verticalArrangement = Arrangement.spacedBy(8.dp)
    ) {
        Text("Stok Girişi", style = MaterialTheme.typography.titleLarge, color = AppColors.TextPrimary)
        OutlinedTextField(
            value = belgeNo,
            onValueChange = {},
            readOnly = true,
            modifier = Modifier.fillMaxWidth(),
            label = { Text("Belge No") },
            singleLine = true,
            shape = AppShapes.medium
        )
        lines.forEachIndexed { index, line ->
            StokGirisLineCard(
                index = index + 1,
                line = line,
                birimler = birimler,
                kategoriler = kategoriler,
                malzemeOnerileri = { viewModel.stokMalzemeOnerileri(it) },
                canDelete = lines.size > 1,
                onMalzemeChange = { lines[index] = lines[index].copy(malzeme = it) },
                onMiktarChange = { lines[index] = lines[index].copy(miktar = it) },
                onBirimChange = { lines[index] = lines[index].copy(birim = it) },
                onKategoriChange = { lines[index] = lines[index].copy(kategori = it) },
                onBirimFiyatiChange = { lines[index] = lines[index].copy(birimFiyati = it) },
                onDelete = { lines.removeAt(index) }
            )
        }
        OutlinedButton(onClick = { lines.add(StokGirisLine()) }, modifier = Modifier.fillMaxWidth()) {
            Icon(Icons.Rounded.Add, null, modifier = Modifier.size(18.dp))
            Spacer(Modifier.width(6.dp))
            Text("Satır Ekle")
        }
        OutlinedTextField(
            value = teslimAlan,
            onValueChange = { teslimAlan = it },
            modifier = Modifier.fillMaxWidth(),
            label = { Text("Teslim Alan") },
            singleLine = true,
            shape = AppShapes.medium
        )
        error?.let { Text(it, color = AppColors.Danger) }
        Button(
            onClick = {
                val payload = lines.mapNotNull { line ->
                    val m = line.miktar.replace(',', '.').toDoubleOrNull() ?: return@mapNotNull null
                    if (line.malzeme.isBlank() || m <= 0) return@mapNotNull null
                    StokRepository.GirisSatir(
                        malzeme = line.malzeme.trim(),
                        miktar = m,
                        birim = line.birim,
                        kategori = line.kategori,
                        birimMaliyet = line.birimFiyati.replace(',', '.').toDoubleOrNull() ?: 0.0
                    )
                }
                viewModel.stokGirisCoklu(belgeNo, teslimAlan, payload) { viewModel.navigate("stok-durum") }
            },
            enabled = !loading,
            modifier = Modifier.fillMaxWidth().height(48.dp),
            colors = ButtonDefaults.buttonColors(containerColor = AppColors.Success)
        ) { Text(if (loading) "Kaydediliyor..." else "Stok Girişi Kaydet") }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun StokCikisScreen(viewModel: AppViewModel) {
    val user by viewModel.user.collectAsState()
    val hareketler by viewModel.stokHareketleri.collectAsState()
    val lines = remember { mutableStateListOf(StokCikisLine()) }
    var belgeNo by remember { mutableStateOf("") }
    var teslimAlan by remember { mutableStateOf("") }
    var fisDialog by remember { mutableStateOf<StokTeslimFisiHelper.Fis?>(null) }
    var fisMesaj by remember { mutableStateOf<String?>(null) }
    val context = LocalContext.current
    val teslimEden = remember(user) {
        StokTeslimFisiHelper.teslimEdenMetni(user?.role, user?.fullName.orEmpty())
    }
    val error by viewModel.submitError.collectAsState()
    val loading by viewModel.loading.collectAsState()

    fun cikisPayload(): List<StokRepository.CikisSatir> =
        lines.mapNotNull { line ->
            val m = line.miktar.replace(',', '.').toDoubleOrNull() ?: return@mapNotNull null
            if (line.malzeme.isBlank() || m <= 0) return@mapNotNull null
            StokRepository.CikisSatir(line.malzeme.trim(), m)
        }

    fun fisHazirla(): StokTeslimFisiHelper.Fis? =
        viewModel.stokCikisFisiOlustur(belgeNo, teslimAlan.trim(), cikisPayload())

    val fisHazir = remember(lines.toList(), belgeNo, teslimAlan) { fisHazirla() != null }

    LaunchedEffect(hareketler.size) {
        if (belgeNo.isBlank()) belgeNo = viewModel.sonrakiCikisBelgeNo()
    }

    fisDialog?.let { fis ->
        StokCikisFisDialog(
            fis = fis,
            onDismiss = { fisDialog = null },
            onYazdir = {
                StokTeslimFisiHelper.yazdirA5(context, fis)
                fisDialog = null
            },
            onIndir = {
                val dosya = StokTeslimFisiHelper.pdfKaydet(context, fis)
                fisMesaj = "Fiş indirildi: ${dosya.name}"
                fisDialog = null
            },
            onPaylas = {
                StokTeslimFisiHelper.paylasPdf(context, fis)
                fisDialog = null
            }
        )
    }

    Column(
        Modifier.fillMaxSize().verticalScroll(rememberScrollState()).padding(horizontal = AppSpacing.screenHorizontal, vertical = AppSpacing.screenVertical),
        verticalArrangement = Arrangement.spacedBy(8.dp)
    ) {
        Text("Stok Çıkışı", style = MaterialTheme.typography.titleLarge, color = AppColors.TextPrimary)
        OutlinedTextField(
            value = belgeNo,
            onValueChange = {},
            readOnly = true,
            modifier = Modifier.fillMaxWidth(),
            label = { Text("Belge No") },
            singleLine = true,
            shape = AppShapes.medium
        )
        OutlinedTextField(
            value = teslimEden,
            onValueChange = {},
            readOnly = true,
            modifier = Modifier.fillMaxWidth(),
            label = { Text("Teslim Eden") },
            singleLine = true,
            shape = AppShapes.medium
        )
        StokLineHeaderCikis()
        HorizontalDivider(color = AppColors.Border)
        lines.forEachIndexed { index, line ->
            StokCikisLineRow(
                line = line,
                malzemeOnerileri = { viewModel.stokMalzemeOnerileri(it, sadeceMevcut = true) },
                canDelete = lines.size > 1,
                onMalzemeChange = { lines[index] = lines[index].copy(malzeme = it) },
                onMiktarChange = { lines[index] = lines[index].copy(miktar = it) },
                onDelete = { lines.removeAt(index) }
            )
            HorizontalDivider(color = AppColors.Border)
        }
        OutlinedButton(onClick = { lines.add(StokCikisLine()) }, modifier = Modifier.fillMaxWidth()) {
            Icon(Icons.Rounded.Add, null, modifier = Modifier.size(18.dp))
            Spacer(Modifier.width(6.dp))
            Text("Satır Ekle")
        }
        OutlinedTextField(
            value = teslimAlan,
            onValueChange = { teslimAlan = it },
            modifier = Modifier.fillMaxWidth(),
            label = { Text("Teslim Alan") },
            singleLine = true,
            shape = AppShapes.medium
        )
        error?.let { Text(it, color = AppColors.Danger) }
        fisMesaj?.let { Text(it, color = AppColors.Primary, style = MaterialTheme.typography.bodySmall) }
        Button(
            onClick = {
                val payload = cikisPayload()
                viewModel.stokCikisCoklu(belgeNo, teslimAlan.trim(), payload) {
                    fisHazirla()?.let { fisDialog = it } ?: viewModel.navigate("stok-durum")
                }
            },
            enabled = !loading && fisHazir,
            modifier = Modifier.fillMaxWidth().height(48.dp),
            colors = ButtonDefaults.buttonColors(containerColor = AppColors.Danger)
        ) { Text(if (loading) "Kaydediliyor..." else "Stok Çıkışı Kaydet") }
        OutlinedButton(
            onClick = { fisHazirla()?.let { fisDialog = it } },
            enabled = fisHazir && !loading,
            modifier = Modifier.fillMaxWidth().height(48.dp)
        ) {
            Icon(Icons.Rounded.Print, null, modifier = Modifier.size(18.dp))
            Spacer(Modifier.width(8.dp))
            Text("Fiş Yazdır / İndir / Paylaş")
        }
    }
}

@Composable
private fun StokCikisFisDialog(
    fis: StokTeslimFisiHelper.Fis,
    onDismiss: () -> Unit,
    onYazdir: () -> Unit,
    onIndir: () -> Unit,
    onPaylas: () -> Unit
) {
    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text("Depo Çıkış Fişi") },
        text = {
            Column(verticalArrangement = Arrangement.spacedBy(6.dp)) {
                Text("Belge: ${fis.belgeNo}", style = MaterialTheme.typography.bodyMedium)
                Text("Teslim alan: ${fis.teslimAlan}", style = MaterialTheme.typography.bodySmall, color = AppColors.TextSecondary)
                Text("${fis.satirlar.size} kalem — A5 boyut", style = MaterialTheme.typography.bodySmall, color = AppColors.TextSecondary)
            }
        },
        confirmButton = {
            TextButton(onClick = onYazdir) {
                Icon(Icons.Rounded.Print, null, modifier = Modifier.size(16.dp))
                Spacer(Modifier.width(4.dp))
                Text("Yazdır")
            }
        },
        dismissButton = {
            Row(horizontalArrangement = Arrangement.spacedBy(4.dp)) {
                TextButton(onClick = onIndir) { Text("İndir") }
                TextButton(onClick = onPaylas) {
                    Icon(Icons.Rounded.Share, null, modifier = Modifier.size(16.dp))
                    Spacer(Modifier.width(4.dp))
                    Text("Paylaş")
                }
                TextButton(onClick = onDismiss) { Text("Kapat") }
            }
        }
    )
}

@Composable
private fun StokLineHeaderCikis() {
    Row(Modifier.fillMaxWidth().padding(horizontal = 4.dp, vertical = 6.dp), horizontalArrangement = Arrangement.spacedBy(6.dp)) {
        Text("Malzeme", Modifier.weight(1.4f), style = MaterialTheme.typography.labelMedium, color = AppColors.TextSecondary)
        Text("Miktar", Modifier.weight(0.7f), style = MaterialTheme.typography.labelMedium, color = AppColors.TextSecondary)
        Spacer(Modifier.width(36.dp))
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun StokCikisLineRow(
    line: StokCikisLine,
    malzemeOnerileri: (String) -> List<String>,
    canDelete: Boolean,
    onMalzemeChange: (String) -> Unit,
    onMiktarChange: (String) -> Unit,
    onDelete: () -> Unit
) {
    Row(Modifier.fillMaxWidth().padding(vertical = 4.dp), horizontalArrangement = Arrangement.spacedBy(6.dp), verticalAlignment = Alignment.CenterVertically) {
        StokMalzemeField(
            value = line.malzeme,
            onValueChange = onMalzemeChange,
            suggestions = malzemeOnerileri(line.malzeme),
            modifier = Modifier.weight(1.4f),
            label = "Malzeme"
        )
        OutlinedTextField(
            value = line.miktar,
            onValueChange = onMiktarChange,
            modifier = Modifier.weight(0.7f),
            label = { Text("Miktar") },
            placeholder = { Text("0") },
            singleLine = true,
            shape = AppShapes.medium
        )
        IconButton(onClick = onDelete, enabled = canDelete, modifier = Modifier.size(36.dp)) {
            Icon(Icons.Rounded.Delete, "Sil", tint = if (canDelete) AppColors.Danger else AppColors.TextSecondary)
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun StokGirisLineCard(
    index: Int,
    line: StokGirisLine,
    birimler: List<String>,
    kategoriler: List<String>,
    malzemeOnerileri: (String) -> List<String>,
    canDelete: Boolean,
    onMalzemeChange: (String) -> Unit,
    onMiktarChange: (String) -> Unit,
    onBirimChange: (String) -> Unit,
    onKategoriChange: (String) -> Unit,
    onBirimFiyatiChange: (String) -> Unit,
    onDelete: () -> Unit
) {
    AppCard {
        Column(
            Modifier.fillMaxWidth().padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            Row(
                Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text(
                    "Malzeme $index",
                    style = MaterialTheme.typography.titleSmall,
                    fontWeight = FontWeight.SemiBold,
                    color = AppColors.TextPrimary
                )
                IconButton(onClick = onDelete, enabled = canDelete, modifier = Modifier.size(36.dp)) {
                    Icon(
                        Icons.Rounded.Delete,
                        "Satırı sil",
                        tint = if (canDelete) AppColors.Danger else AppColors.TextSecondary
                    )
                }
            }
            StokMalzemeField(
                value = line.malzeme,
                onValueChange = onMalzemeChange,
                suggestions = malzemeOnerileri(line.malzeme),
                modifier = Modifier.fillMaxWidth(),
                label = "Malzeme Adı"
            )
            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(12.dp)) {
                OutlinedTextField(
                    value = line.miktar,
                    onValueChange = onMiktarChange,
                    modifier = Modifier.weight(1f),
                    label = { Text("Miktar") },
                    placeholder = { Text("0") },
                    singleLine = true,
                    shape = AppShapes.medium
                )
                StokDropdownField(
                    value = line.birim,
                    options = birimler,
                    modifier = Modifier.weight(1f),
                    label = "Birim",
                    onSelect = onBirimChange
                )
            }
            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(12.dp)) {
                OutlinedTextField(
                    value = line.birimFiyati,
                    onValueChange = onBirimFiyatiChange,
                    modifier = Modifier.weight(1f),
                    label = { Text("Birim Fiyatı") },
                    placeholder = { Text("0,00") },
                    singleLine = true,
                    shape = AppShapes.medium
                )
                StokDropdownField(
                    value = line.kategori,
                    options = kategoriler,
                    modifier = Modifier.weight(1f),
                    label = "Kategori",
                    onSelect = onKategoriChange
                )
            }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun StokMalzemeField(
    value: String,
    onValueChange: (String) -> Unit,
    suggestions: List<String>,
    modifier: Modifier = Modifier,
    label: String = "Malzeme"
) {
    var expanded by remember { mutableStateOf(false) }
    ExposedDropdownMenuBox(
        expanded = expanded && suggestions.isNotEmpty(),
        onExpandedChange = { expanded = it && suggestions.isNotEmpty() },
        modifier = modifier
    ) {
        OutlinedTextField(
            value = value,
            onValueChange = {
                onValueChange(it)
                expanded = true
            },
            modifier = Modifier
                .fillMaxWidth()
                .menuAnchor(MenuAnchorType.PrimaryEditable, enabled = true),
            label = { Text(label) },
            placeholder = { Text("Malzeme seçin veya yazın") },
            singleLine = true,
            shape = AppShapes.medium,
            trailingIcon = {
                if (suggestions.isNotEmpty()) {
                    ExposedDropdownMenuDefaults.TrailingIcon(expanded = expanded)
                }
            }
        )
        ExposedDropdownMenu(
            expanded = expanded && suggestions.isNotEmpty(),
            onDismissRequest = { expanded = false }
        ) {
            suggestions.forEach { option ->
                DropdownMenuItem(
                    text = { Text(option) },
                    onClick = {
                        onValueChange(option)
                        expanded = false
                    },
                    contentPadding = ExposedDropdownMenuDefaults.ItemContentPadding
                )
            }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun StokDropdownField(
    value: String,
    options: List<String>,
    modifier: Modifier,
    label: String,
    onSelect: (String) -> Unit
) {
    var expanded by remember { mutableStateOf(false) }
    Box(modifier) {
        OutlinedTextField(
            value = value,
            onValueChange = {},
            readOnly = true,
            label = { Text(label) },
            singleLine = true,
            modifier = Modifier.fillMaxWidth().clickable { expanded = true },
            shape = AppShapes.medium,
            trailingIcon = { ExposedDropdownMenuDefaults.TrailingIcon(expanded = expanded) }
        )
        DropdownMenu(expanded = expanded, onDismissRequest = { expanded = false }) {
            options.forEach { option ->
                DropdownMenuItem(
                    text = { Text(option) },
                    onClick = {
                        onSelect(option)
                        expanded = false
                    }
                )
            }
        }
    }
}

@Composable
private fun StokSearchField(value: String, onValueChange: (String) -> Unit, placeholder: String) {
    AppSearchField(value = value, onValueChange = onValueChange, placeholder = placeholder)
}

@Composable
private fun stokSearchColors() = OutlinedTextFieldDefaults.colors(
    focusedBorderColor = AppColors.Primary,
    unfocusedBorderColor = AppColors.Border,
    focusedContainerColor = AppColors.Surface,
    unfocusedContainerColor = AppColors.Surface
)

private fun StokKaydi.matchesMalzemeSearch(query: String): Boolean {
    if (query.isBlank()) return true
    return malzemeAdi.contains(query, ignoreCase = true)
}

private fun StokKaydi.matchesSearch(query: String): Boolean {
    if (query.isBlank()) return true
    val q = query.trim()
    return malzemeAdi.contains(q, true)
        || depoSaha.contains(q, true)
        || kategori.contains(q, true)
        || birim.contains(q, true)
        || formatStokMiktar(mevcutMiktar, birim).contains(q, true)
}

private fun StokHareket.matchesSearch(query: String): Boolean {
    if (query.isBlank()) return true
    val q = query.trim()
    return malzemeAdi.contains(q, true)
        || depoSaha.contains(q, true)
        || hareketTipi.contains(q, true)
        || belgeNo.contains(q, true)
        || tarih.contains(q, true)
        || islemYapan.contains(q, true)
        || teslimEdilen.contains(q, true)
        || aciklama.contains(q, true)
        || kategori.contains(q, true)
        || birim.contains(q, true)
        || miktar.toString().contains(q, true)
}

@Composable
private fun stokField(label: String, value: String, onChange: (String) -> Unit) {
    OutlinedTextField(value, onChange, Modifier.fillMaxWidth(), label = { Text(label) }, singleLine = true, shape = AppShapes.medium)
}

@Composable
fun StokSayimScreen(viewModel: AppViewModel) {
    var malzeme by remember { mutableStateOf("") }
    var depo by remember { mutableStateOf("") }
    var sayim by remember { mutableStateOf("") }
    val error by viewModel.submitError.collectAsState()
    Column(
        Modifier.fillMaxSize().verticalScroll(rememberScrollState()).padding(AppSpacing.screenHorizontal),
        verticalArrangement = Arrangement.spacedBy(12.dp)
    ) {
        Text("Stok Sayım", style = MaterialTheme.typography.headlineSmall)
        stokField("Malzeme", malzeme) { malzeme = it }
        stokField("Depo / Şantiye", depo) { depo = it }
        stokField("Sayım Miktarı", sayim) { sayim = it }
        error?.let { Text(it, color = AppColors.Danger) }
        Button(
            onClick = { viewModel.stokSayim(malzeme, depo, sayim) { viewModel.navigate("stok-durum") } },
            modifier = Modifier.fillMaxWidth().height(52.dp),
            colors = ButtonDefaults.buttonColors(containerColor = AppColors.Primary)
        ) { Text("Sayımı Kaydet") }
    }
}

private fun List<StokKaydi>.siralaStokListe(ayarlar: ModulListeAyarlari): List<StokKaydi> {
    val gf = ayarlar.groupField ?: return this
    return sortedWith(
        compareBy<StokKaydi> {
            when (gf) {
                "depo" -> it.depoSaha.lowercase()
                "durum" -> it.durumMetin.lowercase()
                else -> ""
            }
        }.thenBy { it.malzemeAdi.lowercase() }
    )
}

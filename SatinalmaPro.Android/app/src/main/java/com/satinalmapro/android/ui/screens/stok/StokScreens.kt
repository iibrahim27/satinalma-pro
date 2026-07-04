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
import androidx.compose.material.icons.rounded.Search
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
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.OutlinedTextFieldDefaults
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import com.satinalmapro.android.core.model.StokHareket
import com.satinalmapro.android.core.model.StokKaydi
import com.satinalmapro.android.ui.AppViewModel
import com.satinalmapro.android.ui.theme.AppColors
import com.satinalmapro.android.ui.theme.AppShapes

@Composable
fun StokDurumScreen(viewModel: AppViewModel) {
    var search by remember { mutableStateOf("") }
    val stok by viewModel.stokList.collectAsState()
    val filtered = stok.filter { it.matchesSearch(search) }

    Column(Modifier.fillMaxSize().padding(horizontal = 12.dp, vertical = 8.dp)) {
        StokSearchField(
            value = search,
            onValueChange = { search = it },
            placeholder = "Malzeme veya depo ara..."
        )
        Spacer(Modifier.height(8.dp))
        when {
            stok.isEmpty() -> Text("Stok kaydı yok.", color = AppColors.TextSecondary, modifier = Modifier.padding(8.dp))
            filtered.isEmpty() -> Text("Arama sonucu bulunamadı.", color = AppColors.TextSecondary, modifier = Modifier.padding(8.dp))
            else -> {
                StokTableHeader()
                LazyColumn {
                    itemsIndexed(filtered, key = { index, kayit ->
                        "${kayit.malzemeAdi}-${kayit.depoSaha}-$index"
                    }) { index, kayit ->
                        StokTableRow(
                            malzeme = kayit.malzemeAdi,
                            depo = kayit.depoSaha,
                            miktar = formatStokMiktar(kayit.mevcutMiktar, kayit.birim)
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
private fun StokTableHeader() {
    Row(
        Modifier
            .fillMaxWidth()
            .padding(horizontal = 4.dp, vertical = 8.dp),
        horizontalArrangement = Arrangement.spacedBy(8.dp)
    ) {
        Text(
            "Malzeme",
            Modifier.weight(1.3f),
            style = MaterialTheme.typography.labelMedium,
            color = AppColors.TextSecondary
        )
        Text(
            "Depo",
            Modifier.weight(0.9f),
            style = MaterialTheme.typography.labelMedium,
            color = AppColors.TextSecondary
        )
        Text(
            "Miktar",
            Modifier.weight(0.6f),
            style = MaterialTheme.typography.labelMedium,
            color = AppColors.TextSecondary,
            textAlign = TextAlign.End
        )
    }
    HorizontalDivider(color = AppColors.Border)
}

@Composable
private fun StokTableRow(malzeme: String, depo: String, miktar: String) {
    Row(
        Modifier
            .fillMaxWidth()
            .padding(horizontal = 4.dp, vertical = 10.dp),
        horizontalArrangement = Arrangement.spacedBy(8.dp),
        verticalAlignment = Alignment.CenterVertically
    ) {
        Text(
            malzeme,
            Modifier.weight(1.3f),
            style = MaterialTheme.typography.bodyMedium,
            color = AppColors.TextPrimary,
            maxLines = 2,
            overflow = TextOverflow.Ellipsis
        )
        Text(
            depo,
            Modifier.weight(0.9f),
            style = MaterialTheme.typography.bodyMedium,
            color = AppColors.TextPrimary,
            maxLines = 1,
            overflow = TextOverflow.Ellipsis
        )
        Text(
            miktar,
            Modifier.weight(0.6f),
            style = MaterialTheme.typography.bodyMedium,
            color = AppColors.TextPrimary,
            textAlign = TextAlign.End,
            maxLines = 1
        )
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
    var search by remember { mutableStateOf("") }
    val hareketler by viewModel.stokHareketleri.collectAsState()
    val filtered = hareketler.filter { it.matchesSearch(search) }.take(150)

    Column(Modifier.fillMaxSize().padding(horizontal = 12.dp, vertical = 8.dp)) {
        StokSearchField(
            value = search,
            onValueChange = { search = it },
            placeholder = "Malzeme veya hareket tipi ara..."
        )
        Spacer(Modifier.height(8.dp))
        when {
            hareketler.isEmpty() -> Text("Stok hareketi yok.", color = AppColors.TextSecondary, modifier = Modifier.padding(8.dp))
            filtered.isEmpty() -> Text("Arama sonucu bulunamadı.", color = AppColors.TextSecondary, modifier = Modifier.padding(8.dp))
            else -> {
                HareketTableHeader()
                LazyColumn {
                    itemsIndexed(filtered, key = { index, h ->
                        h.id.ifBlank { "hareket-$index-${h.tarih}-${h.malzemeAdi}" }
                    }) { index, h ->
                        HareketTableRow(
                            tip = h.hareketTipi,
                            malzeme = h.malzemeAdi,
                            miktar = formatStokMiktar(h.miktar, h.birim)
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
private fun HareketTableHeader() {
    Row(
        Modifier.fillMaxWidth().padding(horizontal = 4.dp, vertical = 8.dp),
        horizontalArrangement = Arrangement.spacedBy(8.dp)
    ) {
        Text("Giriş/Çıkış", Modifier.weight(0.8f), style = MaterialTheme.typography.labelMedium, color = AppColors.TextSecondary)
        Text("Malzeme", Modifier.weight(1.4f), style = MaterialTheme.typography.labelMedium, color = AppColors.TextSecondary)
        Text("Miktar", Modifier.weight(0.7f), style = MaterialTheme.typography.labelMedium, color = AppColors.TextSecondary, textAlign = TextAlign.End)
    }
    HorizontalDivider(color = AppColors.Border)
}

@Composable
private fun HareketTableRow(tip: String, malzeme: String, miktar: String) {
    val tipRenk = when {
        tip.contains("Giri", true) -> AppColors.Success
        tip.contains("Çık", true) || tip.contains("Cik", true) -> AppColors.Danger
        else -> AppColors.Primary
    }
    Row(
        Modifier.fillMaxWidth().padding(horizontal = 4.dp, vertical = 10.dp),
        horizontalArrangement = Arrangement.spacedBy(8.dp),
        verticalAlignment = Alignment.CenterVertically
    ) {
        Text(tip, Modifier.weight(0.8f), style = MaterialTheme.typography.bodyMedium, color = tipRenk, maxLines = 1)
        Text(malzeme, Modifier.weight(1.4f), style = MaterialTheme.typography.bodyMedium, color = AppColors.TextPrimary, maxLines = 2, overflow = TextOverflow.Ellipsis)
        Text(miktar, Modifier.weight(0.7f), style = MaterialTheme.typography.bodyMedium, color = AppColors.TextPrimary, textAlign = TextAlign.End, maxLines = 1)
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
        Modifier.fillMaxSize().verticalScroll(rememberScrollState()).padding(horizontal = 16.dp, vertical = 8.dp),
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
        StokLineHeaderGiris()
        HorizontalDivider(color = AppColors.Border)
        lines.forEachIndexed { index, line ->
            StokGirisLineRow(
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
            HorizontalDivider(color = AppColors.Border)
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
    val hareketler by viewModel.stokHareketleri.collectAsState()
    val lines = remember { mutableStateListOf(StokCikisLine()) }
    var belgeNo by remember { mutableStateOf("") }
    var teslimAlan by remember { mutableStateOf("") }
    val error by viewModel.submitError.collectAsState()
    val loading by viewModel.loading.collectAsState()

    LaunchedEffect(hareketler.size) {
        if (belgeNo.isBlank()) belgeNo = viewModel.sonrakiCikisBelgeNo()
    }

    Column(
        Modifier.fillMaxSize().verticalScroll(rememberScrollState()).padding(horizontal = 16.dp, vertical = 8.dp),
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
        Button(
            onClick = {
                val payload = lines.mapNotNull { line ->
                    val m = line.miktar.replace(',', '.').toDoubleOrNull() ?: return@mapNotNull null
                    if (line.malzeme.isBlank() || m <= 0) return@mapNotNull null
                    StokRepository.CikisSatir(line.malzeme.trim(), m)
                }
                viewModel.stokCikisCoklu(belgeNo, teslimAlan.trim(), payload) { viewModel.navigate("stok-durum") }
            },
            enabled = !loading,
            modifier = Modifier.fillMaxWidth().height(48.dp),
            colors = ButtonDefaults.buttonColors(containerColor = AppColors.Danger)
        ) { Text(if (loading) "Kaydediliyor..." else "Stok Çıkışı Kaydet") }
    }
}

@Composable
private fun StokLineHeaderGiris() {
    Row(Modifier.fillMaxWidth().padding(horizontal = 4.dp, vertical = 6.dp), horizontalArrangement = Arrangement.spacedBy(6.dp)) {
        Text("Malzeme", Modifier.weight(1.2f), style = MaterialTheme.typography.labelMedium, color = AppColors.TextSecondary)
        Text("Miktar", Modifier.weight(0.6f), style = MaterialTheme.typography.labelMedium, color = AppColors.TextSecondary)
        Text("Birim", Modifier.weight(0.6f), style = MaterialTheme.typography.labelMedium, color = AppColors.TextSecondary)
        Text("Fiyat", Modifier.weight(0.6f), style = MaterialTheme.typography.labelMedium, color = AppColors.TextSecondary)
        Text("Kat.", Modifier.weight(0.6f), style = MaterialTheme.typography.labelMedium, color = AppColors.TextSecondary)
        Spacer(Modifier.width(36.dp))
    }
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
            modifier = Modifier.weight(1.4f)
        )
        OutlinedTextField(
            value = line.miktar,
            onValueChange = onMiktarChange,
            modifier = Modifier.weight(0.7f),
            placeholder = { Text("0") },
            singleLine = true,
            textStyle = MaterialTheme.typography.bodySmall,
            shape = AppShapes.small
        )
        IconButton(onClick = onDelete, enabled = canDelete, modifier = Modifier.size(36.dp)) {
            Icon(Icons.Rounded.Delete, "Sil", tint = if (canDelete) AppColors.Danger else AppColors.TextSecondary)
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun StokGirisLineRow(
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
    Row(Modifier.fillMaxWidth().padding(vertical = 4.dp), horizontalArrangement = Arrangement.spacedBy(6.dp), verticalAlignment = Alignment.CenterVertically) {
        StokMalzemeField(
            value = line.malzeme,
            onValueChange = onMalzemeChange,
            suggestions = malzemeOnerileri(line.malzeme),
            modifier = Modifier.weight(1.2f)
        )
        OutlinedTextField(
            value = line.miktar,
            onValueChange = onMiktarChange,
            modifier = Modifier.weight(0.6f),
            placeholder = { Text("0") },
            singleLine = true,
            textStyle = MaterialTheme.typography.bodySmall,
            shape = AppShapes.small
        )
        StokDropdownField(line.birim, birimler, Modifier.weight(0.6f), onBirimChange)
        OutlinedTextField(
            value = line.birimFiyati,
            onValueChange = onBirimFiyatiChange,
            modifier = Modifier.weight(0.6f),
            placeholder = { Text("TL") },
            singleLine = true,
            textStyle = MaterialTheme.typography.bodySmall,
            shape = AppShapes.small
        )
        StokDropdownField(line.kategori, kategoriler, Modifier.weight(0.6f), onKategoriChange)
        IconButton(onClick = onDelete, enabled = canDelete, modifier = Modifier.size(36.dp)) {
            Icon(Icons.Rounded.Delete, "Sil", tint = if (canDelete) AppColors.Danger else AppColors.TextSecondary)
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun StokMalzemeField(
    value: String,
    onValueChange: (String) -> Unit,
    suggestions: List<String>,
    modifier: Modifier = Modifier
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
                .menuAnchor(),
            placeholder = { Text("Malzeme") },
            singleLine = true,
            textStyle = MaterialTheme.typography.bodySmall,
            shape = AppShapes.small,
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
private fun StokDropdownField(value: String, options: List<String>, modifier: Modifier, onSelect: (String) -> Unit) {
    var expanded by remember { mutableStateOf(false) }
    Box(modifier) {
        OutlinedTextField(
            value = value,
            onValueChange = {},
            readOnly = true,
            singleLine = true,
            textStyle = MaterialTheme.typography.bodySmall,
            modifier = Modifier.fillMaxWidth().clickable { expanded = true },
            shape = AppShapes.small
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
    OutlinedTextField(
        value = value,
        onValueChange = onValueChange,
        modifier = Modifier.fillMaxWidth(),
        placeholder = { Text(placeholder) },
        leadingIcon = { Icon(Icons.Rounded.Search, contentDescription = "Ara") },
        singleLine = true,
        shape = AppShapes.medium,
        colors = OutlinedTextFieldDefaults.colors(
            focusedBorderColor = AppColors.Primary,
            unfocusedBorderColor = AppColors.Border,
            focusedContainerColor = AppColors.Surface,
            unfocusedContainerColor = AppColors.Surface
        )
    )
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
        Modifier.fillMaxSize().verticalScroll(rememberScrollState()).padding(20.dp),
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

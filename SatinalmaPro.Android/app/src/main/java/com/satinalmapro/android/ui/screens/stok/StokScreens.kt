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
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.itemsIndexed
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.rounded.Search
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
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
import com.satinalmapro.android.ui.components.AppCard
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
    val filtered = hareketler.filter { it.matchesSearch(search) }.take(100)

    Column(Modifier.fillMaxSize().padding(horizontal = 16.dp, vertical = 8.dp)) {
        StokSearchField(
            value = search,
            onValueChange = { search = it },
            placeholder = "Malzeme, depo, belge veya hareket ara..."
        )
        Spacer(Modifier.height(8.dp))
        when {
            hareketler.isEmpty() -> Text("Stok hareketi yok.", color = AppColors.TextSecondary)
            filtered.isEmpty() -> Text("Arama sonucu bulunamadı.", color = AppColors.TextSecondary)
            else -> LazyColumn(verticalArrangement = Arrangement.spacedBy(10.dp)) {
                itemsIndexed(filtered, key = { index, h ->
                    h.id.ifBlank { "hareket-$index-${h.tarih}-${h.malzemeAdi}" }
                }) { _, h ->
                    AppCard {
                        Column(Modifier.padding(16.dp)) {
                            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
                                Text(h.hareketTipi, style = MaterialTheme.typography.titleMedium)
                                Text(h.tarih, color = AppColors.TextSecondary)
                            }
                            Text("${h.malzemeAdi} · ${h.miktar} ${h.birim}", color = AppColors.TextSecondary)
                            Text("${h.depoSaha} · ${h.belgeNo}", style = MaterialTheme.typography.labelMedium, color = AppColors.TextSecondary)
                        }
                    }
                }
            }
        }
    }
}

@Composable
fun StokGirisScreen(viewModel: AppViewModel) {
    StokFormScreen(viewModel, "Stok Girişi") { malzeme, miktar, birim, kategori, depo, maliyet, belge, teslim ->
        viewModel.stokGiris(malzeme, miktar, birim, kategori, depo, maliyet, belge, teslim) {
            viewModel.navigate("stok-durum")
        }
    }
}

@Composable
fun StokCikisScreen(viewModel: AppViewModel) {
    var malzeme by remember { mutableStateOf("") }
    var miktar by remember { mutableStateOf("") }
    var depo by remember { mutableStateOf("") }
    var belge by remember { mutableStateOf("") }
    var teslim by remember { mutableStateOf("") }
    val error by viewModel.submitError.collectAsState()
    Column(Modifier.fillMaxSize().verticalScroll(rememberScrollState()).padding(20.dp), verticalArrangement = Arrangement.spacedBy(12.dp)) {
        Text("Stok Çıkışı", style = MaterialTheme.typography.headlineSmall)
        stokField("Malzeme", malzeme) { malzeme = it }
        stokField("Miktar", miktar) { miktar = it }
        stokField("Depo / Şantiye", depo) { depo = it }
        stokField("Belge No", belge) { belge = it }
        stokField("Teslim Alan", teslim) { teslim = it }
        error?.let { Text(it, color = AppColors.Danger) }
        Button(
            onClick = { viewModel.stokCikis(malzeme, miktar, depo, belge, teslim) { viewModel.navigate("stok-durum") } },
            modifier = Modifier.fillMaxWidth().height(52.dp),
            colors = ButtonDefaults.buttonColors(containerColor = AppColors.Danger)
        ) { Text("Stok Çıkışı Kaydet") }
    }
}

@Composable
private fun StokFormScreen(viewModel: AppViewModel, title: String, onSubmit: (String, String, String, String, String, String, String, String) -> Unit) {
    var malzeme by remember { mutableStateOf("") }
    var miktar by remember { mutableStateOf("") }
    var birim by remember { mutableStateOf("Adet") }
    var kategori by remember { mutableStateOf("Genel") }
    var depo by remember { mutableStateOf("") }
    var maliyet by remember { mutableStateOf("") }
    var belge by remember { mutableStateOf("") }
    var teslim by remember { mutableStateOf("") }
    val error by viewModel.submitError.collectAsState()
    Column(Modifier.fillMaxSize().verticalScroll(rememberScrollState()).padding(20.dp), verticalArrangement = Arrangement.spacedBy(12.dp)) {
        Text(title, style = MaterialTheme.typography.headlineSmall)
        stokField("Malzeme", malzeme) { malzeme = it }
        stokField("Miktar", miktar) { miktar = it }
        stokField("Birim", birim) { birim = it }
        stokField("Kategori", kategori) { kategori = it }
        stokField("Depo / Şantiye", depo) { depo = it }
        stokField("Birim Maliyet (TL)", maliyet) { maliyet = it }
        stokField("Belge No", belge) { belge = it }
        stokField("Teslim Alan", teslim) { teslim = it }
        error?.let { Text(it, color = AppColors.Danger) }
        Button(
            onClick = { onSubmit(malzeme, miktar, birim, kategori, depo, maliyet, belge, teslim) },
            modifier = Modifier.fillMaxWidth().height(52.dp),
            colors = ButtonDefaults.buttonColors(containerColor = AppColors.Success)
        ) { Text("Kaydet") }
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

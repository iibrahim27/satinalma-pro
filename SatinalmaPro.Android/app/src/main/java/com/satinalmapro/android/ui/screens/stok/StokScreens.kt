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
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.satinalmapro.android.ui.AppViewModel
import com.satinalmapro.android.ui.components.AppCard
import com.satinalmapro.android.ui.components.DetailRow
import com.satinalmapro.android.ui.components.StatusBadge
import com.satinalmapro.android.ui.theme.AppColors
import com.satinalmapro.android.ui.theme.AppShapes

@Composable
fun StokDurumScreen(viewModel: AppViewModel) {
    val stok by viewModel.stokList.collectAsState()
    LazyColumn(Modifier.fillMaxSize().padding(20.dp), verticalArrangement = Arrangement.spacedBy(10.dp)) {
        if (stok.isEmpty()) item { Text("Stok kaydı yok.", color = AppColors.TextSecondary) }
        itemsIndexed(stok, key = { index, kayit ->
            "${kayit.malzemeAdi}-${kayit.depoSaha}-$index"
        }) { _, kayit ->
            AppCard {
                Column(Modifier.padding(16.dp)) {
                    DetailRow("Malzeme", kayit.malzemeAdi)
                    DetailRow("Depo", kayit.depoSaha)
                    DetailRow("Miktar", "${kayit.mevcutMiktar} ${kayit.birim}")
                    StatusBadge(kayit.durumMetin, AppColors.PrimaryContainer, AppColors.Primary)
                }
            }
        }
    }
}

@Composable
fun StokHareketScreen(viewModel: AppViewModel) {
    val hareketler by viewModel.stokHareketleri.collectAsState()
    LazyColumn(Modifier.fillMaxSize().padding(20.dp), verticalArrangement = Arrangement.spacedBy(10.dp)) {
        if (hareketler.isEmpty()) {
            item { Text("Stok hareketi yok.", color = AppColors.TextSecondary) }
        }
        itemsIndexed(hareketler.take(100), key = { index, h ->
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

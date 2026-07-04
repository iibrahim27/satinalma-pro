package com.satinalmapro.android.ui.screens.teklif

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateMapOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.satinalmapro.android.ui.AppViewModel
import com.satinalmapro.android.ui.theme.AppColors
import com.satinalmapro.android.ui.theme.AppShapes

@Composable
fun TeklifGirisScreen(viewModel: AppViewModel, talepId: String) {
    val talep by viewModel.talepById(talepId).collectAsState(initial = null)
    val error by viewModel.submitError.collectAsState()
    val loading by viewModel.loading.collectAsState()
    var firma by remember { mutableStateOf("") }
    var marka by remember { mutableStateOf("") }
    var vade by remember { mutableStateOf("30") }
    var teslim by remember { mutableStateOf("") }
    var odeme by remember { mutableStateOf("") }
    val fiyatlar = remember { mutableStateMapOf<String, String>() }

    if (talep == null) {
        Column(Modifier.fillMaxSize().padding(24.dp)) { Text("Talep bulunamadı.", color = AppColors.TextSecondary) }
        return
    }

    Column(
        modifier = Modifier.fillMaxSize().verticalScroll(rememberScrollState()).padding(20.dp),
        verticalArrangement = Arrangement.spacedBy(12.dp)
    ) {
        Text("Teklif Girişi · ${talep!!.talepNo}", style = MaterialTheme.typography.headlineSmall, color = AppColors.TextPrimary)
        Text(talep!!.malzemeOzeti, color = AppColors.TextSecondary)
        field("Firma Adı", firma) { firma = it }
        field("Marka", marka) { marka = it }
        field("Vade (gün)", vade) { vade = it }
        field("Teslim Süresi", teslim) { teslim = it }
        field("Ödeme Şekli", odeme) { odeme = it }
        Text("Kalem Fiyatları (TL)", style = MaterialTheme.typography.titleMedium)
        talep!!.kalemler.forEach { kalem ->
            OutlinedTextField(
                value = fiyatlar[kalem.id].orEmpty(),
                onValueChange = { fiyatlar[kalem.id] = it },
                modifier = Modifier.fillMaxWidth(),
                label = { Text("${kalem.malzeme} (${kalem.miktar} ${kalem.birim})") },
                singleLine = true,
                shape = AppShapes.medium
            )
        }
        error?.let { Text(it, color = AppColors.Danger) }
        Button(
            onClick = {
                val map = talep!!.kalemler.associate { k ->
                    k.id to (fiyatlar[k.id]?.replace(',', '.')?.toDoubleOrNull() ?: 0.0)
                }
                viewModel.addTeklif(talepId, firma, marka, vade.toIntOrNull() ?: 0, teslim, odeme, map) {
                    viewModel.navigate("teklif-karsilastirma")
                }
            },
            modifier = Modifier.fillMaxWidth().height(52.dp),
            enabled = firma.isNotBlank() && !loading,
            colors = ButtonDefaults.buttonColors(containerColor = AppColors.Primary)
        ) { Text(if (loading) "Kaydediliyor..." else "Teklifi Kaydet") }
        if ((talep!!.teklifler.size) > 0) {
            Button(
                onClick = { viewModel.sendQuotesToManagement(talepId) { viewModel.navigate("teklif-karsilastirma") } },
                modifier = Modifier.fillMaxWidth(),
                colors = ButtonDefaults.buttonColors(containerColor = AppColors.Success)
            ) { Text("Yönetime Gönder") }
        }
    }
}

@Composable
private fun field(label: String, value: String, onChange: (String) -> Unit) {
    OutlinedTextField(
        value = value,
        onValueChange = onChange,
        modifier = Modifier.fillMaxWidth(),
        label = { Text(label) },
        singleLine = true,
        shape = AppShapes.medium
    )
}

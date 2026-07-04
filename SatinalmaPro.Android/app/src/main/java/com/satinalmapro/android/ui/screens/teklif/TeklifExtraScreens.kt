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
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateMapOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.satinalmapro.android.core.model.TalepItem
import com.satinalmapro.android.core.model.TalepQueue
import com.satinalmapro.android.ui.AppViewModel
import com.satinalmapro.android.ui.components.AppCard
import com.satinalmapro.android.ui.components.DetailRow
import com.satinalmapro.android.ui.screens.talep.TalepListScreen
import com.satinalmapro.android.ui.theme.AppColors
import com.satinalmapro.android.ui.theme.AppShapes

@Composable
fun TeklifsizFirmaFiyatScreen(viewModel: AppViewModel, talepId: String?) {
    if (talepId == null) {
        TalepListScreen(viewModel, TalepQueue.TEKLIFSIZ_FIRMA_FIYAT)
        return
    }
    val talep by viewModel.talepById(talepId).collectAsState(initial = null)
    val error by viewModel.submitError.collectAsState()
    val firmaMap = remember { mutableStateMapOf<String, String>() }
    val fiyatMap = remember { mutableStateMapOf<String, String>() }

    if (talep == null) {
        Column(Modifier.fillMaxSize().padding(24.dp)) { Text("Talep bulunamadı.", color = AppColors.TextSecondary) }
        return
    }
    TeklifsizFirmaFiyatForm(talep!!, firmaMap, fiyatMap, error) { girdiler ->
        viewModel.teklifsizFirmaFiyatKaydet(talepId, girdiler) {
            viewModel.navigate("onaylanan-malzemeler")
        }
    }
}

@Composable
private fun TeklifsizFirmaFiyatForm(
    talep: TalepItem,
    firmaMap: MutableMap<String, String>,
    fiyatMap: MutableMap<String, String>,
    error: String?,
    onSave: (List<Triple<String, String, Double>>) -> Unit
) {
    Column(
        Modifier.fillMaxSize().verticalScroll(rememberScrollState()).padding(20.dp),
        verticalArrangement = Arrangement.spacedBy(12.dp)
    ) {
        Text("Firma/Fiyat Girişi", style = MaterialTheme.typography.headlineSmall)
        Text("${talep.talepNo} · ${talep.talepEden}", color = AppColors.TextSecondary)
        talep.kalemler.forEach { kalem ->
            AppCard {
                Column(Modifier.padding(16.dp)) {
                    DetailRow("Malzeme", kalem.malzeme)
                    HorizontalDivider(color = AppColors.Border)
                    DetailRow("Miktar", "${kalem.miktar} ${kalem.birim}")
                    Spacer(Modifier.height(8.dp))
                    OutlinedTextField(
                        value = firmaMap[kalem.id].orEmpty(),
                        onValueChange = { firmaMap[kalem.id] = it },
                        modifier = Modifier.fillMaxWidth(),
                        label = { Text("Firma") },
                        singleLine = true,
                        shape = AppShapes.medium
                    )
                    Spacer(Modifier.height(8.dp))
                    OutlinedTextField(
                        value = fiyatMap[kalem.id].orEmpty(),
                        onValueChange = { fiyatMap[kalem.id] = it },
                        modifier = Modifier.fillMaxWidth(),
                        label = { Text("Birim Fiyat") },
                        singleLine = true,
                        shape = AppShapes.medium
                    )
                }
            }
        }
        error?.let { Text(it, color = AppColors.Danger) }
        Button(
            onClick = {
                val girdiler = talep.kalemler.mapNotNull { kalem ->
                    val firma = firmaMap[kalem.id]?.trim().orEmpty()
                    val fiyat = fiyatMap[kalem.id]?.replace(',', '.')?.toDoubleOrNull()
                    if (firma.isBlank() || fiyat == null || fiyat <= 0) null
                    else Triple(kalem.id, firma, fiyat)
                }
                if (girdiler.size != talep.kalemler.size) return@Button
                onSave(girdiler)
            },
            modifier = Modifier.fillMaxWidth().height(52.dp),
            colors = ButtonDefaults.buttonColors(containerColor = AppColors.Primary)
        ) { Text("Kaydet") }
    }
}

@Composable
fun TeklifKarsilastirmaScreen(viewModel: AppViewModel, talepId: String?) {
    if (talepId != null) {
        com.satinalmapro.android.ui.screens.talep.TalepDetayScreen(viewModel, talepId)
    } else {
        TalepListScreen(viewModel, TalepQueue.TEKLIF_KARSILASTIRMA)
    }
}

@Composable
fun TeklifOnayDetayScreen(viewModel: AppViewModel, talepId: String) {
    com.satinalmapro.android.ui.screens.talep.TalepDetayScreen(viewModel, talepId)
}

@Composable
fun OnayGecmisiScreen(viewModel: AppViewModel) {
    TalepListScreen(viewModel, TalepQueue.ONAY_GECMISI)
}

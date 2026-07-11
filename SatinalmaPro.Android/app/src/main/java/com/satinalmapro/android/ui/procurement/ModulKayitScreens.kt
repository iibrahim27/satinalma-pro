package com.satinalmapro.android.ui.procurement

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.satinalmapro.android.ui.AppViewModel
import com.satinalmapro.android.ui.theme.MetrikLight

@Composable
fun AgregaListScreen(viewModel: AppViewModel) {
    val list by viewModel.agregaList.collectAsState()
    ModulKayitListScaffold(
        title = "Agrega",
        subtitle = "${list.size} kayıt",
        rows = list.map {
            ModulKayitRow(
                title = listOf(it.agregaTuru, it.agregaCinsi).filter { s -> s.isNotBlank() }.joinToString(" · "),
                meta = "${it.tarih} · ${it.miktar} ${it.birim} · ${it.tedarikci}",
                detail = it.indirildigiSaha
            )
        }
    )
}

@Composable
fun CimentoListScreen(viewModel: AppViewModel) {
    val list by viewModel.cimentoList.collectAsState()
    ModulKayitListScaffold(
        title = "Çimento",
        subtitle = "${list.size} kayıt",
        rows = list.map {
            ModulKayitRow(
                title = listOf(it.cimentoSinifi, it.cimentoCinsi).filter { s -> s.isNotBlank() }.joinToString(" · "),
                meta = "${it.tarih} · ${it.miktar} ${it.birim} · ${it.tedarikci}",
                detail = it.indirildigiSaha
            )
        }
    )
}

@Composable
fun TedarikciListScreen(viewModel: AppViewModel) {
    val agrega by viewModel.agregaList.collectAsState()
    val cimento by viewModel.cimentoList.collectAsState()
    val alinan by viewModel.alinanMalzemeKayitlari.collectAsState()
    val talepler by viewModel.talepler.collectAsState()
    val names = remember(agrega, cimento, alinan, talepler) {
        buildSet {
            agrega.forEach { if (it.tedarikci.isNotBlank()) add(it.tedarikci.trim()) }
            cimento.forEach { if (it.tedarikci.isNotBlank()) add(it.tedarikci.trim()) }
            alinan.forEach { if (it.tedarikci.isNotBlank()) add(it.tedarikci.trim()) }
            talepler.forEach { t ->
                t.teklifler.forEach { q ->
                    if (q.firmaAdi.isNotBlank()) add(q.firmaAdi.trim())
                }
            }
        }.sortedBy { it.lowercase() }
    }
    ModulKayitListScaffold(
        title = "Tedarikçiler",
        subtitle = "${names.size} firma",
        rows = names.map { ModulKayitRow(title = it, meta = "Kayıtlı tedarikçi", detail = "") }
    )
}

@Composable
fun IadeListScreen(viewModel: AppViewModel) {
    val talepler by viewModel.talepler.collectAsState()
    val rows = remember(talepler) {
        talepler
            .filter { it.hasReturnFlag || it.durum.contains("iade", ignoreCase = true) }
            .sortedByDescending { it.guncellemeUtc }
            .map {
                ModulKayitRow(
                    title = it.talepNo.ifBlank { it.id },
                    meta = "${it.durum} · ${it.talepEden}",
                    detail = it.malzemeOzeti
                )
            }
    }
    ModulKayitListScaffold(
        title = "İade İşlemleri",
        subtitle = if (rows.isEmpty()) "İade işaretli talep yok" else "${rows.size} kayıt",
        rows = rows
    )
}

private data class ModulKayitRow(
    val title: String,
    val meta: String,
    val detail: String
)

@Composable
private fun ModulKayitListScaffold(
    title: String,
    subtitle: String,
    rows: List<ModulKayitRow>
) {
    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(MetrikLight.Background)
            .padding(16.dp)
    ) {
        Text(title, style = MaterialTheme.typography.titleLarge, fontWeight = FontWeight.SemiBold)
        Spacer(Modifier.height(4.dp))
        Text(subtitle, style = MaterialTheme.typography.bodyMedium, color = MetrikLight.TextSecondary)
        Spacer(Modifier.height(12.dp))
        if (rows.isEmpty()) {
            Text("Kayıt bulunamadı.", color = MetrikLight.TextSecondary)
        } else {
            LazyColumn(
                verticalArrangement = Arrangement.spacedBy(8.dp),
                contentPadding = PaddingValues(bottom = 24.dp)
            ) {
                items(rows) { row ->
                    Column(
                        modifier = Modifier
                            .fillMaxWidth()
                            .background(MetrikLight.Surface, shape = MaterialTheme.shapes.medium)
                            .padding(12.dp)
                    ) {
                        Text(row.title, fontWeight = FontWeight.SemiBold)
                        Text(row.meta, style = MaterialTheme.typography.bodySmall, color = MetrikLight.TextSecondary)
                        if (row.detail.isNotBlank()) {
                            Text(row.detail, style = MaterialTheme.typography.bodySmall)
                        }
                    }
                }
            }
        }
    }
}

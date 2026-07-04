package com.satinalmapro.android.ui.screens.talep

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.satinalmapro.android.core.model.TalepItem
import com.satinalmapro.android.core.model.TalepQueue
import com.satinalmapro.android.ui.AppViewModel
import com.satinalmapro.android.ui.components.AppCard
import com.satinalmapro.android.ui.components.StatusBadge
import com.satinalmapro.android.ui.theme.AppColors

@Composable
fun TalepListScreen(viewModel: AppViewModel, queue: TalepQueue) {
    val loading by viewModel.loading.collectAsState()
    val items by viewModel.filteredTalepler(queue).collectAsState(initial = emptyList())

    when {
        loading && items.isEmpty() -> {
            Column(Modifier.fillMaxSize(), horizontalAlignment = Alignment.CenterHorizontally, verticalArrangement = Arrangement.Center) {
                CircularProgressIndicator(color = AppColors.Primary)
            }
        }
        items.isEmpty() -> {
            Column(Modifier.fillMaxSize().padding(24.dp), verticalArrangement = Arrangement.Center) {
                Text("Kayıt bulunamadı.", style = MaterialTheme.typography.bodyLarge, color = AppColors.TextSecondary)
            }
        }
        else -> {
            LazyColumn(
                modifier = Modifier.fillMaxSize().padding(horizontal = 20.dp, vertical = 12.dp),
                verticalArrangement = Arrangement.spacedBy(10.dp)
            ) {
                items(items, key = { it.id }) { talep ->
                    val route = when (queue) {
                        TalepQueue.TEKLIF_GIR -> "teklif-gir?id=${talep.id}"
                        TalepQueue.TEKLIF_KARSILASTIRMA -> "talep-detay?id=${talep.id}"
                        else -> "talep-detay?id=${talep.id}"
                    }
                    TalepRow(talep) { viewModel.navigate(route) }
                }
            }
        }
    }
}

@Composable
private fun TalepRow(talep: TalepItem, onClick: () -> Unit) {
    AppCard(onClick = onClick) {
        Row(
            Modifier.fillMaxWidth().padding(16.dp),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Column(Modifier.weight(1f)) {
                Text(talep.talepNo.ifBlank { "Talep" }, style = MaterialTheme.typography.titleMedium, color = AppColors.TextPrimary)
                Text(talep.malzemeOzeti, style = MaterialTheme.typography.bodyMedium, color = AppColors.TextSecondary)
                Text("${talep.talepEden} · ${talep.tarih}", style = MaterialTheme.typography.labelMedium, color = AppColors.TextSecondary)
            }
            StatusBadge(talep.durum, AppColors.PrimaryContainer, AppColors.Primary)
        }
    }
}

package com.satinalmapro.android.ui.procurement

import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import com.satinalmapro.android.core.model.TalepItem
import com.satinalmapro.android.core.model.TalepQueue
import com.satinalmapro.android.core.roles.TalepDurumEtiketi
import com.satinalmapro.android.ui.AppViewModel
import com.satinalmapro.android.ui.components.AppPullRefreshBox
import com.satinalmapro.android.ui.components.AppSearchField
import com.satinalmapro.android.ui.components.EmptyState
import com.satinalmapro.android.ui.components.LoadingState
import com.satinalmapro.android.ui.components.QueueRow

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun TalepListScreen(viewModel: AppViewModel, queue: TalepQueue) {
    var search by remember { mutableStateOf("") }
    val loading by viewModel.loading.collectAsState()
    val initialSyncPending by viewModel.initialSyncPending.collectAsState()
    val items by viewModel.filteredTalepler(queue).collectAsState(initial = emptyList())
    val filtered = items.filter { it.matchesSearch(search) }

    AppPullRefreshBox(
        isRefreshing = loading && items.isNotEmpty(),
        onRefresh = { viewModel.refreshData() }
    ) {
        AppSearchField(
            value = search,
            onValueChange = { search = it },
            placeholder = "Talep no, malzeme veya talep eden ara..."
        )
        when {
            (loading || initialSyncPending) && items.isEmpty() ->
                LoadingState(modifier = Modifier.fillMaxSize())
            filtered.isEmpty() -> EmptyState(
                title = if (items.isEmpty()) "Bu firmada henüz kayıt yok" else "Arama sonucu bulunamadı",
                subtitle = if (items.isEmpty()) "Yeni talep oluştuğunda burada listelenir." else "Farklı bir arama deneyin."
            )
            else -> {
                LazyColumn(modifier = Modifier.fillMaxSize()) {
                    items(filtered, key = { it.id }) { talep ->
                        val route = routeForQueue(queue, talep.id)
                        QueueRow(
                            title = talep.talepNo.ifBlank { "Talep" },
                            subtitle = talep.malzemeOzeti,
                            meta = TalepDurumEtiketi.listeAltMetin(talep, queue),
                            status = talep.durum,
                            onClick = { viewModel.navigate(route) }
                        )
                    }
                }
            }
        }
    }
}

private fun routeForQueue(queue: TalepQueue, talepId: String) = when (queue) {
    TalepQueue.TEKLIF_BEKLEYEN -> "talep-detay?id=$talepId"
    TalepQueue.TEKLIF_GIR, TalepQueue.SATINALMA_TEKLIF_ISTENEN -> "teklif-gir?id=$talepId"
    TalepQueue.TEKLIF_KARSILASTIRMA -> "teklif-karsilastirma?id=$talepId"
    TalepQueue.TEKLIF_ONAY -> "teklif-onay-detay?id=$talepId"
    TalepQueue.SATINALMA_TEKLIF_GIRILEN -> "teklif-onay-detay?id=$talepId"
    TalepQueue.SATINALMA_TEKLIF_DUZELTME -> "teklif-karsilastirma?id=$talepId"
    TalepQueue.TEKLIFSIZ_FIRMA_FIYAT -> "teklifsiz-firma-fiyat?id=$talepId"
    TalepQueue.SATINALMA_ONAYLANAN -> "talep-detay?id=$talepId&view=onaylanan"
    TalepQueue.SATINALMA_SIPARIS -> "talep-detay?id=$talepId&view=siparis"
    TalepQueue.SATINALMA_MAL_KABUL -> "talep-detay?id=$talepId&view=malkabul"
    else -> "talep-detay?id=$talepId"
}

private fun TalepItem.matchesSearch(query: String): Boolean {
    if (query.isBlank()) return true
    val q = query.trim()
    return talepNo.contains(q, true) ||
        malzemeOzeti.contains(q, true) ||
        talepEden.contains(q, true) ||
        durum.contains(q, true) ||
        santiyeAdi.contains(q, true)
}

package com.satinalmapro.android.ui.screens.settings

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.rounded.Add
import androidx.compose.material.icons.rounded.Delete
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.satinalmapro.android.ui.AppViewModel
import com.satinalmapro.android.ui.components.AppCard
import com.satinalmapro.android.ui.components.AppDetailTabRow
import com.satinalmapro.android.ui.theme.AppColors
import com.satinalmapro.android.ui.theme.AppShapes
import com.satinalmapro.android.ui.theme.AppSpacing

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun SettingsScreen(viewModel: AppViewModel) {
    val birimler by viewModel.malzemeBirimleri.collectAsState()
    val kategoriler by viewModel.malzemeKategorileri.collectAsState()
    val loading by viewModel.loading.collectAsState()
    val settingsMessage by viewModel.settingsMessage.collectAsState()
    val settingsError by viewModel.settingsError.collectAsState()
    var tab by remember { mutableIntStateOf(0) }

    LaunchedEffect(Unit) { viewModel.loadSettings() }

    Column(Modifier.fillMaxSize()) {
        AppDetailTabRow(
            tabs = listOf("Birim Terimleri", "Kategoriler"),
            selectedIndex = tab,
            onTabSelected = { tab = it }
        )

        settingsMessage?.let {
            Text(
                it,
                modifier = Modifier.padding(horizontal = AppSpacing.screenHorizontal, vertical = 8.dp),
                style = MaterialTheme.typography.bodySmall,
                color = AppColors.Primary
            )
        }
        settingsError?.let {
            Text(
                it,
                modifier = Modifier.padding(horizontal = 16.dp, vertical = 4.dp),
                style = MaterialTheme.typography.bodySmall,
                color = AppColors.Danger
            )
        }

        when (tab) {
            0 -> TermListTab(
                title = "Malzeme birim terimleri",
                hint = "Yeni birim",
                items = birimler,
                loading = loading,
                onAdd = viewModel::addBirim,
                onRemove = viewModel::removeBirim
            )
            else -> TermListTab(
                title = "Malzeme kategorileri",
                hint = "Yeni kategori",
                items = kategoriler,
                loading = loading,
                onAdd = viewModel::addKategori,
                onRemove = viewModel::removeKategori
            )
        }
    }
}

@Composable
private fun TermListTab(
    title: String,
    hint: String,
    items: List<String>,
    loading: Boolean,
    onAdd: (String, () -> Unit) -> Unit,
    onRemove: (String) -> Unit
) {
    var newTerm by remember { mutableStateOf("") }

    Column(
        Modifier
            .fillMaxSize()
            .padding(16.dp)
    ) {
        Text(title, style = MaterialTheme.typography.titleMedium, color = AppColors.TextPrimary)
        Spacer(Modifier.height(12.dp))
        Row(verticalAlignment = Alignment.CenterVertically) {
            OutlinedTextField(
                value = newTerm,
                onValueChange = { newTerm = it },
                placeholder = { Text(hint) },
                modifier = Modifier.weight(1f),
                singleLine = true,
                shape = AppShapes.small
            )
            Spacer(Modifier.size(8.dp))
            IconButton(
                onClick = {
                    val value = newTerm.trim()
                    if (value.isBlank()) return@IconButton
                    onAdd(value) { newTerm = "" }
                },
                enabled = !loading
            ) {
                Icon(Icons.Rounded.Add, contentDescription = "Ekle", tint = AppColors.Primary)
            }
        }
        Spacer(Modifier.height(12.dp))
        LazyColumn(verticalArrangement = Arrangement.spacedBy(8.dp)) {
            items(items, key = { it.lowercase() }) { item ->
                AppCard {
                    Row(
                        Modifier.fillMaxWidth().padding(horizontal = 16.dp, vertical = 12.dp),
                        horizontalArrangement = Arrangement.SpaceBetween,
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Text(item, color = AppColors.TextPrimary)
                        IconButton(
                            onClick = { onRemove(item) },
                            enabled = !loading && items.size > 1
                        ) {
                            Icon(Icons.Rounded.Delete, contentDescription = "Sil", tint = AppColors.Danger)
                        }
                    }
                }
            }
        }
    }
}

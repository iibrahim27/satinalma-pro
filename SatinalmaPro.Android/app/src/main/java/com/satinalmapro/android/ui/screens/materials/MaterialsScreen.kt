package com.satinalmapro.android.ui.screens.materials

import androidx.compose.foundation.horizontalScroll
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.rememberScrollState
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.rounded.ChevronRight
import androidx.compose.material.icons.rounded.FilterList
import androidx.compose.material.icons.rounded.Search
import androidx.compose.material3.FilterChip
import androidx.compose.material3.FilterChipDefaults
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.OutlinedTextFieldDefaults
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.satinalmapro.android.data.DemoData
import com.satinalmapro.android.data.MaterialItem
import com.satinalmapro.android.ui.components.AppCard
import com.satinalmapro.android.ui.components.StatusBadge
import com.satinalmapro.android.ui.theme.AppColors
import com.satinalmapro.android.ui.theme.AppShapes

private val filters = listOf("Bugün", "Bu Hafta", "Bu Ay", "Teslim Edildi", "Bekliyor")

@Composable
fun MaterialsScreen(onMaterialClick: (String) -> Unit) {
    var search by remember { mutableStateOf("") }
    var selectedFilter by remember { mutableStateOf("Bugün") }

    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(horizontal = 20.dp, vertical = 16.dp)
    ) {
        Row(verticalAlignment = Alignment.CenterVertically) {
            OutlinedTextField(
                value = search,
                onValueChange = { search = it },
                modifier = Modifier.weight(1f),
                placeholder = { Text("Malzeme ara...") },
                leadingIcon = { Icon(Icons.Rounded.Search, null) },
                singleLine = true,
                shape = AppShapes.medium,
                colors = OutlinedTextFieldDefaults.colors(
                    focusedBorderColor = AppColors.Primary,
                    unfocusedBorderColor = AppColors.Border,
                    focusedContainerColor = AppColors.Surface,
                    unfocusedContainerColor = AppColors.Surface
                )
            )
            IconButton(onClick = { }) {
                Icon(Icons.Rounded.FilterList, contentDescription = "Filtre")
            }
        }

        Spacer(Modifier.height(12.dp))
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .horizontalScroll(rememberScrollState()),
            horizontalArrangement = Arrangement.spacedBy(8.dp)
        ) {
            filters.forEach { filter ->
                FilterChip(
                    selected = selectedFilter == filter,
                    onClick = { selectedFilter = filter },
                    label = { Text(filter) },
                    colors = FilterChipDefaults.filterChipColors(
                        selectedContainerColor = AppColors.PrimaryContainer,
                        selectedLabelColor = AppColors.Primary
                    )
                )
            }
        }

        Spacer(Modifier.height(16.dp))
        LazyColumn(
            verticalArrangement = Arrangement.spacedBy(12.dp),
            contentPadding = PaddingValues(bottom = 88.dp)
        ) {
            items(DemoData.materials) { item ->
                MaterialCard(item) { onMaterialClick(item.id) }
            }
        }
    }
}

@Composable
private fun MaterialCard(item: MaterialItem, onClick: () -> Unit) {
    AppCard(onClick = onClick) {
        Row(
            Modifier.padding(16.dp).fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Column(Modifier.weight(1f)) {
                Text(item.company, style = MaterialTheme.typography.titleMedium, color = AppColors.TextPrimary)
                Text(item.material, style = MaterialTheme.typography.bodyMedium, color = AppColors.TextSecondary)
                Text(
                    "${item.quantity} · ${item.date}",
                    style = MaterialTheme.typography.labelMedium,
                    color = AppColors.TextSecondary,
                    modifier = Modifier.padding(top = 4.dp)
                )
            }
            Column(horizontalAlignment = Alignment.End) {
                StatusBadge(item.status.label, item.status.bg, item.status.fg)
                Icon(
                    Icons.Rounded.ChevronRight,
                    contentDescription = null,
                    tint = AppColors.TextSecondary,
                    modifier = Modifier.padding(top = 8.dp)
                )
            }
        }
    }
}

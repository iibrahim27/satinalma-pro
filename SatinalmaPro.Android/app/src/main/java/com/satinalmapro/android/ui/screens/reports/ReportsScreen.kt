package com.satinalmapro.android.ui.screens.reports

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.grid.GridCells
import androidx.compose.foundation.lazy.grid.LazyVerticalGrid
import androidx.compose.foundation.lazy.grid.items
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.satinalmapro.android.ui.AppViewModel
import com.satinalmapro.android.ui.components.AppCard
import com.satinalmapro.android.ui.theme.AppColors
import com.satinalmapro.android.ui.theme.AppSpacing

@Composable
fun ReportsScreen(viewModel: AppViewModel) {
    val cards by viewModel.dashboardCards.collectAsState(initial = emptyList())

    Column(Modifier.fillMaxSize().padding(horizontal = AppSpacing.screenHorizontal, vertical = AppSpacing.screenVertical)) {
        Text("Satınalma Özeti", style = MaterialTheme.typography.headlineMedium, color = AppColors.TextPrimary)
        Text(
            "Bekleyen işlemlere dokunarak ilgili listeye gidebilirsiniz.",
            style = MaterialTheme.typography.bodySmall,
            color = AppColors.TextSecondary,
            modifier = Modifier.padding(top = 4.dp, bottom = 16.dp)
        )
        LazyVerticalGrid(
            columns = GridCells.Fixed(2),
            contentPadding = PaddingValues(bottom = 16.dp),
            horizontalArrangement = Arrangement.spacedBy(AppSpacing.cardGap),
            verticalArrangement = Arrangement.spacedBy(AppSpacing.cardGap),
            modifier = Modifier.fillMaxSize()
        ) {
            items(cards, key = { it.route + it.title }) { card ->
                AppCard(onClick = { viewModel.navigate(card.route) }) {
                    Column {
                        Text(card.title, style = MaterialTheme.typography.labelMedium, color = AppColors.TextSecondary)
                        Text(
                            card.value,
                            style = MaterialTheme.typography.headlineSmall,
                            fontWeight = FontWeight.Bold,
                            color = AppColors.TextPrimary,
                            modifier = Modifier.padding(vertical = 4.dp)
                        )
                        Text(card.subtitle, style = MaterialTheme.typography.bodySmall, color = AppColors.TextSecondary)
                    }
                }
            }
        }
    }
}

package com.satinalmapro.android.ui.screens.quote

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.rounded.ArrowBack
import androidx.compose.material.icons.rounded.Star
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.satinalmapro.android.data.DemoData
import com.satinalmapro.android.data.QuoteItem
import com.satinalmapro.android.ui.components.AppCard
import com.satinalmapro.android.ui.components.StatusBadge
import com.satinalmapro.android.ui.theme.AppColors
import com.satinalmapro.android.ui.theme.AppShapes

@Composable
fun QuoteComparisonScreen(onBack: () -> Unit) {
    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(horizontal = 20.dp, vertical = 16.dp)
    ) {
        Row(verticalAlignment = Alignment.CenterVertically) {
            IconButton(onClick = onBack) {
                Icon(Icons.AutoMirrored.Rounded.ArrowBack, contentDescription = "Geri")
            }
            Column {
                Text("Teklif Karşılaştırma", style = MaterialTheme.typography.headlineMedium, color = AppColors.TextPrimary)
                Text("Demir Ø12 · 25.000 kg", style = MaterialTheme.typography.bodyMedium, color = AppColors.TextSecondary)
            }
        }

        Spacer(Modifier.height(16.dp))

        LazyColumn(
            verticalArrangement = Arrangement.spacedBy(12.dp),
            modifier = Modifier.weight(1f)
        ) {
            items(DemoData.quotes) { quote ->
                QuoteCard(quote)
            }
        }

        Button(
            onClick = { },
            modifier = Modifier
                .fillMaxWidth()
                .height(52.dp)
                .padding(vertical = 12.dp),
            shape = AppShapes.medium,
            colors = ButtonDefaults.buttonColors(containerColor = AppColors.Primary)
        ) {
            Text("Karşılaştır", style = MaterialTheme.typography.labelLarge)
        }
    }
}

@Composable
private fun QuoteCard(quote: QuoteItem) {
    AppCard {
        Column(Modifier.padding(18.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
            Row(
                Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text(quote.company, style = MaterialTheme.typography.titleMedium, color = AppColors.TextPrimary)
                if (quote.recommended) {
                    StatusBadge("Önerilen", AppColors.SuccessContainer, AppColors.Success)
                }
            }
            Text("Toplam: ${quote.totalPrice}", style = MaterialTheme.typography.bodyLarge, color = AppColors.TextPrimary)
            Text("Birim: ${quote.unitPrice}", style = MaterialTheme.typography.bodyMedium, color = AppColors.TextSecondary)
            Row(verticalAlignment = Alignment.CenterVertically) {
                Text("Teslim: ${quote.deliveryDays}", style = MaterialTheme.typography.bodyMedium, color = AppColors.TextSecondary)
                Spacer(Modifier.weight(1f))
                repeat(5) { index ->
                    Icon(
                        Icons.Rounded.Star,
                        contentDescription = null,
                        tint = if (index < quote.rating.toInt()) AppColors.Warning else AppColors.Border,
                        modifier = Modifier.padding(start = 2.dp)
                    )
                }
            }
        }
    }
}

package com.satinalmapro.android.ui.screens.materials

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.rounded.ArrowBack
import androidx.compose.material.icons.rounded.PictureAsPdf
import androidx.compose.material.icons.rounded.Print
import androidx.compose.material.icons.rounded.Share
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.satinalmapro.android.data.DemoData
import com.satinalmapro.android.ui.components.AppCard
import com.satinalmapro.android.ui.components.DetailRow
import com.satinalmapro.android.ui.components.StatusBadge
import com.satinalmapro.android.ui.theme.AppColors

@Composable
fun MaterialDetailScreen(onBack: () -> Unit) {
    val detail = DemoData.materialDetail

    Column(
        modifier = Modifier
            .fillMaxSize()
            .verticalScroll(rememberScrollState())
            .padding(horizontal = 20.dp, vertical = 16.dp)
    ) {
        Row(verticalAlignment = Alignment.CenterVertically) {
            IconButton(onClick = onBack) {
                Icon(Icons.AutoMirrored.Rounded.ArrowBack, contentDescription = "Geri")
            }
            Column(Modifier.weight(1f)) {
                Text(detail.documentNo, style = MaterialTheme.typography.headlineMedium, color = AppColors.TextPrimary)
            }
            StatusBadge(detail.status.label, detail.status.bg, detail.status.fg)
        }

        Spacer(Modifier.height(16.dp))

        AppCard {
            Column(Modifier.padding(20.dp)) {
                Text("Bilgiler", style = MaterialTheme.typography.titleMedium, color = AppColors.TextPrimary)
                HorizontalDivider(modifier = Modifier.padding(vertical = 12.dp), color = AppColors.Border)
                DetailRow("Malzeme", detail.material)
                DetailRow("Tedarikçi", detail.supplier)
                DetailRow("Fatura", detail.invoice)
                DetailRow("Depo", detail.warehouse)
                DetailRow("Miktar", detail.quantity)
                DetailRow("Birim", detail.unit)
                DetailRow("Birim Fiyat", detail.unitPrice)
                DetailRow("Toplam", detail.total)
                DetailRow("Teslim Tarihi", detail.deliveryDate)
                DetailRow("Açıklama", detail.description)
            }
        }

        Spacer(Modifier.height(20.dp))

        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.spacedBy(10.dp)
        ) {
            ActionButton("PDF", Icons.Rounded.PictureAsPdf, Modifier.weight(1f))
            ActionButton("Paylaş", Icons.Rounded.Share, Modifier.weight(1f))
            ActionButton("Yazdır", Icons.Rounded.Print, Modifier.weight(1f))
        }

        Spacer(Modifier.height(88.dp))
    }
}

@Composable
private fun ActionButton(label: String, icon: androidx.compose.ui.graphics.vector.ImageVector, modifier: Modifier) {
    OutlinedButton(onClick = { }, modifier = modifier) {
        Icon(icon, null, modifier = Modifier.padding(end = 6.dp))
        Text(label)
    }
}

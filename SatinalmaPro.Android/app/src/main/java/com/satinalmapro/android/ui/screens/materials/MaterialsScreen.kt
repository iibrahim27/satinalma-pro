package com.satinalmapro.android.ui.screens.materials

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.rounded.ChevronRight
import androidx.compose.material.icons.rounded.Search
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.OutlinedTextFieldDefaults
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateMapOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.satinalmapro.android.core.model.TalepItem
import com.satinalmapro.android.core.roles.KullaniciRolleri
import com.satinalmapro.android.ui.AppViewModel
import com.satinalmapro.android.ui.components.AppCard
import com.satinalmapro.android.ui.components.StatusBadge
import com.satinalmapro.android.ui.theme.AppColors
import com.satinalmapro.android.ui.theme.AppShapes

@Composable
fun MaterialsScreen(viewModel: AppViewModel) {
    var search by remember { mutableStateOf("") }
    val items by viewModel.approvedMaterials().collectAsState()
    val user by viewModel.user.collectAsState()
    val canMalKabul = KullaniciRolleri.canMalKabul(user?.role)
    val kabulMiktarlari = remember { mutableStateMapOf<String, String>() }
    val error by viewModel.submitError.collectAsState()
    val filtered = items.filter {
        search.isBlank() ||
            it.malzemeOzeti.contains(search, true) ||
            it.talepNo.contains(search, true)
    }

    Column(modifier = Modifier.fillMaxSize().padding(horizontal = 20.dp, vertical = 16.dp)) {
        OutlinedTextField(
            value = search,
            onValueChange = { search = it },
            modifier = Modifier.fillMaxWidth(),
            placeholder = { Text("Malzeme ara...") },
            leadingIcon = { Icon(Icons.Rounded.Search, null) },
            singleLine = true,
            shape = AppShapes.medium,
            colors = OutlinedTextFieldDefaults.colors(focusedBorderColor = AppColors.Primary, unfocusedBorderColor = AppColors.Border)
        )
        error?.let { Text(it, color = AppColors.Danger, modifier = Modifier.padding(top = 8.dp)) }
        Spacer(Modifier.padding(top = 8.dp))
        if (filtered.isEmpty()) {
            Text("Onaylı malzeme kaydı yok.", color = AppColors.TextSecondary)
        } else {
            LazyColumn(verticalArrangement = Arrangement.spacedBy(10.dp)) {
                items(filtered, key = { it.id }) { item ->
                    MaterialCard(
                        item = item,
                        canMalKabul = canMalKabul,
                        miktar = kabulMiktarlari[item.id].orEmpty(),
                        onMiktarChange = { kabulMiktarlari[item.id] = it },
                        onDetail = { viewModel.navigate("talep-detay?id=${item.id}") },
                        onMalKabul = { kalemId, miktar ->
                            viewModel.malKabul(item.id, kalemId, miktar) { }
                        }
                    )
                }
            }
        }
    }
}

@Composable
private fun MaterialCard(
    item: TalepItem,
    canMalKabul: Boolean,
    miktar: String,
    onMiktarChange: (String) -> Unit,
    onDetail: () -> Unit,
    onMalKabul: (String, String) -> Unit
) {
    val kalem = item.kalemler.firstOrNull { !it.onaylananTeklifId.isNullOrBlank() } ?: item.kalemler.firstOrNull()
    AppCard(onClick = onDetail) {
        Column(Modifier.padding(16.dp)) {
            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween, verticalAlignment = Alignment.CenterVertically) {
                Column(Modifier.weight(1f)) {
                    Text(item.talepNo, style = MaterialTheme.typography.titleMedium, color = AppColors.TextPrimary)
                    Text(item.malzemeOzeti, style = MaterialTheme.typography.bodyMedium, color = AppColors.TextSecondary)
                    kalem?.let {
                        Text("Sipariş: ${it.miktar} ${it.birim} · Kabul: ${it.kabulEdilenMiktar}", style = MaterialTheme.typography.labelMedium, color = AppColors.TextSecondary)
                    }
                }
                Icon(Icons.Rounded.ChevronRight, null, tint = AppColors.TextSecondary)
            }
            if (canMalKabul && kalem != null && !kalem.siparisTamamlandi && kalem.kalanMiktar > 0) {
                Spacer(Modifier.padding(top = 12.dp))
                OutlinedTextField(
                    value = miktar,
                    onValueChange = onMiktarChange,
                    modifier = Modifier.fillMaxWidth(),
                    label = { Text("Mal kabul miktarı") },
                    singleLine = true,
                    shape = AppShapes.medium
                )
                Button(
                    onClick = { onMalKabul(kalem.id, miktar) },
                    modifier = Modifier.fillMaxWidth().padding(top = 8.dp),
                    colors = ButtonDefaults.buttonColors(containerColor = AppColors.Success),
                    enabled = miktar.isNotBlank()
                ) { Text("Mal Kabul Et") }
            } else if (kalem?.siparisTamamlandi == true) {
                Spacer(Modifier.padding(top = 8.dp))
                StatusBadge("Tamamlandı", AppColors.SuccessContainer, AppColors.Success)
            }
        }
    }
}

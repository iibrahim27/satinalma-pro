package com.satinalmapro.android.ui.screens.request

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.rounded.ArrowBack
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.OutlinedTextFieldDefaults
import androidx.compose.material3.SingleChoiceSegmentedButtonRow
import androidx.compose.material3.SegmentedButton
import androidx.compose.material3.SegmentedButtonDefaults
import androidx.compose.material3.Text
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Box
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.satinalmapro.android.ui.theme.AppColors
import com.satinalmapro.android.ui.theme.AppShapes

private val materials = listOf("Demir Ø12", "Mıcır 0-11", "Çimento CEM I")
private val units = listOf("kg", "ton", "adet")
private val sites = listOf("Merkez Şantiye", "Doğu Sahası", "Batı Sahası")
private val priorities = listOf("Düşük", "Orta", "Yüksek")

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun NewRequestScreen(onBack: () -> Unit, onSubmit: () -> Unit) {
    var material by remember { mutableStateOf(materials[0]) }
    var unit by remember { mutableStateOf(units[0]) }
    var site by remember { mutableStateOf(sites[0]) }
    var quantity by remember { mutableStateOf("") }
    var description by remember { mutableStateOf("") }
    var priorityIndex by remember { mutableIntStateOf(1) }

    Column(
        modifier = Modifier
            .fillMaxSize()
            .verticalScroll(rememberScrollState())
            .padding(horizontal = 20.dp, vertical = 16.dp)
    ) {
        RowBackTitle("Yeni Satınalma Talebi", onBack)

        Spacer(Modifier.height(20.dp))
        DropdownField("Malzeme", material, materials) { material = it }
        Spacer(Modifier.height(12.dp))
        OutlinedTextField(
            value = quantity,
            onValueChange = { quantity = it },
            modifier = Modifier.fillMaxWidth(),
            label = { Text("Miktar") },
            singleLine = true,
            shape = AppShapes.medium,
            colors = fieldColors()
        )
        Spacer(Modifier.height(12.dp))
        DropdownField("Birim", unit, units) { unit = it }
        Spacer(Modifier.height(12.dp))
        DropdownField("Şantiye", site, sites) { site = it }

        Spacer(Modifier.height(20.dp))
        Text("Aciliyet", style = MaterialTheme.typography.titleMedium, color = AppColors.TextPrimary)
        Spacer(Modifier.height(8.dp))
        SingleChoiceSegmentedButtonRow(modifier = Modifier.fillMaxWidth()) {
            priorities.forEachIndexed { index, label ->
                SegmentedButton(
                    selected = priorityIndex == index,
                    onClick = { priorityIndex = index },
                    shape = SegmentedButtonDefaults.itemShape(index, priorities.size),
                    colors = SegmentedButtonDefaults.colors(
                        activeContainerColor = when (index) {
                            2 -> AppColors.DangerContainer
                            1 -> AppColors.WarningContainer
                            else -> AppColors.PrimaryContainer
                        }
                    )
                ) { Text(label) }
            }
        }

        Spacer(Modifier.height(16.dp))
        OutlinedTextField(
            value = description,
            onValueChange = { description = it },
            modifier = Modifier.fillMaxWidth().height(120.dp),
            label = { Text("Açıklama") },
            shape = AppShapes.medium,
            colors = fieldColors()
        )

        Spacer(Modifier.height(24.dp))
        Button(
            onClick = onSubmit,
            modifier = Modifier.fillMaxWidth().height(52.dp),
            shape = AppShapes.medium,
            colors = ButtonDefaults.buttonColors(containerColor = AppColors.Primary)
        ) {
            Text("Talebi Gönder", style = MaterialTheme.typography.labelLarge)
        }
        Spacer(Modifier.height(88.dp))
    }
}

@Composable
private fun DropdownField(label: String, value: String, options: List<String>, onSelect: (String) -> Unit) {
    var expanded by remember { mutableStateOf(false) }
    Box {
        OutlinedTextField(
            value = value,
            onValueChange = {},
            readOnly = true,
            label = { Text(label) },
            modifier = Modifier
                .fillMaxWidth()
                .clickable { expanded = true },
            shape = AppShapes.medium,
            colors = fieldColors()
        )
        DropdownMenu(
            expanded = expanded,
            onDismissRequest = { expanded = false }
        ) {
            options.forEach { option ->
                DropdownMenuItem(
                    text = { Text(option) },
                    onClick = { onSelect(option); expanded = false }
                )
            }
        }
    }
}

@Composable
private fun RowBackTitle(title: String, onBack: () -> Unit) {
    androidx.compose.foundation.layout.Row(verticalAlignment = androidx.compose.ui.Alignment.CenterVertically) {
        IconButton(onClick = onBack) {
            Icon(Icons.AutoMirrored.Rounded.ArrowBack, contentDescription = "Geri")
        }
        Text(title, style = MaterialTheme.typography.headlineMedium, color = AppColors.TextPrimary)
    }
}

@Composable
private fun fieldColors() = OutlinedTextFieldDefaults.colors(
    focusedBorderColor = AppColors.Primary,
    unfocusedBorderColor = AppColors.Border,
    focusedContainerColor = AppColors.Surface,
    unfocusedContainerColor = AppColors.Surface
)

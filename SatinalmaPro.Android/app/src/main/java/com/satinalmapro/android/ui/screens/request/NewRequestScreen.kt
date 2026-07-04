package com.satinalmapro.android.ui.screens.request

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.OutlinedTextFieldDefaults
import androidx.compose.material3.SegmentedButton
import androidx.compose.material3.SegmentedButtonDefaults
import androidx.compose.material3.SingleChoiceSegmentedButtonRow
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateListOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.satinalmapro.android.data.local.RequestDraft
import com.satinalmapro.android.data.local.RequestDraftLine
import com.satinalmapro.android.ui.AppViewModel
import com.satinalmapro.android.ui.components.MaterialAutocompleteField
import com.satinalmapro.android.ui.theme.AppColors
import com.satinalmapro.android.ui.theme.AppShapes

private val units = listOf("kg", "ton", "adet", "m3", "m2")
private val priorities = listOf("Düşük", "Orta", "Yüksek")

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun NewRequestScreen(viewModel: AppViewModel) {
    val user by viewModel.user.collectAsState()
    val lines = remember { mutableStateListOf(RequestDraftLine()) }
    var site by remember { mutableStateOf(user?.site ?: "Merkez Şantiye") }
    var description by remember { mutableStateOf("") }
    var priorityIndex by remember { mutableIntStateOf(1) }

    LaunchedEffect(user?.site) {
        viewModel.loadDraft()?.let { draft ->
            site = draft.site.ifBlank { user?.site ?: site }
            description = draft.aciklama
            priorityIndex = draft.oncelikIndex
            lines.clear()
            lines.addAll(draft.lines.ifEmpty { listOf(RequestDraftLine()) })
        }
    }

    LaunchedEffect(site, description, priorityIndex, lines.size) {
        viewModel.saveDraft(
            RequestDraft(
                site = site,
                aciklama = description,
                oncelikIndex = priorityIndex,
                lines = lines.toList()
            )
        )
    }

    Column(
        modifier = Modifier
            .fillMaxSize()
            .verticalScroll(rememberScrollState())
            .padding(horizontal = 20.dp, vertical = 8.dp)
    ) {
        Text("Birden fazla kalem ekleyebilirsiniz. Taslak otomatik kaydedilir.", color = AppColors.TextSecondary)
        Spacer(Modifier.height(12.dp))

        lines.forEachIndexed { index, line ->
            Text("Kalem ${index + 1}", style = MaterialTheme.typography.titleSmall)
            Spacer(Modifier.height(8.dp))
            MaterialAutocompleteField(
                value = line.malzeme,
                onValueChange = { value -> lines[index] = line.copy(malzeme = value) },
                suggestions = viewModel.materialSuggestions(line.malzeme),
                label = "Malzeme"
            )
            Spacer(Modifier.height(8.dp))
            Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                OutlinedTextField(
                    value = line.miktar,
                    onValueChange = { value -> lines[index] = line.copy(miktar = value) },
                    modifier = Modifier.weight(1f),
                    label = { Text("Miktar") },
                    singleLine = true,
                    shape = AppShapes.medium,
                    colors = fieldColors()
                )
                Box(Modifier.weight(1f)) {
                    var expanded by remember { mutableStateOf(false) }
                    OutlinedTextField(
                        value = line.birim,
                        onValueChange = {},
                        readOnly = true,
                        label = { Text("Birim") },
                        modifier = Modifier.fillMaxWidth().clickable { expanded = true },
                        shape = AppShapes.medium,
                        colors = fieldColors()
                    )
                    DropdownMenu(expanded = expanded, onDismissRequest = { expanded = false }) {
                        units.forEach { option ->
                            DropdownMenuItem(
                                text = { Text(option) },
                                onClick = {
                                    lines[index] = line.copy(birim = option)
                                    expanded = false
                                }
                            )
                        }
                    }
                }
            }
            if (lines.size > 1) {
                OutlinedButton(onClick = { lines.removeAt(index) }, modifier = Modifier.padding(top = 8.dp)) {
                    Text("Kalemi Sil")
                }
            }
            Spacer(Modifier.height(12.dp))
        }

        OutlinedButton(onClick = { lines.add(RequestDraftLine()) }, modifier = Modifier.fillMaxWidth()) {
            Text("+ Kalem Ekle")
        }

        Spacer(Modifier.height(12.dp))
        OutlinedTextField(
            value = site,
            onValueChange = { site = it },
            modifier = Modifier.fillMaxWidth(),
            label = { Text("Şantiye") },
            singleLine = true,
            shape = AppShapes.medium,
            colors = fieldColors()
        )

        Spacer(Modifier.height(16.dp))
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
        val submitError by viewModel.submitError.collectAsState()
        val loading by viewModel.loading.collectAsState()
        submitError?.let { Text(it, color = AppColors.Danger, modifier = Modifier.padding(bottom = 8.dp)) }
        Button(
            onClick = {
                val payload = lines.map { Triple(it.malzeme, it.miktar, it.birim) }
                viewModel.submitRequest(site, description, priorities[priorityIndex], payload) {
                    viewModel.navigate("taleplerim")
                }
            },
            modifier = Modifier.fillMaxWidth().height(52.dp),
            shape = AppShapes.medium,
            colors = ButtonDefaults.buttonColors(containerColor = AppColors.Primary),
            enabled = lines.all { it.malzeme.isNotBlank() && it.miktar.isNotBlank() } && !loading
        ) {
            Text(if (loading) "Gönderiliyor..." else "Talebi Gönder", style = MaterialTheme.typography.labelLarge)
        }
    }
}

@Composable
private fun fieldColors() = OutlinedTextFieldDefaults.colors(
    focusedBorderColor = AppColors.Primary,
    unfocusedBorderColor = AppColors.Border,
    focusedContainerColor = AppColors.Surface,
    unfocusedContainerColor = AppColors.Surface
)

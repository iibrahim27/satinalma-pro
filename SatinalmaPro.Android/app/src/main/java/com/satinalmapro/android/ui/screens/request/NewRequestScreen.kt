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
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.rounded.Add
import androidx.compose.material.icons.rounded.Delete
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
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
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.satinalmapro.android.data.local.RequestDraft
import com.satinalmapro.android.data.local.RequestDraftLine
import com.satinalmapro.android.ui.AppViewModel
import com.satinalmapro.android.ui.theme.AppColors
import com.satinalmapro.android.ui.theme.AppShapes

private val priorities = listOf("Düşük", "Orta", "Yüksek")

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun NewRequestScreen(viewModel: AppViewModel) {
    val user by viewModel.user.collectAsState()
    val birimler by viewModel.malzemeBirimleri.collectAsState()
    val lines = remember { mutableStateListOf(RequestDraftLine()) }
    var site by remember { mutableStateOf(user?.site ?: "Merkez Şantiye") }
    var description by remember { mutableStateOf("") }
    var priorityIndex by remember { mutableIntStateOf(1) }
    var localError by remember { mutableStateOf<String?>(null) }

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

    val completeLines = lines.filter { it.malzeme.isNotBlank() && it.miktar.isNotBlank() }
    val submitError by viewModel.submitError.collectAsState()
    val loading by viewModel.loading.collectAsState()

    Column(
        modifier = Modifier
            .fillMaxSize()
            .verticalScroll(rememberScrollState())
            .padding(horizontal = 16.dp, vertical = 8.dp)
    ) {
        Text("Malzeme kalemleri", style = MaterialTheme.typography.titleMedium, color = AppColors.TextPrimary)
        Spacer(Modifier.height(8.dp))

        RequestLineHeader()
        HorizontalDivider(color = AppColors.Border)

        lines.forEachIndexed { index, line ->
            RequestLineRow(
                line = line,
                birimler = birimler,
                canDelete = lines.size > 1,
                onMalzemeChange = { lines[index] = line.copy(malzeme = it) },
                onMiktarChange = { lines[index] = line.copy(miktar = it) },
                onBirimChange = { lines[index] = line.copy(birim = it) },
                onDelete = { lines.removeAt(index) }
            )
            HorizontalDivider(color = AppColors.Border)
        }

        OutlinedButton(
            onClick = { lines.add(RequestDraftLine()) },
            modifier = Modifier.fillMaxWidth().padding(vertical = 8.dp)
        ) {
            Icon(Icons.Rounded.Add, contentDescription = null, modifier = Modifier.size(18.dp))
            Spacer(Modifier.width(6.dp))
            Text("Satır Ekle")
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

        Spacer(Modifier.height(12.dp))
        Text("Aciliyet", style = MaterialTheme.typography.titleSmall, color = AppColors.TextPrimary)
        Spacer(Modifier.height(6.dp))
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

        Spacer(Modifier.height(12.dp))
        OutlinedTextField(
            value = description,
            onValueChange = { description = it },
            modifier = Modifier.fillMaxWidth().height(96.dp),
            label = { Text("Açıklama") },
            shape = AppShapes.medium,
            colors = fieldColors()
        )

        Spacer(Modifier.height(16.dp))
        localError?.let { Text(it, color = AppColors.Danger, modifier = Modifier.padding(bottom = 8.dp)) }
        submitError?.let { Text(it, color = AppColors.Danger, modifier = Modifier.padding(bottom = 8.dp)) }

        Button(
            onClick = {
                localError = null
                val partial = lines.any {
                    (it.malzeme.isBlank() && it.miktar.isNotBlank()) ||
                        (it.malzeme.isNotBlank() && it.miktar.isBlank())
                }
                if (partial) {
                    localError = "Eksik satırları doldurun veya silin."
                    return@Button
                }
                if (completeLines.isEmpty()) {
                    localError = "En az bir malzeme satırı girin."
                    return@Button
                }
                val payload = completeLines.map { Triple(it.malzeme.trim(), it.miktar.trim(), it.birim) }
                viewModel.submitRequest(site, description, priorities[priorityIndex], payload) {
                    viewModel.navigate("taleplerim")
                }
            },
            modifier = Modifier.fillMaxWidth().height(48.dp),
            shape = AppShapes.medium,
            colors = ButtonDefaults.buttonColors(containerColor = AppColors.Primary),
            enabled = !loading
        ) {
            Text(if (loading) "Gönderiliyor..." else "Talebi Gönder", style = MaterialTheme.typography.labelLarge)
        }
    }
}

@Composable
private fun RequestLineHeader() {
    Row(
        Modifier.fillMaxWidth().padding(horizontal = 4.dp, vertical = 6.dp),
        horizontalArrangement = Arrangement.spacedBy(6.dp)
    ) {
        Text("Malzeme", Modifier.weight(1.4f), style = MaterialTheme.typography.labelMedium, color = AppColors.TextSecondary)
        Text("Miktar", Modifier.weight(0.7f), style = MaterialTheme.typography.labelMedium, color = AppColors.TextSecondary)
        Text("Birim", Modifier.weight(0.7f), style = MaterialTheme.typography.labelMedium, color = AppColors.TextSecondary)
        Spacer(Modifier.width(36.dp))
    }
}

@Composable
private fun RequestLineRow(
    line: RequestDraftLine,
    birimler: List<String>,
    canDelete: Boolean,
    onMalzemeChange: (String) -> Unit,
    onMiktarChange: (String) -> Unit,
    onBirimChange: (String) -> Unit,
    onDelete: () -> Unit
) {
    Row(
        Modifier.fillMaxWidth().padding(vertical = 4.dp),
        horizontalArrangement = Arrangement.spacedBy(6.dp),
        verticalAlignment = Alignment.CenterVertically
    ) {
        OutlinedTextField(
            value = line.malzeme,
            onValueChange = onMalzemeChange,
            modifier = Modifier.weight(1.4f),
            placeholder = { Text("Malzeme") },
            singleLine = true,
            textStyle = MaterialTheme.typography.bodySmall,
            shape = AppShapes.small,
            colors = compactFieldColors()
        )
        OutlinedTextField(
            value = line.miktar,
            onValueChange = onMiktarChange,
            modifier = Modifier.weight(0.7f),
            placeholder = { Text("0") },
            singleLine = true,
            textStyle = MaterialTheme.typography.bodySmall,
            shape = AppShapes.small,
            colors = compactFieldColors()
        )
        Box(Modifier.weight(0.7f)) {
            var expanded by remember { mutableStateOf(false) }
            OutlinedTextField(
                value = line.birim,
                onValueChange = {},
                readOnly = true,
                singleLine = true,
                textStyle = MaterialTheme.typography.bodySmall,
                modifier = Modifier.fillMaxWidth().clickable { expanded = true },
                shape = AppShapes.small,
                colors = compactFieldColors()
            )
            DropdownMenu(expanded = expanded, onDismissRequest = { expanded = false }) {
                birimler.forEach { option ->
                    DropdownMenuItem(
                        text = { Text(option) },
                        onClick = {
                            onBirimChange(option)
                            expanded = false
                        }
                    )
                }
            }
        }
        IconButton(
            onClick = onDelete,
            enabled = canDelete,
            modifier = Modifier.size(36.dp)
        ) {
            Icon(
                Icons.Rounded.Delete,
                contentDescription = "Satır Sil",
                tint = if (canDelete) AppColors.Danger else AppColors.TextSecondary,
                modifier = Modifier.size(20.dp)
            )
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

@Composable
private fun compactFieldColors() = OutlinedTextFieldDefaults.colors(
    focusedBorderColor = AppColors.Primary,
    unfocusedBorderColor = AppColors.Border,
    focusedContainerColor = AppColors.Surface,
    unfocusedContainerColor = AppColors.Surface,
    focusedTextColor = AppColors.TextPrimary,
    unfocusedTextColor = AppColors.TextPrimary
)

package com.satinalmapro.android.ui.procurement

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.heightIn
import androidx.compose.material3.Surface
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
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
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.ExposedDropdownMenuBox
import androidx.compose.material3.ExposedDropdownMenuDefaults
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.MenuAnchorType
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.OutlinedTextFieldDefaults
import androidx.compose.material3.SegmentedButton
import androidx.compose.material3.SegmentedButtonDefaults
import androidx.compose.material3.SingleChoiceSegmentedButtonRow
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
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
import com.satinalmapro.android.ui.components.AppScreenContent
import com.satinalmapro.android.ui.components.AppSearchField
import com.satinalmapro.android.ui.theme.AppColors
import com.satinalmapro.android.ui.theme.AppShapes
import com.satinalmapro.android.ui.theme.AppSpacing
import com.satinalmapro.android.core.model.UygulamaAyarlar
import com.satinalmapro.android.core.roles.TalepTurleri
import com.satinalmapro.android.core.roles.TalepYetkileri

private val talepTurEtiketleri = listOf("Normal", "Öncelikli", "Acil")

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun NewRequestScreen(viewModel: AppViewModel, editTalepId: String? = null) {
    val editTalep by viewModel.talepById(editTalepId.orEmpty()).collectAsState(initial = null)
    val isEdit = editTalepId != null
    val user by viewModel.user.collectAsState()
    val birimler by viewModel.malzemeBirimleri.collectAsState()
    val lines = remember { mutableStateListOf(RequestDraftLine()) }
    var description by remember { mutableStateOf("") }
    var priorityIndex by remember { mutableIntStateOf(0) }
    var localError by remember { mutableStateOf<String?>(null) }
    var silOnay by remember { mutableStateOf(false) }
    val site = editTalep?.santiyeAdi?.takeIf { it.isNotBlank() } ?: user?.site.orEmpty()

    LaunchedEffect(editTalep) {
        editTalep?.let { talep ->
            description = talep.talepAciklamasi
            priorityIndex = TalepTurleri.TUM.indexOf(talep.talepTuru).coerceAtLeast(0)
            lines.clear()
            lines.addAll(
                talep.kalemler.map {
                    RequestDraftLine(malzeme = it.malzeme, miktar = it.miktar.toString(), birim = it.birim)
                }.ifEmpty { listOf(RequestDraftLine()) }
            )
        }
    }

    LaunchedEffect(user?.site) {
        if (isEdit) return@LaunchedEffect
        viewModel.loadDraft()?.let { draft ->
            description = draft.aciklama
            priorityIndex = draft.oncelikIndex.coerceIn(0, 2)
            lines.clear()
            lines.addAll(draft.lines.ifEmpty { listOf(RequestDraftLine()) })
        }
    }

    LaunchedEffect(description, priorityIndex, lines.size) {
        if (isEdit) return@LaunchedEffect
        viewModel.saveDraft(
            RequestDraft(
                site = user?.site.orEmpty(),
                aciklama = description,
                oncelikIndex = priorityIndex,
                lines = lines.toList()
            )
        )
    }

    val completeLines = lines.filter { it.malzeme.isNotBlank() && it.miktar.isNotBlank() }
    val submitError by viewModel.submitError.collectAsState()
    val loading by viewModel.loading.collectAsState()
    val talepTuru = TalepTurleri.fromIndex(priorityIndex)
    val talepSilinebilir = when {
        isEdit && editTalep != null ->
            TalepYetkileri.talepSilebilir(user?.role, editTalep!!, user?.uid, user?.fullName)
        !isEdit -> true
        else -> false
    }
    val yenidenGonderMetni = isEdit && editTalep != null &&
        TalepYetkileri.duzenlemeSonrasiYenidenGonder(editTalep!!)

    if (silOnay) {
        AlertDialog(
            onDismissRequest = { silOnay = false },
            title = { Text("Talebi Sil") },
            text = {
                Text(
                    if (isEdit) "Bu talebi silmek istiyor musunuz? Bu işlem geri alınamaz."
                    else "Girdiğiniz talep bilgileri silinecek. Devam etmek istiyor musunuz?"
                )
            },
            confirmButton = {
                TextButton(
                    onClick = {
                        silOnay = false
                        if (isEdit) {
                            val id = editTalepId!!
                            viewModel.talepSil(id) { viewModel.navigate("taleplerim") }
                        } else {
                            viewModel.clearDraft()
                            viewModel.navigate("taleplerim")
                        }
                    },
                    enabled = !loading
                ) { Text("Evet", color = AppColors.Danger) }
            },
            dismissButton = {
                TextButton(onClick = { silOnay = false }) { Text("Hayır") }
            }
        )
    }

    Column(Modifier.fillMaxSize().verticalScroll(rememberScrollState())) {
    AppScreenContent {
        Text(
            if (isEdit) "Talep Düzenle" else "Malzeme kalemleri",
            style = MaterialTheme.typography.titleMedium,
            color = AppColors.TextPrimary
        )
        Spacer(Modifier.height(8.dp))

        RequestLineHeader()
        HorizontalDivider(color = AppColors.Border)

        lines.forEachIndexed { index, line ->
            RequestLineRow(
                line = line,
                birimler = birimler,
                suggestions = viewModel.materialSuggestions(line.malzeme),
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
        Text("Talep Türü", style = MaterialTheme.typography.titleSmall, color = AppColors.TextPrimary)
        Spacer(Modifier.height(6.dp))
        SingleChoiceSegmentedButtonRow(modifier = Modifier.fillMaxWidth()) {
            talepTurEtiketleri.forEachIndexed { index, label ->
                SegmentedButton(
                    selected = priorityIndex == index,
                    onClick = { priorityIndex = index },
                    shape = SegmentedButtonDefaults.itemShape(index, talepTurEtiketleri.size),
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

        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.spacedBy(8.dp)
        ) {
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
                    if (isEdit) {
                        val id = editTalepId!!
                        viewModel.talepGuncelle(id, site, description, talepTuru, payload) {
                            viewModel.navigate("talep-detay?id=$id")
                        }
                    } else {
                        viewModel.submitRequest(site, description, talepTuru, payload) {
                            viewModel.navigate("taleplerim")
                        }
                    }
                },
                modifier = Modifier.weight(1f).height(48.dp),
                shape = AppShapes.medium,
                colors = ButtonDefaults.buttonColors(containerColor = AppColors.Primary),
                enabled = !loading && (!isEdit || editTalep != null)
            ) {
                Text(
                    when {
                        loading -> "Kaydediliyor..."
                        isEdit && yenidenGonderMetni -> "Kaydet ve Yönetime Gönder"
                        isEdit -> "Değişiklikleri Kaydet"
                        else -> "Talebi Gönder"
                    },
                    style = MaterialTheme.typography.labelLarge
                )
            }

            if (talepSilinebilir) {
                OutlinedButton(
                    onClick = { silOnay = true },
                    modifier = Modifier.weight(1f).height(48.dp),
                    shape = AppShapes.medium,
                    enabled = !loading && (!isEdit || editTalep != null)
                ) {
                    Text("Talebi Sil", color = AppColors.Danger, style = MaterialTheme.typography.labelLarge)
                }
            }
        }
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
    suggestions: List<String>,
    canDelete: Boolean,
    onMalzemeChange: (String) -> Unit,
    onMiktarChange: (String) -> Unit,
    onBirimChange: (String) -> Unit,
    onDelete: () -> Unit
) {
    var malzemeExpanded by remember(line.malzeme) { mutableStateOf(false) }
    Row(
        Modifier.fillMaxWidth().padding(vertical = 4.dp),
        horizontalArrangement = Arrangement.spacedBy(6.dp),
        verticalAlignment = Alignment.Top
    ) {
        Column(Modifier.weight(1.4f)) {
            OutlinedTextField(
                value = line.malzeme,
                onValueChange = {
                    onMalzemeChange(it)
                    malzemeExpanded = it.isNotBlank()
                },
                modifier = Modifier.fillMaxWidth(),
                placeholder = { Text("Malzeme") },
                singleLine = true,
                textStyle = MaterialTheme.typography.bodySmall,
                shape = AppShapes.small,
                colors = compactFieldColors()
            )
            if (malzemeExpanded && suggestions.isNotEmpty()) {
                Surface(
                    modifier = Modifier.fillMaxWidth(),
                    shape = AppShapes.small,
                    color = AppColors.Surface,
                    shadowElevation = 4.dp
                ) {
                    LazyColumn(modifier = Modifier.heightIn(max = 120.dp)) {
                        items(suggestions) { item ->
                            Text(
                                text = item,
                                modifier = Modifier
                                    .fillMaxWidth()
                                    .clickable {
                                        onMalzemeChange(item)
                                        malzemeExpanded = false
                                    }
                                    .padding(horizontal = 12.dp, vertical = 8.dp),
                                style = MaterialTheme.typography.bodySmall,
                                color = AppColors.TextPrimary
                            )
                            HorizontalDivider(color = AppColors.Border)
                        }
                    }
                }
            }
        }
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
            RequestBirimDropdown(
                value = line.birim,
                options = birimler,
                onSelect = onBirimChange,
                modifier = Modifier.fillMaxWidth()
            )
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

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun RequestBirimDropdown(
    value: String,
    options: List<String>,
    onSelect: (String) -> Unit,
    modifier: Modifier = Modifier
) {
    var expanded by remember { mutableStateOf(false) }
    val secenekler = options.ifEmpty { UygulamaAyarlar.varsayilanBirimler }
    val gosterim = value.ifBlank { "Adet" }

    ExposedDropdownMenuBox(
        expanded = expanded,
        onExpandedChange = { expanded = it },
        modifier = modifier
    ) {
        OutlinedTextField(
            value = gosterim,
            onValueChange = {},
            readOnly = true,
            singleLine = true,
            textStyle = MaterialTheme.typography.bodySmall,
            modifier = Modifier
                .fillMaxWidth()
                .menuAnchor(MenuAnchorType.PrimaryNotEditable, enabled = true),
            shape = AppShapes.small,
            colors = compactFieldColors(),
            trailingIcon = { ExposedDropdownMenuDefaults.TrailingIcon(expanded = expanded) }
        )
        ExposedDropdownMenu(
            expanded = expanded,
            onDismissRequest = { expanded = false }
        ) {
            secenekler.forEach { option ->
                DropdownMenuItem(
                    text = { Text(option) },
                    onClick = {
                        onSelect(option)
                        expanded = false
                    },
                    contentPadding = ExposedDropdownMenuDefaults.ItemContentPadding
                )
            }
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

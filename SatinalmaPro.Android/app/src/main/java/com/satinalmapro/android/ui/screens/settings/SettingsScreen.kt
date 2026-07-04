package com.satinalmapro.android.ui.screens.settings

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
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.rounded.Add
import androidx.compose.material.icons.rounded.Delete
import androidx.compose.material.icons.rounded.Refresh
import androidx.compose.material3.Button
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.ScrollableTabRow
import androidx.compose.material3.Switch
import androidx.compose.material3.Tab
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
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.satinalmapro.android.core.model.ManagedUser
import com.satinalmapro.android.core.roles.KullaniciRolleri
import com.satinalmapro.android.ui.AppViewModel
import com.satinalmapro.android.ui.components.AppCard
import com.satinalmapro.android.ui.theme.AppColors
import com.satinalmapro.android.ui.theme.AppShapes

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun SettingsScreen(viewModel: AppViewModel) {
    val users by viewModel.settingsUsers.collectAsState()
    val birimler by viewModel.malzemeBirimleri.collectAsState()
    val kategoriler by viewModel.malzemeKategorileri.collectAsState()
    val loading by viewModel.loading.collectAsState()
    val settingsMessage by viewModel.settingsMessage.collectAsState()
    val settingsError by viewModel.settingsError.collectAsState()
    var tab by remember { mutableIntStateOf(0) }

    LaunchedEffect(Unit) { viewModel.loadSettings() }

    Column(Modifier.fillMaxSize()) {
        ScrollableTabRow(selectedTabIndex = tab, edgePadding = 16.dp) {
            Tab(selected = tab == 0, onClick = { tab = 0 }, text = { Text("Kullanıcılar") })
            Tab(selected = tab == 1, onClick = { tab = 1 }, text = { Text("Birim Terimleri") })
            Tab(selected = tab == 2, onClick = { tab = 2 }, text = { Text("Kategoriler") })
        }

        settingsMessage?.let {
            Text(
                it,
                modifier = Modifier.padding(horizontal = 16.dp, vertical = 8.dp),
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
            0 -> UsersTab(users, loading, viewModel)
            1 -> TermListTab(
                title = "Malzeme birim terimleri",
                hint = "Yeni birim",
                items = birimler,
                loading = loading,
                onAdd = viewModel::addBirim,
                onRemove = viewModel::removeBirim
            )
            2 -> TermListTab(
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
private fun UsersTab(users: List<ManagedUser>, loading: Boolean, viewModel: AppViewModel) {
    var selected by remember { mutableStateOf<ManagedUser?>(null) }
    var email by remember { mutableStateOf("") }
    var password by remember { mutableStateOf("") }
    var fullName by remember { mutableStateOf("") }
    var role by remember { mutableStateOf(KullaniciRolleri.SATINALMA) }
    var site by remember { mutableStateOf("") }
    var active by remember { mutableStateOf(true) }
    var creatingNew by remember { mutableStateOf(true) }

    fun resetForm(forNew: Boolean) {
        creatingNew = forNew
        selected = null
        email = ""
        password = ""
        fullName = ""
        role = KullaniciRolleri.SATINALMA
        site = ""
        active = true
    }

    fun fillForm(user: ManagedUser) {
        creatingNew = false
        selected = user
        email = user.email
        password = ""
        fullName = user.fullName
        role = user.role
        site = user.site
        active = user.active
    }

    Column(Modifier.fillMaxSize()) {
        Row(
            Modifier.fillMaxWidth().padding(horizontal = 16.dp, vertical = 8.dp),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Text("${users.size} kullanıcı", style = MaterialTheme.typography.bodyMedium, color = AppColors.TextSecondary)
            Row {
                IconButton(onClick = { viewModel.loadSettings() }) {
                    Icon(Icons.Rounded.Refresh, contentDescription = "Yenile")
                }
                OutlinedButton(onClick = { resetForm(true) }) {
                    Text("Yeni")
                }
            }
        }

        LazyColumn(
            modifier = Modifier.weight(1f).padding(horizontal = 16.dp),
            verticalArrangement = Arrangement.spacedBy(8.dp)
        ) {
            items(users, key = { it.uid }) { user ->
                AppCard(onClick = { fillForm(user) }) {
                    Row(
                        Modifier.fillMaxWidth().padding(16.dp),
                        horizontalArrangement = Arrangement.SpaceBetween,
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Column(Modifier.weight(1f)) {
                            Text(user.fullName, fontWeight = FontWeight.SemiBold, color = AppColors.TextPrimary)
                            Text(user.email, style = MaterialTheme.typography.bodySmall, color = AppColors.TextSecondary)
                            Text(
                                "${user.role}${user.site.takeIf { it.isNotBlank() }?.let { " · $it" } ?: ""}",
                                style = MaterialTheme.typography.labelMedium,
                                color = AppColors.TextSecondary
                            )
                        }
                        Text(
                            if (user.active) "Aktif" else "Pasif",
                            style = MaterialTheme.typography.labelMedium,
                            color = if (user.active) AppColors.Primary else AppColors.Danger
                        )
                    }
                }
            }
        }

        Column(
            Modifier
                .fillMaxWidth()
                .verticalScroll(rememberScrollState())
                .padding(16.dp)
        ) {
            Text(
                if (creatingNew) "Yeni kullanıcı" else "Kullanıcı düzenle",
                style = MaterialTheme.typography.titleMedium,
                color = AppColors.TextPrimary
            )
            Spacer(Modifier.height(8.dp))
            OutlinedTextField(
                value = email,
                onValueChange = { email = it },
                label = { Text("E-posta") },
                modifier = Modifier.fillMaxWidth(),
                enabled = creatingNew,
                singleLine = true
            )
            Spacer(Modifier.height(8.dp))
            if (creatingNew) {
                OutlinedTextField(
                    value = password,
                    onValueChange = { password = it },
                    label = { Text("Şifre (en az 6 karakter)") },
                    modifier = Modifier.fillMaxWidth(),
                    singleLine = true
                )
                Spacer(Modifier.height(8.dp))
            }
            OutlinedTextField(
                value = fullName,
                onValueChange = { fullName = it },
                label = { Text("Ad Soyad") },
                modifier = Modifier.fillMaxWidth(),
                singleLine = true
            )
            Spacer(Modifier.height(8.dp))
            RolePicker(role = role, onRoleChange = { role = it })
            Spacer(Modifier.height(8.dp))
            OutlinedTextField(
                value = site,
                onValueChange = { site = it },
                label = { Text("Şantiye / Saha") },
                modifier = Modifier.fillMaxWidth(),
                singleLine = true
            )
            Spacer(Modifier.height(8.dp))
            Row(
                Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text("Aktif hesap", color = AppColors.TextPrimary)
                Switch(checked = active, onCheckedChange = { active = it })
            }
            Spacer(Modifier.height(12.dp))
            Button(
                onClick = {
                    if (creatingNew) {
                        viewModel.createUser(email, password, fullName, role, site, active) {
                            resetForm(true)
                        }
                    } else {
                        val uid = selected?.uid ?: return@Button
                        viewModel.saveUser(
                            ManagedUser(uid, email.trim(), fullName.trim(), role, active, site.trim())
                        ) { resetForm(true) }
                    }
                },
                enabled = !loading,
                modifier = Modifier.fillMaxWidth()
            ) {
                Text(if (creatingNew) "Kullanıcı Oluştur" else "Değişiklikleri Kaydet")
            }
        }
    }
}

@Composable
private fun RolePicker(role: String, onRoleChange: (String) -> Unit) {
    var expanded by remember { mutableStateOf(false) }
    Box {
        OutlinedTextField(
            value = role,
            onValueChange = {},
            readOnly = true,
            label = { Text("Rol") },
            modifier = Modifier.fillMaxWidth().clickable { expanded = true },
            singleLine = true
        )
        DropdownMenu(expanded = expanded, onDismissRequest = { expanded = false }) {
            KullaniciRolleri.TUM.forEach { option ->
                DropdownMenuItem(
                    text = { Text(option) },
                    onClick = {
                        onRoleChange(option)
                        expanded = false
                    }
                )
            }
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

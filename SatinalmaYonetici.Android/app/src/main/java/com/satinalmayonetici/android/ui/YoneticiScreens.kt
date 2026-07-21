package com.satinalmayonetici.android.ui

import android.content.Intent
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.ExperimentalLayoutApi
import androidx.compose.foundation.layout.FlowRow
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Button
import androidx.compose.material3.Checkbox
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.ExposedDropdownMenuBox
import androidx.compose.material3.ExposedDropdownMenuDefaults
import androidx.compose.material3.FilterChip
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Tab
import androidx.compose.material3.TabRow
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.unit.dp
import androidx.core.content.FileProvider
import com.satinalmayonetici.android.data.ModulKatalogu
import com.satinalmayonetici.android.data.TenantRow
import com.satinalmayonetici.android.data.UserRow

private val lisansSecenekleri = listOf(
    "deneme" to "15 günlük deneme",
    "yillik" to "1 yıllık",
    "2yil" to "2 yıllık",
    "3yil" to "3 yıllık",
    "manuel" to "Manuel gün"
)

@Composable
fun YoneticiRoot(vm: YoneticiViewModel) {
    when (vm.screen) {
        Screen.Login -> LoginScreen(vm)
        Screen.FirmList -> FirmListScreen(vm)
        Screen.FirmDetail -> FirmDetailScreen(vm)
        Screen.UserEdit -> UserEditScreen(vm)
    }
}

@Composable
fun LoginScreen(vm: YoneticiViewModel) {
    var email by remember { mutableStateOf("") }
    var password by remember { mutableStateOf("") }
    var rememberMe by remember { mutableStateOf(true) }

    Column(
        Modifier.fillMaxSize().padding(24.dp),
        verticalArrangement = Arrangement.Center
    ) {
        Text("Satınalma Yönetici", style = MaterialTheme.typography.headlineMedium, fontWeight = FontWeight.Bold)
        Text("Platform yöneticisi — masaüstü ile aynı yetkiler", style = MaterialTheme.typography.bodyMedium)
        Spacer(Modifier.height(24.dp))
        OutlinedTextField(email, { email = it }, label = { Text("E-posta") }, singleLine = true, modifier = Modifier.fillMaxWidth())
        Spacer(Modifier.height(12.dp))
        OutlinedTextField(
            password, { password = it }, label = { Text("Şifre") }, singleLine = true,
            visualTransformation = PasswordVisualTransformation(), modifier = Modifier.fillMaxWidth()
        )
        Row(verticalAlignment = Alignment.CenterVertically) {
            Checkbox(rememberMe, { rememberMe = it })
            Text("Beni hatırla")
        }
        vm.loginError?.let { Text(it, color = MaterialTheme.colorScheme.error) }
        Spacer(Modifier.height(16.dp))
        Button(
            onClick = { vm.login(email, password, rememberMe) },
            enabled = !vm.busy && email.isNotBlank() && password.isNotBlank(),
            modifier = Modifier.fillMaxWidth()
        ) {
            if (vm.busy) CircularProgressIndicator(Modifier.height(20.dp)) else Text("Giriş yap")
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun FirmListScreen(vm: YoneticiViewModel) {
    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("Firmalar") },
                actions = {
                    TextButton(onClick = { vm.newFirm() }) { Text("Yeni") }
                    TextButton(onClick = { vm.reloadTenants() }) { Text("Yenile") }
                    TextButton(onClick = { vm.detachSelf() }) { Text("Ayır") }
                    TextButton(onClick = { vm.logout() }) { Text("Çıkış") }
                }
            )
        }
    ) { pad ->
        Column(Modifier.padding(pad).fillMaxSize().padding(16.dp)) {
            vm.message?.let { Text(it, color = MaterialTheme.colorScheme.primary); Spacer(Modifier.height(8.dp)) }
            if (vm.busy && vm.tenants.isEmpty()) {
                CircularProgressIndicator(Modifier.align(Alignment.CenterHorizontally))
                return@Column
            }
            LazyColumn(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                items(vm.tenants, key = { it.id }) { t ->
                    Column(
                        Modifier.fillMaxWidth().clickable { vm.openFirm(t) }.padding(12.dp)
                    ) {
                        Text(t.ad, fontWeight = FontWeight.SemiBold)
                        Text("${t.kod} · ${lisansLabel(t.lisansTipi)} · kalan ${t.lisansKalanGun ?: "?"} gün")
                        if (t.lisansSuresiDoldu || !t.aktif) {
                            Text("Pasif / süresi dolmuş", color = MaterialTheme.colorScheme.error)
                        }
                    }
                }
            }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun FirmDetailScreen(vm: YoneticiViewModel) {
    var tab by remember { mutableIntStateOf(0) }
    val context = LocalContext.current
    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text(vm.selected?.ad?.ifBlank { "Firma" } ?: "Firma") },
                navigationIcon = {
                    TextButton(onClick = { vm.goFirmList() }) { Text("←") }
                }
            )
        }
    ) { pad ->
        Column(Modifier.padding(pad).fillMaxSize()) {
            TabRow(selectedTabIndex = tab) {
                Tab(selected = tab == 0, onClick = { tab = 0 }, text = { Text("Firma / Lisans") })
                Tab(selected = tab == 1, onClick = { tab = 1 }, text = { Text("Kullanıcılar") })
            }
            when (tab) {
                0 -> FirmFormTab(vm) { file ->
                    val uri = FileProvider.getUriForFile(
                        context, "${context.packageName}.fileprovider", file
                    )
                    val intent = Intent(Intent.ACTION_SEND).apply {
                        type = "application/zip"
                        putExtra(Intent.EXTRA_STREAM, uri)
                        addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)
                    }
                    context.startActivity(Intent.createChooser(intent, "Yedeği paylaş"))
                }
                else -> UsersTab(vm)
            }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun FirmFormTab(vm: YoneticiViewModel, onBackupFile: (java.io.File) -> Unit) {
    var tipExpanded by remember { mutableStateOf(false) }
    var confirm by remember { mutableStateOf("") }

    Column(
        Modifier.fillMaxSize().verticalScroll(rememberScrollState()).padding(16.dp)
    ) {
        OutlinedTextField(vm.firmaKod, { vm.firmaKod = it }, label = { Text("Firma kodu") }, modifier = Modifier.fillMaxWidth())
        Spacer(Modifier.height(8.dp))
        OutlinedTextField(vm.firmaAd, { vm.firmaAd = it }, label = { Text("Firma adı") }, modifier = Modifier.fillMaxWidth())
        Spacer(Modifier.height(8.dp))
        Row(verticalAlignment = Alignment.CenterVertically) {
            Checkbox(vm.firmaAktif, { vm.firmaAktif = it })
            Text("Aktif")
        }

        ExposedDropdownMenuBox(tipExpanded, { tipExpanded = it }) {
            OutlinedTextField(
                value = lisansLabel(vm.lisansTipi),
                onValueChange = {},
                readOnly = true,
                label = { Text("Lisans tipi") },
                trailingIcon = { ExposedDropdownMenuDefaults.TrailingIcon(tipExpanded) },
                modifier = Modifier.menuAnchor().fillMaxWidth()
            )
            ExposedDropdownMenu(tipExpanded, { tipExpanded = false }) {
                lisansSecenekleri.forEach { (tag, label) ->
                    DropdownMenuItem(text = { Text(label) }, onClick = {
                        vm.lisansTipi = tag
                        tipExpanded = false
                    })
                }
            }
        }
        Spacer(Modifier.height(8.dp))
        OutlinedTextField(
            vm.manuelGun, { vm.manuelGun = it.filter(Char::isDigit).take(4) },
            label = { Text("Manuel / eklenecek gün") }, modifier = Modifier.fillMaxWidth()
        )

        Spacer(Modifier.height(12.dp))
        Button(onClick = { vm.saveFirm() }, enabled = !vm.busy, modifier = Modifier.fillMaxWidth()) {
            Text("Firmayı kaydet")
        }
        Spacer(Modifier.height(8.dp))
        OutlinedButton(onClick = { vm.saveFirm(lisansYenile = true) }, enabled = !vm.busy, modifier = Modifier.fillMaxWidth()) {
            Text("Lisansı yenile")
        }
        Spacer(Modifier.height(8.dp))
        OutlinedButton(onClick = { vm.saveFirm(gunEkleModu = true) }, enabled = !vm.busy, modifier = Modifier.fillMaxWidth()) {
            Text("Gün ekle")
        }

        Spacer(Modifier.height(16.dp))
        Text("Yedek / sıfırla / sil", fontWeight = FontWeight.SemiBold)
        Spacer(Modifier.height(8.dp))
        OutlinedButton(onClick = { vm.backupFirm(onBackupFile) }, enabled = !vm.busy, modifier = Modifier.fillMaxWidth()) {
            Text("Yedek al")
        }
        Spacer(Modifier.height(8.dp))
        OutlinedTextField(
            vm.restorePath, { vm.restorePath = it },
            label = { Text("Yedek Storage yolu") }, modifier = Modifier.fillMaxWidth()
        )
        Spacer(Modifier.height(8.dp))
        OutlinedButton(onClick = { vm.restoreFirm() }, enabled = !vm.busy, modifier = Modifier.fillMaxWidth()) {
            Text("Yedekten yükle")
        }
        Spacer(Modifier.height(8.dp))
        OutlinedTextField(confirm, { confirm = it }, label = { Text("Onay için firma kodu") }, modifier = Modifier.fillMaxWidth())
        Spacer(Modifier.height(8.dp))
        Button(onClick = { vm.resetFirm(confirm) }, enabled = !vm.busy, modifier = Modifier.fillMaxWidth()) {
            Text("Verileri sıfırla")
        }
        Spacer(Modifier.height(8.dp))
        Button(
            onClick = { vm.deleteFirm(confirm) },
            enabled = !vm.busy && !vm.selected?.id.isNullOrBlank(),
            modifier = Modifier.fillMaxWidth()
        ) {
            Text("Firmayı sil")
        }

        vm.message?.let {
            Spacer(Modifier.height(12.dp))
            Text(it)
        }
        if (vm.busy) {
            Spacer(Modifier.height(12.dp))
            CircularProgressIndicator()
        }
    }
}

@Composable
private fun UsersTab(vm: YoneticiViewModel) {
    Column(Modifier.fillMaxSize().padding(16.dp)) {
        Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(8.dp)) {
            Button(onClick = { vm.newUser() }, enabled = !vm.busy) { Text("Yeni kullanıcı") }
            OutlinedButton(onClick = { vm.importLegacy() }, enabled = !vm.busy) { Text("Eski aktar") }
            OutlinedButton(onClick = { vm.reloadUsers() }, enabled = !vm.busy) { Text("Yenile") }
        }
        vm.message?.let {
            Spacer(Modifier.height(8.dp))
            Text(it)
        }
        Spacer(Modifier.height(8.dp))
        LazyColumn(verticalArrangement = Arrangement.spacedBy(8.dp)) {
            items(vm.users, key = { it.uid }) { u ->
                UserRowCard(u) { vm.openUser(u) }
            }
        }
    }
}

@Composable
private fun UserRowCard(u: UserRow, onClick: () -> Unit) {
    Column(Modifier.fillMaxWidth().clickable(onClick = onClick).padding(12.dp)) {
        Text(u.adSoyad.ifBlank { u.kullaniciAdi }, fontWeight = FontWeight.SemiBold)
        Text("${u.kullaniciAdi} · ${u.rol} · ${if (u.aktif) "Aktif" else "Pasif"}")
        if (u.eposta.isNotBlank()) Text(u.eposta, style = MaterialTheme.typography.bodySmall)
    }
}

@OptIn(ExperimentalMaterial3Api::class, ExperimentalLayoutApi::class)
@Composable
fun UserEditScreen(vm: YoneticiViewModel) {
    var rolExpanded by remember { mutableStateOf(false) }
    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text(if (vm.isNewUser) "Yeni kullanıcı" else "Kullanıcı düzenle") },
                navigationIcon = { TextButton(onClick = { vm.backFromUser() }) { Text("←") } },
                actions = {
                    TextButton(onClick = { vm.applyRoleDefaults() }) { Text("Rol vars.") }
                    TextButton(onClick = { vm.clearPermissions() }) { Text("Temizle") }
                }
            )
        }
    ) { pad ->
        Column(
            Modifier.padding(pad).fillMaxSize().verticalScroll(rememberScrollState()).padding(16.dp)
        ) {
            OutlinedTextField(vm.uAdi, { vm.uAdi = it }, label = { Text("Kullanıcı adı") }, modifier = Modifier.fillMaxWidth())
            Spacer(Modifier.height(8.dp))
            OutlinedTextField(vm.uEposta, { vm.uEposta = it }, label = { Text("E-posta") }, modifier = Modifier.fillMaxWidth())
            Spacer(Modifier.height(8.dp))
            OutlinedTextField(vm.uAdSoyad, { vm.uAdSoyad = it }, label = { Text("Ad soyad") }, modifier = Modifier.fillMaxWidth())
            Spacer(Modifier.height(8.dp))
            ExposedDropdownMenuBox(rolExpanded, { rolExpanded = it }) {
                OutlinedTextField(
                    value = vm.uRol,
                    onValueChange = {},
                    readOnly = true,
                    label = { Text("Rol") },
                    trailingIcon = { ExposedDropdownMenuDefaults.TrailingIcon(rolExpanded) },
                    modifier = Modifier.menuAnchor().fillMaxWidth()
                )
                ExposedDropdownMenu(rolExpanded, { rolExpanded = false }) {
                    ModulKatalogu.roller.forEach { r ->
                        DropdownMenuItem(text = { Text(r) }, onClick = {
                            vm.uRol = r
                            vm.applyRoleDefaults()
                            rolExpanded = false
                        })
                    }
                }
            }
            Spacer(Modifier.height(8.dp))
            OutlinedTextField(vm.uSaha, { vm.uSaha = it }, label = { Text("Saha (opsiyonel)") }, modifier = Modifier.fillMaxWidth())
            Spacer(Modifier.height(8.dp))
            OutlinedTextField(
                vm.uSifre, { vm.uSifre = it },
                label = { Text(if (vm.isNewUser) "Şifre (zorunlu)" else "Yeni şifre (opsiyonel)") },
                visualTransformation = PasswordVisualTransformation(),
                modifier = Modifier.fillMaxWidth()
            )
            Row(verticalAlignment = Alignment.CenterVertically) {
                Checkbox(vm.uAktif, { vm.uAktif = it })
                Text("Aktif")
            }

            Spacer(Modifier.height(12.dp))
            Text("Modül yetkileri", fontWeight = FontWeight.SemiBold)
            Spacer(Modifier.height(8.dp))

            val yetkiler = if (vm.uYetkiler.size == ModulKatalogu.tum.size) vm.uYetkiler
            else ModulKatalogu.tum.map { m -> vm.uYetkiler.find { it.modul == m } ?: com.satinalmayonetici.android.data.ModulYetki(m) }

            yetkiler.forEach { y ->
                Column(Modifier.fillMaxWidth().padding(vertical = 6.dp)) {
                    Text(y.modul, fontWeight = FontWeight.Medium)
                    Row {
                        Row(verticalAlignment = Alignment.CenterVertically) {
                            Checkbox(
                                checked = y.okuma,
                                onCheckedChange = { checked ->
                                    vm.updateYetki(y.modul) { it.copy(okuma = checked) }
                                }
                            )
                            Text("Okuma")
                        }
                        Row(verticalAlignment = Alignment.CenterVertically) {
                            val yazmaOk = ModulKatalogu.yazmaAtanabilir(vm.uRol, y.modul)
                            Checkbox(
                                checked = y.yazma,
                                enabled = yazmaOk,
                                onCheckedChange = { checked ->
                                    vm.updateYetki(y.modul) { it.copy(yazma = checked) }
                                }
                            )
                            Text("Yazma")
                        }
                    }
                    val sekmeler = ModulKatalogu.sekmeleriAl(y.modul)
                    if (sekmeler.isNotEmpty() && y.okuma) {
                        FlowRow(horizontalArrangement = Arrangement.spacedBy(4.dp)) {
                            sekmeler.forEach { sekme ->
                                val selected = y.sekmeler.isEmpty() || y.sekmeler.any { it.equals(sekme, true) }
                                FilterChip(
                                    selected = selected,
                                    onClick = {
                                        vm.updateYetki(y.modul) { cur ->
                                            val current = if (cur.sekmeler.isEmpty()) sekmeler.toSet() else cur.sekmeler.toSet()
                                            val next = if (sekme in current) current - sekme else current + sekme
                                            cur.copy(sekmeler = if (next.size == sekmeler.size) sekmeler else next.toList())
                                        }
                                    },
                                    label = { Text(sekme) }
                                )
                            }
                        }
                    }
                }
            }

            Spacer(Modifier.height(16.dp))
            Button(onClick = { vm.saveUser() }, enabled = !vm.busy, modifier = Modifier.fillMaxWidth()) {
                Text("Kullanıcıyı kaydet")
            }
            if (!vm.isNewUser && !vm.editingUser?.uid.isNullOrBlank()) {
                Spacer(Modifier.height(8.dp))
                OutlinedButton(onClick = { vm.deleteUser() }, enabled = !vm.busy, modifier = Modifier.fillMaxWidth()) {
                    Text("Kullanıcıyı sil")
                }
            }
            vm.message?.let {
                Spacer(Modifier.height(12.dp))
                Text(it)
            }
            if (vm.busy) CircularProgressIndicator()
        }
    }
}

private fun lisansLabel(tip: String): String =
    lisansSecenekleri.firstOrNull { it.first == tip }?.second ?: tip


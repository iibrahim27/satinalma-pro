package com.satinalmayonetici.android.ui

import android.app.Activity
import android.content.Intent
import androidx.activity.compose.BackHandler
import androidx.compose.animation.AnimatedVisibility
import androidx.compose.animation.core.FastOutSlowInEasing
import androidx.compose.animation.core.MutableTransitionState
import androidx.compose.animation.core.tween
import androidx.compose.animation.fadeIn
import androidx.compose.animation.fadeOut
import androidx.compose.animation.slideInVertically
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.ExperimentalLayoutApi
import androidx.compose.foundation.layout.FlowRow
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.automirrored.filled.Logout
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.Business
import androidx.compose.material.icons.filled.CheckCircle
import androidx.compose.material.icons.filled.Refresh
import androidx.compose.material.icons.filled.SystemUpdate
import androidx.compose.material.icons.filled.Warning
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.Checkbox
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.ExposedDropdownMenuBox
import androidx.compose.material3.ExposedDropdownMenuDefaults
import androidx.compose.material3.FilterChip
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.LinearProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Surface
import androidx.compose.material3.Tab
import androidx.compose.material3.TabRow
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TopAppBar
import androidx.compose.material3.TopAppBarDefaults
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.core.content.FileProvider
import com.satinalmayonetici.android.data.ModulKatalogu
import com.satinalmayonetici.android.data.ModulYetki
import com.satinalmayonetici.android.data.TenantRow
import com.satinalmayonetici.android.data.UserRow
import com.satinalmayonetici.android.ui.theme.YoneticiColors

private val lisansSecenekleri = listOf(
    "deneme" to "15 günlük deneme",
    "yillik" to "1 yıllık",
    "2yil" to "2 yıllık",
    "3yil" to "3 yıllık",
    "manuel" to "Manuel gün"
)

@Composable
fun YoneticiRoot(vm: YoneticiViewModel) {
    val activity = LocalContext.current as? Activity
    BackHandler {
        if (!vm.handleSystemBack()) {
            activity?.moveTaskToBack(true)
        }
    }

    when (vm.screen) {
        Screen.Login -> LoginScreen(vm)
        Screen.FirmList -> FirmListScreen(vm)
        Screen.FirmDetail -> FirmDetailScreen(vm)
        Screen.UserEdit -> UserEditScreen(vm)
    }

    vm.pendingUpdate?.let { manifest ->
        if (vm.showUpdatePanel) {
            UpdateDialog(
                version = manifest.version,
                build = manifest.build,
                notes = manifest.notes,
                progress = vm.updateProgress,
                message = vm.updateMessage,
                error = vm.updateError,
                onUpdate = { vm.startUpdateDownload() },
                onDismiss = { vm.dismissUpdateDialog() }
            )
        }
    }
}

@Composable
fun LoginScreen(vm: YoneticiViewModel) {
    var email by remember { mutableStateOf("") }
    var password by remember { mutableStateOf("") }
    var rememberMe by remember { mutableStateOf(true) }
    val visible = remember {
        MutableTransitionState(false).apply { targetState = true }
    }

    Box(
        Modifier
            .fillMaxSize()
            .background(YoneticiColors.LoginGradient)
    ) {
        AnimatedVisibility(
            visibleState = visible,
            enter = fadeIn(tween(500)) + slideInVertically(
                animationSpec = tween(650, easing = FastOutSlowInEasing),
                initialOffsetY = { it / 8 }
            ),
            exit = fadeOut(),
            modifier = Modifier
                .align(Alignment.Center)
                .fillMaxWidth()
                .padding(horizontal = 24.dp)
        ) {
            Card(
                shape = RoundedCornerShape(24.dp),
                colors = CardDefaults.cardColors(containerColor = Color.White),
                elevation = CardDefaults.cardElevation(defaultElevation = 10.dp),
                modifier = Modifier.fillMaxWidth()
            ) {
                Column(
                    Modifier.padding(horizontal = 24.dp, vertical = 28.dp),
                    horizontalAlignment = Alignment.CenterHorizontally
                ) {
                    Box(
                        Modifier
                            .size(64.dp)
                            .clip(CircleShape)
                            .background(YoneticiColors.Teal.copy(alpha = 0.12f)),
                        contentAlignment = Alignment.Center
                    ) {
                        Icon(
                            Icons.Default.Business,
                            contentDescription = null,
                            tint = YoneticiColors.Teal,
                            modifier = Modifier.size(34.dp)
                        )
                    }
                    Spacer(Modifier.height(16.dp))
                    Text(
                        "Satınalma Yönetici",
                        style = MaterialTheme.typography.headlineMedium,
                        color = YoneticiColors.Ink,
                        fontWeight = FontWeight.Bold
                    )
                    Spacer(Modifier.height(6.dp))
                    Text(
                        "Platform yönetimi · lisans · firma & kullanıcı",
                        style = MaterialTheme.typography.bodyMedium,
                        color = YoneticiColors.Slate,
                        textAlign = TextAlign.Center
                    )
                    Spacer(Modifier.height(24.dp))
                    OutlinedTextField(
                        email, { email = it },
                        label = { Text("E-posta") },
                        singleLine = true,
                        modifier = Modifier.fillMaxWidth(),
                        shape = RoundedCornerShape(14.dp)
                    )
                    Spacer(Modifier.height(12.dp))
                    OutlinedTextField(
                        password, { password = it },
                        label = { Text("Şifre") },
                        singleLine = true,
                        visualTransformation = PasswordVisualTransformation(),
                        modifier = Modifier.fillMaxWidth(),
                        shape = RoundedCornerShape(14.dp)
                    )
                    Row(
                        Modifier.fillMaxWidth(),
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Checkbox(rememberMe, { rememberMe = it })
                        Text("Beni hatırla", style = MaterialTheme.typography.bodyMedium)
                    }
                    vm.loginError?.let {
                        Text(it, color = MaterialTheme.colorScheme.error, style = MaterialTheme.typography.bodyMedium)
                        Spacer(Modifier.height(8.dp))
                    }
                    Spacer(Modifier.height(8.dp))
                    Button(
                        onClick = { vm.login(email, password, rememberMe) },
                        enabled = !vm.busy && email.isNotBlank() && password.isNotBlank(),
                        modifier = Modifier
                            .fillMaxWidth()
                            .height(52.dp),
                        shape = RoundedCornerShape(14.dp),
                        colors = ButtonDefaults.buttonColors(containerColor = YoneticiColors.Teal)
                    ) {
                        if (vm.busy) {
                            CircularProgressIndicator(
                                Modifier.size(22.dp),
                                color = Color.White,
                                strokeWidth = 2.dp
                            )
                        } else {
                            Text("Giriş yap", fontWeight = FontWeight.SemiBold)
                        }
                    }
                    Spacer(Modifier.height(12.dp))
                    Text(
                        vm.appVersionLabel,
                        style = MaterialTheme.typography.labelMedium,
                        color = YoneticiColors.Slate.copy(alpha = 0.7f)
                    )
                }
            }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun FirmListScreen(vm: YoneticiViewModel) {
    Scaffold(
        containerColor = YoneticiColors.Mist,
        topBar = {
            TopAppBar(
                title = {
                    Column {
                        Text("Firmalar", fontWeight = FontWeight.Bold)
                        Text(
                            vm.session?.email.orEmpty().ifBlank { "Platform yöneticisi" },
                            style = MaterialTheme.typography.bodySmall,
                            color = YoneticiColors.Slate
                        )
                    }
                },
                actions = {
                    IconButton(onClick = { vm.newFirm() }) {
                        Icon(Icons.Default.Add, contentDescription = "Yeni firma")
                    }
                    IconButton(onClick = { vm.reloadTenants() }) {
                        Icon(Icons.Default.Refresh, contentDescription = "Yenile")
                    }
                    IconButton(onClick = { vm.logout() }) {
                        Icon(Icons.AutoMirrored.Filled.Logout, contentDescription = "Çıkış")
                    }
                },
                colors = TopAppBarDefaults.topAppBarColors(containerColor = Color.White)
            )
        }
    ) { pad ->
        LazyColumn(
            Modifier
                .padding(pad)
                .fillMaxSize()
                .padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            item {
                UpdateCard(vm)
            }
            vm.message?.let { msg ->
                item {
                    Text(msg, color = MaterialTheme.colorScheme.primary)
                }
            }
            if (vm.busy && vm.tenants.isEmpty()) {
                item {
                    Box(Modifier.fillMaxWidth(), contentAlignment = Alignment.Center) {
                        CircularProgressIndicator()
                    }
                }
            }
            items(vm.tenants, key = { it.id }) { t ->
                FirmCard(t) { vm.openFirm(t) }
            }
            item {
                TextButton(
                    onClick = { vm.detachSelf() },
                    modifier = Modifier.fillMaxWidth()
                ) { Text("Bu cihazı firmadan ayır") }
            }
        }
    }
}

@Composable
private fun UpdateCard(vm: YoneticiViewModel) {
    Card(
        shape = RoundedCornerShape(18.dp),
        colors = CardDefaults.cardColors(containerColor = Color.White),
        elevation = CardDefaults.cardElevation(2.dp),
        modifier = Modifier.fillMaxWidth()
    ) {
        Column(Modifier.padding(16.dp)) {
            Row(verticalAlignment = Alignment.CenterVertically) {
                Box(
                    Modifier
                        .size(40.dp)
                        .clip(RoundedCornerShape(12.dp))
                        .background(YoneticiColors.Teal.copy(alpha = 0.12f)),
                    contentAlignment = Alignment.Center
                ) {
                    Icon(Icons.Default.SystemUpdate, null, tint = YoneticiColors.Teal)
                }
                Spacer(Modifier.width(12.dp))
                Column(Modifier.weight(1f)) {
                    Text("Uygulama güncelleme", fontWeight = FontWeight.SemiBold)
                    Text(
                        "Yüklü: ${vm.appVersionLabel}",
                        style = MaterialTheme.typography.bodySmall,
                        color = YoneticiColors.Slate
                    )
                }
            }
            Spacer(Modifier.height(12.dp))
            Button(
                onClick = { vm.checkForUpdates() },
                enabled = !vm.updateBusy,
                modifier = Modifier.fillMaxWidth(),
                shape = RoundedCornerShape(12.dp),
                colors = ButtonDefaults.buttonColors(containerColor = YoneticiColors.Teal)
            ) {
                if (vm.updateBusy && vm.updateProgress == null) {
                    CircularProgressIndicator(Modifier.size(18.dp), color = Color.White, strokeWidth = 2.dp)
                    Spacer(Modifier.width(8.dp))
                }
                Text(if (vm.updateBusy) "Kontrol ediliyor..." else "Güncelleme kontrol et")
            }
            vm.updateMessage?.takeIf { vm.pendingUpdate == null }?.let {
                Spacer(Modifier.height(8.dp))
                Text(it, style = MaterialTheme.typography.bodySmall, color = YoneticiColors.Teal)
            }
            vm.updateError?.takeIf { vm.pendingUpdate == null }?.let {
                Spacer(Modifier.height(8.dp))
                Text(it, style = MaterialTheme.typography.bodySmall, color = YoneticiColors.Danger)
            }
        }
    }
}

@Composable
private fun FirmCard(t: TenantRow, onClick: () -> Unit) {
    val warning = t.lisansSuresiDoldu || !t.aktif
    Card(
        onClick = onClick,
        shape = RoundedCornerShape(16.dp),
        colors = CardDefaults.cardColors(containerColor = Color.White),
        elevation = CardDefaults.cardElevation(1.dp),
        modifier = Modifier.fillMaxWidth()
    ) {
        Row(
            Modifier.padding(16.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            Box(
                Modifier
                    .size(44.dp)
                    .clip(RoundedCornerShape(12.dp))
                    .background(
                        if (warning) YoneticiColors.Danger.copy(alpha = 0.12f)
                        else YoneticiColors.Teal.copy(alpha = 0.12f)
                    ),
                contentAlignment = Alignment.Center
            ) {
                Icon(
                    if (warning) Icons.Default.Warning else Icons.Default.CheckCircle,
                    null,
                    tint = if (warning) YoneticiColors.Danger else YoneticiColors.Teal
                )
            }
            Spacer(Modifier.width(12.dp))
            Column(Modifier.weight(1f)) {
                Text(t.ad.ifBlank { "İsimsiz firma" }, fontWeight = FontWeight.SemiBold)
                Text(
                    "${t.kod} · ${lisansLabel(t.lisansTipi)}",
                    style = MaterialTheme.typography.bodySmall,
                    color = YoneticiColors.Slate
                )
                Text(
                    "Kalan ${t.lisansKalanGun ?: "?"} gün",
                    style = MaterialTheme.typography.bodySmall,
                    color = if (warning) YoneticiColors.Danger else YoneticiColors.Slate
                )
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
        containerColor = YoneticiColors.Mist,
        topBar = {
            TopAppBar(
                title = { Text(vm.selected?.ad?.ifBlank { "Firma" } ?: "Firma", fontWeight = FontWeight.Bold) },
                navigationIcon = {
                    IconButton(onClick = { vm.goFirmList() }) {
                        Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = "Geri")
                    }
                },
                colors = TopAppBarDefaults.topAppBarColors(containerColor = Color.White)
            )
        }
    ) { pad ->
        Column(Modifier.padding(pad).fillMaxSize()) {
            TabRow(selectedTabIndex = tab, containerColor = Color.White) {
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
        Modifier
            .fillMaxSize()
            .verticalScroll(rememberScrollState())
            .padding(16.dp),
        verticalArrangement = Arrangement.spacedBy(8.dp)
    ) {
        Surface(shape = RoundedCornerShape(16.dp), color = Color.White) {
            Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
                OutlinedTextField(vm.firmaKod, { vm.firmaKod = it }, label = { Text("Firma kodu") }, modifier = Modifier.fillMaxWidth())
                OutlinedTextField(vm.firmaAd, { vm.firmaAd = it }, label = { Text("Firma adı") }, modifier = Modifier.fillMaxWidth())
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
                OutlinedTextField(
                    vm.manuelGun, { vm.manuelGun = it.filter(Char::isDigit).take(4) },
                    label = { Text("Manuel / eklenecek gün") }, modifier = Modifier.fillMaxWidth()
                )
                Button(
                    onClick = { vm.saveFirm() },
                    enabled = !vm.busy,
                    modifier = Modifier.fillMaxWidth(),
                    colors = ButtonDefaults.buttonColors(containerColor = YoneticiColors.Teal)
                ) { Text("Firmayı kaydet") }
                OutlinedButton(onClick = { vm.saveFirm(lisansYenile = true) }, enabled = !vm.busy, modifier = Modifier.fillMaxWidth()) {
                    Text("Lisansı yenile")
                }
                OutlinedButton(onClick = { vm.saveFirm(gunEkleModu = true) }, enabled = !vm.busy, modifier = Modifier.fillMaxWidth()) {
                    Text("Gün ekle")
                }
            }
        }

        Surface(shape = RoundedCornerShape(16.dp), color = Color.White) {
            Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
                Text("Yedek / sıfırla / sil", fontWeight = FontWeight.SemiBold)
                OutlinedButton(onClick = { vm.backupFirm(onBackupFile) }, enabled = !vm.busy, modifier = Modifier.fillMaxWidth()) {
                    Text("Yedek al")
                }
                OutlinedTextField(
                    vm.restorePath, { vm.restorePath = it },
                    label = { Text("Yedek Storage yolu") }, modifier = Modifier.fillMaxWidth()
                )
                OutlinedButton(onClick = { vm.restoreFirm() }, enabled = !vm.busy, modifier = Modifier.fillMaxWidth()) {
                    Text("Yedekten yükle")
                }
                OutlinedTextField(confirm, { confirm = it }, label = { Text("Onay için firma kodu") }, modifier = Modifier.fillMaxWidth())
                Button(
                    onClick = { vm.resetFirm(confirm) },
                    enabled = !vm.busy,
                    modifier = Modifier.fillMaxWidth(),
                    colors = ButtonDefaults.buttonColors(containerColor = YoneticiColors.Amber)
                ) { Text("Verileri sıfırla") }
                Button(
                    onClick = { vm.deleteFirm(confirm) },
                    enabled = !vm.busy && !vm.selected?.id.isNullOrBlank(),
                    modifier = Modifier.fillMaxWidth(),
                    colors = ButtonDefaults.buttonColors(containerColor = YoneticiColors.Danger)
                ) { Text("Firmayı sil") }
            }
        }

        vm.message?.let { Text(it) }
        if (vm.busy) CircularProgressIndicator(Modifier.align(Alignment.CenterHorizontally))
    }
}

@Composable
private fun UsersTab(vm: YoneticiViewModel) {
    Column(Modifier.fillMaxSize().padding(16.dp)) {
        Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(8.dp)) {
            Button(
                onClick = { vm.newUser() },
                enabled = !vm.busy,
                colors = ButtonDefaults.buttonColors(containerColor = YoneticiColors.Teal)
            ) { Text("Yeni kullanıcı") }
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
                Card(
                    onClick = { vm.openUser(u) },
                    shape = RoundedCornerShape(14.dp),
                    colors = CardDefaults.cardColors(containerColor = Color.White),
                    modifier = Modifier.fillMaxWidth()
                ) {
                    UserRowCard(u)
                }
            }
        }
    }
}

@Composable
private fun UserRowCard(u: UserRow) {
    Column(Modifier.fillMaxWidth().padding(14.dp)) {
        Text(u.adSoyad.ifBlank { u.kullaniciAdi }, fontWeight = FontWeight.SemiBold)
        Text("${u.kullaniciAdi} · ${u.rol} · ${if (u.aktif) "Aktif" else "Pasif"}", color = YoneticiColors.Slate)
        if (u.eposta.isNotBlank()) {
            Text(u.eposta, style = MaterialTheme.typography.bodySmall, color = YoneticiColors.Slate)
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class, ExperimentalLayoutApi::class)
@Composable
fun UserEditScreen(vm: YoneticiViewModel) {
    var rolExpanded by remember { mutableStateOf(false) }
    Scaffold(
        containerColor = YoneticiColors.Mist,
        topBar = {
            TopAppBar(
                title = { Text(if (vm.isNewUser) "Yeni kullanıcı" else "Kullanıcı düzenle", fontWeight = FontWeight.Bold) },
                navigationIcon = {
                    IconButton(onClick = { vm.backFromUser() }) {
                        Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = "Geri")
                    }
                },
                actions = {
                    TextButton(onClick = { vm.applyRoleDefaults() }) { Text("Rol vars.") }
                    TextButton(onClick = { vm.clearPermissions() }) { Text("Temizle") }
                },
                colors = TopAppBarDefaults.topAppBarColors(containerColor = Color.White)
            )
        }
    ) { pad ->
        Column(
            Modifier
                .padding(pad)
                .fillMaxSize()
                .verticalScroll(rememberScrollState())
                .padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(8.dp)
        ) {
            Surface(shape = RoundedCornerShape(16.dp), color = Color.White) {
                Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
                    OutlinedTextField(vm.uAdi, { vm.uAdi = it }, label = { Text("Kullanıcı adı") }, modifier = Modifier.fillMaxWidth())
                    OutlinedTextField(vm.uEposta, { vm.uEposta = it }, label = { Text("E-posta") }, modifier = Modifier.fillMaxWidth())
                    OutlinedTextField(vm.uAdSoyad, { vm.uAdSoyad = it }, label = { Text("Ad soyad") }, modifier = Modifier.fillMaxWidth())
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
                    OutlinedTextField(vm.uSaha, { vm.uSaha = it }, label = { Text("Saha (opsiyonel)") }, modifier = Modifier.fillMaxWidth())
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
                }
            }

            Surface(shape = RoundedCornerShape(16.dp), color = Color.White) {
                Column(Modifier.padding(16.dp)) {
                    Text("Modül yetkileri", fontWeight = FontWeight.SemiBold)
                    Spacer(Modifier.height(8.dp))
                    val yetkiler = if (vm.uYetkiler.size == ModulKatalogu.tum.size) vm.uYetkiler
                    else ModulKatalogu.tum.map { m -> vm.uYetkiler.find { it.modul == m } ?: ModulYetki(m) }

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
                }
            }

            Button(
                onClick = { vm.saveUser() },
                enabled = !vm.busy,
                modifier = Modifier.fillMaxWidth(),
                colors = ButtonDefaults.buttonColors(containerColor = YoneticiColors.Teal)
            ) { Text("Kullanıcıyı kaydet") }
            if (!vm.isNewUser && !vm.editingUser?.uid.isNullOrBlank()) {
                OutlinedButton(onClick = { vm.deleteUser() }, enabled = !vm.busy, modifier = Modifier.fillMaxWidth()) {
                    Text("Kullanıcıyı sil")
                }
            }
            vm.message?.let { Text(it) }
            if (vm.busy) CircularProgressIndicator(Modifier.align(Alignment.CenterHorizontally))
        }
    }
}

private fun lisansLabel(tip: String): String =
    lisansSecenekleri.firstOrNull { it.first == tip }?.second ?: tip

@Composable
private fun UpdateDialog(
    version: String,
    build: Int,
    notes: String,
    progress: Int?,
    message: String?,
    error: String?,
    onUpdate: () -> Unit,
    onDismiss: () -> Unit
) {
    AlertDialog(
        onDismissRequest = { if (progress == null) onDismiss() },
        title = { Text("Yeni sürüm") },
        text = {
            Column {
                Text("v$version (build $build)", fontWeight = FontWeight.SemiBold, color = YoneticiColors.Teal)
                if (notes.isNotBlank()) {
                    Spacer(Modifier.height(8.dp))
                    Text(notes)
                }
                if (progress != null) {
                    Spacer(Modifier.height(12.dp))
                    LinearProgressIndicator(progress = { progress / 100f }, modifier = Modifier.fillMaxWidth())
                    Text("%$progress")
                }
                if (!message.isNullOrBlank()) {
                    Spacer(Modifier.height(8.dp))
                    Text(message, color = YoneticiColors.Slate)
                }
                if (!error.isNullOrBlank()) {
                    Spacer(Modifier.height(8.dp))
                    Text(error, color = YoneticiColors.Danger)
                }
            }
        },
        confirmButton = {
            if (progress == null) {
                Button(onClick = onUpdate, colors = ButtonDefaults.buttonColors(containerColor = YoneticiColors.Teal)) {
                    Text("Güncelle")
                }
            }
        },
        dismissButton = {
            if (progress == null) {
                TextButton(onClick = onDismiss) { Text("Sonra") }
            }
        }
    )
}

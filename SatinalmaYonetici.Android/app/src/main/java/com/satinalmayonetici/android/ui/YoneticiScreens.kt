package com.satinalmayonetici.android.ui

import androidx.compose.foundation.clickable
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
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Button
import androidx.compose.material3.Checkbox
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.ExposedDropdownMenuBox
import androidx.compose.material3.ExposedDropdownMenuDefaults
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.unit.dp
import com.satinalmayonetici.android.data.TenantRow

private val lisansSecenekleri = listOf(
    "deneme" to "15 günlük deneme",
    "yillik" to "1 yıllık",
    "2yil" to "2 yıllık",
    "3yil" to "3 yıllık",
    "manuel" to "Manuel gün"
)

@Composable
fun YoneticiRoot(vm: YoneticiViewModel) {
    if (vm.session == null) {
        LoginScreen(vm)
    } else {
        HomeScreen(vm)
    }
}

@Composable
fun LoginScreen(vm: YoneticiViewModel) {
    var email by remember { mutableStateOf("") }
    var password by remember { mutableStateOf("") }
    var rememberMe by remember { mutableStateOf(true) }

    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(24.dp),
        verticalArrangement = Arrangement.Center
    ) {
        Text(
            "Satınalma Yönetici",
            style = MaterialTheme.typography.headlineMedium,
            fontWeight = FontWeight.Bold
        )
        Text("Platform yöneticisi girişi", style = MaterialTheme.typography.bodyMedium)
        Spacer(Modifier.height(24.dp))
        OutlinedTextField(
            value = email,
            onValueChange = { email = it },
            label = { Text("E-posta") },
            singleLine = true,
            modifier = Modifier.fillMaxWidth()
        )
        Spacer(Modifier.height(12.dp))
        OutlinedTextField(
            value = password,
            onValueChange = { password = it },
            label = { Text("Şifre") },
            singleLine = true,
            visualTransformation = PasswordVisualTransformation(),
            modifier = Modifier.fillMaxWidth()
        )
        Spacer(Modifier.height(8.dp))
        Row(verticalAlignment = Alignment.CenterVertically) {
            Checkbox(checked = rememberMe, onCheckedChange = { rememberMe = it })
            Text("Beni hatırla")
        }
        vm.loginError?.let {
            Spacer(Modifier.height(8.dp))
            Text(it, color = MaterialTheme.colorScheme.error)
        }
        Spacer(Modifier.height(16.dp))
        Button(
            onClick = { vm.login(email, password, rememberMe) },
            enabled = !vm.busy && email.isNotBlank() && password.isNotBlank(),
            modifier = Modifier.fillMaxWidth()
        ) {
            if (vm.busy) CircularProgressIndicator(modifier = Modifier.height(20.dp))
            else Text("Giriş yap")
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun HomeScreen(vm: YoneticiViewModel) {
    val selected = vm.selected
    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("Firmalar") },
                actions = {
                    TextButton(onClick = { vm.reload() }) { Text("Yenile") }
                    TextButton(onClick = { vm.logout() }) { Text("Çıkış") }
                }
            )
        }
    ) { pad ->
        if (selected == null) {
            TenantList(
                listModifier = Modifier.padding(pad),
                tenants = vm.tenants,
                busy = vm.busy,
                message = vm.message,
                onSelect = vm::select
            )
        } else {
            TenantDetail(
                detailModifier = Modifier.padding(pad),
                vm = vm,
                tenant = selected
            )
        }
    }
}

@Composable
private fun TenantList(
    listModifier: Modifier,
    tenants: List<TenantRow>,
    busy: Boolean,
    message: String?,
    onSelect: (TenantRow) -> Unit
) {
    Column(listModifier.fillMaxSize().padding(16.dp)) {
        message?.let { Text(it, color = MaterialTheme.colorScheme.error) }
        if (busy && tenants.isEmpty()) {
            CircularProgressIndicator(Modifier.align(Alignment.CenterHorizontally))
            return
        }
        LazyColumn(verticalArrangement = Arrangement.spacedBy(8.dp)) {
            items(tenants, key = { it.id }) { t ->
                Column(
                    Modifier
                        .fillMaxWidth()
                        .clickable { onSelect(t) }
                        .padding(12.dp)
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

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun TenantDetail(detailModifier: Modifier, vm: YoneticiViewModel, tenant: TenantRow) {
    var confirmKod by remember { mutableStateOf("") }
    var tipExpanded by remember { mutableStateOf(false) }

    Column(
        detailModifier
            .fillMaxSize()
            .verticalScroll(rememberScrollState())
            .padding(16.dp)
    ) {
        TextButton(onClick = { vm.clearSelection() }) { Text("← Listeye dön") }
        Text(tenant.ad, style = MaterialTheme.typography.headlineSmall, fontWeight = FontWeight.Bold)
        Text("Kod: ${tenant.kod}")
        Text("Bitiş: ${tenant.lisansBitis?.take(10) ?: "-"} · kalan ${tenant.lisansKalanGun ?: "?"} gün")
        Spacer(Modifier.height(16.dp))

        ExposedDropdownMenuBox(expanded = tipExpanded, onExpandedChange = { tipExpanded = it }) {
            OutlinedTextField(
                value = lisansLabel(vm.lisansTipi),
                onValueChange = {},
                readOnly = true,
                label = { Text("Lisans tipi") },
                trailingIcon = { ExposedDropdownMenuDefaults.TrailingIcon(tipExpanded) },
                modifier = Modifier
                    .menuAnchor()
                    .fillMaxWidth()
            )
            ExposedDropdownMenu(expanded = tipExpanded, onDismissRequest = { tipExpanded = false }) {
                lisansSecenekleri.forEach { (tag, label) ->
                    DropdownMenuItem(
                        text = { Text(label) },
                        onClick = {
                            vm.lisansTipi = tag
                            tipExpanded = false
                        }
                    )
                }
            }
        }

        Spacer(Modifier.height(12.dp))
        OutlinedTextField(
            value = vm.manuelGun,
            onValueChange = { vm.manuelGun = it.filter { ch -> ch.isDigit() }.take(4) },
            label = { Text("Manuel / eklenecek gün") },
            modifier = Modifier.fillMaxWidth()
        )

        Spacer(Modifier.height(16.dp))
        Button(onClick = { vm.renewLicense() }, enabled = !vm.busy, modifier = Modifier.fillMaxWidth()) {
            Text("Lisansı yenile")
        }
        Spacer(Modifier.height(8.dp))
        OutlinedButton(onClick = { vm.addDays() }, enabled = !vm.busy, modifier = Modifier.fillMaxWidth()) {
            Text("Gün ekle")
        }

        Spacer(Modifier.height(24.dp))
        Text("Operasyonel veri sıfırlama", fontWeight = FontWeight.SemiBold)
        Text("Kullanıcı hesapları kalır. Onay için firma kodunu yazın.")
        Spacer(Modifier.height(8.dp))
        OutlinedTextField(
            value = confirmKod,
            onValueChange = { confirmKod = it },
            label = { Text("Firma kodu onayı") },
            modifier = Modifier.fillMaxWidth()
        )
        Spacer(Modifier.height(8.dp))
        Button(
            onClick = { vm.resetData(confirmKod) },
            enabled = !vm.busy && confirmKod.isNotBlank(),
            modifier = Modifier.fillMaxWidth()
        ) {
            Text("Verileri sıfırla")
        }

        vm.message?.let {
            Spacer(Modifier.height(16.dp))
            Text(it)
        }
        if (vm.busy) {
            Spacer(Modifier.height(12.dp))
            CircularProgressIndicator()
        }
    }
}

private fun lisansLabel(tip: String): String =
    lisansSecenekleri.firstOrNull { it.first == tip }?.second ?: tip

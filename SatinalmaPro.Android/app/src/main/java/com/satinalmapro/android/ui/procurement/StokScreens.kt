package com.satinalmapro.android.ui.procurement

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.horizontalScroll
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
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
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.rounded.Add
import androidx.compose.material.icons.rounded.Close
import androidx.compose.material.icons.rounded.Inventory2
import androidx.compose.material.icons.rounded.Search
import androidx.compose.material.icons.rounded.SwapVert
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateListOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import java.text.SimpleDateFormat
import java.util.Locale
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import com.satinalmapro.android.core.model.StokHareket
import com.satinalmapro.android.core.model.StokHareketTipi
import com.satinalmapro.android.core.model.StokKaydi
import com.satinalmapro.android.core.roles.KullaniciRolleri
import com.satinalmapro.android.data.repository.StokRepository
import com.satinalmapro.android.ui.AppViewModel
import com.satinalmapro.android.ui.components.AppDetailTabRow
import com.satinalmapro.android.ui.components.AppPrimaryButton
import com.satinalmapro.android.ui.components.MetrikField
import com.satinalmapro.android.ui.components.StatusPill
import com.satinalmapro.android.ui.theme.MetrikLight
import com.satinalmapro.android.ui.theme.MetrikSpace

@Composable
fun StokDurumScreen(viewModel: AppViewModel) {
    val stok by viewModel.stokList.collectAsState()
    var query by remember { mutableStateOf("") }
    var selectedTab by remember { mutableStateOf(0) } // 0 = stokta, 1 = tükenen
    val stokta = remember(stok) { stok.filter { it.mevcutMiktar > 0 } }
    val tukenen = remember(stok) { stok.filter { it.mevcutMiktar <= 0 } }
    val kaynak = if (selectedTab == 0) stokta else tukenen
    val filtered = remember(kaynak, query) {
        val q = query.trim()
        kaynak
            .sortedBy { it.malzemeAdi.lowercase() }
            .filter {
                q.isBlank() ||
                    it.malzemeAdi.contains(q, true) ||
                    it.depoSaha.contains(q, true) ||
                    it.kategori.contains(q, true)
            }
    }
    val kritik = stokta.count { it.durumMetin == "Kritik" }

    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(MetrikLight.Background)
    ) {
        StokHeader(
            title = "Stok Durumu",
            subtitle = when {
                selectedTab == 1 -> "${tukenen.size} tükenen malzeme"
                kritik > 0 -> "$kritik kalem kritik stokta"
                else -> "${stokta.size} malzeme stokta"
            }
        )
        AppDetailTabRow(
            tabs = listOf("Stokta (${stokta.size})", "Tükenen (${tukenen.size})"),
            selectedIndex = selectedTab,
            onTabSelected = { selectedTab = it },
            modifier = Modifier.padding(horizontal = MetrikSpace.screen)
        )
        MetrikField(
            value = query,
            onValueChange = { query = it },
            label = "Ara",
            placeholder = "Malzeme, depo, kategori",
            modifier = Modifier.padding(horizontal = MetrikSpace.screen, vertical = 8.dp),
            trailingIcon = {
                Icon(Icons.Rounded.Search, contentDescription = null, tint = MetrikLight.TextTertiary)
            }
        )
        if (filtered.isEmpty()) {
            EmptyStokState(if (selectedTab == 1) "Tükenen malzeme yok" else "Stokta malzeme yok")
        } else {
            LazyColumn(
                contentPadding = PaddingValues(
                    horizontal = MetrikSpace.screen,
                    vertical = MetrikSpace.md
                ),
                verticalArrangement = Arrangement.spacedBy(8.dp)
            ) {
                items(filtered, key = { "${it.malzemeAdi}|${it.depoSaha}" }) { item ->
                    StokDurumRow(item)
                }
                item { Spacer(Modifier.height(24.dp)) }
            }
        }
    }
}

@Composable
fun StokHareketScreen(viewModel: AppViewModel) {
    val hareketler by viewModel.stokHareketleri.collectAsState()
    var query by remember { mutableStateOf("") }
    val filtered = remember(hareketler, query) {
        val q = query.trim()
        hareketler
            .sortedByDescending { hareketTarihMs(it.tarih) }
            .filter {
                q.isBlank() ||
                    it.malzemeAdi.contains(q, true) ||
                    it.belgeNo.contains(q, true) ||
                    it.hareketTipi.contains(q, true) ||
                    it.depoSaha.contains(q, true)
            }
    }

    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(MetrikLight.Background)
    ) {
        StokHeader(
            title = "Stok Hareketleri",
            subtitle = "${filtered.size} kayıt"
        )
        MetrikField(
            value = query,
            onValueChange = { query = it },
            label = "Ara",
            placeholder = "Malzeme, belge, tip",
            modifier = Modifier.padding(horizontal = MetrikSpace.screen, vertical = 8.dp),
            trailingIcon = {
                Icon(Icons.Rounded.Search, contentDescription = null, tint = MetrikLight.TextTertiary)
            }
        )
        if (filtered.isEmpty()) {
            EmptyStokState("Hareket kaydı yok")
        } else {
            LazyColumn(
                contentPadding = PaddingValues(
                    horizontal = MetrikSpace.screen,
                    vertical = MetrikSpace.md
                ),
                verticalArrangement = Arrangement.spacedBy(8.dp)
            ) {
                items(filtered, key = { it.id.ifBlank { "${it.belgeNo}-${it.malzemeAdi}-${it.tarih}" } }) { item ->
                    StokHareketRow(item)
                }
                item { Spacer(Modifier.height(24.dp)) }
            }
        }
    }
}

@Composable
fun StokGirisScreen(viewModel: AppViewModel) {
    val user by viewModel.user.collectAsState()
    val loading by viewModel.loading.collectAsState()
    val error by viewModel.submitError.collectAsState()
    val birimler by viewModel.malzemeBirimleri.collectAsState()
    val canWrite = KullaniciRolleri.canStockWrite(user?.role)
    var belgeNo by remember { mutableStateOf(viewModel.sonrakiGirisBelgeNo()) }
    var depo by remember(user?.site) { mutableStateOf(user?.site.orEmpty()) }
    var teslimAlan by remember(user?.fullName) { mutableStateOf(user?.fullName.orEmpty()) }
    var malzeme by remember { mutableStateOf("") }
    var miktar by remember { mutableStateOf("") }
    var birim by remember { mutableStateOf("Adet") }
    var kategori by remember { mutableStateOf("") }
    var birimMaliyet by remember { mutableStateOf("") }
    val satirlar = remember { mutableStateListOf<StokRepository.GirisSatir>() }
    val oneriler = remember(malzeme) { viewModel.stokMalzemeOnerileri(malzeme) }
    val pendingSatir = remember(malzeme, miktar, birim, kategori, birimMaliyet) {
        val m = miktar.replace(',', '.').toDoubleOrNull() ?: return@remember null
        if (malzeme.isBlank() || m <= 0) null
        else StokRepository.GirisSatir(
            malzeme = malzeme.trim(),
            miktar = m,
            birim = birim.ifBlank { "Adet" },
            kategori = kategori.trim(),
            birimMaliyet = birimMaliyet.replace(',', '.').toDoubleOrNull() ?: 0.0
        )
    }
    val kayitSayisi = satirlar.size + if (pendingSatir != null) 1 else 0
    val kaydetHazir = kayitSayisi > 0 && depo.isNotBlank()

    fun formTemizle() {
        malzeme = ""
        miktar = ""
        kategori = ""
        birimMaliyet = ""
    }

    fun kaydedilecekSatirlar(): List<StokRepository.GirisSatir> {
        val liste = satirlar.toMutableList()
        pendingSatir?.let { liste.add(it) }
        return liste
    }

    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(MetrikLight.Background)
            .verticalScroll(rememberScrollState())
            .padding(horizontal = MetrikSpace.screen, vertical = MetrikSpace.lg),
        verticalArrangement = Arrangement.spacedBy(8.dp)
    ) {
        StokHeader(title = "Stok Girişi", subtitle = "Bilgileri doldurup kaydedin; birden fazla satır da ekleyebilirsiniz")
        if (!canWrite) {
            Text("Bu rol stok girişi yapamaz.", color = MetrikLight.Danger)
        } else {
            Row(horizontalArrangement = Arrangement.spacedBy(8.dp), modifier = Modifier.fillMaxWidth()) {
                MetrikField(belgeNo, { belgeNo = it }, "Belge no", modifier = Modifier.weight(1f))
                MetrikField(depo, { depo = it }, "Depo", modifier = Modifier.weight(1f))
            }
            MetrikField(teslimAlan, { teslimAlan = it }, "Teslim alan")

            Text("Malzeme satırı", style = MaterialTheme.typography.titleSmall, fontWeight = FontWeight.SemiBold, color = MetrikLight.TextPrimary)
            MetrikField(malzeme, { malzeme = it }, "Malzeme")
            if (oneriler.isNotEmpty() && malzeme.isNotBlank()) {
                Row(
                    modifier = Modifier.horizontalScroll(rememberScrollState()),
                    horizontalArrangement = Arrangement.spacedBy(6.dp)
                ) {
                    oneriler.take(6).forEach { o ->
                        Text(
                            o,
                            modifier = Modifier
                                .clip(RoundedCornerShape(8.dp))
                                .background(MetrikLight.Surface)
                                .clickable { malzeme = o }
                                .padding(horizontal = 10.dp, vertical = 6.dp),
                            style = MaterialTheme.typography.labelMedium,
                            color = MetrikLight.TextSecondary,
                            maxLines = 1
                        )
                    }
                }
            }
            Row(horizontalArrangement = Arrangement.spacedBy(8.dp), modifier = Modifier.fillMaxWidth()) {
                MetrikField(
                    miktar, { miktar = it }, "Miktar",
                    keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Decimal),
                    modifier = Modifier.weight(1.1f)
                )
                Column(modifier = Modifier.weight(0.9f)) {
                    Text("Birim", style = MaterialTheme.typography.labelMedium, color = MetrikLight.TextSecondary)
                    RequestBirimDropdown(
                        value = birim,
                        options = birimler,
                        onSelect = { birim = it },
                        modifier = Modifier.fillMaxWidth()
                    )
                }
            }
            Row(horizontalArrangement = Arrangement.spacedBy(8.dp), modifier = Modifier.fillMaxWidth()) {
                MetrikField(kategori, { kategori = it }, "Kategori", modifier = Modifier.weight(1f))
                MetrikField(
                    birimMaliyet, { birimMaliyet = it }, "Birim maliyet",
                    keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Decimal),
                    modifier = Modifier.weight(1f)
                )
            }
            OutlinedButton(
                onClick = {
                    val s = pendingSatir ?: return@OutlinedButton
                    satirlar.add(s)
                    formTemizle()
                },
                enabled = pendingSatir != null,
                modifier = Modifier.fillMaxWidth()
            ) {
                Icon(Icons.Rounded.Add, contentDescription = null, modifier = Modifier.size(18.dp))
                Spacer(Modifier.width(6.dp))
                Text("Satır ekle (çoklu giriş)")
            }

            if (satirlar.isNotEmpty()) {
                Text(
                    "${satirlar.size} satır eklendi",
                    style = MaterialTheme.typography.labelLarge,
                    color = MetrikLight.TextSecondary
                )
                satirlar.forEachIndexed { index, s ->
                    StokSatirChip(
                        title = s.malzeme,
                        subtitle = formatQty(s.miktar, s.birim) +
                            (if (s.kategori.isNotBlank()) " · ${s.kategori}" else ""),
                        onRemove = { satirlar.removeAt(index) }
                    )
                }
            }

            error?.let { Text(it, color = MetrikLight.Danger, style = MaterialTheme.typography.bodySmall) }
            AppPrimaryButton(
                text = if (kayitSayisi <= 1) "Stok girişi kaydet" else "Stok girişi kaydet ($kayitSayisi)",
                loading = loading,
                enabled = kaydetHazir && !loading,
                onClick = {
                    val liste = kaydedilecekSatirlar()
                    if (liste.isEmpty() || depo.isBlank()) return@AppPrimaryButton
                    viewModel.stokGirisCoklu(belgeNo, depo, teslimAlan, "", liste) {
                        satirlar.clear()
                        formTemizle()
                        belgeNo = viewModel.sonrakiGirisBelgeNo()
                        viewModel.navigateFromMenu("stok-durum")
                    }
                },
                modifier = Modifier.fillMaxWidth()
            )
        }
        Spacer(Modifier.height(24.dp))
    }
}

@Composable
fun StokCikisScreen(viewModel: AppViewModel) {
    val user by viewModel.user.collectAsState()
    val stokList by viewModel.stokList.collectAsState()
    val loading by viewModel.loading.collectAsState()
    val error by viewModel.submitError.collectAsState()
    val canWrite = KullaniciRolleri.canStockWrite(user?.role)
    var belgeNo by remember { mutableStateOf(viewModel.sonrakiCikisBelgeNo()) }
    var teslimAlan by remember(user?.fullName) { mutableStateOf(user?.fullName.orEmpty()) }
    var malzeme by remember { mutableStateOf("") }
    var depo by remember { mutableStateOf("") }
    var miktar by remember { mutableStateOf("") }
    val satirlar = remember { mutableStateListOf<StokRepository.CikisSatir>() }
    val oneriler = remember(malzeme, stokList) { viewModel.stokCikisOnerileri(malzeme) }
    val depolar = remember(malzeme, stokList) { viewModel.stokDepolari(malzeme) }
    LaunchedEffect(malzeme, depolar, user?.site) {
        if (malzeme.isBlank()) {
            depo = ""
            return@LaunchedEffect
        }
        if (depo.isNotBlank() && depolar.any { it.equals(depo, true) }) return@LaunchedEffect
        val tercih = user?.site?.trim().orEmpty()
        depo = depolar.firstOrNull { it.equals(tercih, true) } ?: depolar.firstOrNull().orEmpty()
    }
    val mevcutStok = remember(malzeme, depo, stokList) { viewModel.stokMevcutBul(malzeme, depo.ifBlank { null }) }
    val pendingSatir = remember(malzeme, depo, miktar, mevcutStok) {
        val stok = mevcutStok ?: return@remember null
        val m = miktar.replace(',', '.').toDoubleOrNull() ?: return@remember null
        if (m <= 0 || m > stok.mevcutMiktar || stok.depoSaha.isBlank()) null
        else StokRepository.CikisSatir(malzeme = stok.malzemeAdi, miktar = m, depo = stok.depoSaha)
    }
    val kayitSayisi = satirlar.size + if (pendingSatir != null) 1 else 0
    val kaydetHazir = kayitSayisi > 0 && teslimAlan.isNotBlank()

    fun formTemizle() {
        malzeme = ""
        depo = ""
        miktar = ""
    }

    fun kaydedilecekSatirlar(): List<StokRepository.CikisSatir> {
        val liste = satirlar.toMutableList()
        pendingSatir?.let { liste.add(it) }
        return liste
    }

    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(MetrikLight.Background)
            .verticalScroll(rememberScrollState())
            .padding(horizontal = MetrikSpace.screen, vertical = MetrikSpace.lg),
        verticalArrangement = Arrangement.spacedBy(8.dp)
    ) {
        StokHeader(title = "Stok Çıkışı", subtitle = "Bilgileri doldurup kaydedin — kategori stoktan otomatik gelir")
        if (!canWrite) {
            Text("Bu rol stok çıkışı yapamaz.", color = MetrikLight.Danger)
        } else {
            Row(horizontalArrangement = Arrangement.spacedBy(8.dp), modifier = Modifier.fillMaxWidth()) {
                MetrikField(belgeNo, { belgeNo = it }, "Belge no", modifier = Modifier.weight(1f))
                MetrikField(teslimAlan, { teslimAlan = it }, "Teslim alan", modifier = Modifier.weight(1f))
            }

            Text("Malzeme satırı", style = MaterialTheme.typography.titleSmall, fontWeight = FontWeight.SemiBold, color = MetrikLight.TextPrimary)
            MetrikField(malzeme, { malzeme = it }, "Malzeme")
            if (oneriler.isNotEmpty() && malzeme.isNotBlank()) {
                Row(
                    modifier = Modifier.horizontalScroll(rememberScrollState()),
                    horizontalArrangement = Arrangement.spacedBy(6.dp)
                ) {
                    oneriler.take(6).forEach { o ->
                        Text(
                            "${o.malzemeAdi} (${formatQty(o.mevcutMiktar, o.birim)})",
                            modifier = Modifier
                                .clip(RoundedCornerShape(8.dp))
                                .background(MetrikLight.Surface)
                                .clickable {
                                    malzeme = o.malzemeAdi
                                    if (o.depoSaha.isNotBlank()) depo = o.depoSaha
                                }
                                .padding(horizontal = 10.dp, vertical = 6.dp),
                            style = MaterialTheme.typography.labelMedium,
                            color = MetrikLight.TextSecondary,
                            maxLines = 1
                        )
                    }
                }
            }
            if (depolar.isNotEmpty()) {
                Text("Depo / Saha", style = MaterialTheme.typography.labelMedium, color = MetrikLight.TextSecondary)
                Row(
                    modifier = Modifier.horizontalScroll(rememberScrollState()),
                    horizontalArrangement = Arrangement.spacedBy(6.dp)
                ) {
                    depolar.forEach { d ->
                        val secili = d.equals(depo, true)
                        Text(
                            d,
                            modifier = Modifier
                                .clip(RoundedCornerShape(8.dp))
                                .background(if (secili) MetrikLight.Primary.copy(alpha = 0.15f) else MetrikLight.Surface)
                                .clickable { depo = d }
                                .padding(horizontal = 10.dp, vertical = 6.dp),
                            style = MaterialTheme.typography.labelMedium,
                            color = if (secili) MetrikLight.Primary else MetrikLight.TextSecondary,
                            maxLines = 1
                        )
                    }
                }
            }
            if (mevcutStok != null) {
                Text(
                    "Mevcut stok: ${formatQty(mevcutStok.mevcutMiktar, mevcutStok.birim)}" +
                        " · ${mevcutStok.depoSaha}" +
                        (if (mevcutStok.kategori.isNotBlank()) " · ${mevcutStok.kategori}" else ""),
                    style = MaterialTheme.typography.bodyMedium,
                    fontWeight = FontWeight.SemiBold,
                    color = MetrikLight.Primary
                )
            } else if (malzeme.isNotBlank()) {
                Text(
                    "Bu malzeme için mevcut stok bulunamadı",
                    style = MaterialTheme.typography.bodySmall,
                    color = MetrikLight.Danger
                )
            }
            MetrikField(
                miktar, { miktar = it }, "Miktar",
                keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Decimal)
            )
            OutlinedButton(
                onClick = {
                    val s = pendingSatir ?: return@OutlinedButton
                    satirlar.add(s)
                    formTemizle()
                },
                enabled = pendingSatir != null,
                modifier = Modifier.fillMaxWidth()
            ) {
                Icon(Icons.Rounded.Add, contentDescription = null, modifier = Modifier.size(18.dp))
                Spacer(Modifier.width(6.dp))
                Text("Satır ekle (çoklu çıkış)")
            }

            if (satirlar.isNotEmpty()) {
                Text(
                    "${satirlar.size} satır eklendi",
                    style = MaterialTheme.typography.labelLarge,
                    color = MetrikLight.TextSecondary
                )
                satirlar.forEachIndexed { index, s ->
                    StokSatirChip(
                        title = s.malzeme,
                        subtitle = "${formatQty(s.miktar, "")} · ${s.depo}",
                        onRemove = { satirlar.removeAt(index) }
                    )
                }
            }

            error?.let { Text(it, color = MetrikLight.Danger, style = MaterialTheme.typography.bodySmall) }
            if (kayitSayisi > 0 && teslimAlan.isBlank()) {
                Text(
                    "Kaydetmek için teslim alan girin",
                    color = MetrikLight.Warning,
                    style = MaterialTheme.typography.bodySmall
                )
            }
            AppPrimaryButton(
                text = if (kayitSayisi <= 1) "Stok çıkışı kaydet" else "Stok çıkışı kaydet ($kayitSayisi)",
                loading = loading,
                enabled = kaydetHazir && !loading,
                onClick = {
                    val liste = kaydedilecekSatirlar()
                    if (liste.isEmpty() || teslimAlan.isBlank()) return@AppPrimaryButton
                    viewModel.stokCikisCoklu(belgeNo, teslimAlan, liste) {
                        satirlar.clear()
                        formTemizle()
                        belgeNo = viewModel.sonrakiCikisBelgeNo()
                        viewModel.navigateFromMenu("stok-durum")
                    }
                },
                modifier = Modifier.fillMaxWidth()
            )
        }
        Spacer(Modifier.height(24.dp))
    }
}

@Composable
private fun StokSatirChip(title: String, subtitle: String, onRemove: () -> Unit) {
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .clip(RoundedCornerShape(12.dp))
            .background(MetrikLight.Surface)
            .padding(horizontal = 12.dp, vertical = 10.dp),
        verticalAlignment = Alignment.CenterVertically
    ) {
        Column(modifier = Modifier.weight(1f)) {
            Text(
                title,
                style = MaterialTheme.typography.titleSmall,
                fontWeight = FontWeight.SemiBold,
                color = MetrikLight.TextPrimary,
                maxLines = 1,
                overflow = TextOverflow.Ellipsis
            )
            Text(subtitle, style = MaterialTheme.typography.bodySmall, color = MetrikLight.TextTertiary)
        }
        IconButton(onClick = onRemove) {
            Icon(Icons.Rounded.Close, contentDescription = "Kaldır", tint = MetrikLight.Danger, modifier = Modifier.size(18.dp))
        }
    }
}

@Composable
private fun StokHeader(title: String, subtitle: String) {
    Column(
        modifier = Modifier
            .fillMaxWidth()
            .background(MetrikLight.Surface)
            .padding(horizontal = MetrikSpace.screen, vertical = 18.dp)
    ) {
        Text(
            title,
            style = MaterialTheme.typography.headlineMedium,
            color = MetrikLight.TextPrimary,
            fontWeight = FontWeight.Bold
        )
        Spacer(Modifier.height(4.dp))
        Text(subtitle, style = MaterialTheme.typography.bodyMedium, color = MetrikLight.TextSecondary)
    }
}

@Composable
private fun EmptyStokState(message: String) {
    Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
        Text(message, color = MetrikLight.TextSecondary)
    }
}

@Composable
private fun StokDurumRow(item: StokKaydi) {
    val tint = when (item.durumMetin) {
        "Tükendi" -> MetrikLight.Danger
        "Kritik", "Düşük" -> MetrikLight.Warning
        else -> MetrikLight.Success
    }
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .clip(RoundedCornerShape(14.dp))
            .background(MetrikLight.Surface)
            .padding(horizontal = 14.dp, vertical = 14.dp),
        verticalAlignment = Alignment.CenterVertically
    ) {
        Box(
            modifier = Modifier
                .size(42.dp)
                .clip(CircleShape)
                .background(tint.copy(alpha = 0.12f)),
            contentAlignment = Alignment.Center
        ) {
            Icon(Icons.Rounded.Inventory2, contentDescription = null, tint = tint, modifier = Modifier.size(22.dp))
        }
        Spacer(Modifier.width(14.dp))
        Column(modifier = Modifier.weight(1f)) {
            Text(
                item.malzemeAdi,
                style = MaterialTheme.typography.titleMedium,
                color = MetrikLight.TextPrimary,
                fontWeight = FontWeight.SemiBold,
                maxLines = 1,
                overflow = TextOverflow.Ellipsis
            )
            Text(
                listOfNotNull(
                    item.depoSaha.takeIf { it.isNotBlank() },
                    item.kategori.takeIf { it.isNotBlank() }
                ).joinToString(" · ").ifBlank { "—" },
                style = MaterialTheme.typography.bodySmall,
                color = MetrikLight.TextTertiary,
                maxLines = 1,
                overflow = TextOverflow.Ellipsis
            )
        }
        Column(horizontalAlignment = Alignment.End) {
            Text(
                formatQty(item.mevcutMiktar, item.birim),
                style = MaterialTheme.typography.titleSmall,
                color = MetrikLight.TextPrimary,
                fontWeight = FontWeight.Bold
            )
            StatusPill(item.durumMetin)
        }
    }
}

@Composable
private fun StokHareketRow(item: StokHareket) {
    val tint = when (item.hareketTipi) {
        StokHareketTipi.GIRIS -> MetrikLight.Success
        StokHareketTipi.CIKIS -> MetrikLight.Warning
        else -> MetrikLight.Info
    }
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .clip(RoundedCornerShape(14.dp))
            .background(MetrikLight.Surface)
            .padding(horizontal = 14.dp, vertical = 14.dp),
        verticalAlignment = Alignment.CenterVertically
    ) {
        Box(
            modifier = Modifier
                .size(42.dp)
                .clip(CircleShape)
                .background(tint.copy(alpha = 0.12f)),
            contentAlignment = Alignment.Center
        ) {
            Icon(Icons.Rounded.SwapVert, contentDescription = null, tint = tint, modifier = Modifier.size(22.dp))
        }
        Spacer(Modifier.width(14.dp))
        Column(modifier = Modifier.weight(1f)) {
            Text(
                item.malzemeAdi,
                style = MaterialTheme.typography.titleMedium,
                color = MetrikLight.TextPrimary,
                fontWeight = FontWeight.SemiBold,
                maxLines = 1,
                overflow = TextOverflow.Ellipsis
            )
            Text(
                listOfNotNull(
                    item.hareketTipi.takeIf { it.isNotBlank() },
                    item.belgeNo.takeIf { it.isNotBlank() },
                    item.tarih.takeIf { it.isNotBlank() }
                ).joinToString(" · "),
                style = MaterialTheme.typography.bodySmall,
                color = MetrikLight.TextTertiary,
                maxLines = 1,
                overflow = TextOverflow.Ellipsis
            )
            if (item.depoSaha.isNotBlank() || item.teslimEdilen.isNotBlank()) {
                Text(
                    listOfNotNull(
                        item.depoSaha.takeIf { it.isNotBlank() },
                        item.teslimEdilen.takeIf { it.isNotBlank() }?.let { "→ $it" }
                    ).joinToString(" "),
                    style = MaterialTheme.typography.labelSmall,
                    color = MetrikLight.TextSecondary,
                    maxLines = 1,
                    overflow = TextOverflow.Ellipsis
                )
            }
        }
        Text(
            formatQty(item.miktar, item.birim),
            style = MaterialTheme.typography.titleSmall,
            color = tint,
            fontWeight = FontWeight.Bold
        )
    }
}

private fun formatQty(qty: Double, birim: String): String {
    val n = if (qty % 1.0 == 0.0) qty.toInt().toString() else String.format("%.2f", qty)
    return if (birim.isBlank()) n else "$n $birim"
}

/** dd.MM.yyyy (ve saat) metnini sıralama için ms'ye çevir — string sıralama yanlış kronoloji verir. */
private fun hareketTarihMs(metin: String?): Long {
    if (metin.isNullOrBlank()) return 0L
    val temiz = metin.trim()
    val formatlar = arrayOf(
        "dd.MM.yyyy HH:mm:ss", "dd.MM.yyyy HH:mm", "dd.MM.yyyy",
        "d.M.yyyy HH:mm", "d.M.yyyy", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd"
    )
    for (f in formatlar) {
        val t = runCatching {
            SimpleDateFormat(f, Locale("tr", "TR")).apply { isLenient = false }.parse(temiz)?.time
        }.getOrNull()
        if (t != null) return t
    }
    return 0L
}

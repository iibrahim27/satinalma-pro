package com.satinalmapro.android.ui.procurement

import androidx.compose.foundation.background
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
import androidx.compose.material.icons.rounded.Inventory2
import androidx.compose.material.icons.rounded.Search
import androidx.compose.material.icons.rounded.SwapVert
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
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
import com.satinalmapro.android.ui.AppViewModel
import com.satinalmapro.android.ui.components.AppPrimaryButton
import com.satinalmapro.android.ui.components.MetrikField
import com.satinalmapro.android.ui.components.StatusPill
import com.satinalmapro.android.ui.theme.MetrikLight
import com.satinalmapro.android.ui.theme.MetrikSpace

@Composable
fun StokDurumScreen(viewModel: AppViewModel) {
    val stok by viewModel.stokList.collectAsState()
    var query by remember { mutableStateOf("") }
    val filtered = remember(stok, query) {
        val q = query.trim()
        stok
            .sortedBy { it.malzemeAdi.lowercase() }
            .filter {
                q.isBlank() ||
                    it.malzemeAdi.contains(q, true) ||
                    it.depoSaha.contains(q, true) ||
                    it.kategori.contains(q, true)
            }
    }
    val kritik = stok.count { it.durumMetin == "Kritik" || it.durumMetin == "Tükendi" }

    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(MetrikLight.Background)
    ) {
        StokHeader(
            title = "Stok Durumu",
            subtitle = if (kritik > 0) "$kritik kalem kritik veya tükenmiş" else "Kritik stok uyarısı yok"
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
            EmptyStokState("Stok kaydı yok")
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
            .sortedByDescending { it.tarih }
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
    val canWrite = KullaniciRolleri.canStockWrite(user?.role)
    var malzeme by remember { mutableStateOf("") }
    var miktar by remember { mutableStateOf("") }
    var birim by remember { mutableStateOf("Adet") }
    var kategori by remember { mutableStateOf("") }
    var depo by remember { mutableStateOf(user?.site.orEmpty()) }
    var birimMaliyet by remember { mutableStateOf("") }
    var belgeNo by remember { mutableStateOf(viewModel.sonrakiGirisBelgeNo()) }
    var teslimAlan by remember { mutableStateOf("") }

    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(MetrikLight.Background)
            .verticalScroll(rememberScrollState())
            .padding(horizontal = MetrikSpace.screen, vertical = MetrikSpace.lg),
        verticalArrangement = Arrangement.spacedBy(10.dp)
    ) {
        StokHeader(title = "Stok Girişi", subtitle = "Depoya malzeme girişi")
        if (!canWrite) {
            Text("Bu rol stok girişi yapamaz.", color = MetrikLight.Danger)
        } else {
            MetrikField(malzeme, { malzeme = it }, "Malzeme")
            MetrikField(
                miktar, { miktar = it }, "Miktar",
                keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Decimal)
            )
            MetrikField(birim, { birim = it }, "Birim")
            MetrikField(kategori, { kategori = it }, "Kategori")
            MetrikField(depo, { depo = it }, "Depo / saha")
            MetrikField(
                birimMaliyet, { birimMaliyet = it }, "Birim maliyet",
                keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Decimal)
            )
            MetrikField(belgeNo, { belgeNo = it }, "Belge no")
            MetrikField(teslimAlan, { teslimAlan = it }, "Teslim alan")
            error?.let { Text(it, color = MetrikLight.Danger, style = MaterialTheme.typography.bodySmall) }
            AppPrimaryButton(
                text = "Giriş kaydet",
                loading = loading,
                enabled = malzeme.isNotBlank() && miktar.isNotBlank() && depo.isNotBlank(),
                onClick = {
                    viewModel.stokGiris(
                        malzeme, miktar, birim, kategori, depo, birimMaliyet, belgeNo, teslimAlan
                    ) {
                        miktar = ""
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
    val loading by viewModel.loading.collectAsState()
    val error by viewModel.submitError.collectAsState()
    val canWrite = KullaniciRolleri.canStockWrite(user?.role)
    var malzeme by remember { mutableStateOf("") }
    var miktar by remember { mutableStateOf("") }
    var depo by remember { mutableStateOf(user?.site.orEmpty()) }
    var belgeNo by remember { mutableStateOf(viewModel.sonrakiCikisBelgeNo()) }
    var teslimAlan by remember { mutableStateOf("") }

    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(MetrikLight.Background)
            .verticalScroll(rememberScrollState())
            .padding(horizontal = MetrikSpace.screen, vertical = MetrikSpace.lg),
        verticalArrangement = Arrangement.spacedBy(10.dp)
    ) {
        StokHeader(title = "Stok Çıkışı", subtitle = "Depodan malzeme çıkışı")
        if (!canWrite) {
            Text("Bu rol stok çıkışı yapamaz.", color = MetrikLight.Danger)
        } else {
            MetrikField(malzeme, { malzeme = it }, "Malzeme")
            MetrikField(
                miktar, { miktar = it }, "Miktar",
                keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Decimal)
            )
            MetrikField(depo, { depo = it }, "Depo / saha")
            MetrikField(belgeNo, { belgeNo = it }, "Belge no")
            MetrikField(teslimAlan, { teslimAlan = it }, "Teslim alan")
            error?.let { Text(it, color = MetrikLight.Danger, style = MaterialTheme.typography.bodySmall) }
            AppPrimaryButton(
                text = "Çıkış kaydet",
                loading = loading,
                enabled = malzeme.isNotBlank() && miktar.isNotBlank() && depo.isNotBlank() && teslimAlan.isNotBlank(),
                onClick = {
                    viewModel.stokCikis(malzeme, miktar, depo, belgeNo, teslimAlan) {
                        miktar = ""
                        belgeNo = viewModel.sonrakiCikisBelgeNo()
                        viewModel.navigateFromMenu("stok-hareket")
                    }
                },
                modifier = Modifier.fillMaxWidth()
            )
        }
        Spacer(Modifier.height(24.dp))
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
        "Kritik" -> MetrikLight.Warning
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

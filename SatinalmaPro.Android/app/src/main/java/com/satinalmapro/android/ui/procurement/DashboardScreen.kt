package com.satinalmapro.android.ui.procurement

import androidx.compose.animation.core.LinearEasing
import androidx.compose.animation.core.RepeatMode
import androidx.compose.animation.core.animateFloat
import androidx.compose.animation.core.infiniteRepeatable
import androidx.compose.animation.core.rememberInfiniteTransition
import androidx.compose.animation.core.tween
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.horizontalScroll
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
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.rounded.KeyboardArrowRight
import androidx.compose.material.icons.rounded.Add
import androidx.compose.material.icons.rounded.Assignment
import androidx.compose.material.icons.rounded.Inventory2
import androidx.compose.material.icons.rounded.NotificationsNone
import androidx.compose.material.icons.rounded.Settings
import androidx.compose.material.icons.rounded.SwapVert
import androidx.compose.material3.FloatingActionButton
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import com.satinalmapro.android.core.roles.KullaniciRolleri
import com.satinalmapro.android.core.roles.RolNavigasyon
import com.satinalmapro.android.core.saas.TenantSession
import com.satinalmapro.android.ui.AppViewModel
import com.satinalmapro.android.ui.theme.MetrikLight
import com.satinalmapro.android.ui.theme.MetrikSpace
import com.satinalmapro.android.ui.theme.statusFromText
import kotlin.math.cos
import kotlin.math.sin

@Composable
fun DashboardScreen(viewModel: AppViewModel) {
    val user by viewModel.user.collectAsState()
    val talepler by viewModel.talepler.collectAsState()
    val stok by viewModel.stokList.collectAsState()
    val stokHareketleri by viewModel.stokHareketleri.collectAsState()
    val badges by viewModel.menuBadges.collectAsState()
    val notifications by viewModel.notifications.collectAsState()
    val role = user?.role
    val normalized = KullaniciRolleri.normalize(role)
    val stockFocused = normalized in setOf(KullaniciRolleri.ATOLYE, KullaniciRolleri.DEPO)
    val depoFocused = normalized == KullaniciRolleri.DEPO
    val queues = remember(role) {
        runCatching { RolNavigasyon.queueMenus(role) }.getOrDefault(emptyList())
    }
    val priorityQueues = remember(queues, badges) {
        queues
            .filter { RolNavigasyon.isActionQueue(it.route) }
            .map { it to (badges[it.route] ?: 0) }
            .sortedByDescending { it.second }
            .take(4)
    }
    val firma = TenantSession.tenantName().orEmpty()
    val recent = remember(talepler) {
        runCatching { talepler.sortedByDescending { it.guncellemeUtc }.take(6) }
            .getOrDefault(emptyList())
    }
    val recentStok = remember(stokHareketleri) {
        stokHareketleri.sortedByDescending { it.tarih }.take(6)
    }
    val canCreate = KullaniciRolleri.canCreateRequest(role)
    val canStockWrite = KullaniciRolleri.canStockWrite(role)
    val canStockView = KullaniciRolleri.canStockView(role) &&
        queues.any { it.route.startsWith("stok-") }
    val canSettings = normalized in setOf(
        KullaniciRolleri.ADMIN,
        KullaniciRolleri.SATINALMA
    )
    val unread = notifications.count { !it.read }
    val waitingTotal = badges.values.sum()
    val kritikStok = stok.count { it.durumMetin == "Kritik" || it.durumMetin == "Tükendi" }
    val tukenenStok = stok.count { it.durumMetin == "Tükendi" }
    val yoldakiSayisi = badges["satinalma-siparis"] ?: 0
    val displayName = remember(user?.fullName) {
        user?.fullName?.trim()?.takeIf { it.isNotBlank() }
    }

    Box(modifier = Modifier.fillMaxSize().background(MetrikLight.Background)) {
        Column(
            modifier = Modifier
                .fillMaxSize()
                .verticalScroll(rememberScrollState())
                .padding(bottom = if (canCreate) 96.dp else 28.dp)
        ) {
            AnimatedHeroBanner(
                firma = firma.ifBlank { "Satınalma Pro" },
                userName = displayName.orEmpty(),
                role = role.orEmpty()
            )

            // Özet şerit
            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = MetrikSpace.screen)
                    .padding(top = 16.dp),
                horizontalArrangement = Arrangement.spacedBy(10.dp)
            ) {
                if (depoFocused) {
                    SummaryTile(
                        label = "Stok",
                        value = stok.size.toString(),
                        accent = MetrikLight.Info,
                        modifier = Modifier.weight(1f),
                        onClick = { viewModel.navigateFromMenu("stok-durum") }
                    )
                    SummaryTile(
                        label = "Kritik",
                        value = kritikStok.toString(),
                        accent = if (kritikStok > 0) MetrikLight.Warning else MetrikLight.Success,
                        modifier = Modifier.weight(1f),
                        onClick = { viewModel.navigateFromMenu("stok-durum") }
                    )
                    SummaryTile(
                        label = "Yoldaki",
                        value = yoldakiSayisi.toString(),
                        accent = if (yoldakiSayisi > 0) MetrikLight.Accent else MetrikLight.TextSecondary,
                        modifier = Modifier.weight(1f),
                        onClick = { viewModel.navigateFromMenu("satinalma-siparis") }
                    )
                } else if (stockFocused) {
                    SummaryTile(
                        label = "Stok",
                        value = stok.size.toString(),
                        accent = MetrikLight.Info,
                        modifier = Modifier.weight(1f),
                        onClick = { viewModel.navigateFromMenu("stok-durum") }
                    )
                    SummaryTile(
                        label = "Kritik",
                        value = kritikStok.toString(),
                        accent = if (kritikStok > 0) MetrikLight.Warning else MetrikLight.Success,
                        modifier = Modifier.weight(1f),
                        onClick = { viewModel.navigateFromMenu("stok-durum") }
                    )
                    SummaryTile(
                        label = "Bildirim",
                        value = unread.toString(),
                        accent = if (unread > 0) MetrikLight.Warning else MetrikLight.TextSecondary,
                        modifier = Modifier.weight(1f),
                        onClick = { viewModel.navigateFromMenu("bildirimler") }
                    )
                } else {
                    SummaryTile(
                        label = "Bekleyen",
                        value = waitingTotal.toString(),
                        accent = MetrikLight.Accent,
                        modifier = Modifier.weight(1f),
                        onClick = { viewModel.navigateFromMenu("isler") }
                    )
                    SummaryTile(
                        label = "Talepler",
                        value = talepler.size.toString(),
                        accent = MetrikLight.Info,
                        modifier = Modifier.weight(1f),
                        onClick = {
                            queues.firstOrNull()?.let { viewModel.navigateFromMenu(it.route) }
                                ?: viewModel.navigateFromMenu("isler")
                        }
                    )
                    SummaryTile(
                        label = "Bildirim",
                        value = unread.toString(),
                        accent = if (unread > 0) MetrikLight.Warning else MetrikLight.TextSecondary,
                        modifier = Modifier.weight(1f),
                        onClick = { viewModel.navigateFromMenu("bildirimler") }
                    )
                }
            }
            if (depoFocused) {
                Spacer(Modifier.height(10.dp))
                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(horizontal = MetrikSpace.screen),
                    horizontalArrangement = Arrangement.spacedBy(10.dp)
                ) {
                    SummaryTile(
                        label = "Tükenen",
                        value = tukenenStok.toString(),
                        accent = if (tukenenStok > 0) MetrikLight.Danger else MetrikLight.Success,
                        modifier = Modifier.weight(1f),
                        onClick = { viewModel.navigateFromMenu("stok-durum") }
                    )
                    SummaryTile(
                        label = "Hareket",
                        value = stokHareketleri.size.toString(),
                        accent = MetrikLight.Info,
                        modifier = Modifier.weight(1f),
                        onClick = { viewModel.navigateFromMenu("stok-hareket") }
                    )
                    SummaryTile(
                        label = "Bildirim",
                        value = unread.toString(),
                        accent = if (unread > 0) MetrikLight.Warning else MetrikLight.TextSecondary,
                        modifier = Modifier.weight(1f),
                        onClick = { viewModel.navigateFromMenu("bildirimler") }
                    )
                }
            }

            // Öncelikli kuyruklar — yatay, sayı odaklı
            Spacer(Modifier.height(22.dp))
            SectionHeader(
                title = "Öncelikli",
                action = "Tümü",
                onAction = { viewModel.navigateFromMenu("isler") }
            )
            Spacer(Modifier.height(10.dp))
            if (priorityQueues.isEmpty()) {
                EmptyBlock("Bu rol için iş kuyruğu yok")
            } else {
                Row(
                    modifier = Modifier
                        .horizontalScroll(rememberScrollState())
                        .padding(horizontal = MetrikSpace.screen),
                    horizontalArrangement = Arrangement.spacedBy(10.dp)
                ) {
                    priorityQueues.forEach { (item, count) ->
                        PriorityChip(
                            title = item.title,
                            count = count,
                            onClick = { viewModel.navigateFromMenu(item.route) }
                        )
                    }
                }
            }

            // Hızlı işlemler
            Spacer(Modifier.height(22.dp))
            SectionHeader(title = "Hızlı işlem")
            Spacer(Modifier.height(10.dp))
            Column(
                modifier = Modifier.padding(horizontal = MetrikSpace.screen),
                verticalArrangement = Arrangement.spacedBy(8.dp)
            ) {
                if (canCreate) {
                    ActionRow(
                        icon = Icons.Rounded.Add,
                        title = "Yeni talep oluştur",
                        subtitle = "Malzeme / hizmet talebi",
                        tint = MetrikLight.Accent,
                        onClick = { viewModel.navigateFromMenu("yeni-talep") }
                    )
                }
                if (canStockView) {
                    ActionRow(
                        icon = Icons.Rounded.Inventory2,
                        title = "Stok durumu",
                        subtitle = if (kritikStok > 0) "$kritikStok kritik kalem" else "${stok.size} kalem",
                        tint = MetrikLight.Success,
                        onClick = { viewModel.navigateFromMenu("stok-durum") }
                    )
                }
                if (queues.any { it.route == "stok-hareket" }) {
                    ActionRow(
                        icon = Icons.Rounded.SwapVert,
                        title = "Stok hareketleri",
                        subtitle = "${stokHareketleri.size} kayıt",
                        tint = MetrikLight.Info,
                        onClick = { viewModel.navigateFromMenu("stok-hareket") }
                    )
                }
                if (canStockWrite && queues.any { it.route == "stok-giris" }) {
                    ActionRow(
                        icon = Icons.Rounded.Add,
                        title = "Stok girişi",
                        subtitle = "Depoya malzeme gir",
                        tint = MetrikLight.Accent,
                        onClick = { viewModel.navigateFromMenu("stok-giris") }
                    )
                }
                if (canStockWrite && queues.any { it.route == "stok-cikis" }) {
                    ActionRow(
                        icon = Icons.Rounded.SwapVert,
                        title = "Stok çıkışı",
                        subtitle = "Depodan malzeme çıkar",
                        tint = MetrikLight.Warning,
                        onClick = { viewModel.navigateFromMenu("stok-cikis") }
                    )
                }
                if (depoFocused && queues.any { it.route == "satinalma-siparis" }) {
                    ActionRow(
                        icon = Icons.Rounded.Assignment,
                        title = "Yoldaki malzemeler",
                        subtitle = if (yoldakiSayisi > 0) "$yoldakiSayisi sipariş · mal kabul" else "Sipariş ve mal kabul",
                        tint = MetrikLight.Accent,
                        onClick = { viewModel.navigateFromMenu("satinalma-siparis") }
                    )
                }
                ActionRow(
                    icon = Icons.Rounded.Assignment,
                    title = if (stockFocused) "Tüm stok işlemleri" else "Tüm iş kuyrukları",
                    subtitle = "${queues.size} kuyruk",
                    tint = MetrikLight.Info,
                    onClick = { viewModel.navigateFromMenu("isler") }
                )
                ActionRow(
                    icon = Icons.Rounded.NotificationsNone,
                    title = "Bildirimler",
                    subtitle = if (unread > 0) "$unread okunmamış" else "Hepsi okundu",
                    tint = MetrikLight.Warning,
                    onClick = { viewModel.navigateFromMenu("bildirimler") }
                )
                if (canSettings) {
                    ActionRow(
                        icon = Icons.Rounded.Settings,
                        title = "Ayarlar",
                        subtitle = "Katalog ve kullanıcılar",
                        tint = MetrikLight.TextSecondary,
                        onClick = { viewModel.navigateFromMenu("ayarlar") }
                    )
                }
            }

            // Son hareketler
            Spacer(Modifier.height(22.dp))
            SectionHeader(title = if (stockFocused) "Son stok hareketleri" else "Son hareketler")
            Spacer(Modifier.height(10.dp))
            if (stockFocused) {
                if (recentStok.isEmpty()) {
                    EmptyBlock("Henüz stok hareketi yok")
                } else {
                    Column(
                        modifier = Modifier.padding(horizontal = MetrikSpace.screen),
                        verticalArrangement = Arrangement.spacedBy(8.dp)
                    ) {
                        recentStok.forEach { h ->
                            Row(
                                modifier = Modifier
                                    .fillMaxWidth()
                                    .clip(RoundedCornerShape(14.dp))
                                    .background(MetrikLight.Surface)
                                    .clickable { viewModel.navigateFromMenu("stok-hareket") }
                                    .padding(14.dp),
                                verticalAlignment = Alignment.CenterVertically
                            ) {
                                Box(
                                    modifier = Modifier
                                        .size(10.dp)
                                        .clip(CircleShape)
                                        .background(statusFromText(h.hareketTipi))
                                )
                                Spacer(Modifier.width(12.dp))
                                Column(modifier = Modifier.weight(1f)) {
                                    Text(
                                        h.malzemeAdi.ifBlank { "Stok" },
                                        style = MaterialTheme.typography.titleMedium,
                                        color = MetrikLight.TextPrimary,
                                        fontWeight = FontWeight.SemiBold
                                    )
                                    Text(
                                        listOfNotNull(
                                            h.hareketTipi.takeIf { it.isNotBlank() },
                                            h.belgeNo.takeIf { it.isNotBlank() }
                                        ).joinToString(" · ").ifBlank { "—" },
                                        style = MaterialTheme.typography.bodySmall,
                                        color = MetrikLight.TextSecondary,
                                        maxLines = 1,
                                        overflow = TextOverflow.Ellipsis
                                    )
                                }
                                Text(
                                    h.tarih.ifBlank { "—" },
                                    style = MaterialTheme.typography.labelMedium,
                                    color = MetrikLight.TextTertiary,
                                    maxLines = 1
                                )
                            }
                        }
                    }
                }
            } else if (recent.isEmpty()) {
                EmptyBlock("Bu firmada henüz kayıt yok")
            } else {
                Column(
                    modifier = Modifier.padding(horizontal = MetrikSpace.screen),
                    verticalArrangement = Arrangement.spacedBy(8.dp)
                ) {
                    recent.forEach { t ->
                        Row(
                            modifier = Modifier
                                .fillMaxWidth()
                                .clip(RoundedCornerShape(14.dp))
                                .background(MetrikLight.Surface)
                                .clickable { viewModel.navigate("talep-detay?id=${t.id}") }
                                .padding(14.dp),
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            Box(
                                modifier = Modifier
                                    .size(10.dp)
                                    .clip(CircleShape)
                                    .background(statusFromText(t.durum))
                            )
                            Spacer(Modifier.width(12.dp))
                            Column(modifier = Modifier.weight(1f)) {
                                Text(
                                    t.talepNo.ifBlank { "Talep" },
                                    style = MaterialTheme.typography.titleMedium,
                                    color = MetrikLight.TextPrimary,
                                    fontWeight = FontWeight.SemiBold
                                )
                                Text(
                                    t.malzemeOzeti.ifBlank { t.talepAciklamasi }.ifBlank { "—" },
                                    style = MaterialTheme.typography.bodySmall,
                                    color = MetrikLight.TextSecondary,
                                    maxLines = 1,
                                    overflow = TextOverflow.Ellipsis
                                )
                            }
                            Text(
                                t.durum.ifBlank { "—" },
                                style = MaterialTheme.typography.labelMedium,
                                color = statusFromText(t.durum),
                                maxLines = 1
                            )
                        }
                    }
                }
            }
        }

        if (canCreate) {
            FloatingActionButton(
                onClick = { viewModel.navigateFromMenu("yeni-talep") },
                modifier = Modifier
                    .align(Alignment.BottomEnd)
                    .padding(MetrikSpace.screen),
                containerColor = MetrikLight.Accent,
                contentColor = MetrikLight.TextOnAccent,
                shape = RoundedCornerShape(16.dp)
            ) {
                Icon(Icons.Rounded.Add, contentDescription = "Yeni talep")
            }
        }
    }
}

@Composable
private fun AnimatedHeroBanner(
    firma: String,
    userName: String,
    role: String
) {
    val transition = rememberInfiniteTransition(label = "hero")
    val drift by transition.animateFloat(
        initialValue = 0f,
        targetValue = 1f,
        animationSpec = infiniteRepeatable(
            animation = tween(durationMillis = 7_000, easing = LinearEasing),
            repeatMode = RepeatMode.Reverse
        ),
        label = "drift"
    )
    val pulse by transition.animateFloat(
        initialValue = 0.7f,
        targetValue = 1f,
        animationSpec = infiniteRepeatable(
            animation = tween(durationMillis = 4_500, easing = LinearEasing),
            repeatMode = RepeatMode.Reverse
        ),
        label = "pulse"
    )

    Box(
        modifier = Modifier
            .fillMaxWidth()
            .height(88.dp)
    ) {
        Canvas(modifier = Modifier.fillMaxSize()) {
            val w = size.width
            val h = size.height
            drawRect(
                brush = Brush.horizontalGradient(
                    colors = listOf(
                        Color(0xFFF2F7F6),
                        Color(0xFFEEF4F8),
                        Color(0xFFE8F1F0)
                    )
                )
            )
            val ox = w * (0.78f + 0.06f * sin(drift * Math.PI.toFloat()))
            val oy = h * (0.42f + 0.10f * cos(drift * Math.PI.toFloat()))
            drawCircle(
                brush = Brush.radialGradient(
                    colors = listOf(
                        MetrikLight.Accent.copy(alpha = 0.16f * pulse),
                        Color.Transparent
                    ),
                    center = Offset(ox, oy),
                    radius = w * 0.42f
                ),
                center = Offset(ox, oy),
                radius = w * 0.42f
            )
            val ox2 = w * (0.18f + 0.05f * cos(drift * Math.PI.toFloat()))
            val oy2 = h * (0.70f - 0.08f * sin(drift * Math.PI.toFloat()))
            drawCircle(
                brush = Brush.radialGradient(
                    colors = listOf(
                        Color(0xFF38BDF8).copy(alpha = 0.12f * pulse),
                        Color.Transparent
                    ),
                    center = Offset(ox2, oy2),
                    radius = w * 0.32f
                ),
                center = Offset(ox2, oy2),
                radius = w * 0.32f
            )
        }

        Text(
            firma,
            style = MaterialTheme.typography.titleMedium,
            color = MetrikLight.TextPrimary,
            fontWeight = FontWeight.Bold,
            maxLines = 1,
            overflow = TextOverflow.Ellipsis,
            modifier = Modifier
                .align(Alignment.TopStart)
                .padding(start = MetrikSpace.screen, top = 12.dp)
                .fillMaxWidth(0.62f)
        )

        Column(
            modifier = Modifier
                .align(Alignment.BottomEnd)
                .padding(end = MetrikSpace.screen, bottom = 10.dp),
            horizontalAlignment = Alignment.End
        ) {
            if (role.isNotBlank()) {
                Box(
                    modifier = Modifier
                        .background(MetrikLight.Accent.copy(alpha = 0.12f), RoundedCornerShape(6.dp))
                        .padding(horizontal = 8.dp, vertical = 2.dp)
                ) {
                    Text(
                        role,
                        style = MaterialTheme.typography.labelSmall,
                        color = MetrikLight.Accent,
                        fontWeight = FontWeight.SemiBold
                    )
                }
                Spacer(Modifier.height(3.dp))
            }
            if (userName.isNotBlank()) {
                Text(
                    userName,
                    style = MaterialTheme.typography.labelLarge,
                    color = MetrikLight.TextPrimary,
                    fontWeight = FontWeight.SemiBold,
                    maxLines = 1,
                    overflow = TextOverflow.Ellipsis
                )
            }
        }
    }
}

@Composable
private fun SummaryTile(
    label: String,
    value: String,
    accent: Color,
    modifier: Modifier = Modifier,
    onClick: () -> Unit
) {
    Column(
        modifier = modifier
            .clip(RoundedCornerShape(16.dp))
            .background(MetrikLight.Surface)
            .clickable(onClick = onClick)
            .padding(14.dp)
    ) {
        Text(
            value,
            style = MaterialTheme.typography.headlineMedium,
            color = accent,
            fontWeight = FontWeight.Bold
        )
        Spacer(Modifier.height(2.dp))
        Text(label, style = MaterialTheme.typography.labelMedium, color = MetrikLight.TextSecondary)
    }
}

@Composable
private fun SectionHeader(
    title: String,
    action: String? = null,
    onAction: (() -> Unit)? = null
) {
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .padding(horizontal = MetrikSpace.screen),
        horizontalArrangement = Arrangement.SpaceBetween,
        verticalAlignment = Alignment.CenterVertically
    ) {
        Text(
            title,
            style = MaterialTheme.typography.titleLarge,
            color = MetrikLight.TextPrimary,
            fontWeight = FontWeight.SemiBold
        )
        if (action != null && onAction != null) {
            Text(
                action,
                style = MaterialTheme.typography.labelLarge,
                color = MetrikLight.Accent,
                modifier = Modifier.clickable(onClick = onAction)
            )
        }
    }
}

@Composable
private fun PriorityChip(title: String, count: Int, onClick: () -> Unit) {
    Column(
        modifier = Modifier
            .width(148.dp)
            .clip(RoundedCornerShape(16.dp))
            .background(MetrikLight.Surface)
            .border(
                width = 1.dp,
                color = if (count > 0) MetrikLight.Accent.copy(alpha = 0.35f) else MetrikLight.Border,
                shape = RoundedCornerShape(16.dp)
            )
            .clickable(onClick = onClick)
            .padding(14.dp)
    ) {
        Text(
            if (count > 0) count.toString() else "0",
            style = MaterialTheme.typography.headlineMedium,
            color = if (count > 0) MetrikLight.Accent else MetrikLight.TextTertiary,
            fontWeight = FontWeight.Bold
        )
        Spacer(Modifier.height(4.dp))
        Text(
            title,
            style = MaterialTheme.typography.bodySmall,
            color = MetrikLight.TextPrimary,
            fontWeight = FontWeight.Medium,
            maxLines = 2,
            overflow = TextOverflow.Ellipsis,
            modifier = Modifier.height(36.dp)
        )
    }
}

@Composable
private fun ActionRow(
    icon: androidx.compose.ui.graphics.vector.ImageVector,
    title: String,
    subtitle: String,
    tint: Color,
    onClick: () -> Unit
) {
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .clip(RoundedCornerShape(14.dp))
            .background(MetrikLight.Surface)
            .clickable(onClick = onClick)
            .padding(14.dp),
        verticalAlignment = Alignment.CenterVertically
    ) {
        Box(
            modifier = Modifier
                .size(40.dp)
                .clip(CircleShape)
                .background(tint.copy(alpha = 0.12f)),
            contentAlignment = Alignment.Center
        ) {
            Icon(icon, contentDescription = null, tint = tint, modifier = Modifier.size(22.dp))
        }
        Spacer(Modifier.width(12.dp))
        Column(modifier = Modifier.weight(1f)) {
            Text(
                title,
                style = MaterialTheme.typography.titleMedium,
                color = MetrikLight.TextPrimary,
                fontWeight = FontWeight.SemiBold
            )
            Text(subtitle, style = MaterialTheme.typography.bodySmall, color = MetrikLight.TextSecondary)
        }
        Icon(
            Icons.AutoMirrored.Rounded.KeyboardArrowRight,
            contentDescription = null,
            tint = MetrikLight.TextTertiary
        )
    }
}

@Composable
private fun EmptyBlock(message: String) {
    Box(
        modifier = Modifier
            .fillMaxWidth()
            .padding(horizontal = MetrikSpace.screen)
            .clip(RoundedCornerShape(14.dp))
            .background(MetrikLight.Surface)
            .padding(vertical = 28.dp),
        contentAlignment = Alignment.Center
    ) {
        Text(message, style = MaterialTheme.typography.bodyMedium, color = MetrikLight.TextSecondary)
    }
}
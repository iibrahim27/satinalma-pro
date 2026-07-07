package com.satinalmapro.android.ui.components

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.rounded.Tune
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.satinalmapro.android.core.liste.ModulGrupSecenegi
import com.satinalmapro.android.core.liste.ModulListeAyarlari
import com.satinalmapro.android.core.liste.ModulTabloKolon
import com.satinalmapro.android.ui.theme.AppColors

@Composable
fun ModulTabloToolbar(
    ayarlar: ModulListeAyarlari,
    kolonlar: List<ModulTabloKolon>,
    grupSecenekleri: List<ModulGrupSecenegi>,
    kayitSayisi: Int,
    guncelSayfa: Int,
    toplamSayfa: Int,
    onAyarlarDegisti: (ModulListeAyarlari) -> Unit,
    onSayfaDegisti: (Int) -> Unit,
    modifier: Modifier = Modifier
) {
    var menuAcik by remember { mutableStateOf(false) }
    var kolonMenuAcik by remember { mutableStateOf(false) }
    var grupMenuAcik by remember { mutableStateOf(false) }
    var sayfaMenuAcik by remember { mutableStateOf(false) }

    Row(
        modifier.fillMaxWidth().padding(vertical = 4.dp),
        horizontalArrangement = Arrangement.SpaceBetween,
        verticalAlignment = Alignment.CenterVertically
    ) {
        Text(
            text = buildString {
                append("$kayitSayisi kayıt")
                ayarlar.groupField?.let { gf ->
                    val etiket = grupSecenekleri.firstOrNull { it.field == gf }?.label ?: gf
                    append(" · Gruplama: $etiket")
                }
                if (ayarlar.dense) append(" · Yoğun")
            },
            style = MaterialTheme.typography.labelMedium,
            color = AppColors.TextSecondary
        )

        Row(horizontalArrangement = Arrangement.spacedBy(2.dp), verticalAlignment = Alignment.CenterVertically) {
            if (toplamSayfa > 1) {
                Text(
                    "$guncelSayfa / $toplamSayfa",
                    style = MaterialTheme.typography.labelSmall,
                    color = AppColors.TextSecondary,
                    modifier = Modifier.padding(end = 4.dp)
                )
                IconButton(onClick = { if (guncelSayfa > 1) onSayfaDegisti(guncelSayfa - 1) }) {
                    Text("‹", style = MaterialTheme.typography.titleMedium)
                }
                IconButton(onClick = { if (guncelSayfa < toplamSayfa) onSayfaDegisti(guncelSayfa + 1) }) {
                    Text("›", style = MaterialTheme.typography.titleMedium)
                }
            }

            IconButton(onClick = { menuAcik = true }) {
                Icon(Icons.Rounded.Tune, contentDescription = "İnce ayar")
            }

            DropdownMenu(expanded = menuAcik, onDismissRequest = { menuAcik = false }) {
                DropdownMenuItem(
                    text = { Text("Kolonlar") },
                    onClick = { menuAcik = false; kolonMenuAcik = true }
                )
                DropdownMenuItem(
                    text = { Text("Grupla") },
                    onClick = { menuAcik = false; grupMenuAcik = true }
                )
                DropdownMenuItem(
                    text = { Text(if (ayarlar.dense) "Normal satır" else "Yoğun görünüm") },
                    onClick = {
                        menuAcik = false
                        onAyarlarDegisti(ayarlar.copy(dense = !ayarlar.dense))
                    }
                )
                DropdownMenuItem(
                    text = { Text("Sayfa boyutu") },
                    onClick = { menuAcik = false; sayfaMenuAcik = true }
                )
                DropdownMenuItem(
                    text = { Text(if (ayarlar.fullscreen) "Tam ekrandan çık" else "Tam ekran") },
                    onClick = {
                        menuAcik = false
                        onAyarlarDegisti(ayarlar.copy(fullscreen = !ayarlar.fullscreen))
                    }
                )
            }

            DropdownMenu(expanded = kolonMenuAcik, onDismissRequest = { kolonMenuAcik = false }) {
                kolonlar.forEach { kolon ->
                    val gizli = kolon.id in ayarlar.hiddenColumnIds
                    DropdownMenuItem(
                        text = { Text((if (!gizli) "✓ " else "") + kolon.label) },
                        onClick = {
                            val yeniGizli = if (gizli) {
                                ayarlar.hiddenColumnIds - kolon.id
                            } else {
                                val visible = kolonlar.count { it.id !in ayarlar.hiddenColumnIds }
                                if (visible <= 1) return@DropdownMenuItem
                                ayarlar.hiddenColumnIds + kolon.id
                            }
                            onAyarlarDegisti(ayarlar.copy(hiddenColumnIds = yeniGizli))
                        }
                    )
                }
            }

            DropdownMenu(expanded = grupMenuAcik, onDismissRequest = { grupMenuAcik = false }) {
                DropdownMenuItem(
                    text = { Text(if (ayarlar.groupField == null) "✓ Gruplama yok" else "Gruplamayı kaldır") },
                    onClick = {
                        grupMenuAcik = false
                        onAyarlarDegisti(ayarlar.copy(groupField = null))
                    }
                )
                grupSecenekleri.forEach { secenek ->
                    DropdownMenuItem(
                        text = {
                            Text(
                                if (ayarlar.groupField == secenek.field) "✓ ${secenek.label}"
                                else secenek.label
                            )
                        },
                        onClick = {
                            grupMenuAcik = false
                            onAyarlarDegisti(ayarlar.copy(groupField = secenek.field))
                        }
                    )
                }
            }

            DropdownMenu(expanded = sayfaMenuAcik, onDismissRequest = { sayfaMenuAcik = false }) {
                listOf(25, 50, 100, 200).forEach { boyut ->
                    DropdownMenuItem(
                        text = { Text(if (ayarlar.pageSize == boyut) "✓ $boyut" else boyut.toString()) },
                        onClick = {
                            sayfaMenuAcik = false
                            onAyarlarDegisti(ayarlar.copy(pageSize = boyut))
                            onSayfaDegisti(1)
                        }
                    )
                }
            }
        }
    }
}

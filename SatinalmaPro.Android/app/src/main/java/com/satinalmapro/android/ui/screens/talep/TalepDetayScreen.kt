package com.satinalmapro.android.ui.screens.talep

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.satinalmapro.android.core.roles.KullaniciRolleri
import com.satinalmapro.android.core.roles.TalepDurumlari
import com.satinalmapro.android.core.roles.TalepKuyrugu
import com.satinalmapro.android.ui.AppViewModel
import com.satinalmapro.android.ui.components.AppCard
import com.satinalmapro.android.ui.components.DetailRow
import com.satinalmapro.android.ui.components.StatusBadge
import com.satinalmapro.android.ui.theme.AppColors
import com.satinalmapro.android.ui.theme.AppShapes

@Composable
fun TalepDetayScreen(viewModel: AppViewModel, talepId: String) {
    val talep by viewModel.talepById(talepId).collectAsState(initial = null)
    val user by viewModel.user.collectAsState()
    val error by viewModel.submitError.collectAsState()
    var redGerekce by remember { mutableStateOf("") }

    if (talep == null) {
        Column(Modifier.fillMaxSize().padding(24.dp)) { Text("Talep bulunamadı.", color = AppColors.TextSecondary) }
        return
    }

    val item = talep!!
    val role = user?.role
    val yonetimKarar = KullaniciRolleri.canManagementDecide(role) &&
        (TalepKuyrugu.yonetimTalepler(item) || TalepKuyrugu.yonetimTeklifBekleyen(item))
    val teklifOnay = KullaniciRolleri.canManagementDecide(role) &&
        (item.durum == TalepDurumlari.YONETIM_ONAY || TalepKuyrugu.karsilastirma(item))
    val teklifGir = KullaniciRolleri.canEnterQuotes(role) && TalepKuyrugu.teklifGirisi(item)

    Column(
        modifier = Modifier.fillMaxSize().verticalScroll(rememberScrollState()).padding(horizontal = 20.dp, vertical = 16.dp)
    ) {
        RowHeader(item.talepNo.ifBlank { "Talep" }, item.durum)
        Spacer(Modifier.height(16.dp))
        AppCard {
            Column(Modifier.padding(20.dp)) {
                DetailRow("Talep Eden", item.talepEden)
                HorizontalDivider(color = AppColors.Border)
                DetailRow("Şantiye", item.santiyeAdi)
                HorizontalDivider(color = AppColors.Border)
                DetailRow("Tarih", item.tarih)
                if (item.redGerekcesi.isNotBlank()) {
                    HorizontalDivider(color = AppColors.Border)
                    DetailRow("Red Gerekçesi", item.redGerekcesi)
                }
                if (item.talepAciklamasi.isNotBlank()) {
                    HorizontalDivider(color = AppColors.Border)
                    DetailRow("Açıklama", item.talepAciklamasi)
                }
            }
        }

        Spacer(Modifier.height(16.dp))
        Text("Kalemler", style = MaterialTheme.typography.titleMedium)
        Spacer(Modifier.height(8.dp))
        item.kalemler.forEach { kalem ->
            AppCard {
                Column(Modifier.padding(16.dp)) {
                    DetailRow("Malzeme", kalem.malzeme)
                    HorizontalDivider(color = AppColors.Border)
                    DetailRow("Miktar", "${kalem.miktar} ${kalem.birim}")
                    if (kalem.kabulEdilenMiktar > 0) {
                        HorizontalDivider(color = AppColors.Border)
                        DetailRow("Kabul", "${kalem.kabulEdilenMiktar} / ${kalem.miktar}")
                    }
                }
            }
            Spacer(Modifier.height(8.dp))
        }

        if (item.teklifler.isNotEmpty()) {
            Text("Teklifler", style = MaterialTheme.typography.titleMedium)
            Spacer(Modifier.height(8.dp))
            item.teklifler.forEach { teklif ->
                AppCard(onClick = {
                    if (teklifOnay) viewModel.yonetimTeklifOnayla(item.id, teklif.id) { viewModel.navigate("onaylanan-malzemeler") }
                }) {
                    Column(Modifier.padding(16.dp)) {
                        DetailRow("Firma", teklif.firmaAdi)
                        HorizontalDivider(color = AppColors.Border)
                        DetailRow("Toplam", "%.2f TL".format(teklif.genelToplam))
                        if (teklif.onaylandi) StatusBadge("Onaylandı", AppColors.SuccessContainer, AppColors.Success)
                    }
                }
                Spacer(Modifier.height(8.dp))
            }
        }

        error?.let { Text(it, color = AppColors.Danger, modifier = Modifier.padding(vertical = 8.dp)) }

        if (teklifGir) {
            Button(
                onClick = { viewModel.navigate("teklif-gir?id=${item.id}") },
                modifier = Modifier.fillMaxWidth(),
                colors = ButtonDefaults.buttonColors(containerColor = AppColors.Primary)
            ) { Text("Teklif Gir") }
            Spacer(Modifier.height(8.dp))
        }

        if (yonetimKarar && !item.teklifGirilmis) {
            Button(
                onClick = { viewModel.yonetimOnayla(item.id, teklifIste = false) { viewModel.navigate("gelen-talepler") } },
                modifier = Modifier.fillMaxWidth(),
                colors = ButtonDefaults.buttonColors(containerColor = AppColors.Success)
            ) { Text("Teklifsiz Onayla") }
            Spacer(Modifier.height(8.dp))
            Button(
                onClick = { viewModel.yonetimOnayla(item.id, teklifIste = true) { viewModel.navigate("gelen-talepler") } },
                modifier = Modifier.fillMaxWidth(),
                colors = ButtonDefaults.buttonColors(containerColor = AppColors.Warning)
            ) { Text("Teklif İste") }
            Spacer(Modifier.height(8.dp))
            OutlinedTextField(
                value = redGerekce,
                onValueChange = { redGerekce = it },
                modifier = Modifier.fillMaxWidth(),
                label = { Text("Red gerekçesi") },
                shape = AppShapes.medium
            )
            Spacer(Modifier.height(8.dp))
            Button(
                onClick = { viewModel.yonetimReddet(item.id, redGerekce) { viewModel.navigate("red-talepler") } },
                modifier = Modifier.fillMaxWidth(),
                colors = ButtonDefaults.buttonColors(containerColor = AppColors.Danger)
            ) { Text("Reddet") }
        }
    }
}

@Composable
private fun RowHeader(title: String, status: String) {
    Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween, verticalAlignment = androidx.compose.ui.Alignment.CenterVertically) {
        Text(title, style = MaterialTheme.typography.headlineMedium, color = AppColors.TextPrimary)
        StatusBadge(status, AppColors.PrimaryContainer, AppColors.Primary)
    }
}

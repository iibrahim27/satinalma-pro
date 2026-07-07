package com.satinalmapro.android.ui.screens.teklif

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
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateMapOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.unit.dp
import com.satinalmapro.android.core.model.TeklifItem
import com.satinalmapro.android.core.roles.KullaniciRolleri
import com.satinalmapro.android.core.roles.TalepDurumlari
import com.satinalmapro.android.services.SatinalmaPdfHelper
import com.satinalmapro.android.ui.AppViewModel
import com.satinalmapro.android.ui.components.AppCard
import com.satinalmapro.android.ui.components.DetailRow
import com.satinalmapro.android.ui.components.StatusBadge
import com.satinalmapro.android.ui.components.AppPrimaryButton
import com.satinalmapro.android.ui.components.AppScreenContent
import com.satinalmapro.android.ui.components.SectionTitle
import com.satinalmapro.android.ui.theme.AppColors
import com.satinalmapro.android.ui.theme.AppShapes
import com.satinalmapro.android.ui.theme.AppSpacing
import com.satinalmapro.android.ui.theme.appFieldColors

@Composable
fun TeklifGirisScreen(viewModel: AppViewModel, talepId: String) {
    val talep by viewModel.talepById(talepId).collectAsState(initial = null)
    val error by viewModel.submitError.collectAsState()
    val loading by viewModel.loading.collectAsState()
    var firma by remember { mutableStateOf("") }
    var marka by remember { mutableStateOf("") }
    var vade by remember { mutableStateOf("30") }
    var teslim by remember { mutableStateOf("") }
    var odeme by remember { mutableStateOf("") }
    var successMessage by remember { mutableStateOf<String?>(null) }
    var editingTeklifId by remember { mutableStateOf<String?>(null) }
    val fiyatlar = remember { mutableStateMapOf<String, String>() }
    val context = LocalContext.current

    if (talep == null) {
        Column(Modifier.fillMaxSize().padding(24.dp)) { Text("Talep bulunamadı.", color = AppColors.TextSecondary) }
        return
    }

    val item = talep!!
    val canEnterQuotes = KullaniciRolleri.canEnterQuotes(viewModel.currentUser()?.role)
    if (!canEnterQuotes) {
        Column(Modifier.fillMaxSize().padding(24.dp)) {
            Text("Teklif girişi yalnızca satınalma yetkisine açıktır.", color = AppColors.TextSecondary)
            AppPrimaryButton(
                text = "Talep Detayına Dön",
                onClick = { viewModel.navigate("talep-detay?id=$talepId") }
            )
        }
        return
    }

    val yonetimeGonderilebilir = item.teklifler.isNotEmpty() &&
        item.durum != TalepDurumlari.YONETIM_ONAY &&
        item.durum != TalepDurumlari.ONAYLANDI
    val duzenlemeModu = editingTeklifId != null

    fun formuTemizle() {
        editingTeklifId = null
        firma = ""
        marka = ""
        vade = "30"
        teslim = ""
        odeme = ""
        fiyatlar.clear()
    }

    fun teklifiFormaYukle(teklif: TeklifItem) {
        editingTeklifId = teklif.id
        firma = teklif.firmaAdi
        marka = teklif.marka
        vade = teklif.vadeGunu.toString()
        teslim = teklif.teslimSuresi
        odeme = teklif.odemeSekli
        fiyatlar.clear()
        teklif.fiyatlar.forEach { fiyatlar[it.kalemId] = it.birimFiyat.toString() }
    }

    Column(Modifier.fillMaxSize().verticalScroll(rememberScrollState())) {
    AppScreenContent {
        Text("Teklif Girişi · ${item.talepNo}", style = MaterialTheme.typography.headlineSmall, color = AppColors.TextPrimary)
        Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
            StatusBadge(item.durum, AppColors.PrimaryContainer, AppColors.Primary)
            if (item.teklifler.isNotEmpty()) {
                Text("${item.teklifler.size} teklif", style = MaterialTheme.typography.labelMedium, color = AppColors.TextSecondary)
            }
        }
        Text(item.malzemeOzeti, color = AppColors.TextSecondary)
        if (item.durum == TalepDurumlari.YONETIM_ONAY) {
            Text(
                "Bu talep yönetim onayında. Karşılaştırma menüsünden takip edebilirsiniz.",
                color = AppColors.Success,
                style = MaterialTheme.typography.bodySmall
            )
        }

        if (item.teklifler.isNotEmpty()) {
            OutlinedButton(
                onClick = { SatinalmaPdfHelper.tedarikciTeklifTalebiPaylas(context, item, viewModel.pdfBaglam()) },
                modifier = Modifier.fillMaxWidth()
            ) { Text("Tedarikçi Teklif Talebi PDF") }
        }

        if (item.teklifler.isNotEmpty() && item.durum != TalepDurumlari.YONETIM_ONAY) {
            Text("Kayıtlı Teklifler", style = MaterialTheme.typography.titleMedium)
            val oneri = item.onerilenTeklif()
            if (item.teklifler.any { it.genelToplam > 0 }) {
                Text(
                    "Satınalma önerisi: ${oneri?.firmaAdi ?: "—"}${if (item.satinalmaOnerisiElleSecildi) " (elle seçildi)" else " (en düşük fiyat)"}",
                    style = MaterialTheme.typography.bodySmall,
                    color = AppColors.Primary,
                    modifier = Modifier.padding(bottom = 4.dp)
                )
                OutlinedButton(
                    onClick = { viewModel.satinalmaOnerisiOtomatigeAl(talepId) { successMessage = "Öneri otomatiğe alındı." } },
                    modifier = Modifier.fillMaxWidth(),
                    enabled = item.satinalmaOnerisiElleSecildi && !loading
                ) { Text("Otomatik Öneri (En Düşük Fiyat)") }
                Spacer(Modifier.height(8.dp))
            }
            item.teklifler.forEach { teklif ->
                val oneriMi = oneri?.id == teklif.id
                AppCard {
                    Column {
                        DetailRow("Firma", teklif.firmaAdi)
                        HorizontalDivider(color = AppColors.Border)
                        DetailRow("Toplam", "%.2f TL".format(teklif.genelToplam))
                        if (oneriMi) {
                            Spacer(Modifier.height(4.dp))
                            StatusBadge("Öneri", AppColors.SuccessContainer, AppColors.Success)
                        }
                        Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                            OutlinedButton(
                                onClick = { teklifiFormaYukle(teklif) },
                                modifier = Modifier.weight(1f),
                                enabled = !loading
                            ) { Text("Düzenle") }
                            OutlinedButton(
                                onClick = {
                                    viewModel.deleteTeklif(talepId, teklif.id) {
                                        if (editingTeklifId == teklif.id) formuTemizle()
                                        successMessage = "Teklif silindi."
                                    }
                                },
                                modifier = Modifier.weight(1f),
                                enabled = !loading
                            ) { Text("Sil", color = AppColors.Danger) }
                        }
                        if (teklif.genelToplam > 0 && !oneriMi) {
                            OutlinedButton(
                                onClick = {
                                    viewModel.satinalmaOnerisiSec(talepId, teklif.id) {
                                        successMessage = "${teklif.firmaAdi} öneri olarak işaretlendi."
                                    }
                                },
                                modifier = Modifier.fillMaxWidth(),
                                enabled = !loading
                            ) { Text("Öneri Olarak İşaretle") }
                        }
                    }
                }
            }
        }

        Text(
            if (duzenlemeModu) "Teklif Düzenle" else "Yeni Teklif",
            style = MaterialTheme.typography.titleMedium,
            color = AppColors.TextPrimary
        )
        field("Firma Adı", firma) { firma = it }
        field("Marka", marka) { marka = it }
        field("Vade (gün)", vade) { vade = it }
        field("Teslim Süresi", teslim) { teslim = it }
        field("Ödeme Şekli", odeme) { odeme = it }
        Text("Kalem Fiyatları (TL)", style = MaterialTheme.typography.titleMedium, color = AppColors.TextPrimary)
        item.kalemler.forEach { kalem ->
            OutlinedTextField(
                value = fiyatlar[kalem.id].orEmpty(),
                onValueChange = { fiyatlar[kalem.id] = it },
                modifier = Modifier.fillMaxWidth(),
                label = { Text("${kalem.malzeme} (${kalem.miktar} ${kalem.birim})") },
                singleLine = true,
                shape = AppShapes.medium,
                colors = appFieldColors()
            )
        }
        if (duzenlemeModu) {
            TextButton(onClick = { formuTemizle() }) { Text("Düzenlemeyi İptal") }
        }
        successMessage?.let { Text(it, color = AppColors.Success, style = MaterialTheme.typography.bodySmall) }
        error?.let { Text(it, color = AppColors.Danger) }
        Button(
            onClick = {
                successMessage = null
                val map = item.kalemler.associate { k ->
                    k.id to (fiyatlar[k.id]?.replace(',', '.')?.toDoubleOrNull() ?: 0.0)
                }
                val onDone = {
                    successMessage = if (duzenlemeModu) "Teklif güncellendi." else "Teklif kaydedildi."
                    formuTemizle()
                }
                if (duzenlemeModu) {
                    viewModel.updateTeklif(
                        talepId, editingTeklifId!!, firma, marka,
                        vade.toIntOrNull() ?: 0, teslim, odeme, map, onDone
                    )
                } else {
                    viewModel.addTeklif(talepId, firma, marka, vade.toIntOrNull() ?: 0, teslim, odeme, map, onDone)
                }
            },
            modifier = Modifier.fillMaxWidth().height(52.dp),
            enabled = firma.isNotBlank() && !loading,
            colors = ButtonDefaults.buttonColors(containerColor = AppColors.Primary)
        ) { Text(if (loading) "Kaydediliyor..." else if (duzenlemeModu) "Teklifi Güncelle" else "Teklifi Kaydet") }
        if (yonetimeGonderilebilir) {
            Button(
                onClick = {
                    successMessage = null
                    viewModel.sendQuotesToManagement(talepId) {
                        viewModel.navigate("teklif-karsilastirma?id=$talepId")
                    }
                },
                modifier = Modifier.fillMaxWidth(),
                enabled = !loading,
                colors = ButtonDefaults.buttonColors(containerColor = AppColors.Success)
            ) { Text("Yönetime Gönder") }
        }
        OutlinedButton(
            onClick = { viewModel.navigate("teklif-karsilastirma?id=$talepId") },
            modifier = Modifier.fillMaxWidth(),
            enabled = item.teklifler.isNotEmpty()
        ) { Text("Karşılaştırmaya Git") }
    }
    }
}

@Composable
private fun field(label: String, value: String, onChange: (String) -> Unit) {
    OutlinedTextField(
        value = value,
        onValueChange = onChange,
        modifier = Modifier.fillMaxWidth(),
        label = { Text(label) },
        singleLine = true,
        shape = AppShapes.medium,
        colors = appFieldColors()
    )
}

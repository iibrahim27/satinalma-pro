package com.satinalmapro.android.ui.screens.teklif

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.ExposedDropdownMenuBox
import androidx.compose.material3.ExposedDropdownMenuDefaults
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.MenuAnchorType
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateMapOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.satinalmapro.android.core.model.TalepItem
import com.satinalmapro.android.core.model.TalepQueue
import com.satinalmapro.android.core.model.TeklifItem
import com.satinalmapro.android.core.roles.KullaniciRolleri
import androidx.compose.runtime.LaunchedEffect
import com.satinalmapro.android.core.roles.IsAkisRotalari
import com.satinalmapro.android.core.roles.TalepKuyrugu
import com.satinalmapro.shared.filter.detail.PurchaseRequestDetailAction
import com.satinalmapro.shared.filter.detail.PurchaseRequestDetailPresenter
import com.satinalmapro.shared.filter.detail.PurchaseRequestDetailScreen
import com.satinalmapro.android.services.SatinalmaPdfHelper
import com.satinalmapro.android.ui.AppViewModel
import com.satinalmapro.android.ui.components.AppCard
import com.satinalmapro.android.ui.components.DetailRow
import com.satinalmapro.android.ui.components.StatusBadge
import com.satinalmapro.android.ui.screens.talep.TalepListScreen
import com.satinalmapro.android.ui.components.AppPrimaryButton
import com.satinalmapro.android.ui.components.AppScreenContent
import com.satinalmapro.android.ui.theme.AppColors
import com.satinalmapro.android.ui.theme.AppShapes
import com.satinalmapro.android.ui.theme.AppSpacing

@Composable
fun TeklifsizFirmaFiyatScreen(viewModel: AppViewModel, talepId: String?) {
    if (talepId == null) {
        TalepListScreen(viewModel, TalepQueue.TEKLIFSIZ_FIRMA_FIYAT)
        return
    }
    val talep by viewModel.talepById(talepId).collectAsState(initial = null)
    val error by viewModel.submitError.collectAsState()
    val firmaMap = remember { mutableStateMapOf<String, String>() }
    val fiyatMap = remember { mutableStateMapOf<String, String>() }

    if (talep == null) {
        Column(Modifier.fillMaxSize().padding(24.dp)) { Text("Talep bulunamadı.", color = AppColors.TextSecondary) }
        return
    }
    val item = talep!!
    LaunchedEffect(item.id) {
        if (!TalepKuyrugu.teklifsizFirmaFiyatBekliyor(item)) {
            val hedef = if (TalepKuyrugu.onaylananMalzeme(item)) "onaylanan-malzemeler"
            else "talep-detay?id=$talepId"
            viewModel.navigate(hedef)
        }
    }
    if (!TalepKuyrugu.teklifsizFirmaFiyatBekliyor(item)) return
    TeklifsizFirmaFiyatForm(item, firmaMap, fiyatMap, error) { girdiler ->
        viewModel.teklifsizFirmaFiyatKaydet(talepId, girdiler) {
            viewModel.navigate("onaylanan-malzemeler")
        }
    }
}

@Composable
private fun TeklifsizFirmaFiyatForm(
    talep: TalepItem,
    firmaMap: MutableMap<String, String>,
    fiyatMap: MutableMap<String, String>,
    error: String?,
    onSave: (List<Triple<String, String, Double>>) -> Unit
) {
    Column(Modifier.fillMaxSize().verticalScroll(rememberScrollState())) {
    AppScreenContent {
        Text("Firma/Fiyat Girişi", style = MaterialTheme.typography.headlineSmall)
        Text("${talep.talepNo} · ${talep.talepEden}", color = AppColors.TextSecondary)
        talep.kalemler.forEach { kalem ->
            AppCard {
                Column {
                    DetailRow("Malzeme", kalem.malzeme)
                    HorizontalDivider(color = AppColors.Border)
                    DetailRow("Miktar", "${kalem.miktar} ${kalem.birim}")
                    Spacer(Modifier.height(8.dp))
                    OutlinedTextField(
                        value = firmaMap[kalem.id].orEmpty(),
                        onValueChange = { firmaMap[kalem.id] = it },
                        modifier = Modifier.fillMaxWidth(),
                        label = { Text("Firma") },
                        singleLine = true,
                        shape = AppShapes.medium
                    )
                    Spacer(Modifier.height(8.dp))
                    OutlinedTextField(
                        value = fiyatMap[kalem.id].orEmpty(),
                        onValueChange = { fiyatMap[kalem.id] = it },
                        modifier = Modifier.fillMaxWidth(),
                        label = { Text("Birim Fiyat") },
                        singleLine = true,
                        shape = AppShapes.medium
                    )
                }
            }
        }
        error?.let { Text(it, color = AppColors.Danger) }
        AppPrimaryButton("Kaydet", onClick = {
                val girdiler = talep.kalemler.mapNotNull { kalem ->
                    val firma = firmaMap[kalem.id]?.trim().orEmpty()
                    val fiyat = fiyatMap[kalem.id]?.replace(',', '.')?.toDoubleOrNull()
                    if (firma.isBlank() || fiyat == null || fiyat <= 0) null
                    else Triple(kalem.id, firma, fiyat)
                }
                if (girdiler.size != talep.kalemler.size) return@AppPrimaryButton
                onSave(girdiler)
        })
    }
    }
}

@Composable
fun TeklifKarsilastirmaScreen(viewModel: AppViewModel, talepId: String?) {
    if (talepId == null) {
        TalepListScreen(viewModel, TalepQueue.TEKLIF_KARSILASTIRMA)
        return
    }
    val talep by viewModel.talepById(talepId).collectAsState(initial = null)
    val user by viewModel.user.collectAsState()
    val error by viewModel.submitError.collectAsState()

    if (talep == null) {
        Column(Modifier.fillMaxSize().padding(24.dp)) { Text("Talep bulunamadı.", color = AppColors.TextSecondary) }
        return
    }

    val item = talep!!
    val context = LocalContext.current
    val canSend = KullaniciRolleri.canEnterQuotes(user?.role) &&
        TalepKuyrugu.karsilastirma(item) &&
        item.durum != com.satinalmapro.android.core.roles.TalepDurumlari.YONETIM_ONAY

    Column(Modifier.fillMaxSize().verticalScroll(rememberScrollState())) {
    AppScreenContent {
        Row(
            Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Text(item.talepNo, style = MaterialTheme.typography.headlineMedium, color = AppColors.TextPrimary)
            StatusBadge(item.durum, AppColors.PrimaryContainer, AppColors.Primary)
        }
        Text(
            "${item.talepEden} · ${item.santiyeAdi}",
            style = MaterialTheme.typography.bodySmall,
            color = AppColors.TextSecondary,
            modifier = Modifier.padding(top = 4.dp, bottom = 8.dp)
        )
        if (item.teklifler.isNotEmpty()) {
            val oneri = item.onerilenTeklif()
            if (oneri != null) {
                Text(
                    "Satınalma önerisi: ${oneri.firmaAdi} — %.2f TL".format(oneri.genelToplam),
                    style = MaterialTheme.typography.bodySmall,
                    color = AppColors.Primary,
                    modifier = Modifier.padding(bottom = 8.dp)
                )
            }
            OutlinedButton(
                onClick = { SatinalmaPdfHelper.karsilastirmaPaylas(context, item, viewModel.pdfBaglam()) },
                modifier = Modifier.fillMaxWidth()
            ) { Text("Karşılaştırma PDF Paylaş") }
            Spacer(Modifier.height(8.dp))
            OutlinedButton(
                onClick = { SatinalmaPdfHelper.tedarikciTeklifTalebiPaylas(context, item, viewModel.pdfBaglam()) },
                modifier = Modifier.fillMaxWidth()
            ) { Text("Tedarikçi Teklif Talebi PDF") }
            Spacer(Modifier.height(8.dp))
        }

        item.kalemler.forEach { kalem ->
            AppCard {
                Column {
                    Text(kalem.malzeme, fontWeight = FontWeight.SemiBold, color = AppColors.TextPrimary)
                    Text("${kalem.miktar} ${kalem.birim}", color = AppColors.TextSecondary, modifier = Modifier.padding(bottom = 8.dp))
                    HorizontalDivider(color = AppColors.Border)
                    item.teklifler.filter { it.firmaAdi.isNotBlank() }.forEach { teklif ->
                        val fiyat = teklif.fiyatlar.firstOrNull { it.kalemId == kalem.id }
                        val tutar = fiyat?.toplamKdvDahil ?: 0.0
                        Row(
                            Modifier.fillMaxWidth().padding(vertical = 4.dp),
                            horizontalArrangement = Arrangement.SpaceBetween
                        ) {
                            Text(teklif.firmaAdi, color = AppColors.TextPrimary)
                            Text("%.2f TL".format(tutar), fontWeight = FontWeight.Medium, color = AppColors.Primary)
                        }
                    }
                }
            }
            Spacer(Modifier.height(8.dp))
        }

        if (item.teklifler.isNotEmpty()) {
            Text("Firma Toplamları", style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.SemiBold)
            Spacer(Modifier.height(8.dp))
            item.teklifler.filter { it.firmaAdi.isNotBlank() }.forEach { teklif ->
                TeklifOzetKarti(teklif)
                Spacer(Modifier.height(8.dp))
            }
        }

        error?.let { Text(it, color = AppColors.Danger, modifier = Modifier.padding(vertical = 8.dp)) }

        Button(
            onClick = { viewModel.navigate("teklif-gir?id=$talepId") },
            modifier = Modifier.fillMaxWidth(),
            colors = ButtonDefaults.buttonColors(containerColor = AppColors.Primary)
        ) { Text("Teklif Düzenle / Ekle") }

        if (canSend) {
            Spacer(Modifier.height(8.dp))
            Button(
                onClick = {
                    viewModel.sendQuotesToManagement(talepId) {
                        viewModel.navigate(IsAkisRotalari.yonetimGonderSonrasi(user?.role))
                    }
                },
                modifier = Modifier.fillMaxWidth(),
                colors = ButtonDefaults.buttonColors(containerColor = AppColors.Success)
            ) { Text("Yönetime Gönder") }
        }
    }
    }
}

@Composable
fun TeklifOnayDetayScreen(viewModel: AppViewModel, talepId: String) {
    val talep by viewModel.talepById(talepId).collectAsState(initial = null)
    val user by viewModel.user.collectAsState()
    val error by viewModel.submitError.collectAsState()
    var geriGonderGerekce by remember { mutableStateOf("") }

    if (talep == null) {
        Column(Modifier.fillMaxSize().padding(24.dp)) { Text("Talep bulunamadı.", color = AppColors.TextSecondary) }
        return
    }

    val item = talep!!
    val canDecide = KullaniciRolleri.canManagementDecide(user?.role) &&
        TalepKuyrugu.yonetimTeklifKarariBekliyor(item)
    val loading by viewModel.loading.collectAsState()
    var kararVardi by remember(item.id) { mutableStateOf(false) }

    androidx.compose.runtime.LaunchedEffect(canDecide, loading) {
        if (canDecide) kararVardi = true
        if (kararVardi && !canDecide && !loading) {
            viewModel.navigate(IsAkisRotalari.teklifOnayListesi(user?.role))
        }
    }
    val context = LocalContext.current
    var redGerekce by remember { mutableStateOf("") }
    val secimMap = remember(item.id) {
        mutableStateMapOf<String, String>().apply {
            item.kalemler.forEach { kalem ->
                kalem.onaylananTeklifId?.let { put(kalem.id, it) }
            }
        }
    }

    Column(Modifier.fillMaxSize().verticalScroll(rememberScrollState())) {
    AppScreenContent {
        Row(
            Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Text(item.talepNo, style = MaterialTheme.typography.headlineMedium, color = AppColors.TextPrimary)
            StatusBadge(item.durum, AppColors.PrimaryContainer, AppColors.Primary)
        }
        Text(
            "${item.talepEden} · ${item.santiyeAdi}",
            style = MaterialTheme.typography.bodySmall,
            color = AppColors.TextSecondary,
            modifier = Modifier.padding(top = 4.dp, bottom = 12.dp)
        )

        if (item.teklifDuzeltmeNotu.isNotBlank()) {
            AppCard {
                Column {
                    Text("Düzeltme Notu", fontWeight = FontWeight.SemiBold)
                    Text(item.teklifDuzeltmeNotu, color = AppColors.TextSecondary)
                }
            }
            Spacer(Modifier.height(12.dp))
        }

        item.onerilenTeklif()?.let { oneri ->
            AppCard(
                modifier = Modifier
                    .fillMaxWidth()
                    .border(1.dp, AppColors.Success, RoundedCornerShape(10.dp))
                    .background(AppColors.SuccessContainer, RoundedCornerShape(10.dp))
            ) {
                Column(Modifier.padding(12.dp)) {
                    Text("Satınalma Önerisi", fontWeight = FontWeight.SemiBold, color = AppColors.Success)
                    Text(
                        "${oneri.firmaAdi} — KDV Hariç: %.2f TL · KDV Dahil: %.2f TL".format(
                            oneri.araToplam,
                            oneri.genelToplam
                        ),
                        color = AppColors.Success,
                        modifier = Modifier.padding(top = 4.dp)
                    )
                    if (item.satinalmaOnerisiElleSecildi) {
                        Text("Elle seçilmiş öneri", style = MaterialTheme.typography.labelSmall, color = AppColors.TextSecondary)
                    }
                }
            }
            Spacer(Modifier.height(12.dp))
        }

        val quoteReviewUi = PurchaseRequestDetailPresenter.buildUiState(
            item,
            user?.role,
            PurchaseRequestDetailScreen.MANAGEMENT_QUOTE_REVIEW
        )
        val quoteRows = PurchaseRequestDetailPresenter.buildQuoteRows(item, user?.role)
        if (quoteReviewUi.showQuotesList && quoteRows.isNotEmpty()) {
            SectionTitle("Teklifler")
            quoteRows.forEach { row ->
                AppCard {
                    Row(
                        Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.SpaceBetween,
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Text(row.firmName, fontWeight = FontWeight.SemiBold)
                        if (row.canApprove) {
                            Button(
                                onClick = {
                                    viewModel.applyTalepDetayAction(
                                        talepId,
                                        PurchaseRequestDetailAction.APPROVE_QUOTE,
                                        quoteId = row.quoteId
                                    ) {
                                        viewModel.navigate(
                                            IsAkisRotalari.teklifOnaySonrasi(user?.role, talepId)
                                        )
                                    }
                                },
                                enabled = !loading,
                                colors = ButtonDefaults.buttonColors(containerColor = AppColors.Success)
                            ) {
                                Text(quoteReviewUi.labelFor(PurchaseRequestDetailAction.APPROVE_QUOTE))
                            }
                        }
                    }
                }
                Spacer(Modifier.height(8.dp))
            }
            Spacer(Modifier.height(8.dp))
        }

        Text(
            "Fiyat Karşılaştırma Tablosu",
            style = MaterialTheme.typography.titleMedium,
            fontWeight = FontWeight.SemiBold
        )
        Text(
            "Birim fiyatları karşılaştırın. Seçim için fiyat hücresine dokunun.",
            style = MaterialTheme.typography.bodySmall,
            color = AppColors.TextSecondary,
            modifier = Modifier.padding(top = 4.dp, bottom = 8.dp)
        )

        YonetimTeklifKarsilastirmaTablo(
            talep = item,
            secimMap = secimMap,
            secimAktif = canDecide,
            onKalemSec = { kalemId, teklifId -> secimMap[kalemId] = teklifId }
        )

        val seciliSayisi = item.kalemler.count { secimMap[it.id] != null }
        Text(
            if (seciliSayisi == 0) "Henüz kalem seçimi yapılmadı."
            else "$seciliSayisi/${item.kalemler.size} kalem için firma seçildi.",
            style = MaterialTheme.typography.bodySmall,
            color = AppColors.TextSecondary,
            modifier = Modifier.padding(top = 8.dp, bottom = 8.dp)
        )

        if (canDecide && item.onerilenTeklif() != null) {
            OutlinedButton(
                onClick = {
                    val oneriId = item.onerilenTeklif()!!.id
                    item.kalemler.forEach { secimMap[it.id] = oneriId }
                },
                modifier = Modifier.fillMaxWidth()
            ) { Text("Öneriyi tüm kalemlere uygula") }
            Spacer(Modifier.height(8.dp))
        }

        OutlinedButton(
            onClick = { SatinalmaPdfHelper.karsilastirmaPaylas(context, item, viewModel.pdfBaglam()) },
            modifier = Modifier.fillMaxWidth()
        ) { Text("Karşılaştırma PDF") }
        Spacer(Modifier.height(8.dp))
        OutlinedButton(
            onClick = { SatinalmaPdfHelper.yonetimOnayBelgesiPaylas(context, item, viewModel.pdfBaglam()) },
            modifier = Modifier.fillMaxWidth()
        ) { Text("Onay Belgesi Taslağı PDF") }
        Spacer(Modifier.height(12.dp))

        error?.let { Text(it, color = AppColors.Danger, modifier = Modifier.padding(vertical = 8.dp)) }

        if (canDecide) {
            Button(
                onClick = {
                    val atamalar = item.kalemler.mapNotNull { kalem ->
                        secimMap[kalem.id]?.let { kalem.id to it }
                    }.toMap()
                    if (atamalar.isEmpty()) return@Button
                    viewModel.kalemBazliOnayla(talepId, atamalar) {
                        viewModel.navigate(IsAkisRotalari.teklifOnaySonrasi(user?.role, talepId))
                    }
                },
                modifier = Modifier.fillMaxWidth(),
                enabled = !loading,
                colors = ButtonDefaults.buttonColors(containerColor = AppColors.Success)
            ) { Text(if (loading) "Lütfen bekleyin..." else "Teklifleri Onayla") }
            Spacer(Modifier.height(8.dp))

            OutlinedTextField(
                value = geriGonderGerekce,
                onValueChange = { geriGonderGerekce = it },
                modifier = Modifier.fillMaxWidth(),
                label = { Text("Geri gönderme gerekçesi (isteğe bağlı)") },
                shape = AppShapes.medium
            )
            Spacer(Modifier.height(8.dp))
            Button(
                onClick = {
                    viewModel.applyTalepDetayAction(
                        talepId,
                        PurchaseRequestDetailAction.SEND_QUOTES_FOR_REVISION,
                        note = geriGonderGerekce
                    ) {
                        viewModel.navigate(IsAkisRotalari.duzeltmeGonderSonrasi(user?.role))
                    }
                },
                modifier = Modifier.fillMaxWidth(),
                enabled = !loading,
                colors = ButtonDefaults.buttonColors(containerColor = AppColors.Warning)
            ) {
                Text(quoteReviewUi.labelFor(PurchaseRequestDetailAction.SEND_QUOTES_FOR_REVISION))
            }
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
                onClick = {
                    if (redGerekce.isBlank()) return@Button
                    viewModel.applyTalepDetayAction(
                        talepId,
                        PurchaseRequestDetailAction.REJECT_ENTIRE_REQUEST,
                        note = redGerekce
                    ) {
                        viewModel.navigate("red-talepler")
                    }
                },
                modifier = Modifier.fillMaxWidth(),
                enabled = !loading && redGerekce.isNotBlank(),
                colors = ButtonDefaults.buttonColors(containerColor = AppColors.Danger)
            ) {
                Text(quoteReviewUi.labelFor(PurchaseRequestDetailAction.REJECT_ENTIRE_REQUEST))
            }
        }
    }
    }
}

@Composable
private fun TeklifOzetKarti(teklif: TeklifItem) {
    AppCard {
        Column {
            DetailRow("Firma", teklif.firmaAdi)
            HorizontalDivider(color = AppColors.Border)
            DetailRow("Toplam", "%.2f TL".format(teklif.genelToplam))
            if (teklif.onaylandi) {
                Spacer(Modifier.height(4.dp))
                StatusBadge("Onaylandı", AppColors.SuccessContainer, AppColors.Success)
            }
        }
    }
}

@Composable
fun OnayGecmisiScreen(viewModel: AppViewModel) {
    TalepListScreen(viewModel, TalepQueue.ONAY_GECMISI)
}

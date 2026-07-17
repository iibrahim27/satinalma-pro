package com.satinalmapro.android.ui.procurement

import androidx.compose.foundation.border
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.AlertDialog
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
import androidx.compose.material3.OutlinedTextFieldDefaults
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateMapOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.satinalmapro.android.core.model.KalemFirmaAtamasi
import com.satinalmapro.android.core.model.TalepItem
import com.satinalmapro.android.core.model.TalepKalem
import com.satinalmapro.android.core.model.TalepQueue
import com.satinalmapro.android.core.model.TeklifItem
import com.satinalmapro.android.core.roles.IsAkisRotalari
import com.satinalmapro.android.core.roles.KalemFirmaAtamaYardimcisi
import com.satinalmapro.android.core.roles.KullaniciRolleri
import com.satinalmapro.android.core.roles.TalepKuyrugu
import com.satinalmapro.android.services.SatinalmaPdfFormats
import com.satinalmapro.android.services.SatinalmaPdfHelper
import com.satinalmapro.android.ui.AppViewModel
import com.satinalmapro.android.ui.components.AppCard
import com.satinalmapro.android.ui.components.AppDetailTabRow
import com.satinalmapro.android.ui.components.AppPrimaryButton
import com.satinalmapro.android.ui.components.AppScreenContent
import com.satinalmapro.android.ui.components.DetailRow
import com.satinalmapro.android.ui.components.SectionTitle
import com.satinalmapro.android.ui.components.StatusBadge
import com.satinalmapro.android.ui.theme.AppColors
import com.satinalmapro.android.ui.theme.AppShapes
import com.satinalmapro.android.ui.theme.AppSpacing
import com.satinalmapro.shared.filter.detail.PurchaseRequestDetailAction
import com.satinalmapro.shared.filter.detail.PurchaseRequestDetailPresenter
import com.satinalmapro.shared.filter.detail.PurchaseRequestDetailScreen
import com.satinalmapro.shared.filter.detail.PurchaseRequestDetailUiState
import com.satinalmapro.shared.filter.detail.PurchaseRequestQuoteRow

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
    val alinan by viewModel.alinanMalzemeKayitlari.collectAsState()
    var selectedTab by remember { mutableIntStateOf(0) }
    val tabs = listOf("Karşılaştırma", "Fiyat Analiz")
    val duzeltmeBekliyor = TalepKuyrugu.teklifDuzeltmeBekliyor(item)
    val canSend = KullaniciRolleri.canEnterQuotes(user?.role) &&
        (TalepKuyrugu.karsilastirma(item) || duzeltmeBekliyor) &&
        item.durum != com.satinalmapro.android.core.roles.TalepDurumlari.YONETIM_ONAY

    Column(Modifier.fillMaxSize().verticalScroll(rememberScrollState())) {
        AppScreenContent {
            Row(
                Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text(item.talepNo, style = MaterialTheme.typography.headlineMedium, color = AppColors.TextPrimary)
                StatusBadge(
                    if (duzeltmeBekliyor) "Düzeltme" else item.durum,
                    AppColors.PrimaryContainer,
                    AppColors.Primary
                )
            }
            Text(
                "${item.talepEden} · ${item.santiyeAdi} · ${item.teklifler.size} teklif",
                style = MaterialTheme.typography.bodySmall,
                color = AppColors.TextSecondary,
                modifier = Modifier.padding(top = 4.dp, bottom = 8.dp)
            )
            if (duzeltmeBekliyor) {
                AppCard(contentPadding = AppSpacing.sm) {
                    Text(
                        "Yönetim revize istedi — teklifleriniz silinmedi.",
                        style = MaterialTheme.typography.bodySmall,
                        fontWeight = FontWeight.SemiBold,
                        color = AppColors.Primary
                    )
                    if (item.teklifDuzeltmeNotu.isNotBlank()) {
                        Text(
                            "Not: ${item.teklifDuzeltmeNotu}",
                            style = MaterialTheme.typography.bodySmall,
                            color = AppColors.TextSecondary,
                            modifier = Modifier.padding(top = 4.dp)
                        )
                    }
                    Text(
                        "Aşağıda mevcut teklifler var. Düzenleyip «Yönetime Gönder» ile tekrar iletin. Liste: Düzeltme Bekleyen.",
                        style = MaterialTheme.typography.labelSmall,
                        color = AppColors.TextSecondary,
                        modifier = Modifier.padding(top = 4.dp)
                    )
                }
                Spacer(Modifier.height(8.dp))
            }
            AppDetailTabRow(tabs = tabs, selectedIndex = selectedTab, onTabSelected = { selectedTab = it })
        }

        AppScreenContent {
            when (selectedTab) {
                0 -> TeklifKarsilastirmaTabIcerik(
                    item = item,
                    talepId = talepId,
                    canSend = canSend,
                    error = error,
                    context = context,
                    viewModel = viewModel,
                    userRole = user?.role
                )
                1 -> FiyatAnalizTabContent(
                    talep = item,
                    alinanMalzemeler = alinan,
                    onRefreshAlinan = { viewModel.refreshAlinanMalzemeler() }
                )
            }
        }
    }
}

@Composable
private fun TeklifKarsilastirmaTabIcerik(
    item: TalepItem,
    talepId: String,
    canSend: Boolean,
    error: String?,
    context: android.content.Context,
    viewModel: AppViewModel,
    userRole: String?
) {
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
            onClick = {
                viewModel.withPdfBaglam { baglam ->
                    SatinalmaPdfHelper.karsilastirmaPaylas(context, item, baglam)
                }
            },
            modifier = Modifier.fillMaxWidth()
        ) { Text("Karşılaştırma PDF Paylaş") }
        Spacer(Modifier.height(8.dp))
        OutlinedButton(
            onClick = {
                viewModel.withPdfBaglam { baglam ->
                    SatinalmaPdfHelper.tedarikciTeklifTalebiPaylas(context, item, baglam)
                }
            },
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
                    viewModel.navigate(IsAkisRotalari.yonetimGonderSonrasi(userRole))
                }
            },
            modifier = Modifier.fillMaxWidth(),
            colors = ButtonDefaults.buttonColors(containerColor = AppColors.Success)
        ) { Text("Yönetime Gönder") }
    }
}

@Composable
fun TeklifOnayDetayScreen(viewModel: AppViewModel, talepId: String) {
    val talep by viewModel.talepById(talepId).collectAsState(initial = null)
    val user by viewModel.user.collectAsState()
    val error by viewModel.submitError.collectAsState()
    val alinan by viewModel.alinanMalzemeKayitlari.collectAsState()
    var selectedTab by remember { mutableIntStateOf(0) }
    val tabs = listOf("Teklif Onay", "Fiyat Analiz")
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

    LaunchedEffect(canDecide, loading) {
        if (canDecide) kararVardi = true
        if (kararVardi && !canDecide && !loading) {
            viewModel.navigate(IsAkisRotalari.teklifOnayListesi(user?.role))
        }
    }
    val context = LocalContext.current
    var redGerekce by remember { mutableStateOf("") }
    val atamaMap = remember(item.id) {
        mutableStateMapOf<String, List<KalemFirmaAtamasi>>().apply {
            item.kalemler.forEach { kalem ->
                val atamalar = KalemFirmaAtamaYardimcisi.etkinAtamalar(kalem)
                if (atamalar.isNotEmpty()) put(kalem.id, atamalar)
            }
        }
    }

    val quoteReviewUi = PurchaseRequestDetailPresenter.buildUiState(
        item,
        user?.role,
        PurchaseRequestDetailScreen.MANAGEMENT_QUOTE_REVIEW
    )
    val quoteRows = PurchaseRequestDetailPresenter.buildQuoteRows(item, user?.role)
    val compactPad = PaddingValues(horizontal = 12.dp, vertical = 6.dp)
    val fieldColors = OutlinedTextFieldDefaults.colors(
        focusedContainerColor = AppColors.Surface,
        unfocusedContainerColor = AppColors.Surface
    )

    Column(
        Modifier
            .fillMaxSize()
            .verticalScroll(rememberScrollState())
            .padding(horizontal = AppSpacing.screenHorizontal, vertical = AppSpacing.sm),
        verticalArrangement = Arrangement.spacedBy(AppSpacing.sm)
    ) {
        Row(
            Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Text(item.talepNo, style = MaterialTheme.typography.titleLarge, color = AppColors.TextPrimary)
            StatusBadge(item.durum, AppColors.PrimaryContainer, AppColors.Primary)
        }
        Text(
            "${item.talepEden} · ${item.santiyeAdi}",
            style = MaterialTheme.typography.bodySmall,
            color = AppColors.TextSecondary
        )

        AppDetailTabRow(tabs = tabs, selectedIndex = selectedTab, onTabSelected = { selectedTab = it })

        when (selectedTab) {
            0 -> TeklifOnayTabIcerik(
                item = item,
                talepId = talepId,
                canDecide = canDecide,
                loading = loading,
                error = error,
                context = context,
                viewModel = viewModel,
                userRole = user?.role,
                quoteReviewUi = quoteReviewUi,
                quoteRows = quoteRows,
                atamaMap = atamaMap,
                compactPad = compactPad,
                fieldColors = fieldColors,
                geriGonderGerekce = geriGonderGerekce,
                onGeriGonderGerekce = { geriGonderGerekce = it },
                redGerekce = redGerekce,
                onRedGerekce = { redGerekce = it }
            )
            1 -> FiyatAnalizTabContent(
                talep = item,
                alinanMalzemeler = alinan,
                onRefreshAlinan = { viewModel.refreshAlinanMalzemeler() }
            )
        }

        Spacer(Modifier.height(AppSpacing.md))
    }
}

@Composable
private fun TeklifOnayTabIcerik(
    item: TalepItem,
    talepId: String,
    canDecide: Boolean,
    loading: Boolean,
    error: String?,
    context: android.content.Context,
    viewModel: AppViewModel,
    userRole: String?,
    quoteReviewUi: PurchaseRequestDetailUiState,
    quoteRows: List<PurchaseRequestQuoteRow>,
    atamaMap: MutableMap<String, List<KalemFirmaAtamasi>>,
    compactPad: PaddingValues,
    fieldColors: androidx.compose.material3.TextFieldColors,
    geriGonderGerekce: String,
    onGeriGonderGerekce: (String) -> Unit,
    redGerekce: String,
    onRedGerekce: (String) -> Unit
) {
    var bolDialog by remember { mutableStateOf<Pair<TalepKalem, TeklifItem>?>(null) }
    var bolMiktar by remember { mutableStateOf("") }
    Column(verticalArrangement = Arrangement.spacedBy(AppSpacing.sm)) {
        if (item.teklifDuzeltmeNotu.isNotBlank()) {
            AppCard(contentPadding = AppSpacing.sm) {
                Text("Düzeltme Notu", fontWeight = FontWeight.SemiBold, style = MaterialTheme.typography.labelLarge)
                Text(
                    item.teklifDuzeltmeNotu,
                    color = AppColors.TextSecondary,
                    style = MaterialTheme.typography.bodySmall,
                    modifier = Modifier.padding(top = 2.dp)
                )
            }
        }

        item.onerilenTeklif()?.let { oneri ->
            AppCard(
                contentPadding = AppSpacing.sm,
                containerColor = AppColors.SuccessContainer,
                modifier = Modifier
                    .fillMaxWidth()
                    .border(1.dp, AppColors.Success, AppShapes.medium)
            ) {
                Text("Satınalma Önerisi", fontWeight = FontWeight.SemiBold, color = AppColors.Success)
                Text(
                    oneri.firmaAdi,
                    fontWeight = FontWeight.Bold,
                    color = AppColors.Success,
                    modifier = Modifier.padding(top = 2.dp)
                )
                Text(
                    "KDV Hariç: %.2f TL".format(oneri.araToplam),
                    style = MaterialTheme.typography.bodySmall,
                    color = AppColors.Success
                )
                Text(
                    "KDV Dahil: %.2f TL".format(oneri.genelToplam),
                    style = MaterialTheme.typography.bodySmall,
                    color = AppColors.Success
                )
                if (item.satinalmaOnerisiElleSecildi) {
                    Text(
                        "Elle seçilmiş öneri",
                        style = MaterialTheme.typography.labelSmall,
                        color = AppColors.TextSecondary,
                        modifier = Modifier.padding(top = 2.dp)
                    )
                }
            }
        }

        if (quoteReviewUi.showQuotesList && quoteRows.isNotEmpty()) {
            SectionTitle("Teklifler")
            Column(verticalArrangement = Arrangement.spacedBy(AppSpacing.xs)) {
                quoteRows.forEach { row ->
                    AppCard(contentPadding = AppSpacing.sm) {
                        Row(
                            Modifier.fillMaxWidth(),
                            horizontalArrangement = Arrangement.SpaceBetween,
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            Text(
                                row.firmName,
                                fontWeight = FontWeight.SemiBold,
                                modifier = Modifier.weight(1f, fill = false)
                            )
                            if (row.canApprove) {
                                Button(
                                    onClick = {
                                        viewModel.applyTalepDetayAction(
                                            talepId,
                                            PurchaseRequestDetailAction.APPROVE_QUOTE,
                                            quoteId = row.quoteId
                                        ) {
                                            viewModel.navigate(
                                                IsAkisRotalari.teklifOnaySonrasi(userRole, talepId)
                                            )
                                        }
                                    },
                                    enabled = !loading,
                                    contentPadding = compactPad,
                                    modifier = Modifier.heightIn(min = 36.dp),
                                    colors = ButtonDefaults.buttonColors(containerColor = AppColors.Success)
                                ) {
                                    Text(
                                        quoteReviewUi.labelFor(PurchaseRequestDetailAction.APPROVE_QUOTE),
                                        style = MaterialTheme.typography.labelLarge
                                    )
                                }
                            }
                        }
                    }
                }
            }
        }

        Row(
            Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Text(
                "Fiyat Karşılaştırma",
                style = MaterialTheme.typography.titleMedium,
                fontWeight = FontWeight.SemiBold
            )
            if (canDecide && item.onerilenTeklif() != null) {
                OutlinedButton(
                    onClick = {
                        val oneriId = item.onerilenTeklif()!!.id
                        item.kalemler.forEach { kalem ->
                            atamaMap[kalem.id] = listOf(
                                KalemFirmaAtamasi(teklifId = oneriId, miktar = kalem.miktar)
                            )
                        }
                    },
                    contentPadding = compactPad,
                    modifier = Modifier.heightIn(min = 32.dp)
                ) {
                    Text("Öneriyi uygula", style = MaterialTheme.typography.labelLarge)
                }
            }
        }
        Text(
            "Dokun: tüm miktar o firmaya. Uzun bas: miktarı böl (ör. 80 A + 20 B).",
            style = MaterialTheme.typography.bodySmall,
            color = AppColors.TextSecondary
        )

        YonetimTeklifKarsilastirmaTablo(
            talep = item,
            atamaMap = atamaMap,
            secimAktif = canDecide,
            onKalemTamSec = { kalemId, teklifId ->
                val kalem = item.kalemler.firstOrNull { it.id == kalemId } ?: return@YonetimTeklifKarsilastirmaTablo
                atamaMap[kalemId] = listOf(KalemFirmaAtamasi(teklifId = teklifId, miktar = kalem.miktar))
            },
            onKalemBol = { kalemId, teklifId ->
                val kalem = item.kalemler.firstOrNull { it.id == kalemId } ?: return@YonetimTeklifKarsilastirmaTablo
                val teklif = item.teklifler.firstOrNull { it.id == teklifId } ?: return@YonetimTeklifKarsilastirmaTablo
                val mevcut = atamaMap[kalemId].orEmpty()
                    .firstOrNull { it.teklifId.equals(teklifId, true) }?.miktar
                    ?: (kalem.miktar - atamaMap[kalemId].orEmpty()
                        .filter { !it.teklifId.equals(teklifId, true) }
                        .sumOf { it.miktar }).coerceAtLeast(0.0).let { if (it <= 0.0001) kalem.miktar else it }
                bolMiktar = SatinalmaPdfFormats.miktar(mevcut)
                bolDialog = kalem to teklif
            }
        )

        val seciliSayisi = item.kalemler.count { !atamaMap[it.id].isNullOrEmpty() }
        val ozet = item.kalemler
            .filter { !atamaMap[it.id].isNullOrEmpty() }
            .joinToString(" · ") { k ->
                val metin = KalemFirmaAtamaYardimcisi.ozetMetni(
                    k.copy(firmaAtamalari = atamaMap[k.id].orEmpty(), onaylananTeklifId = atamaMap[k.id]?.firstOrNull()?.teklifId),
                    item.teklifler
                )
                "${k.malzeme}: $metin"
            }
        Text(
            when {
                seciliSayisi == 0 -> "Henüz kalem seçimi yapılmadı."
                else -> "$seciliSayisi/${item.kalemler.size} kalem · $ozet"
            },
            style = MaterialTheme.typography.bodySmall,
            color = AppColors.TextSecondary
        )

        bolDialog?.let { (kalem, teklif) ->
            AlertDialog(
                onDismissRequest = { bolDialog = null },
                title = { Text("Miktar böl") },
                text = {
                    Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                        Text(
                            "${kalem.malzeme} — ${teklif.firmaAdi}\nTalep: ${SatinalmaPdfFormats.miktar(kalem.miktar)} ${kalem.birim}",
                            style = MaterialTheme.typography.bodySmall
                        )
                        OutlinedTextField(
                            value = bolMiktar,
                            onValueChange = { bolMiktar = it },
                            label = { Text("Bu firmaya miktar") },
                            singleLine = true,
                            shape = AppShapes.medium,
                            colors = fieldColors
                        )
                    }
                },
                confirmButton = {
                    TextButton(onClick = {
                        val m = bolMiktar.replace(',', '.').toDoubleOrNull()
                        if (m == null || m <= 0) return@TextButton
                        try {
                            val taslak = kalem.copy(
                                firmaAtamalari = atamaMap[kalem.id].orEmpty(),
                                onaylananTeklifId = atamaMap[kalem.id]?.maxByOrNull { it.miktar }?.teklifId
                            )
                            val guncel = KalemFirmaAtamaYardimcisi.firmaMiktariniAyarla(taslak, teklif.id, m)
                            atamaMap[kalem.id] = guncel.firmaAtamalari
                            bolDialog = null
                        } catch (_: Exception) {
                            // dialog açık kalır; kullanıcı miktarı düzeltir
                        }
                    }) { Text("Kaydet") }
                },
                dismissButton = {
                    TextButton(onClick = { bolDialog = null }) { Text("İptal") }
                }
            )
        }

        Row(
            Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.spacedBy(AppSpacing.sm)
        ) {
            OutlinedButton(
                onClick = {
                    viewModel.withPdfBaglam { baglam ->
                        SatinalmaPdfHelper.karsilastirmaPaylas(context, item, baglam)
                    }
                },
                modifier = Modifier.weight(1f).heightIn(min = 40.dp),
                contentPadding = compactPad
            ) { Text("Fiyat Karşılaştırma PDF", style = MaterialTheme.typography.labelLarge) }
            OutlinedButton(
                onClick = {
                    viewModel.withPdfBaglam { baglam ->
                        SatinalmaPdfHelper.yonetimOnayBelgesiPaylas(context, item, baglam)
                    }
                },
                modifier = Modifier.weight(1f).heightIn(min = 40.dp),
                contentPadding = compactPad
            ) { Text("Onay Belgesi PDF", style = MaterialTheme.typography.labelLarge) }
        }

        error?.let { Text(it, color = AppColors.Danger, style = MaterialTheme.typography.bodySmall) }

        if (canDecide) {
            HorizontalDivider(color = AppColors.Border, modifier = Modifier.padding(vertical = AppSpacing.xs))

            Button(
                onClick = {
                    val firmaAtamalari = atamaMap
                        .filterValues { it.isNotEmpty() }
                        .mapValues { it.value }
                    if (firmaAtamalari.isEmpty()) return@Button
                    try {
                        item.kalemler.filter { firmaAtamalari.containsKey(it.id) }.forEach { kalem ->
                            KalemFirmaAtamaYardimcisi.dogrula(kalem, firmaAtamalari[kalem.id]!!)
                        }
                    } catch (_: Exception) {
                        return@Button
                    }
                    viewModel.kalemBazliOnaylaBolunmus(talepId, firmaAtamalari) {
                        viewModel.navigate(IsAkisRotalari.teklifOnaySonrasi(userRole, talepId))
                    }
                },
                modifier = Modifier.fillMaxWidth().heightIn(min = 44.dp),
                enabled = !loading,
                contentPadding = compactPad,
                colors = ButtonDefaults.buttonColors(containerColor = AppColors.Success)
            ) { Text(if (loading) "Lütfen bekleyin..." else "Teklifleri Onayla") }

            OutlinedTextField(
                value = geriGonderGerekce,
                onValueChange = onGeriGonderGerekce,
                modifier = Modifier.fillMaxWidth().heightIn(min = 48.dp),
                label = { Text("Revize gerekçesi (isteğe bağlı)") },
                singleLine = true,
                shape = AppShapes.medium,
                colors = fieldColors
            )
            Button(
                onClick = {
                    viewModel.applyTalepDetayAction(
                        talepId,
                        PurchaseRequestDetailAction.SEND_QUOTES_FOR_REVISION,
                        note = geriGonderGerekce
                    ) {
                        viewModel.navigate(IsAkisRotalari.duzeltmeGonderSonrasi(userRole))
                    }
                },
                modifier = Modifier.fillMaxWidth().heightIn(min = 40.dp),
                enabled = !loading,
                contentPadding = compactPad,
                colors = ButtonDefaults.buttonColors(containerColor = AppColors.Warning)
            ) {
                Text(quoteReviewUi.labelFor(PurchaseRequestDetailAction.SEND_QUOTES_FOR_REVISION))
            }

            OutlinedTextField(
                value = redGerekce,
                onValueChange = onRedGerekce,
                modifier = Modifier.fillMaxWidth().heightIn(min = 48.dp),
                label = { Text("Red gerekçesi") },
                singleLine = true,
                shape = AppShapes.medium,
                colors = fieldColors
            )
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
                modifier = Modifier.fillMaxWidth().heightIn(min = 40.dp),
                enabled = !loading && redGerekce.isNotBlank(),
                contentPadding = compactPad,
                colors = ButtonDefaults.buttonColors(containerColor = AppColors.Danger)
            ) {
                Text(quoteReviewUi.labelFor(PurchaseRequestDetailAction.REJECT_ENTIRE_REQUEST))
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

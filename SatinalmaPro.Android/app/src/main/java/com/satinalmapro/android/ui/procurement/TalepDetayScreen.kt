package com.satinalmapro.android.ui.procurement

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
import androidx.compose.material3.AlertDialog
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
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.unit.dp
import com.satinalmapro.android.core.model.OnaylananMalzemeSatiri
import com.satinalmapro.android.core.roles.KullaniciRolleri
import com.satinalmapro.android.core.roles.OnaylananMalzemeOlusturucu
import com.satinalmapro.android.core.roles.TalepDurumlari
import com.satinalmapro.android.core.roles.TalepKuyrugu
import com.satinalmapro.android.core.roles.IsAkisRotalari
import com.satinalmapro.android.core.roles.TalepDurumEtiketi
import com.satinalmapro.shared.filter.detail.PurchaseRequestDetailAction
import com.satinalmapro.shared.filter.detail.PurchaseRequestDetailPresenter
import com.satinalmapro.shared.filter.detail.PurchaseRequestDetailScreen
import com.satinalmapro.android.core.roles.TalepTurleri
import com.satinalmapro.android.core.roles.TalepYetkileri
import com.satinalmapro.android.services.SatinalmaPdfHelper
import com.satinalmapro.android.ui.AppViewModel
import com.satinalmapro.android.ui.components.AppCard
import com.satinalmapro.android.ui.components.DetailRow
import com.satinalmapro.android.ui.procurement.MalKabulDialog
import com.satinalmapro.android.ui.components.StatusBadge
import androidx.compose.runtime.mutableIntStateOf
import com.satinalmapro.android.ui.components.AppDetailTabRow
import com.satinalmapro.android.ui.components.AppPrimaryButton
import com.satinalmapro.android.ui.components.AppScreenContent
import com.satinalmapro.android.ui.components.SectionTitle
import com.satinalmapro.android.ui.theme.AppColors
import com.satinalmapro.android.ui.theme.AppShapes
import com.satinalmapro.android.ui.theme.AppSpacing

@Composable
fun TalepDetayScreen(viewModel: AppViewModel, talepId: String, viewMode: String? = null) {
    val talep by viewModel.talepById(talepId).collectAsState(initial = null)
    val user by viewModel.user.collectAsState()
    val error by viewModel.submitError.collectAsState()
    var redGerekce by remember { mutableStateOf("") }
    val context = LocalContext.current
    var silOnay by remember { mutableStateOf(false) }
    var malKabulSatir by remember { mutableStateOf<OnaylananMalzemeSatiri?>(null) }
    var sevkiyatTamamlaSatir by remember { mutableStateOf<OnaylananMalzemeSatiri?>(null) }

    if (talep == null) {
        Column(Modifier.fillMaxSize().padding(24.dp)) { Text("Talep bulunamadı.", color = AppColors.TextSecondary) }
        return
    }

    val item = talep!!
    val role = user?.role
    val malzemeler = remember(item.id, viewMode, item.durum) {
        val goster = viewMode in setOf("malkabul", "siparis", "onaylanan") ||
            item.durum == TalepDurumlari.SIPARIS ||
            item.durum == TalepDurumlari.ONAYLANDI
        if (goster) {
            OnaylananMalzemeOlusturucu.olustur(listOf(item)).filter { it.talepId == item.id }
        } else emptyList()
    }
    val readOnlyView = viewMode in setOf("malkabul", "siparis", "onaylanan")
    val acilTalep = item.talepTuru == TalepTurleri.ACIL
    val yonetimKarar = KullaniciRolleri.canManagementDecide(role) &&
        (TalepKuyrugu.yonetimTalepler(item) || TalepKuyrugu.yonetimTeklifBekleyen(item))
    val teklifOnay = KullaniciRolleri.canManagementDecide(role) &&
        TalepKuyrugu.yonetimTeklifKarariBekliyor(item)
    val teklifGir = KullaniciRolleri.canEnterQuotes(role) && TalepKuyrugu.teklifGirisi(item)
    val karsilastirma = KullaniciRolleri.canEnterQuotes(role) && TalepKuyrugu.karsilastirma(item)
    val yonetimeGonder = KullaniciRolleri.canEnterQuotes(role) &&
        item.teklifler.isNotEmpty() &&
        TalepKuyrugu.karsilastirma(item) &&
        item.durum != TalepDurumlari.YONETIM_ONAY
    val teklifsizFirmaGir = KullaniciRolleri.canEnterQuotes(role) &&
        TalepKuyrugu.teklifsizFirmaFiyatBekliyor(item)
    val canPlaceOrder = KullaniciRolleri.canPlaceOrder(role) && item.durum == TalepDurumlari.ONAYLANDI
    val canMalKabul = KullaniciRolleri.canMalKabul(role)
    val duzenle = TalepYetkileri.talepDuzenleyebilir(role, item, user?.uid, user?.fullName)
    val sil = TalepYetkileri.talepSilebilir(role, item, user?.uid, user?.fullName)
    val loading by viewModel.loading.collectAsState()
    var yonetimKararVardi by remember(item.id) { mutableStateOf(false) }

    androidx.compose.runtime.LaunchedEffect(yonetimKarar, loading) {
        if (yonetimKarar) yonetimKararVardi = true
        if (yonetimKararVardi && !yonetimKarar && !loading && viewMode == null) {
            val hedef = when (item.durum) {
                TalepDurumlari.TEKLIF_GIRISI -> IsAkisRotalari.teklifIsteSonrasi(role)
                TalepDurumlari.REDDEDILDI -> "red-talepler"
                TalepDurumlari.ONAYLANDI, TalepDurumlari.SIPARIS -> IsAkisRotalari.gecmisTalepListe(role)
                else -> "gelen-talepler"
            }
            viewModel.navigate(hedef)
        }
    }

    var selectedTab by remember { mutableIntStateOf(0) }
    val tabs = listOf("Özet", "Geçmiş", "Belgeler")

    Column(
        modifier = Modifier.fillMaxSize().verticalScroll(rememberScrollState())
    ) {
        AppScreenContent {
            RowHeader(item.talepNo.ifBlank { "Talep" }, item.durum)
            if (viewMode == "malkabul") {
                Text("Mal kabul tamamlanan talep özeti", style = MaterialTheme.typography.bodySmall, color = AppColors.Success)
            } else if (viewMode == "siparis") {
                Text("Sipariş verilmiş talep özeti", style = MaterialTheme.typography.bodySmall, color = AppColors.Primary)
            } else if (viewMode == "onaylanan") {
                Text("Onaylanmış talep — sipariş bekliyor", style = MaterialTheme.typography.bodySmall, color = AppColors.Warning)
            }
            AppDetailTabRow(tabs = tabs, selectedIndex = selectedTab, onTabSelected = { selectedTab = it })
        }

        AppScreenContent {
            when (selectedTab) {
                0 -> OzetTabContent(
                    item, viewMode, acilTalep, readOnlyView, malzemeler, canMalKabul,
                    malKabulSatir, { malKabulSatir = it },
                    teklifsizFirmaGir, canPlaceOrder, teklifGir, karsilastirma, yonetimeGonder,
                    duzenle, sil, silOnay, { silOnay = it }, yonetimKarar, redGerekce, { redGerekce = it },
                    teklifOnay, error, viewModel, talepId, loading, role
                )
                1 -> GecmisTabContent(
                    item, viewMode, malzemeler, canMalKabul, malKabulSatir, { malKabulSatir = it },
                    { sevkiyatTamamlaSatir = it }
                )
                2 -> BelgelerTabContent(context, item, viewModel)
            }
        }
    }

    malKabulSatir?.let { satir ->
        MalKabulDialog(
            satir = satir,
            onDismiss = { malKabulSatir = null },
            onConfirm = { form ->
                viewModel.malKabulVeDepoyaKaydet(
                    talepId = item.id,
                    kalemId = satir.kalemId,
                    form = form
                ) { malKabulSatir = null }
            }
        )
    }

    sevkiyatTamamlaSatir?.let { satir ->
        AlertDialog(
            onDismissRequest = { sevkiyatTamamlaSatir = null },
            title = { Text("Sevkiyatı Tamamla") },
            text = {
                Text(
                    "${satir.malzeme}\n\nSipariş: ${satir.siparisMiktari} ${satir.birim}\n" +
                        "Kabul edilen: ${satir.kabulEdilenMiktar} ${satir.birim}\n\n" +
                        "Talep ve teklif miktarları kabul edilen miktara göre güncellenecek."
                )
            },
            confirmButton = {
                Button(onClick = {
                    viewModel.sevkiyatiTamamla(item.id, satir.kalemId) {
                        sevkiyatTamamlaSatir = null
                    }
                }) { Text("Tamamla") }
            },
            dismissButton = {
                TextButton(onClick = { sevkiyatTamamlaSatir = null }) { Text("İptal") }
            }
        )
    }
}

@Composable
private fun OzetTabContent(
    item: com.satinalmapro.android.core.model.TalepItem,
    viewMode: String?,
    acilTalep: Boolean,
    readOnlyView: Boolean,
    malzemeler: List<OnaylananMalzemeSatiri>,
    canMalKabul: Boolean,
    malKabulSatir: OnaylananMalzemeSatiri?,
    onMalKabulSatir: (OnaylananMalzemeSatiri?) -> Unit,
    teklifsizFirmaGir: Boolean,
    canPlaceOrder: Boolean,
    teklifGir: Boolean,
    karsilastirma: Boolean,
    yonetimeGonder: Boolean,
    duzenle: Boolean,
    sil: Boolean,
    silOnay: Boolean,
    onSilOnay: (Boolean) -> Unit,
    yonetimKarar: Boolean,
    redGerekce: String,
    onRedGerekce: (String) -> Unit,
    teklifOnay: Boolean,
    error: String?,
    viewModel: AppViewModel,
    talepId: String,
    loading: Boolean,
    role: String?
) {
    if (item.talepTuru != TalepTurleri.NORMAL) {
        StatusBadge(
            TalepTurleri.gorunenAd(item.talepTuru),
            if (acilTalep) AppColors.DangerContainer else AppColors.WarningContainer,
            if (acilTalep) AppColors.Danger else AppColors.Warning
        )
    }
    AppCard {
        Column {
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
    SectionTitle("Kalemler")
    item.kalemler.forEach { kalem ->
        AppCard {
            Column {
                DetailRow("Malzeme", kalem.malzeme)
                HorizontalDivider(color = AppColors.Border)
                DetailRow("Miktar", "${kalem.miktar} ${kalem.birim}")
                if (kalem.kabulEdilenMiktar > 0) {
                    HorizontalDivider(color = AppColors.Border)
                    DetailRow("Kabul", "${kalem.kabulEdilenMiktar} / ${kalem.miktar}")
                }
            }
        }
    }

    val context = LocalContext.current
    OutlinedButton(
        onClick = {
            viewModel.withPdfBaglam { baglam ->
                SatinalmaPdfHelper.talepFormuPaylas(context, item, baglam)
            }
        },
        modifier = Modifier.fillMaxWidth()
    ) { Text("İmzalı Talep PDF") }
    if (item.teklifler.any { it.firmaAdi.isNotBlank() }) {
        OutlinedButton(
            onClick = {
                viewModel.withPdfBaglam { baglam ->
                    SatinalmaPdfHelper.karsilastirmaPaylas(context, item, baglam)
                }
            },
            modifier = Modifier.fillMaxWidth()
        ) { Text("Fiyat Karşılaştırma PDF") }
    }

    if (!readOnlyView) {
        error?.let { Text(it, color = AppColors.Danger) }
        if (teklifsizFirmaGir) {
            AppPrimaryButton("Firma / Fiyat Gir", onClick = { viewModel.navigate("teklifsiz-firma-fiyat?id=${item.id}") })
        }
        if (canPlaceOrder) {
            AppPrimaryButton("Sipariş Ver", onClick = { viewModel.siparisVer(item.id) {} })
        }
        if (teklifGir) {
            AppPrimaryButton("Teklif Gir", onClick = { viewModel.navigate("teklif-gir?id=${item.id}") })
        }
        if (karsilastirma && item.teklifler.size > 1) {
            OutlinedButton(onClick = { viewModel.navigate("teklif-karsilastirma?id=${item.id}") }, modifier = Modifier.fillMaxWidth()) {
                Text("Teklif Karşılaştırması")
            }
        }
        if (yonetimeGonder) {
            AppPrimaryButton("Yönetime Gönder", onClick = {
                viewModel.sendQuotesToManagement(item.id) {
                    viewModel.navigate(IsAkisRotalari.yonetimGonderSonrasi(role))
                }
            })
        }
        if (teklifOnay) {
            AppPrimaryButton("Teklif Onayı", onClick = {
                viewModel.navigate("teklif-onay-detay?id=${item.id}")
            })
        }
        if (duzenle) {
            OutlinedButton(onClick = { viewModel.navigate("talep-duzenle?id=${item.id}") }, modifier = Modifier.fillMaxWidth()) {
                Text("Talebi Düzenle")
            }
        }
        if (sil) {
            if (silOnay) {
                Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                    OutlinedButton(onClick = { onSilOnay(false) }, modifier = Modifier.weight(1f)) { Text("İptal") }
                    Button(
                        onClick = { viewModel.talepSil(item.id) { viewModel.navigate("taleplerim") } },
                        modifier = Modifier.weight(1f),
                        colors = ButtonDefaults.buttonColors(containerColor = AppColors.Danger)
                    ) { Text("Sil") }
                }
            } else {
                OutlinedButton(onClick = { onSilOnay(true) }, modifier = Modifier.fillMaxWidth()) {
                    Text("Talebi Sil", color = AppColors.Danger)
                }
            }
        }
        if (yonetimKarar && !item.teklifGirilmis) {
            val detailUi = PurchaseRequestDetailPresenter.buildUiState(
                item,
                role,
                PurchaseRequestDetailScreen.MANAGEMENT_SUBMITTED_REVIEW
            )
            if (detailUi.isVisible(PurchaseRequestDetailAction.DIRECT_APPROVE)) {
                if (acilTalep) {
                    Text(
                        "Acil talep — teklifsiz onaylanır.",
                        style = MaterialTheme.typography.bodySmall,
                        color = AppColors.Danger
                    )
                }
                AppPrimaryButton(
                    detailUi.labelFor(PurchaseRequestDetailAction.DIRECT_APPROVE),
                    loading = loading,
                    onClick = {
                        viewModel.applyTalepDetayAction(
                            item.id,
                            PurchaseRequestDetailAction.DIRECT_APPROVE
                        ) {
                            viewModel.navigate(IsAkisRotalari.teklifsizOnaySonrasi(role, item.id))
                        }
                    }
                )
            }
            if (detailUi.isVisible(PurchaseRequestDetailAction.START_QUOTE_PROCESS)) {
                Button(
                    onClick = {
                        viewModel.applyTalepDetayAction(
                            item.id,
                            PurchaseRequestDetailAction.START_QUOTE_PROCESS
                        ) {
                            viewModel.navigate(IsAkisRotalari.teklifIsteSonrasi(role))
                        }
                    },
                    modifier = Modifier.fillMaxWidth(),
                    enabled = !loading,
                    colors = ButtonDefaults.buttonColors(containerColor = AppColors.Warning)
                ) { Text(detailUi.labelFor(PurchaseRequestDetailAction.START_QUOTE_PROCESS)) }
            }
            if (detailUi.isVisible(PurchaseRequestDetailAction.REJECT_REQUEST)) {
                OutlinedTextField(
                    value = redGerekce,
                    onValueChange = onRedGerekce,
                    modifier = Modifier.fillMaxWidth(),
                    label = { Text("Red gerekçesi") },
                    shape = AppShapes.small
                )
                Button(
                    onClick = {
                        viewModel.applyTalepDetayAction(
                            item.id,
                            PurchaseRequestDetailAction.REJECT_REQUEST,
                            note = redGerekce
                        ) { viewModel.navigate("red-talepler") }
                    },
                    modifier = Modifier.fillMaxWidth(),
                    enabled = !loading,
                    colors = ButtonDefaults.buttonColors(containerColor = AppColors.Danger)
                ) { Text(detailUi.labelFor(PurchaseRequestDetailAction.REJECT_REQUEST)) }
            }
        }
    }
}

@Composable
private fun GecmisTabContent(
    item: com.satinalmapro.android.core.model.TalepItem,
    viewMode: String?,
    malzemeler: List<OnaylananMalzemeSatiri>,
    canMalKabul: Boolean,
    malKabulSatir: OnaylananMalzemeSatiri?,
    onMalKabulSatir: (OnaylananMalzemeSatiri?) -> Unit,
    onSevkiyatTamamlaSatir: (OnaylananMalzemeSatiri?) -> Unit
) {
    SectionTitle("İşlem Özeti")
    AppCard {
        Column {
            TalepDurumEtiketi.islemSatirlari(item).forEachIndexed { index, (etiket, deger) ->
                if (index > 0) HorizontalDivider(color = AppColors.Border)
                DetailRow(etiket, deger)
            }
        }
    }
    if (item.teklifler.isNotEmpty()) {
        SectionTitle("Teklifler")
        item.teklifler.forEach { teklif ->
            AppCard {
                Column {
                    DetailRow("Firma", teklif.firmaAdi)
                    HorizontalDivider(color = AppColors.Border)
                    DetailRow("Toplam", "%.2f TL".format(teklif.genelToplam))
                    if (teklif.onaylandi) StatusBadge("Onaylandı", AppColors.SuccessContainer, AppColors.Success)
                }
            }
        }
        if (item.herhangiKalemOnayli) {
            SectionTitle("Onaylanan Kalem Seçimleri")
            item.kalemler.forEach { kalem ->
                val teklif = item.teklifler.firstOrNull { it.id == kalem.onaylananTeklifId }
                if (teklif != null) {
                    AppCard {
                        Column {
                            DetailRow("Malzeme", kalem.malzeme)
                            HorizontalDivider(color = AppColors.Border)
                            DetailRow("Seçilen firma", teklif.firmaAdi)
                        }
                    }
                }
            }
        }
    } else {
        Text("Henüz teklif kaydı yok.", color = AppColors.TextSecondary, style = MaterialTheme.typography.bodyMedium)
    }
    if (malzemeler.isNotEmpty()) {
        val baslik = when (viewMode) {
            "malkabul" -> "Mal Kabul Özeti"
            "siparis" -> "Sipariş Kalemleri"
            else -> "Onaylanan Kalemler"
        }
        SectionTitle(baslik)
        malzemeler.forEach { satir ->
            MalzemeOzetKarti(
                satir = satir,
                viewMode = viewMode,
                canMalKabul = canMalKabul && OnaylananMalzemeOlusturucu.malKabulBekleyen(satir),
                canSevkiyatTamamla = canMalKabul && OnaylananMalzemeOlusturucu.sevkiyatTamamlanabilir(satir),
                onMalKabul = { onMalKabulSatir(satir) },
                onSevkiyatTamamla = { onSevkiyatTamamlaSatir(satir) }
            )
        }
    }
}

@Composable
private fun BelgelerTabContent(
    context: android.content.Context,
    item: com.satinalmapro.android.core.model.TalepItem,
    viewModel: AppViewModel
) {
    SectionTitle("Belgeler")
    OutlinedButton(
        onClick = {
            viewModel.withPdfBaglam { baglam ->
                SatinalmaPdfHelper.talepFormuPaylas(context, item, baglam)
            }
        },
        modifier = Modifier.fillMaxWidth()
    ) {
        Text("İmzalı Talep PDF")
    }
    if (item.teklifler.any { it.firmaAdi.isNotBlank() }) {
        OutlinedButton(
            onClick = {
                viewModel.withPdfBaglam { baglam ->
                    SatinalmaPdfHelper.karsilastirmaPaylas(context, item, baglam)
                }
            },
            modifier = Modifier.fillMaxWidth()
        ) {
            Text("Fiyat Karşılaştırma PDF")
        }
    }
    if (item.durum == TalepDurumlari.ONAYLANDI || item.durum == TalepDurumlari.SIPARIS) {
        if (item.herhangiKalemOnayli || item.teklifsizYonetimOnayi) {
            OutlinedButton(
                onClick = {
                    viewModel.withPdfBaglam { baglam ->
                        SatinalmaPdfHelper.yonetimOnayBelgesiPaylas(context, item, baglam)
                    }
                },
                modifier = Modifier.fillMaxWidth()
            ) {
                Text("Yönetim Onay PDF")
            }
        }
    }
    if (item.durum == TalepDurumlari.SIPARIS) {
        OutlinedButton(
            onClick = {
                viewModel.withPdfBaglam { baglam ->
                    SatinalmaPdfHelper.siparisFormuPaylas(context, item, baglam)
                }
            },
            modifier = Modifier.fillMaxWidth()
        ) {
            Text("Sipariş Formu PDF")
        }
    }
}

@Composable
private fun MalzemeOzetKarti(
    satir: OnaylananMalzemeSatiri,
    viewMode: String?,
    canMalKabul: Boolean = false,
    canSevkiyatTamamla: Boolean = false,
    onMalKabul: () -> Unit = {},
    onSevkiyatTamamla: () -> Unit = {}
) {
    AppCard {
        Column {
            DetailRow("Malzeme", satir.malzeme)
            HorizontalDivider(color = AppColors.Border)
            DetailRow("Miktar", "${satir.siparisMiktari} ${satir.birim}")
            if (satir.firma.isNotBlank()) {
                HorizontalDivider(color = AppColors.Border)
                DetailRow("Firma", satir.firma)
            }
            if (satir.siparisNo.isNotBlank()) {
                HorizontalDivider(color = AppColors.Border)
                DetailRow("Sipariş No", satir.siparisNo)
            }
            if (viewMode == "malkabul" || satir.kabulEdilenMiktar > 0) {
                HorizontalDivider(color = AppColors.Border)
                DetailRow("Kabul", "${satir.kabulEdilenMiktar} / ${satir.siparisMiktari}")
                if (satir.kalanMiktar <= 0.0001) {
                    Spacer(Modifier.height(4.dp))
                    StatusBadge("Tamamlandı", AppColors.SuccessContainer, AppColors.Success)
                }
            }
            if (canMalKabul) {
                AppPrimaryButton("Mal Kabul — Depoya Kaydet", onClick = onMalKabul)
            }
            if (canSevkiyatTamamla) {
                OutlinedButton(
                    onClick = onSevkiyatTamamla,
                    modifier = Modifier.fillMaxWidth().padding(top = if (canMalKabul) 8.dp else 0.dp)
                ) { Text("Sevkiyatı Tamamla") }
            }
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

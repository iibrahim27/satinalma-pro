package com.satinalmapro.android.ui.procurement

import androidx.compose.foundation.ExperimentalFoundationApi
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.combinedClickable
import androidx.compose.foundation.horizontalScroll
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import com.satinalmapro.android.core.model.KalemFirmaAtamasi
import com.satinalmapro.android.core.model.TalepItem
import com.satinalmapro.android.services.SatinalmaPdfFormats
import com.satinalmapro.android.ui.theme.AppColors
import java.util.Locale

@Composable
fun YonetimTeklifKarsilastirmaTablo(
    talep: TalepItem,
    atamaMap: Map<String, List<KalemFirmaAtamasi>>,
    secimAktif: Boolean,
    onKalemTamSec: (kalemId: String, teklifId: String) -> Unit,
    onKalemBol: (kalemId: String, teklifId: String) -> Unit,
    modifier: Modifier = Modifier
) {
    val teklifler = talep.teklifler.filter { it.firmaAdi.isNotBlank() }.sortedBy { it.firmaAdi.lowercase(Locale.ROOT) }
    if (teklifler.isEmpty()) return

    val onerilen = talep.onerilenTeklif()
    val kalemler = talep.kalemler.sortedBy { it.siraNo }
    val scroll = rememberScrollState()
    val firmaGen = 108.dp

    Column(
        modifier
            .fillMaxWidth()
            .clip(RoundedCornerShape(10.dp))
            .border(1.dp, AppColors.Border, RoundedCornerShape(10.dp))
            .background(AppColors.Surface)
            .padding(8.dp)
    ) {
        Row(Modifier.horizontalScroll(scroll)) {
            KarsilastirmaHucre(
                metin = "Malzeme",
                genislik = 140.dp,
                kalin = true,
                arkaPlan = AppColors.Background
            )
            teklifler.forEach { teklif ->
                val oneri = onerilen?.id == teklif.id
                KarsilastirmaHucre(
                    metin = if (oneri) "${teklif.firmaAdi}\n★ Öneri" else teklif.firmaAdi,
                    genislik = firmaGen,
                    kalin = true,
                    oneri = oneri,
                    arkaPlan = if (oneri) AppColors.SuccessContainer else AppColors.Background
                )
            }
        }
        HorizontalDivider(color = AppColors.Border)
        Row(Modifier.horizontalScroll(scroll)) {
            KarsilastirmaHucre("Birim fiyatı", 140.dp, kalin = true, arkaPlan = AppColors.Background)
            teklifler.forEach { teklif ->
                val oneri = onerilen?.id == teklif.id
                KarsilastirmaHucre(
                    "Birim Fiyat",
                    firmaGen,
                    kalin = true,
                    oneri = oneri,
                    arkaPlan = if (oneri) AppColors.SuccessContainer else AppColors.Background
                )
            }
        }
        HorizontalDivider(color = AppColors.Border)

        kalemler.forEach { kalem ->
            val fiyatlar = teklifler.map { teklif ->
                SatinalmaPdfFormats.teklifFiyati(talep, teklif, kalem.id)
            }
            val enDusuk = fiyatlar.filterNotNull().filter { it.toplamTutar > 0 }.minOfOrNull { it.toplamTutar }
            val atamalar = atamaMap[kalem.id].orEmpty()

            Row(Modifier.horizontalScroll(scroll)) {
                KarsilastirmaHucre(
                    metin = "${kalem.malzeme}\n${SatinalmaPdfFormats.miktar(kalem.miktar)} ${kalem.birim}",
                    genislik = 140.dp
                )
                teklifler.forEachIndexed { index, teklif ->
                    val fiyat = fiyatlar[index]
                    val oneri = onerilen?.id == teklif.id
                    val enDusukMu = fiyat != null && fiyat.toplamTutar > 0 && fiyat.toplamTutar == enDusuk
                    val atama = atamalar.firstOrNull { it.teklifId.equals(teklif.id, true) }
                    val secili = atama != null
                    var metin = fiyat?.let {
                        SatinalmaPdfFormats.birimFiyatGosterim(
                            it.birimFiyat,
                            it.paraBirimi,
                            teklif.usdKuru,
                            teklif.eurKuru
                        )
                    } ?: "—"
                    if (atama != null) {
                        metin = "$metin\n→ ${SatinalmaPdfFormats.miktar(atama.miktar)} ${kalem.birim}"
                    }
                    KarsilastirmaHucre(
                        metin = metin,
                        genislik = firmaGen,
                        oneri = oneri,
                        enDusuk = enDusukMu && !oneri,
                        secili = secili,
                        tiklanabilir = secimAktif && fiyat != null && fiyat.birimFiyat > 0,
                        onClick = { onKalemTamSec(kalem.id, teklif.id) },
                        onLongClick = { onKalemBol(kalem.id, teklif.id) }
                    )
                }
            }
            HorizontalDivider(color = AppColors.Border.copy(alpha = 0.5f))
        }

        Row(Modifier.horizontalScroll(scroll)) {
            KarsilastirmaHucre(
                metin = "ARA TOPLAM\n(KDV Hariç)",
                genislik = 140.dp,
                kalin = true,
                arkaPlan = AppColors.Background
            )
            teklifler.forEach { teklif ->
                val oneri = onerilen?.id == teklif.id
                KarsilastirmaHucre(
                    metin = SatinalmaPdfFormats.tl(teklif.araToplam),
                    genislik = firmaGen,
                    kalin = true,
                    oneri = oneri,
                    arkaPlan = if (oneri) AppColors.SuccessContainer else AppColors.Background
                )
            }
        }
    }
}

@OptIn(ExperimentalFoundationApi::class)
@Composable
private fun KarsilastirmaHucre(
    metin: String,
    genislik: androidx.compose.ui.unit.Dp,
    kalin: Boolean = false,
    oneri: Boolean = false,
    enDusuk: Boolean = false,
    secili: Boolean = false,
    tiklanabilir: Boolean = false,
    arkaPlan: androidx.compose.ui.graphics.Color = AppColors.Surface,
    onClick: () -> Unit = {},
    onLongClick: () -> Unit = {}
) {
    val zemin = when {
        secili -> AppColors.PrimaryContainer
        oneri -> AppColors.SuccessContainer
        enDusuk -> AppColors.SuccessContainer.copy(alpha = 0.35f)
        else -> arkaPlan
    }
    val cerceve = when {
        secili -> AppColors.Primary
        oneri -> AppColors.Success
        else -> AppColors.Border
    }
    val kalinlik = if (secili) 2.dp else 1.dp

    Box(
        modifier = Modifier
            .width(genislik)
            .padding(1.dp)
            .clip(RoundedCornerShape(4.dp))
            .border(kalinlik, cerceve, RoundedCornerShape(4.dp))
            .background(zemin)
            .then(
                if (tiklanabilir) {
                    Modifier.combinedClickable(onClick = onClick, onLongClick = onLongClick)
                } else Modifier
            )
            .padding(horizontal = 6.dp, vertical = 8.dp),
        contentAlignment = Alignment.Center
    ) {
        Text(
            text = metin,
            style = MaterialTheme.typography.labelSmall,
            fontWeight = if (kalin) FontWeight.SemiBold else FontWeight.Normal,
            color = when {
                oneri -> AppColors.Success
                secili -> AppColors.Primary
                else -> AppColors.TextPrimary
            },
            textAlign = if (genislik >= 120.dp) TextAlign.Center else TextAlign.Start
        )
    }
}

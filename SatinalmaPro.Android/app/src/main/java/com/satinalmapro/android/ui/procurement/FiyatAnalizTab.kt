package com.satinalmapro.android.ui.procurement

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.satinalmapro.android.core.model.AlinanMalzemeKaydi
import com.satinalmapro.android.core.model.TalepItem
import com.satinalmapro.android.services.KarsilastirmaAlimGecmisiYardimcisi
import com.satinalmapro.android.services.SatinalmaPdfFormats
import com.satinalmapro.android.ui.components.AppCard
import com.satinalmapro.android.ui.theme.AppColors
import com.satinalmapro.android.ui.theme.AppSpacing

/**
 * Teklif onay / karşılaştırma ekranlarındaki «Fiyat Analiz» sekmesi.
 * Son alım (Alınan Malzemeler modülü / veri/alinan_malzemeler) × en düşük teklif birim fiyatı.
 */
@Composable
fun FiyatAnalizTabContent(
    talep: TalepItem,
    alinanMalzemeler: List<AlinanMalzemeKaydi>,
    onRefreshAlinan: () -> Unit = {}
) {
    LaunchedEffect(talep.id) {
        onRefreshAlinan()
    }

    val satirlari = remember(talep.id, talep.kalemler, talep.teklifler, alinanMalzemeler) {
        val alimlar = KarsilastirmaAlimGecmisiYardimcisi.malzemeBazliAlimlariTopla(
            talep.kalemler,
            alinanMalzemeler
        )
        KarsilastirmaAlimGecmisiYardimcisi.malzemeBazliFiyatKarsilastirmasiTopla(
            talep.kalemler,
            talep.teklifler,
            alimlar
        )
    }

    Column(
        Modifier.fillMaxWidth(),
        verticalArrangement = Arrangement.spacedBy(AppSpacing.sm)
    ) {
        Text(
            "Talep kalemlerinin Alınan Malzemeler’deki son alımı ile teklifteki en düşük birim fiyatı.",
            style = MaterialTheme.typography.bodySmall,
            color = AppColors.TextSecondary
        )
        Text(
            "Artış = (en düşük teklif − son alınan) / son alınan. Negatif değer düşüşü gösterir.",
            style = MaterialTheme.typography.labelSmall,
            color = AppColors.TextSecondary
        )

        if (alinanMalzemeler.isEmpty()) {
            AppCard {
                Text(
                    "Alınan Malzemeler kaydı henüz yüklenmedi veya liste boş. Masaüstündeki Alınan Malzemeler modülü ile senkronu kontrol edin.",
                    color = AppColors.TextSecondary,
                    style = MaterialTheme.typography.bodyMedium
                )
            }
        }

        if (satirlari.isEmpty()) {
            AppCard {
                Text(
                    "Analiz için kalem bulunamadı.",
                    color = AppColors.TextSecondary,
                    style = MaterialTheme.typography.bodyMedium
                )
            }
            return
        }

        satirlari.forEach { satir ->
            AppCard(contentPadding = AppSpacing.sm) {
                Column(verticalArrangement = Arrangement.spacedBy(6.dp)) {
                    Text(
                        "${satir.kalemSiraNo}. ${satir.malzeme}",
                        fontWeight = FontWeight.SemiBold,
                        color = AppColors.TextPrimary,
                        style = MaterialTheme.typography.titleSmall
                    )

                    if (satir.sonAlimYok) {
                        Text(
                            if (alinanMalzemeler.isEmpty())
                                "Son alım kaydı yok (Alınan Malzemeler listesi boş)"
                            else
                                "Son alım kaydı yok — bu malzeme adı Alınan Malzemeler’de bulunamadı",
                            style = MaterialTheme.typography.bodySmall,
                            color = AppColors.TextSecondary
                        )
                    } else {
                        AnalizSatir(
                            "Son alınan miktar",
                            satir.sonAlinanMiktar?.let {
                                "${SatinalmaPdfFormats.miktar(it)} ${satir.sonAlinanBirim}"
                            } ?: "—"
                        )
                        AnalizSatir(
                            "Son alınan birim fiyat",
                            satir.sonAlinanBirimFiyat?.let { SatinalmaPdfFormats.tl(it) } ?: "—"
                        )
                        if (satir.sonAlinanTarih.isNotBlank() && satir.sonAlinanTarih != "—") {
                            AnalizSatir("Son alım tarihi", satir.sonAlinanTarih)
                        }
                        if (satir.sonAlinanTedarikci.isNotBlank() && satir.sonAlinanTedarikci != "—") {
                            AnalizSatir("Son tedarikçi", satir.sonAlinanTedarikci)
                        }
                    }

                    HorizontalDivider(color = AppColors.Border, modifier = Modifier.padding(vertical = 2.dp))

                    AnalizSatir(
                        "En düşük teklif BF",
                        satir.enDusukTeklifBirimFiyat?.let { SatinalmaPdfFormats.tl(it) } ?: "—"
                    )
                    if (!satir.teklifYok && satir.enDusukTeklifFirma.isNotBlank()) {
                        AnalizSatir("Firma", satir.enDusukTeklifFirma)
                    }

                    val fark = satir.farkTl
                    val yuzde = satir.artisYuzde
                    if (fark != null && yuzde != null) {
                        HorizontalDivider(color = AppColors.Border, modifier = Modifier.padding(vertical = 2.dp))
                        val artisRenk = when {
                            fark > 0.009 -> AppColors.Danger
                            fark < -0.009 -> AppColors.Success
                            else -> AppColors.TextPrimary
                        }
                        val isaret = if (fark >= 0) "+" else ""
                        AnalizSatir(
                            "Fark (TL)",
                            "$isaret${SatinalmaPdfFormats.sayi(fark)} ₺",
                            degerRenk = artisRenk
                        )
                        AnalizSatir(
                            "Artış %",
                            "$isaret${SatinalmaPdfFormats.sayi(yuzde)} %",
                            degerRenk = artisRenk
                        )
                    } else {
                        Text(
                            "Karşılaştırma için son alım ve teklif birim fiyatı gerekli.",
                            style = MaterialTheme.typography.labelSmall,
                            color = AppColors.TextSecondary,
                            modifier = Modifier.padding(top = 2.dp)
                        )
                    }
                }
            }
        }
    }
}

@Composable
private fun AnalizSatir(
    etiket: String,
    deger: String,
    degerRenk: androidx.compose.ui.graphics.Color = AppColors.TextPrimary
) {
    Row(
        Modifier.fillMaxWidth(),
        horizontalArrangement = Arrangement.SpaceBetween,
        verticalAlignment = Alignment.CenterVertically
    ) {
        Text(
            etiket,
            style = MaterialTheme.typography.bodySmall,
            color = AppColors.TextSecondary,
            modifier = Modifier.weight(1f, fill = false).padding(end = 8.dp)
        )
        Text(
            deger,
            style = MaterialTheme.typography.bodyMedium,
            fontWeight = FontWeight.Medium,
            color = degerRenk
        )
    }
}

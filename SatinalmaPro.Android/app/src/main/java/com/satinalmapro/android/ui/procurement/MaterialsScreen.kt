package com.satinalmapro.android.ui.procurement



import androidx.compose.foundation.layout.Arrangement

import androidx.compose.foundation.layout.Column

import androidx.compose.foundation.layout.Row

import androidx.compose.foundation.layout.Spacer

import androidx.compose.foundation.layout.fillMaxSize

import androidx.compose.foundation.layout.fillMaxWidth

import androidx.compose.foundation.layout.height

import androidx.compose.foundation.layout.padding

import androidx.compose.foundation.lazy.LazyColumn

import androidx.compose.foundation.lazy.items

import androidx.compose.foundation.rememberScrollState

import androidx.compose.foundation.verticalScroll

import androidx.compose.material.icons.Icons

import androidx.compose.material.icons.rounded.Search

import androidx.compose.material3.AlertDialog

import androidx.compose.material3.Button

import androidx.compose.material3.ButtonDefaults

import androidx.compose.material3.Icon

import androidx.compose.material3.MaterialTheme

import androidx.compose.material3.OutlinedButton

import androidx.compose.material3.OutlinedTextField

import androidx.compose.material3.OutlinedTextFieldDefaults

import androidx.compose.material3.Text

import androidx.compose.material3.TextButton

import androidx.compose.runtime.Composable

import androidx.compose.runtime.collectAsState

import androidx.compose.runtime.getValue

import androidx.compose.runtime.mutableStateOf

import androidx.compose.runtime.remember

import androidx.compose.runtime.setValue

import androidx.compose.ui.Alignment

import androidx.compose.ui.Modifier

import androidx.compose.ui.text.font.FontWeight

import androidx.compose.ui.unit.dp

import com.satinalmapro.android.core.model.OnaylananMalzemeSatiri

import com.satinalmapro.android.core.roles.OnaylananMalzemeOlusturucu
import com.satinalmapro.android.core.roles.KullaniciRolleri

import com.satinalmapro.android.ui.AppViewModel

import com.satinalmapro.android.ui.components.AppCard
import com.satinalmapro.android.ui.procurement.MalKabulDialog

import com.satinalmapro.android.ui.components.StatusBadge

import com.satinalmapro.android.ui.components.AppScreenContent
import com.satinalmapro.android.ui.components.AppSearchField
import com.satinalmapro.android.ui.theme.AppColors
import com.satinalmapro.android.ui.theme.AppShapes
import com.satinalmapro.android.ui.theme.AppSpacing



@Composable

fun MaterialsScreen(viewModel: AppViewModel, initialSection: String? = null) {

    var search by remember { mutableStateOf("") }

    val siparisItems by viewModel.siparisBekleyenMalzemeler().collectAsState()

    val malKabulItems by viewModel.approvedMaterials().collectAsState()

    val user by viewModel.user.collectAsState()

    val canPlaceOrder = KullaniciRolleri.canPlaceOrder(user?.role)

    val canMalKabul = KullaniciRolleri.canMalKabul(user?.role)

    val error by viewModel.submitError.collectAsState()

    var dialogSatir by remember { mutableStateOf<OnaylananMalzemeSatiri?>(null) }
    var sevkiyatDialogSatir by remember { mutableStateOf<OnaylananMalzemeSatiri?>(null) }

    val section = initialSection?.lowercase()
    val showSiparis = section != "malkabul" && section != "mal-kabul"
    val showMalKabul = section != "siparis"



    fun matchesSearch(satir: OnaylananMalzemeSatiri) =

        search.isBlank() ||

            satir.malzeme.contains(search, true) ||

            satir.talepNo.contains(search, true) ||

            satir.firma.contains(search, true)



    val filteredSiparis = siparisItems.filter(::matchesSearch)

    val filteredMalKabul = malKabulItems.filter(::matchesSearch)

    val siparisTalepGruplari = filteredSiparis.groupBy { it.talepId }



    AppScreenContent {

        Text(

            "Alınan Malzemeler",

            style = MaterialTheme.typography.titleMedium,

            fontWeight = FontWeight.SemiBold

        )

        Text(

            "Onaylanan kalemler için önce sipariş verin; mal geldiğinde mal kabul yapıp depoya kaydedin.",

            style = MaterialTheme.typography.bodySmall,

            color = AppColors.TextSecondary,

            modifier = Modifier.padding(top = 4.dp, bottom = 12.dp)

        )

        OutlinedTextField(

            value = search,

            onValueChange = { search = it },

            modifier = Modifier.fillMaxWidth(),

            placeholder = { Text("Malzeme, talep no veya firma ara...") },

            leadingIcon = { Icon(Icons.Rounded.Search, null) },

            singleLine = true,

            shape = AppShapes.medium,

            colors = OutlinedTextFieldDefaults.colors(

                focusedBorderColor = AppColors.Primary,

                unfocusedBorderColor = AppColors.Border

            )

        )

        error?.let { Text(it, color = AppColors.Danger, modifier = Modifier.padding(top = 8.dp)) }

        Spacer(Modifier.height(12.dp))



        LazyColumn(verticalArrangement = Arrangement.spacedBy(10.dp)) {

            if (canPlaceOrder && filteredSiparis.isNotEmpty() && showSiparis) {

                item {

                    Text(

                        "Sipariş Ver",

                        style = MaterialTheme.typography.titleSmall,

                        fontWeight = FontWeight.SemiBold,

                        modifier = Modifier.padding(bottom = 4.dp)

                    )

                    Text(

                        "Onaylanmış talepler için tedarikçiye sipariş oluşturun.",

                        style = MaterialTheme.typography.bodySmall,

                        color = AppColors.TextSecondary,

                        modifier = Modifier.padding(bottom = 8.dp)

                    )

                }

                siparisTalepGruplari.forEach { (talepId, satirlar) ->

                    item(key = "siparis_$talepId") {

                        SiparisVerCard(

                            satirlar = satirlar,

                            onSiparisVer = { viewModel.siparisVer(talepId) {} },

                            onDetail = { viewModel.navigate("talep-detay?id=$talepId") }

                        )

                    }

                }

                item { Spacer(Modifier.height(8.dp)) }

            }



            if (showMalKabul) {

            item {

                Text(

                    "Mal Kabul",

                    style = MaterialTheme.typography.titleSmall,

                    fontWeight = FontWeight.SemiBold,

                    modifier = Modifier.padding(bottom = 4.dp)

                )

                Text(

                    "Sipariş verilmiş kalemler için mal kabul yapın. Fazla teslimat kabul edilir; eksik kalan için sevkiyatı tamamlayabilirsiniz.",

                    style = MaterialTheme.typography.bodySmall,

                    color = AppColors.TextSecondary,

                    modifier = Modifier.padding(bottom = 8.dp)

                )

            }



            if (filteredMalKabul.isEmpty()) {

                item {

                    Text("Mal kabul bekleyen kalem yok.", color = AppColors.TextSecondary)

                }

            } else {

                items(filteredMalKabul, key = { "mk_${it.talepId}_${it.kalemId}" }) { satir ->

                    MaterialLineCard(

                        satir = satir,

                        canMalKabul = canMalKabul && OnaylananMalzemeOlusturucu.malKabulBekleyen(satir),

                        canSevkiyatTamamla = canMalKabul && OnaylananMalzemeOlusturucu.sevkiyatTamamlanabilir(satir),

                        onMalKabul = { dialogSatir = satir },

                        onSevkiyatTamamla = { sevkiyatDialogSatir = satir },

                        onDetail = { viewModel.navigate("talep-detay?id=${satir.talepId}") }

                    )

                }

            }

            }

        }



    dialogSatir?.let { satir ->

        MalKabulDialog(

            satir = satir,

            onDismiss = { dialogSatir = null },

            onConfirm = { form ->

                viewModel.malKabulVeDepoyaKaydet(

                    talepId = satir.talepId,

                    kalemId = satir.kalemId,

                    form = form,

                    teklifId = satir.teklifId

                ) { dialogSatir = null }

            }

        )

    }

    sevkiyatDialogSatir?.let { satir ->

        AlertDialog(

            onDismissRequest = { sevkiyatDialogSatir = null },

            title = { Text("Sevkiyatı Tamamla") },

            text = {

                Text(

                    "${satir.malzeme}\n\nSipariş: ${satir.siparisMiktari} ${satir.birim}\n" +

                        "Kabul: ${satir.kabulEdilenMiktar} ${satir.birim}\n\n" +

                        "Talep ve teklif miktarları güncellenecek."

                )

            },

            confirmButton = {

                Button(onClick = {

                    viewModel.sevkiyatiTamamla(satir.talepId, satir.kalemId, teklifId = satir.teklifId) {

                        sevkiyatDialogSatir = null

                    }

                }) { Text("Tamamla") }

            },

            dismissButton = {

                TextButton(onClick = { sevkiyatDialogSatir = null }) { Text("İptal") }

            }

        )

    }

    }

}



@Composable

private fun SiparisVerCard(

    satirlar: List<OnaylananMalzemeSatiri>,

    onSiparisVer: () -> Unit,

    onDetail: () -> Unit

) {

    val ilk = satirlar.first()

    AppCard(onClick = onDetail) {

        Column {

            Row(

                Modifier.fillMaxWidth(),

                horizontalArrangement = Arrangement.SpaceBetween,

                verticalAlignment = Alignment.CenterVertically

            ) {

                Column(Modifier.weight(1f)) {

                    Text(ilk.talepNo, style = MaterialTheme.typography.titleMedium, color = AppColors.TextPrimary)

                    Text(

                        "${satirlar.size} kalem · ${ilk.tarih}",

                        style = MaterialTheme.typography.bodySmall,

                        color = AppColors.TextSecondary

                    )

                    satirlar.take(3).forEach { satir ->

                        Text(

                            "• ${satir.malzeme} — ${satir.siparisMiktari} ${satir.birim}" +

                                if (satir.firma.isNotBlank()) " (${satir.firma})" else "",

                            style = MaterialTheme.typography.bodySmall,

                            color = AppColors.TextSecondary

                        )

                    }

                    if (satirlar.size > 3) {

                        Text(

                            "+${satirlar.size - 3} kalem daha",

                            style = MaterialTheme.typography.labelSmall,

                            color = AppColors.TextSecondary

                        )

                    }

                }

                StatusBadge("Onaylandı", AppColors.PrimaryContainer, AppColors.Primary)

            }

            Button(

                onClick = onSiparisVer,

                modifier = Modifier.fillMaxWidth().padding(top = 12.dp),

                colors = ButtonDefaults.buttonColors(containerColor = AppColors.Primary)

            ) { Text("Sipariş Ver") }

        }

    }

}



@Composable

private fun MaterialLineCard(

    satir: OnaylananMalzemeSatiri,

    canMalKabul: Boolean,

    canSevkiyatTamamla: Boolean = false,

    onMalKabul: () -> Unit,

    onSevkiyatTamamla: () -> Unit = {},

    onDetail: () -> Unit

) {

    AppCard(onClick = onDetail) {

        Column {

            Row(

                Modifier.fillMaxWidth(),

                horizontalArrangement = Arrangement.SpaceBetween,

                verticalAlignment = Alignment.CenterVertically

            ) {

                Column(Modifier.weight(1f)) {

                    Text(satir.malzeme, style = MaterialTheme.typography.titleMedium, color = AppColors.TextPrimary)

                    Text(

                        buildString {

                            append(satir.talepNo)

                            if (satir.siparisNo.isNotBlank() && satir.siparisNo != satir.talepNo) {

                                append(" · Sipariş: ")

                                append(satir.siparisNo)

                            }

                        },

                        style = MaterialTheme.typography.labelMedium,

                        color = AppColors.TextSecondary

                    )

                    Text(

                        "Sipariş: ${satir.siparisMiktari} ${satir.birim} · Kabul: ${satir.kabulEdilenMiktar} · Kalan: ${satir.kalanMiktar}",

                        style = MaterialTheme.typography.bodySmall,

                        color = AppColors.TextSecondary

                    )

                    if (satir.firma.isNotBlank()) {

                        Text(

                            "Firma: ${satir.firma}" + if (satir.birimFiyati > 0) " · ${satir.birimFiyati} ₺/birim" else "",

                            style = MaterialTheme.typography.bodySmall,

                            color = AppColors.TextSecondary

                        )

                    }

                }

                StatusBadge(

                    satir.kabulDurumu,

                    if (satir.kabulDurumu == "Tamamlandı") AppColors.SuccessContainer else AppColors.WarningContainer,

                    if (satir.kabulDurumu == "Tamamlandı") AppColors.Success else AppColors.Warning

                )

            }

            if (canMalKabul) {

                Button(

                    onClick = onMalKabul,

                    modifier = Modifier.fillMaxWidth().padding(top = 12.dp),

                    colors = ButtonDefaults.buttonColors(containerColor = AppColors.Success)

                ) { Text("Mal Kabul — Depoya Kaydet") }

            }

            if (canSevkiyatTamamla) {

                OutlinedButton(

                    onClick = onSevkiyatTamamla,

                    modifier = Modifier.fillMaxWidth().padding(top = 8.dp)

                ) { Text("Sevkiyatı Tamamla") }

            }

        }

    }

}


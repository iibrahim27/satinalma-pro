package com.satinalmapro.android.ui.procurement

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Checkbox
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.satinalmapro.android.core.model.OnaylananMalzemeSatiri
import com.satinalmapro.android.ui.theme.AppColors

data class MalKabulFormSonuc(
    val miktar: String,
    val firma: String,
    val birimFiyat: String,
    val kategori: String,
    val fisNo: String,
    val teslimAlan: String,
    val depoSaha: String,
    val sahayaDirekt: Boolean,
    val sahaHedef: String
)

@Composable
fun MalKabulDialog(
    satir: OnaylananMalzemeSatiri,
    onDismiss: () -> Unit,
    onConfirm: (MalKabulFormSonuc) -> Unit
) {
    val varsayilanMiktar = if (satir.kalanMiktar > 0.0001) satir.kalanMiktar.toString() else ""
    var miktar by remember { mutableStateOf(varsayilanMiktar) }
    var firma by remember { mutableStateOf(satir.firma) }
    var birimFiyat by remember {
        mutableStateOf(if (satir.birimFiyati > 0) satir.birimFiyati.toString() else "")
    }
    var kategori by remember { mutableStateOf("Malzeme") }
    var fisNo by remember { mutableStateOf(satir.siparisNo.ifBlank { satir.talepNo }) }
    var teslimAlan by remember { mutableStateOf("") }
    var depoSaha by remember { mutableStateOf("") }
    var sahayaDirekt by remember { mutableStateOf(false) }
    var sahaHedef by remember { mutableStateOf("") }

    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text("Mal Kabul") },
        text = {
            Column(
                Modifier
                    .fillMaxWidth()
                    .verticalScroll(rememberScrollState()),
                verticalArrangement = Arrangement.spacedBy(10.dp)
            ) {
                Text(
                    "${satir.malzeme}\nSipariş: ${satir.siparisMiktari} ${satir.birim} · Kabul: ${satir.kabulEdilenMiktar} · Kalan: ${satir.kalanMiktar}\nFazla teslimat kabul edilir; kategori listesinde yoksa yazarak ekleyebilirsiniz.",
                    style = MaterialTheme.typography.bodySmall,
                    color = AppColors.TextSecondary
                )
                OutlinedTextField(
                    value = miktar,
                    onValueChange = { miktar = it },
                    label = { Text("Miktar") },
                    singleLine = true,
                    modifier = Modifier.fillMaxWidth()
                )
                OutlinedTextField(
                    value = firma,
                    onValueChange = { firma = it },
                    label = { Text("Firma / Tedarikçi") },
                    singleLine = true,
                    modifier = Modifier.fillMaxWidth()
                )
                OutlinedTextField(
                    value = birimFiyat,
                    onValueChange = { birimFiyat = it },
                    label = { Text("Birim Fiyat (₺)") },
                    singleLine = true,
                    modifier = Modifier.fillMaxWidth()
                )
                OutlinedTextField(
                    value = kategori,
                    onValueChange = { kategori = it },
                    label = { Text("Kategori (yazarak yeni ekleyin)") },
                    singleLine = true,
                    modifier = Modifier.fillMaxWidth()
                )
                OutlinedTextField(
                    value = fisNo,
                    onValueChange = { fisNo = it },
                    label = { Text("Fiş No") },
                    singleLine = true,
                    modifier = Modifier.fillMaxWidth()
                )
                OutlinedTextField(
                    value = teslimAlan,
                    onValueChange = { teslimAlan = it },
                    label = { Text("Teslim Alan") },
                    singleLine = true,
                    modifier = Modifier.fillMaxWidth()
                )
                androidx.compose.foundation.layout.Row(
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Checkbox(checked = sahayaDirekt, onCheckedChange = { sahayaDirekt = it })
                    Text(
                        "Malzeme sahaya indi (depo giriş + çıkış)",
                        style = MaterialTheme.typography.bodySmall
                    )
                }
                OutlinedTextField(
                    value = depoSaha,
                    onValueChange = { depoSaha = it },
                    label = { Text(if (sahayaDirekt) "Giriş Deposu" else "İndirildiği Saha / Depo") },
                    singleLine = true,
                    modifier = Modifier.fillMaxWidth()
                )
                if (sahayaDirekt) {
                    OutlinedTextField(
                        value = sahaHedef,
                        onValueChange = { sahaHedef = it },
                        label = { Text("Malzemenin indiği saha") },
                        singleLine = true,
                        modifier = Modifier.fillMaxWidth()
                    )
                }
            }
        },
        confirmButton = {
            Button(
                onClick = {
                    onConfirm(
                        MalKabulFormSonuc(
                            miktar = miktar,
                            firma = firma,
                            birimFiyat = birimFiyat,
                            kategori = kategori,
                            fisNo = fisNo,
                            teslimAlan = teslimAlan,
                            depoSaha = depoSaha,
                            sahayaDirekt = sahayaDirekt,
                            sahaHedef = sahaHedef
                        )
                    )
                },
                colors = ButtonDefaults.buttonColors(containerColor = AppColors.Success)
            ) { Text("Kaydet") }
        },
        dismissButton = {
            TextButton(onClick = onDismiss) { Text("İptal") }
        }
    )
}

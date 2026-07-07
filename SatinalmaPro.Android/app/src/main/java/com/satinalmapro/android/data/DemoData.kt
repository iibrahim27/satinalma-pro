package com.satinalmapro.android.data

import androidx.compose.ui.graphics.Color
import com.satinalmapro.android.ui.theme.LightAppColors

enum class DeliveryStatus(val label: String, val bg: Color, val fg: Color) {
    Delivered("Teslim Edildi", LightAppColors.SuccessContainer, LightAppColors.Success),
    Waiting("Teslim Bekliyor", LightAppColors.WarningContainer, LightAppColors.Warning),
    Partial("Kısmi Teslim", LightAppColors.PrimaryContainer, LightAppColors.Primary)
}

data class MaterialItem(
    val id: String,
    val company: String,
    val material: String,
    val quantity: String,
    val date: String,
    val status: DeliveryStatus
)

data class MaterialDetail(
    val documentNo: String,
    val status: DeliveryStatus,
    val material: String,
    val supplier: String,
    val invoice: String,
    val warehouse: String,
    val quantity: String,
    val unit: String,
    val unitPrice: String,
    val total: String,
    val deliveryDate: String,
    val description: String
)

data class QuoteItem(
    val company: String,
    val totalPrice: String,
    val unitPrice: String,
    val deliveryDays: String,
    val rating: Float,
    val recommended: Boolean
)

data class NotificationItem(
    val time: String,
    val title: String,
    val description: String,
    val accent: Color
)

data class RecentActivity(
    val company: String,
    val material: String,
    val status: DeliveryStatus
)

object DemoData {
    const val USER_NAME = "İbrahim Pekbalcı"
    const val USER_ROLE = "Admin"
    const val USER_USERNAME = "ibrahim.pekbalci"
    const val USER_PHONE = "+90 532 000 00 00"
    const val USER_EMAIL = "ibrahim@mvinsaat.com.tr"
    const val USER_DEPARTMENT = "Satınalma"
    const val VERSION = "2.1.2"

    val summaryStats = listOf(
        Triple("Bekleyen Talepler", "12", LightAppColors.IconBlue),
        Triple("Bekleyen Siparişler", "8", LightAppColors.IconGreen),
        Triple("Bekleyen Teslimatlar", "5", LightAppColors.IconOrange),
        Triple("Kritik Stoklar", "7", LightAppColors.IconRed)
    )

    val materials = listOf(
        MaterialItem("1", "ABC Beton", "Demir Ø12", "25.000 kg", "04.07.2026", DeliveryStatus.Delivered),
        MaterialItem("2", "Delta Madencilik", "Mıcır 0-11", "42 ton", "03.07.2026", DeliveryStatus.Partial),
        MaterialItem("3", "XYZ Yapı", "Çimento CEM I", "18 ton", "02.07.2026", DeliveryStatus.Waiting),
        MaterialItem("4", "Ege Demir", "Nervürlü Demir", "12 ton", "01.07.2026", DeliveryStatus.Delivered)
    )

    val materialDetail = MaterialDetail(
        documentNo = "ALM-2024-1005",
        status = DeliveryStatus.Delivered,
        material = "Demir Ø12",
        supplier = "ABC Beton",
        invoice = "FT-2026-1842",
        warehouse = "Merkez Şantiye",
        quantity = "25.000",
        unit = "kg",
        unitPrice = "₺28.500",
        total = "₺712.500",
        deliveryDate = "04.07.2026",
        description = "Acil sevkiyat — Doğu sahası kullanımı"
    )

    val quotes = listOf(
        QuoteItem("ABC Beton", "₺712.500", "₺28,50/kg", "2 Gün", 4.8f, true),
        QuoteItem("Ege Demir", "₺735.000", "₺29,40/kg", "3 Gün", 4.2f, false),
        QuoteItem("Delta Çelik", "₺698.000", "₺27,92/kg", "5 Gün", 3.9f, false)
    )

    val notifications = listOf(
        NotificationItem("10:24", "Talep Onaylandı", "1005 nolu satınalma onaylandı", LightAppColors.Success),
        NotificationItem("09:15", "Teklif Bekleniyor", "Demir Ø12 için teklif girin", LightAppColors.Primary),
        NotificationItem("Dün", "Mal Kabul", "ABC Beton teslimatı tamamlandı", LightAppColors.IconGreen),
        NotificationItem("Dün", "Kritik Stok", "Demir Ø12 minimum seviyede", LightAppColors.Danger)
    )

    val recentActivities = listOf(
        RecentActivity("ABC Beton", "Demir Ø12", DeliveryStatus.Delivered),
        RecentActivity("Delta Madencilik", "Mıcır 0-11", DeliveryStatus.Partial)
    )
}

package com.satinalmapro.android.ui.theme

import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color

enum class StatusType { Pending, Approved, Rejected, Info }

@Composable
fun statusColors(type: StatusType): Pair<Color, Color> = when (type) {
    StatusType.Pending -> AppColors.WarningContainer to AppColors.Warning
    StatusType.Approved -> AppColors.SuccessContainer to AppColors.Success
    StatusType.Rejected -> AppColors.DangerContainer to AppColors.Danger
    StatusType.Info -> AppColors.InfoContainer to AppColors.Info
}

@Composable
fun statusFromText(text: String): StatusType {
    val lower = text.lowercase()
    return when {
        "onay" in lower || "tamam" in lower || "kabul" in lower -> StatusType.Approved
        "bekl" in lower || "bekleyen" in lower || "bekliyor" in lower -> StatusType.Pending
        "red" in lower || "iptal" in lower -> StatusType.Rejected
        else -> StatusType.Info
    }
}

@Composable
fun statusColorsFromText(text: String): Pair<Color, Color> = statusColors(statusFromText(text))

@Composable
fun notificationStatusColor(type: String): Color = when (type) {
    "Onaylandi", "MalKabulEdildi" -> AppColors.Success
    "Reddedildi" -> AppColors.Danger
    "Bekliyor", "OnayBekliyor" -> AppColors.Warning
    else -> AppColors.Info
}

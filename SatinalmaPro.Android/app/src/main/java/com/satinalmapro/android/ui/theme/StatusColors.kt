package com.satinalmapro.android.ui.theme

import androidx.compose.material3.OutlinedTextFieldDefaults
import androidx.compose.material3.TextFieldColors
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color

enum class StatusType { Info, Pending, Approved, Rejected, Warning }

fun statusColors(type: StatusType): Pair<Color, Color> = when (type) {
    StatusType.Approved -> MetrikLight.SuccessMuted to MetrikLight.Success
    StatusType.Rejected -> MetrikLight.DangerMuted to MetrikLight.Danger
    StatusType.Pending, StatusType.Warning -> MetrikLight.WarningMuted to MetrikLight.Warning
    StatusType.Info -> MetrikLight.InfoMuted to MetrikLight.Info
}

fun statusColorsFromText(text: String): Pair<Color, Color> {
    val c = statusFromText(text)
    return c.copy(alpha = 0.14f) to c
}

fun notificationStatusColor(type: String): Color = when (type) {
    "Onaylandi", "MalKabulEdildi", "SiparisOlusturuldu" -> MetrikLight.Success
    "Reddedildi" -> MetrikLight.Danger
    "TeklifIstendi", "TeklifOnayda", "TeklifDuzeltmeIstendi" -> MetrikLight.Warning
    else -> MetrikLight.Info
}

object StatusColors {
    val success = MetrikLight.Success
    val warning = MetrikLight.Warning
    val danger = MetrikLight.Danger
    val info = MetrikLight.Info
    val neutral = MetrikLight.TextSecondary
}

@Composable
fun appFieldColors(): TextFieldColors = OutlinedTextFieldDefaults.colors(
    focusedBorderColor = MetrikColors.Primary,
    unfocusedBorderColor = MetrikColors.Border,
    focusedLabelColor = MetrikColors.Primary,
    unfocusedLabelColor = MetrikColors.TextSecondary,
    cursorColor = MetrikColors.Accent,
    focusedContainerColor = MetrikColors.Surface,
    unfocusedContainerColor = MetrikColors.Surface
)

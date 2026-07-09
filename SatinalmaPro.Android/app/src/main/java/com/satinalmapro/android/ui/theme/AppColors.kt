package com.satinalmapro.android.ui.theme

import androidx.compose.runtime.Composable
import androidx.compose.runtime.ReadOnlyComposable
import androidx.compose.runtime.staticCompositionLocalOf
import androidx.compose.ui.graphics.Color

/** İnşaat / satınalma: slate + teal — mor/indigo generic AI görünümünden uzak. */
data class MetrikPalette(
    val Primary: Color,
    val PrimaryDark: Color,
    val Accent: Color,
    val AccentMuted: Color,
    val Background: Color,
    val Surface: Color,
    val SurfaceMuted: Color,
    val Border: Color,
    val Divider: Color,
    val Success: Color,
    val SuccessMuted: Color,
    val Warning: Color,
    val WarningMuted: Color,
    val Danger: Color,
    val DangerMuted: Color,
    val Info: Color,
    val InfoMuted: Color,
    val TextPrimary: Color,
    val TextSecondary: Color,
    val TextTertiary: Color,
    val TextOnPrimary: Color,
    val TextOnAccent: Color
)

val MetrikLight = MetrikPalette(
    Primary = Color(0xFF1B2838),
    PrimaryDark = Color(0xFF111A24),
    Accent = Color(0xFF0F766E),
    AccentMuted = Color(0xFFCCFBF1),
    Background = Color(0xFFEEF2F5),
    Surface = Color(0xFFFFFFFF),
    SurfaceMuted = Color(0xFFE4E9EE),
    Border = Color(0xFFCDD5DD),
    Divider = Color(0xFFE2E8EE),
    Success = Color(0xFF047857),
    SuccessMuted = Color(0xFFD1FAE5),
    Warning = Color(0xFFC2410C),
    WarningMuted = Color(0xFFFFEDD5),
    Danger = Color(0xFFB91C1C),
    DangerMuted = Color(0xFFFEE2E2),
    Info = Color(0xFF0369A1),
    InfoMuted = Color(0xFFE0F2FE),
    TextPrimary = Color(0xFF0F172A),
    TextSecondary = Color(0xFF64748B),
    TextTertiary = Color(0xFF94A3B8),
    TextOnPrimary = Color(0xFFFFFFFF),
    TextOnAccent = Color(0xFFFFFFFF)
)

object LightAppColors {
    val Primary = MetrikLight.Primary
    val PrimaryDark = MetrikLight.PrimaryDark
    val Secondary = MetrikLight.Accent
    val PrimaryContainer = MetrikLight.SurfaceMuted
    val Background = MetrikLight.Background
    val Surface = MetrikLight.Surface
    val Border = MetrikLight.Border
    val Success = MetrikLight.Success
    val SuccessContainer = MetrikLight.SuccessMuted
    val Warning = MetrikLight.Warning
    val WarningContainer = MetrikLight.WarningMuted
    val Danger = MetrikLight.Danger
    val DangerContainer = MetrikLight.DangerMuted
    val Info = MetrikLight.Info
    val InfoContainer = MetrikLight.InfoMuted
    val TextPrimary = MetrikLight.TextPrimary
    val TextSecondary = MetrikLight.TextSecondary
    val TextOnPrimary = MetrikLight.TextOnPrimary
    val IconBlue = MetrikLight.Info
    val IconGreen = MetrikLight.Success
    val IconOrange = MetrikLight.Warning
    val IconRed = MetrikLight.Danger
    val IconPurple = MetrikLight.Accent
}

val DarkAppColors = LightAppColors

val LocalMetrikColors = staticCompositionLocalOf { MetrikLight }
val LocalAppColors = LocalMetrikColors

object MetrikColors {
    val current: MetrikPalette
        @Composable
        @ReadOnlyComposable
        get() = LocalMetrikColors.current

    val Primary: Color @Composable @ReadOnlyComposable get() = current.Primary
    val PrimaryDark: Color @Composable @ReadOnlyComposable get() = current.PrimaryDark
    val Accent: Color @Composable @ReadOnlyComposable get() = current.Accent
    val AccentMuted: Color @Composable @ReadOnlyComposable get() = current.AccentMuted
    val Background: Color @Composable @ReadOnlyComposable get() = current.Background
    val Surface: Color @Composable @ReadOnlyComposable get() = current.Surface
    val SurfaceMuted: Color @Composable @ReadOnlyComposable get() = current.SurfaceMuted
    val Border: Color @Composable @ReadOnlyComposable get() = current.Border
    val Divider: Color @Composable @ReadOnlyComposable get() = current.Divider
    val Success: Color @Composable @ReadOnlyComposable get() = current.Success
    val SuccessMuted: Color @Composable @ReadOnlyComposable get() = current.SuccessMuted
    val Warning: Color @Composable @ReadOnlyComposable get() = current.Warning
    val WarningMuted: Color @Composable @ReadOnlyComposable get() = current.WarningMuted
    val Danger: Color @Composable @ReadOnlyComposable get() = current.Danger
    val DangerMuted: Color @Composable @ReadOnlyComposable get() = current.DangerMuted
    val Info: Color @Composable @ReadOnlyComposable get() = current.Info
    val InfoMuted: Color @Composable @ReadOnlyComposable get() = current.InfoMuted
    val TextPrimary: Color @Composable @ReadOnlyComposable get() = current.TextPrimary
    val TextSecondary: Color @Composable @ReadOnlyComposable get() = current.TextSecondary
    val TextTertiary: Color @Composable @ReadOnlyComposable get() = current.TextTertiary
    val TextOnPrimary: Color @Composable @ReadOnlyComposable get() = current.TextOnPrimary
    val TextOnAccent: Color @Composable @ReadOnlyComposable get() = current.TextOnAccent
}

object AppColors {
    val Primary: Color @Composable @ReadOnlyComposable get() = MetrikColors.Primary
    val PrimaryDark: Color @Composable @ReadOnlyComposable get() = MetrikColors.PrimaryDark
    val Secondary: Color @Composable @ReadOnlyComposable get() = MetrikColors.Accent
    val PrimaryContainer: Color @Composable @ReadOnlyComposable get() = MetrikColors.SurfaceMuted
    val Background: Color @Composable @ReadOnlyComposable get() = MetrikColors.Background
    val Surface: Color @Composable @ReadOnlyComposable get() = MetrikColors.Surface
    val SurfaceElevated: Color @Composable @ReadOnlyComposable get() = MetrikColors.Surface
    val Border: Color @Composable @ReadOnlyComposable get() = MetrikColors.Border
    val Success: Color @Composable @ReadOnlyComposable get() = MetrikColors.Success
    val SuccessContainer: Color @Composable @ReadOnlyComposable get() = MetrikColors.SuccessMuted
    val Warning: Color @Composable @ReadOnlyComposable get() = MetrikColors.Warning
    val WarningContainer: Color @Composable @ReadOnlyComposable get() = MetrikColors.WarningMuted
    val Danger: Color @Composable @ReadOnlyComposable get() = MetrikColors.Danger
    val DangerContainer: Color @Composable @ReadOnlyComposable get() = MetrikColors.DangerMuted
    val Info: Color @Composable @ReadOnlyComposable get() = MetrikColors.Info
    val InfoContainer: Color @Composable @ReadOnlyComposable get() = MetrikColors.InfoMuted
    val TextPrimary: Color @Composable @ReadOnlyComposable get() = MetrikColors.TextPrimary
    val TextSecondary: Color @Composable @ReadOnlyComposable get() = MetrikColors.TextSecondary
    val TextOnPrimary: Color @Composable @ReadOnlyComposable get() = MetrikColors.TextOnPrimary
    val IconBlue: Color @Composable @ReadOnlyComposable get() = MetrikColors.Info
    val IconGreen: Color @Composable @ReadOnlyComposable get() = MetrikColors.Success
    val IconOrange: Color @Composable @ReadOnlyComposable get() = MetrikColors.Warning
    val IconRed: Color @Composable @ReadOnlyComposable get() = MetrikColors.Danger
    val IconPurple: Color @Composable @ReadOnlyComposable get() = MetrikColors.Accent
}

package com.satinalmapro.android.ui.theme

import androidx.compose.runtime.Composable
import androidx.compose.runtime.ReadOnlyComposable
import androidx.compose.runtime.staticCompositionLocalOf
import androidx.compose.ui.graphics.Color

/** Kurumsal lacivert (#0F2A5A) tabanlı MD3 renk paleti */
data class AppColorPalette(
    val Primary: Color,
    val PrimaryDark: Color,
    val Secondary: Color,
    val PrimaryContainer: Color,
    val Background: Color,
    val Surface: Color,
    val SurfaceElevated: Color,
    val Border: Color,
    val Success: Color,
    val SuccessContainer: Color,
    val Warning: Color,
    val WarningContainer: Color,
    val Danger: Color,
    val DangerContainer: Color,
    val Info: Color,
    val InfoContainer: Color,
    val TextPrimary: Color,
    val TextSecondary: Color,
    val TextOnPrimary: Color,
    val IconBlue: Color,
    val IconGreen: Color,
    val IconOrange: Color,
    val IconRed: Color,
    val IconPurple: Color
)

val LightAppColors = AppColorPalette(
    Primary = Color(0xFF0F2A5A),
    PrimaryDark = Color(0xFF0A1F42),
    Secondary = Color(0xFF1E4A8C),
    PrimaryContainer = Color(0xFFE8EEF7),
    Background = Color(0xFFF4F6FA),
    Surface = Color(0xFFFFFFFF),
    SurfaceElevated = Color(0xFFFFFFFF),
    Border = Color(0xFFE8ECF2),
    Success = Color(0xFF16A34A),
    SuccessContainer = Color(0xFFDCFCE7),
    Warning = Color(0xFFF59E0B),
    WarningContainer = Color(0xFFFEF3C7),
    Danger = Color(0xFFDC2626),
    DangerContainer = Color(0xFFFEE2E2),
    Info = Color(0xFF2563EB),
    InfoContainer = Color(0xFFEFF6FF),
    TextPrimary = Color(0xFF0F172A),
    TextSecondary = Color(0xFF64748B),
    TextOnPrimary = Color(0xFFFFFFFF),
    IconBlue = Color(0xFF2563EB),
    IconGreen = Color(0xFF16A34A),
    IconOrange = Color(0xFFF59E0B),
    IconRed = Color(0xFFDC2626),
    IconPurple = Color(0xFF7C3AED)
)

val DarkAppColors = AppColorPalette(
    Primary = Color(0xFF5B8DEF),
    PrimaryDark = Color(0xFF0F2A5A),
    Secondary = Color(0xFF7AA8F5),
    PrimaryContainer = Color(0xFF1A3358),
    Background = Color(0xFF0B1220),
    Surface = Color(0xFF141D2E),
    SurfaceElevated = Color(0xFF1A2438),
    Border = Color(0xFF2A3548),
    Success = Color(0xFF4ADE80),
    SuccessContainer = Color(0xFF14532D),
    Warning = Color(0xFFFBBF24),
    WarningContainer = Color(0xFF78350F),
    Danger = Color(0xFFF87171),
    DangerContainer = Color(0xFF7F1D1D),
    Info = Color(0xFF60A5FA),
    InfoContainer = Color(0xFF1E3A5F),
    TextPrimary = Color(0xFFF1F5F9),
    TextSecondary = Color(0xFF94A3B8),
    TextOnPrimary = Color(0xFFFFFFFF),
    IconBlue = Color(0xFF60A5FA),
    IconGreen = Color(0xFF4ADE80),
    IconOrange = Color(0xFFFBBF24),
    IconRed = Color(0xFFF87171),
    IconPurple = Color(0xFFA78BFA)
)

val LocalAppColors = staticCompositionLocalOf { LightAppColors }

object AppColors {
    val Primary: Color @Composable @ReadOnlyComposable get() = LocalAppColors.current.Primary
    val PrimaryDark: Color @Composable @ReadOnlyComposable get() = LocalAppColors.current.PrimaryDark
    val Secondary: Color @Composable @ReadOnlyComposable get() = LocalAppColors.current.Secondary
    val PrimaryContainer: Color @Composable @ReadOnlyComposable get() = LocalAppColors.current.PrimaryContainer
    val Background: Color @Composable @ReadOnlyComposable get() = LocalAppColors.current.Background
    val Surface: Color @Composable @ReadOnlyComposable get() = LocalAppColors.current.Surface
    val SurfaceElevated: Color @Composable @ReadOnlyComposable get() = LocalAppColors.current.SurfaceElevated
    val Border: Color @Composable @ReadOnlyComposable get() = LocalAppColors.current.Border
    val Success: Color @Composable @ReadOnlyComposable get() = LocalAppColors.current.Success
    val SuccessContainer: Color @Composable @ReadOnlyComposable get() = LocalAppColors.current.SuccessContainer
    val Warning: Color @Composable @ReadOnlyComposable get() = LocalAppColors.current.Warning
    val WarningContainer: Color @Composable @ReadOnlyComposable get() = LocalAppColors.current.WarningContainer
    val Danger: Color @Composable @ReadOnlyComposable get() = LocalAppColors.current.Danger
    val DangerContainer: Color @Composable @ReadOnlyComposable get() = LocalAppColors.current.DangerContainer
    val Info: Color @Composable @ReadOnlyComposable get() = LocalAppColors.current.Info
    val InfoContainer: Color @Composable @ReadOnlyComposable get() = LocalAppColors.current.InfoContainer
    val TextPrimary: Color @Composable @ReadOnlyComposable get() = LocalAppColors.current.TextPrimary
    val TextSecondary: Color @Composable @ReadOnlyComposable get() = LocalAppColors.current.TextSecondary
    val TextOnPrimary: Color @Composable @ReadOnlyComposable get() = LocalAppColors.current.TextOnPrimary
    val IconBlue: Color @Composable @ReadOnlyComposable get() = LocalAppColors.current.IconBlue
    val IconGreen: Color @Composable @ReadOnlyComposable get() = LocalAppColors.current.IconGreen
    val IconOrange: Color @Composable @ReadOnlyComposable get() = LocalAppColors.current.IconOrange
    val IconRed: Color @Composable @ReadOnlyComposable get() = LocalAppColors.current.IconRed
    val IconPurple: Color @Composable @ReadOnlyComposable get() = LocalAppColors.current.IconPurple
}

package com.satinalmapro.android.ui.theme

import androidx.compose.runtime.Composable
import androidx.compose.runtime.ReadOnlyComposable
import androidx.compose.runtime.staticCompositionLocalOf
import androidx.compose.ui.graphics.Color

data class AppColorPalette(
    val Primary: Color,
    val Secondary: Color,
    val PrimaryContainer: Color,
    val Background: Color,
    val Surface: Color,
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
    val IconBlue: Color,
    val IconGreen: Color,
    val IconOrange: Color,
    val IconRed: Color,
    val IconPurple: Color
)

val LightAppColors = AppColorPalette(
    Primary = Color(0xFF2563EB),
    Secondary = Color(0xFF4F46E5),
    PrimaryContainer = Color(0xFFEFF6FF),
    Background = Color(0xFFF7F9FC),
    Surface = Color(0xFFFFFFFF),
    Border = Color(0xFFE2E8F0),
    Success = Color(0xFF22C55E),
    SuccessContainer = Color(0xFFDCFCE7),
    Warning = Color(0xFFF59E0B),
    WarningContainer = Color(0xFFFEF3C7),
    Danger = Color(0xFFEF4444),
    DangerContainer = Color(0xFFFEE2E2),
    Info = Color(0xFF0EA5E9),
    InfoContainer = Color(0xFFE0F2FE),
    TextPrimary = Color(0xFF0F172A),
    TextSecondary = Color(0xFF64748B),
    IconBlue = Color(0xFF2563EB),
    IconGreen = Color(0xFF22C55E),
    IconOrange = Color(0xFFF59E0B),
    IconRed = Color(0xFFEF4444),
    IconPurple = Color(0xFF8B5CF6)
)

val DarkAppColors = AppColorPalette(
    Primary = Color(0xFF60A5FA),
    Secondary = Color(0xFF818CF8),
    PrimaryContainer = Color(0xFF1E3A5F),
    Background = Color(0xFF0F172A),
    Surface = Color(0xFF1E293B),
    Border = Color(0xFF334155),
    Success = Color(0xFF4ADE80),
    SuccessContainer = Color(0xFF14532D),
    Warning = Color(0xFFFBBF24),
    WarningContainer = Color(0xFF78350F),
    Danger = Color(0xFFF87171),
    DangerContainer = Color(0xFF7F1D1D),
    Info = Color(0xFF38BDF8),
    InfoContainer = Color(0xFF0C4A6E),
    TextPrimary = Color(0xFFF1F5F9),
    TextSecondary = Color(0xFF94A3B8),
    IconBlue = Color(0xFF60A5FA),
    IconGreen = Color(0xFF4ADE80),
    IconOrange = Color(0xFFFBBF24),
    IconRed = Color(0xFFF87171),
    IconPurple = Color(0xFFA78BFA)
)

val LocalAppColors = staticCompositionLocalOf { LightAppColors }

object AppColors {
    val Primary: Color @Composable @ReadOnlyComposable get() = LocalAppColors.current.Primary
    val Secondary: Color @Composable @ReadOnlyComposable get() = LocalAppColors.current.Secondary
    val PrimaryContainer: Color @Composable @ReadOnlyComposable get() = LocalAppColors.current.PrimaryContainer
    val Background: Color @Composable @ReadOnlyComposable get() = LocalAppColors.current.Background
    val Surface: Color @Composable @ReadOnlyComposable get() = LocalAppColors.current.Surface
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
    val IconBlue: Color @Composable @ReadOnlyComposable get() = LocalAppColors.current.IconBlue
    val IconGreen: Color @Composable @ReadOnlyComposable get() = LocalAppColors.current.IconGreen
    val IconOrange: Color @Composable @ReadOnlyComposable get() = LocalAppColors.current.IconOrange
    val IconRed: Color @Composable @ReadOnlyComposable get() = LocalAppColors.current.IconRed
    val IconPurple: Color @Composable @ReadOnlyComposable get() = LocalAppColors.current.IconPurple
}

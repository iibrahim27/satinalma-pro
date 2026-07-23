package com.satinalmayonetici.android.ui.theme

import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Typography
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.sp

object YoneticiColors {
    val TealDeep = Color(0xFF0B3D3A)
    val Teal = Color(0xFF0F766E)
    val TealBright = Color(0xFF14B8A6)
    val Ink = Color(0xFF0F172A)
    val Slate = Color(0xFF334155)
    val Mist = Color(0xFFF1F5F9)
    val Card = Color(0xFFFFFFFF)
    val Danger = Color(0xFFDC2626)
    val Amber = Color(0xFFD97706)
    val Line = Color(0xFFE2E8F0)

    val LoginGradient = Brush.verticalGradient(
        colors = listOf(
            Color(0xFF062E2B),
            Color(0xFF0F766E),
            Color(0xFF115E59)
        )
    )
}

private val LightScheme = lightColorScheme(
    primary = YoneticiColors.Teal,
    onPrimary = Color.White,
    primaryContainer = Color(0xFFCCFBF1),
    onPrimaryContainer = YoneticiColors.TealDeep,
    secondary = YoneticiColors.TealDeep,
    onSecondary = Color.White,
    background = YoneticiColors.Mist,
    onBackground = YoneticiColors.Ink,
    surface = YoneticiColors.Card,
    onSurface = YoneticiColors.Ink,
    surfaceVariant = Color(0xFFE2E8F0),
    onSurfaceVariant = YoneticiColors.Slate,
    error = YoneticiColors.Danger,
    outline = YoneticiColors.Line
)

private val DarkScheme = darkColorScheme(
    primary = YoneticiColors.TealBright,
    onPrimary = YoneticiColors.TealDeep,
    secondary = Color(0xFF5EEAD4),
    background = Color(0xFF0B1220),
    onBackground = Color(0xFFF8FAFC),
    surface = Color(0xFF111827),
    onSurface = Color(0xFFF8FAFC),
    error = Color(0xFFF87171)
)

private val AppTypography = Typography(
    headlineLarge = TextStyle(
        fontFamily = FontFamily.SansSerif,
        fontWeight = FontWeight.Bold,
        fontSize = 30.sp,
        letterSpacing = (-0.5).sp
    ),
    headlineMedium = TextStyle(
        fontFamily = FontFamily.SansSerif,
        fontWeight = FontWeight.Bold,
        fontSize = 24.sp
    ),
    titleLarge = TextStyle(
        fontFamily = FontFamily.SansSerif,
        fontWeight = FontWeight.SemiBold,
        fontSize = 20.sp
    ),
    titleMedium = TextStyle(
        fontFamily = FontFamily.SansSerif,
        fontWeight = FontWeight.SemiBold,
        fontSize = 16.sp
    ),
    bodyLarge = TextStyle(
        fontFamily = FontFamily.SansSerif,
        fontWeight = FontWeight.Normal,
        fontSize = 16.sp
    ),
    bodyMedium = TextStyle(
        fontFamily = FontFamily.SansSerif,
        fontWeight = FontWeight.Normal,
        fontSize = 14.sp,
        lineHeight = 20.sp
    ),
    labelLarge = TextStyle(
        fontFamily = FontFamily.SansSerif,
        fontWeight = FontWeight.SemiBold,
        fontSize = 14.sp
    )
)

@Composable
fun YoneticiTheme(
    darkTheme: Boolean = isSystemInDarkTheme(),
    content: @Composable () -> Unit
) {
    MaterialTheme(
        colorScheme = if (darkTheme) DarkScheme else LightScheme,
        typography = AppTypography,
        content = content
    )
}

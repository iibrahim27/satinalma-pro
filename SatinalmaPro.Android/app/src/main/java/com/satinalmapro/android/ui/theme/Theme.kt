package com.satinalmapro.android.ui.theme

import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextFieldDefaults
import androidx.compose.material3.Typography
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.sp

private val LightColorScheme = lightColorScheme(
    primary = LightAppColors.Primary,
    onPrimary = LightAppColors.Surface,
    primaryContainer = LightAppColors.PrimaryContainer,
    onPrimaryContainer = LightAppColors.Primary,
    secondary = LightAppColors.TextSecondary,
    background = LightAppColors.Background,
    onBackground = LightAppColors.TextPrimary,
    surface = LightAppColors.Surface,
    onSurface = LightAppColors.TextPrimary,
    surfaceVariant = LightAppColors.Background,
    onSurfaceVariant = LightAppColors.TextSecondary,
    outline = LightAppColors.Border,
    error = LightAppColors.Danger,
    onError = LightAppColors.Surface
)

private val DarkColorScheme = darkColorScheme(
    primary = DarkAppColors.Primary,
    onPrimary = DarkAppColors.Background,
    primaryContainer = DarkAppColors.PrimaryContainer,
    onPrimaryContainer = DarkAppColors.Primary,
    secondary = DarkAppColors.TextSecondary,
    background = DarkAppColors.Background,
    onBackground = DarkAppColors.TextPrimary,
    surface = DarkAppColors.Surface,
    onSurface = DarkAppColors.TextPrimary,
    surfaceVariant = DarkAppColors.Background,
    onSurfaceVariant = DarkAppColors.TextSecondary,
    outline = DarkAppColors.Border,
    error = DarkAppColors.Danger,
    onError = DarkAppColors.TextPrimary
)

private val AppTypography = Typography(
    displaySmall = TextStyle(
        fontFamily = FontFamily.SansSerif,
        fontWeight = FontWeight.Bold,
        fontSize = 28.sp,
        lineHeight = 34.sp
    ),
    headlineMedium = TextStyle(
        fontFamily = FontFamily.SansSerif,
        fontWeight = FontWeight.SemiBold,
        fontSize = 22.sp,
        lineHeight = 28.sp
    ),
    titleLarge = TextStyle(
        fontFamily = FontFamily.SansSerif,
        fontWeight = FontWeight.SemiBold,
        fontSize = 18.sp,
        lineHeight = 24.sp
    ),
    titleMedium = TextStyle(
        fontFamily = FontFamily.SansSerif,
        fontWeight = FontWeight.SemiBold,
        fontSize = 16.sp,
        lineHeight = 22.sp
    ),
    bodyLarge = TextStyle(
        fontFamily = FontFamily.SansSerif,
        fontWeight = FontWeight.Medium,
        fontSize = 16.sp,
        lineHeight = 24.sp
    ),
    bodyMedium = TextStyle(
        fontFamily = FontFamily.SansSerif,
        fontWeight = FontWeight.Medium,
        fontSize = 14.sp,
        lineHeight = 20.sp
    ),
    labelLarge = TextStyle(
        fontFamily = FontFamily.SansSerif,
        fontWeight = FontWeight.SemiBold,
        fontSize = 14.sp,
        lineHeight = 18.sp
    ),
    labelMedium = TextStyle(
        fontFamily = FontFamily.SansSerif,
        fontWeight = FontWeight.Medium,
        fontSize = 12.sp,
        lineHeight = 16.sp
    )
)

@Composable
fun SatinalmaProTheme(
    darkTheme: Boolean = isSystemInDarkTheme(),
    content: @Composable () -> Unit
) {
    val palette = if (darkTheme) DarkAppColors else LightAppColors
    val colorScheme = if (darkTheme) DarkColorScheme else LightColorScheme

    CompositionLocalProvider(LocalAppColors provides palette) {
        MaterialTheme(
            colorScheme = colorScheme,
            typography = AppTypography,
            shapes = AppShapes,
            content = content
        )
    }
}

@Composable
fun appFieldColors() = OutlinedTextFieldDefaults.colors(
    focusedTextColor = AppColors.TextPrimary,
    unfocusedTextColor = AppColors.TextPrimary,
    focusedLabelColor = AppColors.TextSecondary,
    unfocusedLabelColor = AppColors.TextSecondary,
    cursorColor = AppColors.Primary,
    focusedBorderColor = AppColors.Primary,
    unfocusedBorderColor = AppColors.Border,
    focusedContainerColor = AppColors.Surface,
    unfocusedContainerColor = AppColors.Surface
)

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
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.sp

private val LightColorScheme = lightColorScheme(
    primary = LightAppColors.Primary,
    onPrimary = LightAppColors.TextOnPrimary,
    primaryContainer = LightAppColors.PrimaryContainer,
    onPrimaryContainer = LightAppColors.Primary,
    secondary = LightAppColors.Secondary,
    onSecondary = LightAppColors.TextOnPrimary,
    secondaryContainer = LightAppColors.InfoContainer,
    onSecondaryContainer = LightAppColors.Primary,
    tertiary = LightAppColors.IconPurple,
    background = LightAppColors.Background,
    onBackground = LightAppColors.TextPrimary,
    surface = LightAppColors.Surface,
    onSurface = LightAppColors.TextPrimary,
    surfaceVariant = LightAppColors.Background,
    onSurfaceVariant = LightAppColors.TextSecondary,
    surfaceContainerLow = LightAppColors.Background,
    surfaceContainerHigh = LightAppColors.PrimaryContainer,
    outline = LightAppColors.Border,
    outlineVariant = LightAppColors.Border,
    error = LightAppColors.Danger,
    onError = LightAppColors.TextOnPrimary
)

private val DarkColorScheme = darkColorScheme(
    primary = DarkAppColors.Primary,
    onPrimary = DarkAppColors.TextOnPrimary,
    primaryContainer = DarkAppColors.PrimaryContainer,
    onPrimaryContainer = DarkAppColors.Primary,
    secondary = DarkAppColors.Secondary,
    onSecondary = DarkAppColors.TextOnPrimary,
    secondaryContainer = DarkAppColors.InfoContainer,
    onSecondaryContainer = DarkAppColors.Primary,
    tertiary = DarkAppColors.IconPurple,
    background = DarkAppColors.Background,
    onBackground = DarkAppColors.TextPrimary,
    surface = DarkAppColors.Surface,
    onSurface = DarkAppColors.TextPrimary,
    surfaceVariant = DarkAppColors.SurfaceElevated,
    onSurfaceVariant = DarkAppColors.TextSecondary,
    surfaceContainerLow = DarkAppColors.Background,
    surfaceContainerHigh = DarkAppColors.PrimaryContainer,
    outline = DarkAppColors.Border,
    outlineVariant = DarkAppColors.Border,
    error = DarkAppColors.Danger,
    onError = DarkAppColors.TextPrimary
)

private val AppTypography = Typography(
    displaySmall = TextStyle(
        fontFamily = AppFontFamily,
        fontWeight = FontWeight.Bold,
        fontSize = 28.sp,
        lineHeight = 34.sp,
        letterSpacing = (-0.5).sp
    ),
    headlineMedium = TextStyle(
        fontFamily = AppFontFamily,
        fontWeight = FontWeight.Bold,
        fontSize = 22.sp,
        lineHeight = 28.sp,
        letterSpacing = (-0.25).sp
    ),
    titleLarge = TextStyle(
        fontFamily = AppFontFamily,
        fontWeight = FontWeight.SemiBold,
        fontSize = 18.sp,
        lineHeight = 24.sp
    ),
    titleMedium = TextStyle(
        fontFamily = AppFontFamily,
        fontWeight = FontWeight.SemiBold,
        fontSize = 16.sp,
        lineHeight = 22.sp
    ),
    titleSmall = TextStyle(
        fontFamily = AppFontFamily,
        fontWeight = FontWeight.Medium,
        fontSize = 14.sp,
        lineHeight = 20.sp
    ),
    bodyLarge = TextStyle(
        fontFamily = AppFontFamily,
        fontWeight = FontWeight.Normal,
        fontSize = 16.sp,
        lineHeight = 24.sp
    ),
    bodyMedium = TextStyle(
        fontFamily = AppFontFamily,
        fontWeight = FontWeight.Normal,
        fontSize = 14.sp,
        lineHeight = 20.sp
    ),
    bodySmall = TextStyle(
        fontFamily = AppFontFamily,
        fontWeight = FontWeight.Normal,
        fontSize = 12.sp,
        lineHeight = 16.sp
    ),
    labelLarge = TextStyle(
        fontFamily = AppFontFamily,
        fontWeight = FontWeight.SemiBold,
        fontSize = 14.sp,
        lineHeight = 18.sp
    ),
    labelMedium = TextStyle(
        fontFamily = AppFontFamily,
        fontWeight = FontWeight.Medium,
        fontSize = 12.sp,
        lineHeight = 16.sp
    ),
    labelSmall = TextStyle(
        fontFamily = AppFontFamily,
        fontWeight = FontWeight.Medium,
        fontSize = 11.sp,
        lineHeight = 14.sp
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

@Composable
fun heroSearchFieldColors() = OutlinedTextFieldDefaults.colors(
    focusedTextColor = AppColors.TextOnPrimary,
    unfocusedTextColor = AppColors.TextOnPrimary,
    focusedPlaceholderColor = AppColors.TextOnPrimary.copy(alpha = 0.65f),
    unfocusedPlaceholderColor = AppColors.TextOnPrimary.copy(alpha = 0.65f),
    cursorColor = AppColors.TextOnPrimary,
    focusedBorderColor = AppColors.TextOnPrimary.copy(alpha = 0.35f),
    unfocusedBorderColor = AppColors.TextOnPrimary.copy(alpha = 0.25f),
    focusedContainerColor = AppColors.TextOnPrimary.copy(alpha = 0.12f),
    unfocusedContainerColor = AppColors.TextOnPrimary.copy(alpha = 0.10f),
    focusedLeadingIconColor = AppColors.TextOnPrimary.copy(alpha = 0.75f),
    unfocusedLeadingIconColor = AppColors.TextOnPrimary.copy(alpha = 0.75f)
)

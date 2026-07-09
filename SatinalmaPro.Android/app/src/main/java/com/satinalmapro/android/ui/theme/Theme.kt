package com.satinalmapro.android.ui.theme

import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.ui.graphics.Color

private val MetrikColorScheme = lightColorScheme(
    primary = MetrikLight.Primary,
    onPrimary = MetrikLight.TextOnPrimary,
    primaryContainer = MetrikLight.AccentMuted,
    onPrimaryContainer = MetrikLight.PrimaryDark,
    secondary = MetrikLight.Accent,
    onSecondary = MetrikLight.TextOnAccent,
    secondaryContainer = MetrikLight.AccentMuted,
    onSecondaryContainer = MetrikLight.PrimaryDark,
    tertiary = MetrikLight.Info,
    background = MetrikLight.Background,
    onBackground = MetrikLight.TextPrimary,
    surface = MetrikLight.Surface,
    onSurface = MetrikLight.TextPrimary,
    surfaceVariant = MetrikLight.SurfaceMuted,
    onSurfaceVariant = MetrikLight.TextSecondary,
    outline = MetrikLight.Border,
    error = MetrikLight.Danger,
    onError = MetrikLight.TextOnPrimary
)

@Composable
fun MetrikTheme(
    darkTheme: Boolean = isSystemInDarkTheme(),
    content: @Composable () -> Unit
) {
    CompositionLocalProvider(LocalMetrikColors provides MetrikLight) {
        MaterialTheme(
            colorScheme = MetrikColorScheme,
            typography = MetrikTypography,
            shapes = MetrikShapes,
            content = content
        )
    }
}

@Composable
fun SatinalmaProTheme(
    darkTheme: Boolean = isSystemInDarkTheme(),
    content: @Composable () -> Unit
) = MetrikTheme(darkTheme = darkTheme, content = content)

fun statusFromText(text: String): Color {
    val t = text.lowercase()
    return when {
        t.contains("onay") || t.contains("tamam") || t.contains("kabul") -> MetrikLight.Success
        t.contains("red") || t.contains("iptal") || t.contains("hata") -> MetrikLight.Danger
        t.contains("bekle") || t.contains("taslak") || t.contains("düzelt") -> MetrikLight.Warning
        t.contains("teklif") || t.contains("sipariş") -> MetrikLight.Info
        else -> MetrikLight.TextSecondary
    }
}

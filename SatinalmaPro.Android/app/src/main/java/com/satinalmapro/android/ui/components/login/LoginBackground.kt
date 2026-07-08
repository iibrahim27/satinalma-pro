package com.satinalmapro.android.ui.components.login

import androidx.compose.animation.core.LinearEasing
import androidx.compose.animation.core.RepeatMode
import androidx.compose.animation.core.animateFloat
import androidx.compose.animation.core.infiniteRepeatable
import androidx.compose.animation.core.rememberInfiniteTransition
import androidx.compose.animation.core.tween
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import kotlin.math.sin
import kotlin.random.Random

private data class Particle(val x: Float, val y: Float, val radius: Float, val speed: Float, val phase: Float)

@Composable
fun LoginBackground(modifier: Modifier = Modifier) {
    val dark = isSystemInDarkTheme()
    val bgTop = if (dark) Color(0xFF0B1220) else Color(0xFFF8FAFC)
    val bgBottom = if (dark) Color(0xFF050810) else Color(0xFFEEF2F8)
    val particleColor = if (dark) Color(0xFF4F46E5) else Color(0xFF6D5BFF)
    val lineColor = if (dark) Color(0xFF1E3A6E) else Color(0xFFC7D2FE)

    val particles = remember {
        List(28) {
            Particle(
                x = Random.nextFloat(),
                y = Random.nextFloat(),
                radius = Random.nextFloat() * 2.5f + 1f,
                speed = Random.nextFloat() * 0.35f + 0.15f,
                phase = Random.nextFloat() * 6.28f
            )
        }
    }

    val transition = rememberInfiniteTransition(label = "login_bg")
    val drift by transition.animateFloat(
        initialValue = 0f,
        targetValue = 1f,
        animationSpec = infiniteRepeatable(tween(12000, easing = LinearEasing), RepeatMode.Restart),
        label = "drift"
    )

    Box(
        modifier = modifier
            .fillMaxSize()
            .background(Brush.verticalGradient(listOf(bgTop, bgBottom)))
    ) {
        Canvas(Modifier.fillMaxSize()) {
            val w = size.width
            val h = size.height
            particles.forEach { p ->
                val y = ((p.y + drift * p.speed) % 1.2f - 0.1f) * h
                val x = p.x * w + sin(drift * 6.28f + p.phase) * 18f
                drawCircle(
                    color = particleColor.copy(alpha = if (dark) 0.35f else 0.22f),
                    radius = p.radius * 2.2f,
                    center = Offset(x, y)
                )
            }
            for (i in 0..4) {
                val y = h * (0.15f + i * 0.18f) + sin(drift * 6.28f + i) * 24f
                drawLine(
                    color = lineColor.copy(alpha = if (dark) 0.18f else 0.28f),
                    start = Offset(0f, y),
                    end = Offset(w, y + 40f),
                    strokeWidth = 1.2f
                )
            }
        }
    }
}

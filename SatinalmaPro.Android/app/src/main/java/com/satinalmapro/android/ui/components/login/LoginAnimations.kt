package com.satinalmapro.android.ui.components.login

import androidx.compose.animation.core.Animatable
import androidx.compose.animation.core.FastOutSlowInEasing
import androidx.compose.animation.core.tween
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.remember
import androidx.compose.ui.Modifier
import androidx.compose.ui.composed
import androidx.compose.ui.graphics.graphicsLayer
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.unit.dp

fun Modifier.loginCardEntrance(visible: Boolean = true): Modifier = composed {
    val offsetY = remember { Animatable(48f) }
    val alpha = remember { Animatable(0f) }
    LaunchedEffect(visible) {
        if (visible) {
            offsetY.animateTo(0f, tween(500, easing = FastOutSlowInEasing))
            alpha.animateTo(1f, tween(500, easing = FastOutSlowInEasing))
        }
    }
    val density = LocalDensity.current
    graphicsLayer {
        translationY = with(density) { offsetY.value.dp.toPx() }
        this.alpha = alpha.value
    }
}

fun Modifier.loginShake(trigger: Int): Modifier = composed {
    val offsetX = remember { Animatable(0f) }
    LaunchedEffect(trigger) {
        if (trigger > 0) {
            val pattern = listOf(0f, 12f, -12f, 8f, -8f, 4f, 0f)
            pattern.forEach { target ->
                offsetX.animateTo(target, tween(45))
            }
        }
    }
    val density = LocalDensity.current
    graphicsLayer {
        translationX = with(density) { offsetX.value.dp.toPx() }
    }
}

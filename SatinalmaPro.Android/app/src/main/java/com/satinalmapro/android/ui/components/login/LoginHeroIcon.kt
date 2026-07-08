package com.satinalmapro.android.ui.components.login

import androidx.compose.animation.core.Animatable
import androidx.compose.animation.core.FastOutSlowInEasing
import androidx.compose.animation.core.tween
import androidx.compose.foundation.Image
import androidx.compose.foundation.background
import androidx.compose.foundation.gestures.detectTapGestures
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableFloatStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.alpha
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.rotate
import androidx.compose.ui.draw.scale
import androidx.compose.ui.draw.shadow
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.input.pointer.pointerInput
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import com.satinalmapro.android.R
import com.satinalmapro.android.ui.theme.AppColors
import kotlinx.coroutines.coroutineScope
import kotlinx.coroutines.launch

@Composable
fun LoginHeroIcon(
    modifier: Modifier = Modifier,
    size: Dp = 108.dp,
    playEntrance: Boolean = true,
    contentDescription: String = "Satınalma Pro"
) {
    val scale = remember { Animatable(if (playEntrance) 0.8f else 1f) }
    val alpha = remember { Animatable(if (playEntrance) 0f else 1f) }
    var tilt by remember { mutableFloatStateOf(0f) }

    LaunchedEffect(playEntrance) {
        if (playEntrance) {
            coroutineScope {
                launch {
                    scale.animateTo(1f, tween(700, easing = FastOutSlowInEasing))
                }
                launch {
                    alpha.animateTo(1f, tween(700, easing = FastOutSlowInEasing))
                }
            }
        }
    }

    Box(
        modifier = modifier
            .size(size * 1.2f)
            .pointerInput(Unit) {
                detectTapGestures(
                    onPress = {
                        tilt = 3f
                        tryAwaitRelease()
                        tilt = -3f
                        kotlinx.coroutines.delay(120)
                        tilt = 0f
                    }
                )
            },
        contentAlignment = Alignment.Center
    ) {
        Box(
            modifier = Modifier
                .size(size * 1.05f)
                .scale(scale.value)
                .alpha(alpha.value * 0.55f)
                .background(
                    Brush.radialGradient(
                        listOf(Color(0xFF4F46E5), Color(0xFF6D5BFF), Color.Transparent)
                    ),
                    CircleShape
                )
        )
        Box(
            modifier = Modifier
                .size(size * 1.02f)
                .scale(scale.value)
                .alpha(alpha.value)
                .shadow(16.dp, CircleShape, ambientColor = AppColors.Primary, spotColor = Color(0xFF6D5BFF))
                .clip(CircleShape)
                .background(
                    Brush.linearGradient(listOf(Color(0x334F46E5), Color(0x226D5BFF))),
                    CircleShape
                )
        )
        Image(
            painter = painterResource(R.drawable.app_icon),
            contentDescription = contentDescription,
            modifier = Modifier
                .size(size * 0.88f)
                .scale(scale.value)
                .alpha(alpha.value)
                .rotate(tilt),
            contentScale = ContentScale.Fit
        )
    }
}

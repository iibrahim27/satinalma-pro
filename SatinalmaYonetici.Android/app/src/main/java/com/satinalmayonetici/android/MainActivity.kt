package com.satinalmayonetici.android

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.viewModels
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.material3.lightColorScheme
import androidx.compose.ui.graphics.Color
import com.satinalmayonetici.android.ui.YoneticiRoot
import com.satinalmayonetici.android.ui.YoneticiViewModel

class MainActivity : ComponentActivity() {
    private val vm: YoneticiViewModel by viewModels()

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContent {
            MaterialTheme(
                colorScheme = lightColorScheme(
                    primary = Color(0xFF0F766E),
                    secondary = Color(0xFF115E59)
                )
            ) {
                Surface {
                    YoneticiRoot(vm)
                }
            }
        }
    }
}

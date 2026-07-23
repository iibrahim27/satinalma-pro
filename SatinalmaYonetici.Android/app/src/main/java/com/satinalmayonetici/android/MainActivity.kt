package com.satinalmayonetici.android

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.activity.viewModels
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.material3.Surface
import androidx.compose.ui.Modifier
import com.satinalmayonetici.android.ui.YoneticiRoot
import com.satinalmayonetici.android.ui.YoneticiViewModel
import com.satinalmayonetici.android.ui.theme.YoneticiTheme

class MainActivity : ComponentActivity() {
    private val vm: YoneticiViewModel by viewModels()

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        setContent {
            YoneticiTheme(darkTheme = false) {
                Surface(Modifier.fillMaxSize()) {
                    YoneticiRoot(vm)
                }
            }
        }
    }
}

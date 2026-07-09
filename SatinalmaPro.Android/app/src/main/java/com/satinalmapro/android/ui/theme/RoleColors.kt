package com.satinalmapro.android.ui.theme

import androidx.compose.ui.graphics.Color
import com.satinalmapro.android.core.roles.KullaniciRolleri

object RoleColors {
    fun accent(role: String?): Color = when (KullaniciRolleri.normalize(role)) {
        KullaniciRolleri.ADMIN -> MetrikLight.Primary
        KullaniciRolleri.YONETIM -> MetrikLight.Info
        KullaniciRolleri.SATINALMA -> MetrikLight.Accent
        KullaniciRolleri.SEF -> MetrikLight.Warning
        KullaniciRolleri.SAHA -> MetrikLight.Success
        KullaniciRolleri.DEPO -> MetrikLight.PrimaryDark
        else -> MetrikLight.TextSecondary
    }
}

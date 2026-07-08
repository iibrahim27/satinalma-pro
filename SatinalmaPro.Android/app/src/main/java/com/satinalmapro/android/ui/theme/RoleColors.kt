package com.satinalmapro.android.ui.theme

import androidx.compose.ui.graphics.Color
import com.satinalmapro.android.core.roles.KullaniciRolleri

data class RoleVisual(
    val title: String,
    val subtitle: String,
    val color: Color,
    val container: Color
)

object RoleColors {
    fun forRole(role: String?): RoleVisual {
        return when (KullaniciRolleri.normalize(role)) {
            KullaniciRolleri.ADMIN -> RoleVisual(
                "Admin",
                "Tam erişim ve sistem yönetimi",
                Color(0xFFEF4444),
                Color(0xFFFEE2E2)
            )
            KullaniciRolleri.YONETIM -> RoleVisual(
                "Yönetim",
                "Onay ve teklif karar süreçleri",
                Color(0xFF8B5CF6),
                Color(0xFFEDE9FE)
            )
            KullaniciRolleri.SATINALMA -> RoleVisual(
                "Satınalma",
                "Operasyon ve tedarik merkezi",
                Color(0xFF0F2A5A),
                Color(0xFFE8EEF7)
            )
            KullaniciRolleri.SEF -> RoleVisual(
                "Şef",
                "Saha yönetimi ve talep takibi",
                Color(0xFF0EA5E9),
                Color(0xFFE0F2FE)
            )
            KullaniciRolleri.SAHA -> RoleVisual(
                "Saha",
                "Talep oluşturma ve takip",
                Color(0xFF14B8A6),
                Color(0xFFCCFBF1)
            )
            KullaniciRolleri.DEPO -> RoleVisual(
                "Depo",
                "Stok giriş, çıkış ve sayım",
                Color(0xFFF59E0B),
                Color(0xFFFEF3C7)
            )
            KullaniciRolleri.ATOLYE -> RoleVisual(
                "Atölye",
                "Stok görüntüleme",
                Color(0xFF64748B),
                Color(0xFFF1F5F9)
            )
            else -> RoleVisual(
                "Kullanıcı",
                "Satınalma Pro",
                Color(0xFF2563EB),
                Color(0xFFEFF6FF)
            )
        }
    }
}

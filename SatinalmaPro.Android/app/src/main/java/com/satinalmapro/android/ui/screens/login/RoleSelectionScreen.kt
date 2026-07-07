package com.satinalmapro.android.ui.screens.login

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.satinalmapro.android.core.roles.KullaniciRolleri
import com.satinalmapro.android.ui.components.RoleSelectionCard
import com.satinalmapro.android.ui.theme.AppColors
import com.satinalmapro.android.ui.theme.AppSpacing
import com.satinalmapro.android.ui.theme.RoleColors

private val allRoles = listOf(
    KullaniciRolleri.ADMIN,
    KullaniciRolleri.YONETIM,
    KullaniciRolleri.SATINALMA,
    KullaniciRolleri.SEF,
    KullaniciRolleri.SAHA,
    KullaniciRolleri.DEPO,
    KullaniciRolleri.ATOLYE
)

@Composable
fun RoleSelectionScreen(
    currentRole: String?,
    onRoleSelected: (String) -> Unit,
    modifier: Modifier = Modifier
) {
    LazyColumn(
        modifier = modifier.fillMaxSize(),
        contentPadding = PaddingValues(
            horizontal = AppSpacing.screenHorizontal,
            vertical = AppSpacing.screenVertical
        ),
        verticalArrangement = Arrangement.spacedBy(AppSpacing.cardGap)
    ) {
        item {
            Text(
                "Rol Seçimi",
                style = MaterialTheme.typography.headlineMedium,
                color = AppColors.TextPrimary
            )
            Text(
                "Devam etmek için rolünüzü seçin",
                style = MaterialTheme.typography.bodyMedium,
                color = AppColors.TextSecondary,
                modifier = Modifier.padding(top = 4.dp, bottom = 8.dp)
            )
        }
        items(allRoles) { role ->
            val visual = RoleColors.forRole(role)
            RoleSelectionCard(
                visual = visual,
                selected = KullaniciRolleri.normalize(currentRole) == role,
                onClick = { onRoleSelected(role) }
            )
        }
    }
}

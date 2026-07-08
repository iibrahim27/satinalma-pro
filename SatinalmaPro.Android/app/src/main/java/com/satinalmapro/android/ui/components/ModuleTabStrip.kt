package com.satinalmapro.android.ui.components

import androidx.compose.foundation.horizontalScroll
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.material3.Badge
import androidx.compose.material3.BadgedBox
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.FilterChip
import androidx.compose.material3.FilterChipDefaults
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.satinalmapro.android.core.model.MenuItem
import com.satinalmapro.android.ui.theme.AppColors
import com.satinalmapro.android.ui.theme.AppShapes
import com.satinalmapro.android.ui.theme.AppSpacing

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ModuleTabStrip(
    menus: List<MenuItem>,
    selectedRoute: String,
    menuBadges: Map<String, Int>,
    onSelect: (String) -> Unit,
    modifier: Modifier = Modifier
) {
    val tabs = menus.filter { it.route !in excludedRoutes }
    if (tabs.size < 2) return

    Surface(
        modifier = modifier.fillMaxWidth(),
        color = AppColors.Surface,
        shadowElevation = 1.dp
    ) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .horizontalScroll(rememberScrollState())
                .padding(vertical = 10.dp),
            horizontalArrangement = Arrangement.spacedBy(8.dp)
        ) {
            tabs.forEach { item ->
                val selected = item.route == selectedRoute
                val badge = menuBadges[item.route] ?: 0
                FilterChip(
                    selected = selected,
                    onClick = { onSelect(item.route) },
                    modifier = Modifier.padding(start = if (item == tabs.first()) AppSpacing.screenHorizontal else 0.dp),
                    label = {
                        if (badge > 0) {
                            BadgedBox(
                                badge = {
                                    Badge(containerColor = AppColors.Danger) {
                                        Text(if (badge > 99) "99+" else badge.toString())
                                    }
                                }
                            ) {
                                Text(
                                    item.title,
                                    style = MaterialTheme.typography.labelLarge,
                                    fontWeight = if (selected) FontWeight.SemiBold else FontWeight.Medium
                                )
                            }
                        } else {
                            Text(
                                item.title,
                                style = MaterialTheme.typography.labelLarge,
                                fontWeight = if (selected) FontWeight.SemiBold else FontWeight.Medium
                            )
                        }
                    },
                    shape = AppShapes.extraSmall,
                    colors = FilterChipDefaults.filterChipColors(
                        selectedContainerColor = AppColors.Primary,
                        selectedLabelColor = AppColors.TextOnPrimary,
                        containerColor = AppColors.Background,
                        labelColor = AppColors.TextSecondary
                    ),
                    border = FilterChipDefaults.filterChipBorder(
                        enabled = true,
                        selected = selected,
                        borderColor = AppColors.Border,
                        selectedBorderColor = AppColors.Primary
                    )
                )
            }
        }
    }
}

private val excludedRoutes = setOf("dashboard", "profil", "bildirimler", "raporlar", "ayarlar", "yeni-talep")

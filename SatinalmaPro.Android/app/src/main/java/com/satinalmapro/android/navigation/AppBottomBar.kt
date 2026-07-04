package com.satinalmapro.android.navigation

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.offset
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.rounded.Add
import androidx.compose.material.icons.rounded.Assessment
import androidx.compose.material.icons.rounded.Home
import androidx.compose.material.icons.rounded.Inventory2
import androidx.compose.material.icons.rounded.Person
import androidx.compose.material3.FloatingActionButton
import androidx.compose.material3.FloatingActionButtonDefaults
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.NavigationBar
import androidx.compose.material3.NavigationBarItem
import androidx.compose.material3.NavigationBarItemDefaults
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.unit.dp
import com.satinalmapro.android.ui.theme.AppColors

private data class NavBarEntry(
    val item: BottomNavItem?,
    val icon: ImageVector,
    val label: String,
    val isFab: Boolean = false
)

private val navEntries = listOf(
    NavBarEntry(BottomNavItem.Home, Icons.Rounded.Home, "Ana Sayfa"),
    NavBarEntry(BottomNavItem.Materials, Icons.Rounded.Inventory2, "Malzemeler"),
    NavBarEntry(null, Icons.Rounded.Add, "+", isFab = true),
    NavBarEntry(BottomNavItem.Reports, Icons.Rounded.Assessment, "Raporlar"),
    NavBarEntry(BottomNavItem.Profile, Icons.Rounded.Person, "Profil")
)

@Composable
fun AppBottomBar(
    currentRoute: String?,
    onNavigate: (String) -> Unit,
    onFabClick: () -> Unit
) {
    Box(modifier = Modifier.fillMaxWidth()) {
        Surface(
            modifier = Modifier.fillMaxWidth(),
            color = AppColors.Surface,
            shadowElevation = 8.dp,
            tonalElevation = 0.dp
        ) {
            NavigationBar(
                containerColor = AppColors.Surface,
                tonalElevation = 0.dp
            ) {
                navEntries.forEach { entry ->
                    if (entry.isFab) {
                        NavigationBarItem(
                            selected = false,
                            onClick = onFabClick,
                            icon = { Box(Modifier.size(48.dp)) },
                            label = { Text("") },
                            colors = NavigationBarItemDefaults.colors(
                                indicatorColor = AppColors.Surface
                            )
                        )
                    } else {
                        val item = entry.item!!
                        val selected = currentRoute == item.route
                        NavigationBarItem(
                            selected = selected,
                            onClick = { onNavigate(item.route) },
                            icon = {
                                Icon(
                                    entry.icon,
                                    contentDescription = entry.label,
                                    tint = if (selected) AppColors.Primary else AppColors.TextSecondary
                                )
                            },
                            label = {
                                Text(
                                    entry.label,
                                    style = MaterialTheme.typography.labelMedium,
                                    color = if (selected) AppColors.Primary else AppColors.TextSecondary
                                )
                            },
                            colors = NavigationBarItemDefaults.colors(
                                selectedIconColor = AppColors.Primary,
                                selectedTextColor = AppColors.Primary,
                                indicatorColor = AppColors.PrimaryContainer,
                                unselectedIconColor = AppColors.TextSecondary,
                                unselectedTextColor = AppColors.TextSecondary
                            )
                        )
                    }
                }
            }
        }

        FloatingActionButton(
            onClick = onFabClick,
            modifier = Modifier
                .align(Alignment.TopCenter)
                .offset(y = (-22).dp)
                .size(56.dp),
            shape = CircleShape,
            containerColor = AppColors.Primary,
            elevation = FloatingActionButtonDefaults.elevation(defaultElevation = 6.dp)
        ) {
            Icon(Icons.Rounded.Add, contentDescription = "Yeni Talep", tint = AppColors.Surface)
        }
    }
}

package com.satinalmapro.android.ui.components

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.ColumnScope
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.rounded.KeyboardArrowRight
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import com.satinalmapro.android.ui.theme.AppColors
import com.satinalmapro.android.ui.theme.AppElevation
import com.satinalmapro.android.ui.theme.AppShapes
import com.satinalmapro.android.ui.theme.AppSpacing
import com.satinalmapro.android.ui.theme.StatusType
import com.satinalmapro.android.ui.theme.statusColors

@Composable
fun AppCard(
    modifier: Modifier = Modifier,
    onClick: (() -> Unit)? = null,
    contentPadding: Dp = AppSpacing.cardPadding,
    containerColor: Color = AppColors.Surface,
    content: @Composable ColumnScope.() -> Unit
) {
    val colors = CardDefaults.cardColors(containerColor = containerColor)
    val elevation = CardDefaults.cardElevation(defaultElevation = AppElevation.card)
    val shape = AppShapes.medium
    val wrapped: @Composable ColumnScope.() -> Unit = {
        Column(Modifier.padding(contentPadding), content = content)
    }
    if (onClick != null) {
        Card(
            onClick = onClick,
            modifier = modifier.fillMaxWidth(),
            shape = shape,
            colors = colors,
            elevation = elevation,
            content = wrapped
        )
    } else {
        Card(
            modifier = modifier.fillMaxWidth(),
            shape = shape,
            colors = colors,
            elevation = elevation,
            content = wrapped
        )
    }
}

@Composable
fun ModernListCard(
    title: String,
    description: String,
    icon: ImageVector,
    iconBg: Color,
    iconFg: Color,
    onClick: () -> Unit,
    modifier: Modifier = Modifier,
    statusLabel: String? = null,
    statusType: StatusType = StatusType.Info,
    counter: Int? = null,
    trailing: String? = null
) {
    val (statusBg, statusFg) = statusColors(statusType)
    AppCard(modifier = modifier, onClick = onClick) {
        Row(
            modifier = Modifier.fillMaxWidth(),
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.spacedBy(14.dp)
        ) {
            Box(
                modifier = Modifier
                    .size(AppSpacing.cardPadding + 28.dp)
                    .then(Modifier),
                contentAlignment = Alignment.Center
            ) {
                Surface(
                    shape = AppShapes.small,
                    color = iconBg,
                    modifier = Modifier.size(48.dp)
                ) {
                    Box(contentAlignment = Alignment.Center) {
                        Icon(icon, contentDescription = null, tint = iconFg, modifier = Modifier.size(24.dp))
                    }
                }
            }
            Column(Modifier.weight(1f), verticalArrangement = Arrangement.spacedBy(4.dp)) {
                Row(
                    verticalAlignment = Alignment.CenterVertically,
                    horizontalArrangement = Arrangement.spacedBy(8.dp)
                ) {
                    Text(
                        title,
                        style = MaterialTheme.typography.titleMedium,
                        color = AppColors.TextPrimary,
                        maxLines = 1,
                        overflow = TextOverflow.Ellipsis,
                        modifier = Modifier.weight(1f, fill = false)
                    )
                    counter?.takeIf { it > 0 }?.let { count ->
                        Surface(shape = AppShapes.extraSmall, color = AppColors.PrimaryContainer) {
                            Text(
                                "$count",
                                modifier = Modifier.padding(horizontal = 8.dp, vertical = 2.dp),
                                style = MaterialTheme.typography.labelMedium,
                                color = AppColors.Primary,
                                fontWeight = FontWeight.Bold
                            )
                        }
                    }
                }
                Text(
                    description,
                    style = MaterialTheme.typography.bodyMedium,
                    color = AppColors.TextSecondary,
                    maxLines = 2,
                    overflow = TextOverflow.Ellipsis
                )
                if (!statusLabel.isNullOrBlank()) {
                    StatusBadge(statusLabel, statusBg, statusFg)
                }
            }
            Column(horizontalAlignment = Alignment.End, verticalArrangement = Arrangement.spacedBy(4.dp)) {
                trailing?.let {
                    Text(it, style = MaterialTheme.typography.labelSmall, color = AppColors.TextSecondary)
                }
                Icon(
                    Icons.AutoMirrored.Rounded.KeyboardArrowRight,
                    contentDescription = null,
                    tint = AppColors.TextSecondary.copy(alpha = 0.6f),
                    modifier = Modifier.size(22.dp)
                )
            }
        }
    }
}

@Composable
fun StatusBadge(
    text: String,
    background: Color,
    foreground: Color,
    modifier: Modifier = Modifier
) {
    Surface(
        modifier = modifier,
        shape = AppShapes.extraSmall,
        color = background
    ) {
        Text(
            text = text,
            modifier = Modifier.padding(horizontal = 10.dp, vertical = 4.dp),
            style = MaterialTheme.typography.labelMedium,
            color = foreground,
            fontWeight = FontWeight.Medium
        )
    }
}

@Composable
fun SectionTitle(
    title: String,
    modifier: Modifier = Modifier,
    action: (@Composable () -> Unit)? = null
) {
    Row(
        modifier = modifier.fillMaxWidth(),
        horizontalArrangement = Arrangement.SpaceBetween,
        verticalAlignment = Alignment.CenterVertically
    ) {
        Text(
            text = title,
            style = MaterialTheme.typography.titleMedium,
            color = AppColors.TextPrimary,
            fontWeight = FontWeight.Bold
        )
        action?.invoke()
    }
}

@Composable
fun SectionTitleWithAction(
    title: String,
    actionLabel: String,
    onAction: () -> Unit,
    modifier: Modifier = Modifier
) {
    SectionTitle(
        title = title,
        modifier = modifier,
        action = {
            TextButton(onClick = onAction) {
                Text(actionLabel, color = AppColors.Primary, fontWeight = FontWeight.SemiBold)
            }
        }
    )
}

@Composable
fun DetailRow(label: String, value: String) {
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .padding(vertical = 10.dp),
        horizontalArrangement = Arrangement.SpaceBetween
    ) {
        Text(
            text = label,
            style = MaterialTheme.typography.bodyMedium,
            color = AppColors.TextSecondary
        )
        Text(
            text = value,
            style = MaterialTheme.typography.bodyMedium,
            color = AppColors.TextPrimary,
            fontWeight = FontWeight.Medium
        )
    }
}

@Composable
fun IconBadge(
    background: Color,
    content: @Composable () -> Unit
) {
    Box(
        modifier = Modifier.size(48.dp),
        contentAlignment = Alignment.Center
    ) {
        Surface(
            shape = AppShapes.small,
            color = background.copy(alpha = 0.15f)
        ) {
            Box(
                modifier = Modifier.size(48.dp),
                contentAlignment = Alignment.Center
            ) {
                content()
            }
        }
    }
}

@Composable
fun ScreenScaffold(
    modifier: Modifier = Modifier,
    content: @Composable ColumnScope.() -> Unit
) {
    Column(
        modifier = modifier
            .fillMaxWidth()
            .padding(horizontal = AppSpacing.screenHorizontal, vertical = AppSpacing.screenVertical),
        verticalArrangement = Arrangement.spacedBy(AppSpacing.sectionGap),
        content = content
    )
}

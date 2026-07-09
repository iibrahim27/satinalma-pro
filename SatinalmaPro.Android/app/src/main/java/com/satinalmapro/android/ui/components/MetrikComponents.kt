package com.satinalmapro.android.ui.components

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.VisualTransformation
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import com.satinalmapro.android.ui.theme.AppShapes
import com.satinalmapro.android.ui.theme.MetrikColors
import com.satinalmapro.android.ui.theme.MetrikLight
import com.satinalmapro.android.ui.theme.MetrikSpace
import com.satinalmapro.android.ui.theme.appFieldColors
import com.satinalmapro.android.ui.theme.statusFromText

@Composable
fun MetrikButton(
    text: String,
    onClick: () -> Unit,
    modifier: Modifier = Modifier,
    enabled: Boolean = true,
    accent: Boolean = false,
    loading: Boolean = false
) {
    Button(
        onClick = onClick,
        enabled = enabled && !loading,
        modifier = modifier.height(48.dp),
        shape = AppShapes.button,
        colors = ButtonDefaults.buttonColors(
            containerColor = if (accent) MetrikColors.Accent else MetrikColors.Primary,
            contentColor = if (accent) MetrikColors.TextOnAccent else MetrikColors.TextOnPrimary,
            disabledContainerColor = MetrikColors.SurfaceMuted,
            disabledContentColor = MetrikColors.TextTertiary
        ),
        contentPadding = PaddingValues(horizontal = MetrikSpace.lg)
    ) {
        if (loading) {
            CircularProgressIndicator(
                modifier = Modifier.size(18.dp),
                color = MetrikColors.TextOnPrimary,
                strokeWidth = 2.dp
            )
        } else {
            Text(text, style = MaterialTheme.typography.labelLarge)
        }
    }
}

@Composable
fun AppPrimaryButton(
    text: String,
    onClick: () -> Unit,
    modifier: Modifier = Modifier,
    enabled: Boolean = true,
    loading: Boolean = false
) = MetrikButton(
    text = text,
    onClick = onClick,
    modifier = modifier,
    enabled = enabled,
    accent = true,
    loading = loading
)

@Composable
fun MetrikGhostButton(
    text: String,
    onClick: () -> Unit,
    modifier: Modifier = Modifier,
    enabled: Boolean = true
) {
    OutlinedButton(
        onClick = onClick,
        enabled = enabled,
        modifier = modifier.height(48.dp),
        shape = AppShapes.button,
        colors = ButtonDefaults.outlinedButtonColors(contentColor = MetrikColors.Primary),
        border = androidx.compose.foundation.BorderStroke(1.dp, MetrikColors.Border)
    ) {
        Text(text, style = MaterialTheme.typography.labelLarge)
    }
}

@Composable
fun MetrikField(
    value: String,
    onValueChange: (String) -> Unit,
    label: String,
    modifier: Modifier = Modifier,
    placeholder: String = "",
    singleLine: Boolean = true,
    enabled: Boolean = true,
    visualTransformation: VisualTransformation = VisualTransformation.None,
    keyboardOptions: KeyboardOptions = KeyboardOptions.Default,
    trailingIcon: @Composable (() -> Unit)? = null
) {
    OutlinedTextField(
        value = value,
        onValueChange = onValueChange,
        modifier = modifier.fillMaxWidth(),
        label = { Text(label) },
        placeholder = if (placeholder.isNotBlank()) ({ Text(placeholder) }) else null,
        singleLine = singleLine,
        enabled = enabled,
        visualTransformation = visualTransformation,
        keyboardOptions = keyboardOptions,
        trailingIcon = trailingIcon,
        shape = AppShapes.field,
        colors = appFieldColors()
    )
}

@Composable
fun StatusPill(text: String, modifier: Modifier = Modifier) {
    val color = statusFromText(text)
    Text(
        text = text,
        modifier = modifier
            .background(color.copy(alpha = 0.12f), AppShapes.chip)
            .padding(horizontal = 8.dp, vertical = 3.dp),
        color = color,
        style = MaterialTheme.typography.labelMedium,
        maxLines = 1,
        overflow = TextOverflow.Ellipsis
    )
}

@Composable
fun EmptyState(
    title: String = "Bu firmada henüz kayıt yok",
    subtitle: String = "Yeni kayıt oluştuğunda burada listelenir.",
    modifier: Modifier = Modifier
) {
    Column(
        modifier = modifier
            .fillMaxSize()
            .padding(MetrikSpace.xl),
        verticalArrangement = Arrangement.Center,
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        Text(title, style = MaterialTheme.typography.titleLarge, color = MetrikColors.TextPrimary)
        Spacer(Modifier.height(MetrikSpace.sm))
        Text(subtitle, style = MaterialTheme.typography.bodyMedium, color = MetrikColors.TextSecondary)
    }
}

@Composable
fun LoadingState(modifier: Modifier = Modifier) {
    androidx.compose.foundation.layout.Box(
        modifier = modifier.fillMaxSize(),
        contentAlignment = Alignment.Center
    ) {
        CircularProgressIndicator(color = MetrikColors.Primary)
    }
}

@Composable
fun QueueRow(
    title: String,
    subtitle: String,
    meta: String,
    status: String,
    onClick: () -> Unit,
    modifier: Modifier = Modifier
) {
    Column(
        modifier = modifier
            .fillMaxWidth()
            .clickable(onClick = onClick)
            .padding(horizontal = MetrikSpace.screen, vertical = MetrikSpace.md)
    ) {
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Text(
                title,
                style = MaterialTheme.typography.titleMedium,
                color = MetrikColors.TextPrimary,
                modifier = Modifier.weight(1f),
                maxLines = 1,
                overflow = TextOverflow.Ellipsis
            )
            Spacer(Modifier.width(MetrikSpace.sm))
            StatusPill(status)
        }
        Spacer(Modifier.height(4.dp))
        Text(
            subtitle,
            style = MaterialTheme.typography.bodyMedium,
            color = MetrikColors.TextSecondary,
            maxLines = 2,
            overflow = TextOverflow.Ellipsis
        )
        if (meta.isNotBlank()) {
            Spacer(Modifier.height(4.dp))
            Text(
                meta,
                style = MaterialTheme.typography.labelMedium,
                color = MetrikColors.TextTertiary,
                maxLines = 1,
                overflow = TextOverflow.Ellipsis
            )
        }
    }
    HorizontalDivider(color = MetrikColors.Divider, thickness = 1.dp)
}

@Composable
fun SectionHeader(title: String, action: String? = null, onAction: (() -> Unit)? = null) {
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .padding(horizontal = MetrikSpace.screen, vertical = MetrikSpace.sm),
        horizontalArrangement = Arrangement.SpaceBetween,
        verticalAlignment = Alignment.CenterVertically
    ) {
        Text(
            title.uppercase(),
            style = MaterialTheme.typography.labelLarge,
            color = MetrikColors.TextSecondary,
            fontWeight = FontWeight.SemiBold
        )
        if (action != null && onAction != null) {
            TextButton(onClick = onAction) {
                Text(action, color = MetrikColors.Accent)
            }
        }
    }
}

@Composable
fun TenantFooter(firma: String, kullanici: String, rol: String) {
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .background(MetrikLight.PrimaryDark)
            .padding(horizontal = MetrikSpace.screen, vertical = 8.dp),
        horizontalArrangement = Arrangement.SpaceBetween,
        verticalAlignment = Alignment.CenterVertically
    ) {
        Text(
            firma.ifBlank { "Firma seçilmedi" },
            color = MetrikLight.TextOnPrimary,
            style = MaterialTheme.typography.labelMedium,
            maxLines = 1,
            overflow = TextOverflow.Ellipsis,
            modifier = Modifier.weight(1f)
        )
        Text(
            listOf(kullanici, rol).filter { it.isNotBlank() }.joinToString(" · "),
            color = MetrikLight.TextOnPrimary.copy(alpha = 0.75f),
            style = MaterialTheme.typography.labelSmall,
            maxLines = 1,
            overflow = TextOverflow.Ellipsis
        )
    }
}

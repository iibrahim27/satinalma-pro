package com.satinalmapro.android.ui.components

import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.height
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.LinearProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.satinalmapro.android.core.model.UpdateManifest
import com.satinalmapro.android.ui.theme.MetrikColors

@Composable
fun UpdateDialog(
    manifest: UpdateManifest,
    progress: Int?,
    message: String?,
    error: String?,
    onUpdate: () -> Unit,
    onDismiss: () -> Unit
) {
    AlertDialog(
        onDismissRequest = { if (progress == null) onDismiss() },
        title = { Text("Yeni sürüm") },
        text = {
            Column {
                Text(
                    "v${manifest.version} (build ${manifest.build})",
                    style = MaterialTheme.typography.titleMedium,
                    color = MetrikColors.Primary
                )
                if (manifest.notes.isNotBlank()) {
                    Spacer(Modifier.height(8.dp))
                    Text(manifest.notes, style = MaterialTheme.typography.bodyMedium)
                }
                if (progress != null) {
                    Spacer(Modifier.height(12.dp))
                    LinearProgressIndicator(progress = { progress / 100f })
                    Text("%$progress", style = MaterialTheme.typography.labelMedium)
                }
                if (!message.isNullOrBlank()) {
                    Spacer(Modifier.height(8.dp))
                    Text(message, color = MetrikColors.TextSecondary)
                }
                if (!error.isNullOrBlank()) {
                    Spacer(Modifier.height(8.dp))
                    Text(error, color = MetrikColors.Danger)
                }
            }
        },
        confirmButton = {
            if (progress == null) {
                MetrikButton(text = "Güncelle", onClick = onUpdate, accent = true)
            }
        },
        dismissButton = {
            if (progress == null) {
                TextButton(onClick = onDismiss) { Text("Sonra") }
            }
        }
    )
}

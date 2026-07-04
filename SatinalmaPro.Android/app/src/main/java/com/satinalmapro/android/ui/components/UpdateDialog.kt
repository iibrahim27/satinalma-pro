package com.satinalmapro.android.ui.components

import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.height
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Button
import androidx.compose.material3.LinearProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.satinalmapro.android.core.model.UpdateManifest
import com.satinalmapro.android.ui.theme.AppColors

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
        title = { Text("Yeni sürüm mevcut") },
        text = {
            Column {
                Text(
                    "v${manifest.version} (build ${manifest.build})",
                    style = MaterialTheme.typography.titleMedium,
                    color = AppColors.Primary
                )
                if (manifest.notes.isNotBlank()) {
                    Spacer(Modifier.height(8.dp))
                    Text(manifest.notes, style = MaterialTheme.typography.bodyMedium)
                }
                message?.let {
                    Spacer(Modifier.height(12.dp))
                    Text(it, style = MaterialTheme.typography.bodySmall, color = AppColors.TextSecondary)
                }
                if (progress != null) {
                    Spacer(Modifier.height(12.dp))
                    LinearProgressIndicator(progress = { progress / 100f }, modifier = Modifier.height(6.dp))
                }
                error?.let {
                    Spacer(Modifier.height(8.dp))
                    Text(it, color = AppColors.Danger, style = MaterialTheme.typography.bodySmall)
                }
            }
        },
        confirmButton = {
            Button(onClick = onUpdate, enabled = progress == null) {
                Text(if (progress == null) "Güncelle" else "İndiriliyor...")
            }
        },
        dismissButton = {
            if (progress == null) {
                TextButton(onClick = onDismiss) { Text("Sonra") }
            }
        }
    )
}

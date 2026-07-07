package com.satinalmapro.android.core

import android.content.Context
import com.google.firebase.FirebaseOptions
import org.json.JSONObject

object GoogleServicesLoader {
    fun loadOptions(context: Context, fallbackApiKey: String, fallbackProjectId: String): FirebaseOptions? {
        val json = runCatching {
            context.assets.open("google-services.json").bufferedReader().use { it.readText() }
        }.getOrNull() ?: return fallbackOptions(fallbackApiKey, fallbackProjectId)

        return parse(json) ?: fallbackOptions(fallbackApiKey, fallbackProjectId)
    }

    private fun parse(json: String): FirebaseOptions? {
        return try {
            val root = JSONObject(json.trim().removePrefix("\uFEFF"))
            val projectInfo = root.optJSONObject("project_info") ?: return null
            val projectId = projectInfo.optString("project_id")
            val projectNumber = projectInfo.optString("project_number")
            val clients = root.optJSONArray("client") ?: return null
            for (i in 0 until clients.length()) {
                val client = clients.optJSONObject(i) ?: continue
                val clientInfo = client.optJSONObject("client_info") ?: continue
                val appId = clientInfo.optString("mobilesdk_app_id")
                val packageName = clientInfo.optJSONObject("android_client_info")?.optString("package_name").orEmpty()
                if (packageName != "com.metrik.satinalmapro") continue
                val apiKey = client.optJSONArray("api_key")?.optJSONObject(0)?.optString("current_key").orEmpty()
                if (appId.isBlank() || apiKey.isBlank() || projectId.isBlank()) continue
                return FirebaseOptions.Builder()
                    .setApplicationId(appId)
                    .setProjectId(projectId)
                    .setGcmSenderId(projectNumber)
                    .setApiKey(apiKey)
                    .build()
            }
            null
        } catch (_: Exception) {
            null
        }
    }

    private fun fallbackOptions(apiKey: String, projectId: String): FirebaseOptions? {
        if (apiKey.isBlank() || projectId.isBlank()) return null
        return FirebaseOptions.Builder()
            .setApplicationId("1:000000000000:android:0000000000000000000000")
            .setProjectId(projectId)
            .setApiKey(apiKey)
            .build()
    }

    /** google-services.json içinden projectId ve apiKey okur (firebase_ayarlar.json yoksa). */
    fun readProjectFromAssets(context: Context): Pair<String, String>? {
        val json = runCatching {
            context.assets.open("google-services.json").bufferedReader().use { it.readText() }
        }.getOrNull() ?: return null
        return try {
            val root = JSONObject(json.trim().removePrefix("\uFEFF"))
            val projectId = root.optJSONObject("project_info")?.optString("project_id").orEmpty()
            val clients = root.optJSONArray("client") ?: return null
            for (i in 0 until clients.length()) {
                val client = clients.optJSONObject(i) ?: continue
                val packageName = client.optJSONObject("client_info")
                    ?.optJSONObject("android_client_info")?.optString("package_name").orEmpty()
                if (packageName != "com.metrik.satinalmapro") continue
                val apiKey = client.optJSONArray("api_key")?.optJSONObject(0)?.optString("current_key").orEmpty()
                if (projectId.isNotBlank() && apiKey.isNotBlank()) return projectId to apiKey
            }
            null
        } catch (_: Exception) {
            null
        }
    }
}

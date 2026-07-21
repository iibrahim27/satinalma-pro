package com.satinalmayonetici.android

import android.app.Application
import android.content.Context
import com.google.firebase.FirebaseApp
import com.google.firebase.FirebaseOptions
import com.satinalmayonetici.android.data.FirebaseConfig
import com.satinalmayonetici.android.data.PlatformApi
import org.json.JSONObject

class YoneticiApp : Application() {
    lateinit var config: FirebaseConfig
        private set
    lateinit var api: PlatformApi
        private set

    override fun onCreate() {
        super.onCreate()
        config = loadConfig(this)
        api = PlatformApi(config)
        initFirebase()
    }

    private fun initFirebase() {
        if (FirebaseApp.getApps(this).isNotEmpty()) return
        val options = loadFirebaseOptions(this, config) ?: return
        FirebaseApp.initializeApp(this, options)
    }

    companion object {
        fun from(context: Context): YoneticiApp = context.applicationContext as YoneticiApp

        fun loadConfig(context: Context): FirebaseConfig {
            val json = runCatching {
                context.assets.open("firebase_ayarlar.json").bufferedReader().use { it.readText() }
            }.getOrNull()
            if (!json.isNullOrBlank()) {
                val root = JSONObject(json)
                val apiKey = root.optString("apiKey")
                val projectId = root.optString("projectId")
                if (apiKey.isNotBlank() && projectId.isNotBlank()) {
                    return FirebaseConfig(apiKey, projectId)
                }
            }
            val fromGs = readFromGoogleServices(context)
            if (fromGs != null) return fromGs
            throw IllegalStateException("firebase_ayarlar.json / google-services.json bulunamadı.")
        }

        private fun readFromGoogleServices(context: Context): FirebaseConfig? {
            val json = runCatching {
                context.assets.open("google-services.json").bufferedReader().use { it.readText() }
            }.getOrNull() ?: return null
            val root = JSONObject(json)
            val projectId = root.optJSONObject("project_info")?.optString("project_id").orEmpty()
            val clients = root.optJSONArray("client") ?: return null
            for (i in 0 until clients.length()) {
                val client = clients.optJSONObject(i) ?: continue
                val pkg = client.optJSONObject("client_info")
                    ?.optJSONObject("android_client_info")
                    ?.optString("package_name").orEmpty()
                if (pkg != "com.metrik.satinalmapro.admin" && pkg != "com.metrik.satinalmapro") continue
                val apiKey = client.optJSONArray("api_key")?.optJSONObject(0)?.optString("current_key").orEmpty()
                if (projectId.isNotBlank() && apiKey.isNotBlank()) {
                    return FirebaseConfig(apiKey, projectId)
                }
            }
            return null
        }

        private fun loadFirebaseOptions(context: Context, config: FirebaseConfig): FirebaseOptions? {
            val json = runCatching {
                context.assets.open("google-services.json").bufferedReader().use { it.readText() }
            }.getOrNull() ?: return null
            val root = JSONObject(json)
            val projectInfo = root.optJSONObject("project_info") ?: return null
            val projectId = projectInfo.optString("project_id")
            val projectNumber = projectInfo.optString("project_number")
            val clients = root.optJSONArray("client") ?: return null
            for (i in 0 until clients.length()) {
                val client = clients.optJSONObject(i) ?: continue
                val clientInfo = client.optJSONObject("client_info") ?: continue
                val pkg = clientInfo.optJSONObject("android_client_info")?.optString("package_name").orEmpty()
                if (pkg != "com.metrik.satinalmapro.admin") continue
                val appId = clientInfo.optString("mobilesdk_app_id")
                if (appId.isBlank()) continue
                return FirebaseOptions.Builder()
                    .setApplicationId(appId)
                    .setProjectId(projectId)
                    .setGcmSenderId(projectNumber)
                    .setApiKey(config.apiKey)
                    .build()
            }
            return null
        }
    }
}

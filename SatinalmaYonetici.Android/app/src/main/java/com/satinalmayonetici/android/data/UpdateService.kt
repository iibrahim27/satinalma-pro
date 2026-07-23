package com.satinalmayonetici.android.data

import android.content.Context
import android.content.pm.PackageManager
import android.os.Build
import com.satinalmayonetici.android.BuildConfig
import com.satinalmayonetici.android.services.ApkUpdateInstaller
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import okhttp3.OkHttpClient
import okhttp3.Request
import org.json.JSONObject
import java.io.File
import java.util.concurrent.TimeUnit

data class UpdateManifest(
    val version: String = "",
    val build: Int = 0,
    val downloadUrlApk: String = "",
    val notes: String = ""
)

enum class UpdateInstallResult { SUCCESS, NEEDS_PERMISSION, FAILED }

data class UpdateCheckResult(
    val available: Boolean,
    val manifest: UpdateManifest? = null,
    val error: String? = null
)

class UpdateService(private val context: Context) {
    private val http = OkHttpClient.Builder()
        .connectTimeout(30, TimeUnit.SECONDS)
        .readTimeout(120, TimeUnit.SECONDS)
        .build()
    private val installer = ApkUpdateInstaller(context)

    suspend fun checkForUpdate(): UpdateCheckResult = withContext(Dispatchers.IO) {
        val errors = mutableListOf<String>()
        var best: UpdateManifest? = null
        for (baseUrl in manifestUrls()) {
            try {
                val json = get(cacheBust(baseUrl))
                if (json.isBlank()) {
                    errors.add("Boş yanıt")
                    continue
                }
                val manifest = parseManifest(json)
                if (manifest.version.isBlank() || manifest.build <= 0) {
                    errors.add("Geçersiz manifest")
                    continue
                }
                if (best == null ||
                    manifest.build > best!!.build ||
                    (manifest.build == best!!.build && versionGreater(manifest.version, best!!.version))
                ) {
                    best = manifest
                }
            } catch (e: Exception) {
                errors.add(e.message ?: e.javaClass.simpleName)
            }
        }
        val manifest = best ?: return@withContext UpdateCheckResult(
            false,
            error = errors.lastOrNull() ?: "Güncelleme sunucusuna ulaşılamadı"
        )
        val needs = manifest.build > BuildConfig.VERSION_CODE ||
            versionGreater(manifest.version, BuildConfig.VERSION_NAME)
        UpdateCheckResult(needs, if (needs) manifest else null)
    }

    suspend fun downloadAndInstall(
        manifest: UpdateManifest,
        onProgress: (String, Int) -> Unit
    ): UpdateInstallResult = withContext(Dispatchers.IO) {
        val urls = apkUrls(manifest)
        onProgress("Güncelleme indiriliyor...", 8)
        val target = File(context.cacheDir, "SatinalmaYonetici_${manifest.version}_b${manifest.build}.apk")
        var lastError: Exception? = null
        for ((index, url) in urls.withIndex()) {
            try {
                download(url, target) { p ->
                    onProgress("İndiriliyor... %$p", 10 + (p * 0.8).toInt())
                }
                lastError = null
                break
            } catch (e: Exception) {
                lastError = e
                if (index == urls.lastIndex) throw e
            }
        }
        if (lastError != null) throw lastError!!
        if (!validateApk(target, manifest)) {
            target.delete()
            throw IllegalStateException("İndirilen APK geçersiz veya sürüm uyuşmuyor")
        }
        onProgress("Kurulum başlatılıyor...", 95)
        if (!installer.ensureInstallPermission()) return@withContext UpdateInstallResult.NEEDS_PERMISSION
        return@withContext try {
            installer.install(target)
            UpdateInstallResult.SUCCESS
        } catch (_: Exception) {
            UpdateInstallResult.FAILED
        }
    }

    private fun manifestUrls(): List<String> = listOf(
        "https://raw.githubusercontent.com/iibrahim27/satinalma-pro/main/version-yonetici.json",
        "https://github.com/iibrahim27/satinalma-pro/raw/main/version-yonetici.json",
        "https://cdn.jsdelivr.net/gh/iibrahim27/satinalma-pro@main/version-yonetici.json"
    )

    private fun apkUrls(manifest: UpdateManifest): List<String> = buildList {
        manifest.downloadUrlApk.takeIf { it.isNotBlank() }?.let(::add)
        add("https://github.com/iibrahim27/satinalma-pro/releases/download/yonetici-v${manifest.version}/SatinalmaYonetici.apk")
    }.distinct()

    private fun cacheBust(url: String): String =
        if (url.contains('?')) "$url&_=${System.currentTimeMillis()}"
        else "$url?_=${System.currentTimeMillis()}"

    private fun get(url: String): String {
        val req = Request.Builder().url(url).get().header("Cache-Control", "no-cache").build()
        http.newCall(req).execute().use { res ->
            if (!res.isSuccessful) throw IllegalStateException("HTTP ${res.code}")
            return res.body?.string().orEmpty()
        }
    }

    private fun download(url: String, target: File, onProgress: (Int) -> Unit) {
        val req = Request.Builder().url(url).get().build()
        http.newCall(req).execute().use { res ->
            if (!res.isSuccessful) throw IllegalStateException("İndirme HTTP ${res.code}")
            val body = res.body ?: throw IllegalStateException("Boş APK yanıtı")
            val total = body.contentLength()
            target.outputStream().use { out ->
                body.byteStream().use { input ->
                    val buf = ByteArray(16 * 1024)
                    var readTotal = 0L
                    var n: Int
                    while (input.read(buf).also { n = it } >= 0) {
                        out.write(buf, 0, n)
                        readTotal += n
                        if (total > 0) onProgress(((readTotal * 100) / total).toInt().coerceIn(0, 100))
                    }
                }
            }
        }
        if (totalInvalid(target)) throw IllegalStateException("APK indirilemedi")
    }

    private fun totalInvalid(file: File) = !file.exists() || file.length() < 1024

    private fun parseManifest(json: String): UpdateManifest {
        val o = JSONObject(json)
        return UpdateManifest(
            version = o.optString("version"),
            build = o.optInt("build"),
            downloadUrlApk = o.optString("downloadUrlApk"),
            notes = o.optString("notes")
        )
    }

    private fun validateApk(file: File, manifest: UpdateManifest): Boolean {
        val info = context.packageManager.getPackageArchiveInfo(
            file.absolutePath,
            PackageManager.GET_ACTIVITIES
        ) ?: return true
        val apkBuild = if (Build.VERSION.SDK_INT >= 28) info.longVersionCode else {
            @Suppress("DEPRECATION")
            info.versionCode.toLong()
        }
        return apkBuild >= manifest.build
    }

    private fun versionGreater(a: String, b: String): Boolean {
        val pa = a.split('.').mapNotNull { it.toIntOrNull() }
        val pb = b.split('.').mapNotNull { it.toIntOrNull() }
        val n = maxOf(pa.size, pb.size)
        for (i in 0 until n) {
            val x = pa.getOrElse(i) { 0 }
            val y = pb.getOrElse(i) { 0 }
            if (x != y) return x > y
        }
        return false
    }
}

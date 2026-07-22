plugins {
    id("com.android.application")
    id("org.jetbrains.kotlin.android")
    id("org.jetbrains.kotlin.plugin.compose")
}

import java.util.Properties

val keystorePropertiesFile = rootProject.file("../SatinalmaPro.Android/keystore.properties")
val keystoreProperties = Properties().apply {
    if (keystorePropertiesFile.exists()) {
        keystorePropertiesFile.inputStream().use { load(it) }
    }
}

android {
    namespace = "com.satinalmayonetici.android"
    compileSdk = 35

    defaultConfig {
        applicationId = "com.metrik.satinalmapro.admin"
        minSdk = 31
        targetSdk = 35
        versionCode = 34
        versionName = "1.0.34"
    }

    signingConfigs {
        create("release") {
            if (keystorePropertiesFile.exists()) {
                storeFile = file(keystoreProperties["storeFile"] as String)
                storePassword = keystoreProperties["storePassword"] as String
                keyAlias = keystoreProperties["keyAlias"] as String
                keyPassword = keystoreProperties["keyPassword"] as String
            } else {
                val home = System.getProperty("user.home")
                storeFile = file("$home/.android/debug.keystore")
                storePassword = "android"
                keyAlias = "androiddebugkey"
                keyPassword = "android"
            }
        }
    }

    buildTypes {
        release {
            signingConfig = signingConfigs.getByName("release")
            isMinifyEnabled = false
        }
    }

    buildFeatures {
        compose = true
        buildConfig = true
    }

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }

    kotlinOptions {
        jvmTarget = "17"
    }
}

val firebaseAssetsDir = file("src/main/assets")
val satinalmaProDir = file("../../Satinalma Pro")

tasks.register("copyFirebaseAssets") {
    doLast {
        firebaseAssetsDir.mkdirs()
        val googleServices = file("${satinalmaProDir}/google-services.json")
        if (googleServices.exists()) {
            googleServices.copyTo(file("${firebaseAssetsDir}/google-services.json"), overwrite = true)
        }
        val firebaseAyarlar = file("${satinalmaProDir}/firebase_ayarlar.json")
        if (firebaseAyarlar.exists()) {
            val json = firebaseAyarlar.readText()
                .replace(
                    "https://raw.githubusercontent.com/iibrahim27/satinalma-pro/main/version.json",
                    "https://raw.githubusercontent.com/iibrahim27/satinalma-pro/main/version-yonetici.json"
                )
            file("${firebaseAssetsDir}/firebase_ayarlar.json").writeText(json)
        }
    }
}

tasks.named("preBuild") {
    dependsOn("copyFirebaseAssets")
}

dependencies {
    val composeBom = platform("androidx.compose:compose-bom:2024.12.01")
    implementation(composeBom)

    implementation("androidx.core:core-ktx:1.15.0")
    implementation("androidx.activity:activity-compose:1.9.3")
    implementation("androidx.lifecycle:lifecycle-runtime-ktx:2.8.7")
    implementation("androidx.lifecycle:lifecycle-viewmodel-compose:2.8.7")

    implementation("androidx.compose.ui:ui")
    implementation("androidx.compose.ui:ui-tooling-preview")
    implementation("androidx.compose.material3:material3")
    implementation("androidx.compose.material:material-icons-extended")
    implementation("androidx.compose.foundation:foundation")

    implementation(platform("com.google.firebase:firebase-bom:33.7.0"))
    implementation("com.google.firebase:firebase-auth-ktx")

    implementation("com.squareup.okhttp3:okhttp:4.12.0")
    implementation("org.jetbrains.kotlinx:kotlinx-coroutines-android:1.9.0")
    implementation("org.jetbrains.kotlinx:kotlinx-coroutines-play-services:1.9.0")

    debugImplementation("androidx.compose.ui:ui-tooling")
}

plugins {
    id("com.android.application")
    id("org.jetbrains.kotlin.android")
    id("org.jetbrains.kotlin.plugin.compose")
}

import java.util.Properties

val keystorePropertiesFile = rootProject.file("keystore.properties")
val keystoreProperties = Properties().apply {
    if (keystorePropertiesFile.exists()) {
        keystorePropertiesFile.inputStream().use { load(it) }
    }
}

android {
    namespace = "com.satinalmapro.android"
    compileSdk = 35

    defaultConfig {
        applicationId = "com.metrik.satinalmapro"
        minSdk = 31
        targetSdk = 35
        versionCode = 145
        versionName = "2.1.76"
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

    testOptions {
        unitTests.isIncludeAndroidResources = true
    }

    sourceSets {
        getByName("androidTest") {
            java.srcDir("src/test/java")
        }
    }

    packaging {
        resources {
            excludes += "/META-INF/{AL2.0,LGPL2.1}"
            excludes += "META-INF/INDEX.LIST"
            excludes += "META-INF/DEPENDENCIES"
        }
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
        val fcm = file("${satinalmaProDir}/fcm-service-account.json")
        if (fcm.exists()) {
            fcm.copyTo(file("${firebaseAssetsDir}/fcm-service-account.json"), overwrite = true)
        }
        val firebaseAyarlar = file("${satinalmaProDir}/firebase_ayarlar.json")
        if (firebaseAyarlar.exists()) {
            firebaseAyarlar.copyTo(file("${firebaseAssetsDir}/firebase_ayarlar.json"), overwrite = true)
        }
    }
}

tasks.named("preBuild") {
    dependsOn("copyFirebaseAssets")
}

dependencies {
    val composeBom = platform("androidx.compose:compose-bom:2024.12.01")
    implementation(composeBom)
    androidTestImplementation(composeBom)

    implementation("androidx.core:core-ktx:1.15.0")
    implementation("androidx.activity:activity-compose:1.9.3")
    implementation("androidx.lifecycle:lifecycle-runtime-ktx:2.8.7")
    implementation("androidx.lifecycle:lifecycle-viewmodel-compose:2.8.7")

    implementation("androidx.compose.ui:ui")
    implementation("androidx.compose.ui:ui-tooling-preview")
    implementation("androidx.compose.material3:material3")
    implementation("androidx.compose.material:material-icons-extended")
    implementation("androidx.compose.animation:animation")

    implementation("androidx.navigation:navigation-compose:2.8.5")
    implementation("androidx.compose.material3:material3-window-size-class:1.3.1")
    implementation("androidx.appcompat:appcompat:1.7.0")
    implementation("androidx.biometric:biometric:1.1.0")
    implementation("androidx.fragment:fragment-ktx:1.8.5")

    implementation(platform("com.google.firebase:firebase-bom:33.7.0"))
    implementation("com.google.firebase:firebase-messaging-ktx")
    implementation("com.google.firebase:firebase-auth-ktx")
    implementation("com.google.firebase:firebase-firestore-ktx")
    // google-auth, Firestore'un gRPC classpath'ini bozabiliyor (InternalGlobalInterceptors).
    // gRPC artifact'lerini hizala; auth kütüphanesinden çakışan grpc'yi dışla.
    implementation("com.google.auth:google-auth-library-oauth2-http:1.30.1") {
        exclude(group = "io.grpc")
    }
    implementation("io.grpc:grpc-android:1.68.2")
    implementation("io.grpc:grpc-okhttp:1.68.2")
    implementation("io.grpc:grpc-protobuf-lite:1.68.2")
    implementation("io.grpc:grpc-stub:1.68.2")
    implementation("io.grpc:grpc-api:1.68.2")


    implementation("com.squareup.okhttp3:okhttp:4.12.0")
    implementation("com.google.code.gson:gson:2.11.0")
    implementation("org.jetbrains.kotlinx:kotlinx-coroutines-android:1.9.0")
    implementation("org.jetbrains.kotlinx:kotlinx-coroutines-play-services:1.9.0")
    implementation("androidx.work:work-runtime-ktx:2.10.0")

    debugImplementation("androidx.compose.ui:ui-tooling")
    debugImplementation("androidx.compose.ui:ui-test-manifest")

    testImplementation("junit:junit:4.13.2")
    androidTestImplementation("androidx.test.ext:junit:1.2.1")
    androidTestImplementation("androidx.test:runner:1.6.2")
    androidTestImplementation("androidx.test:rules:1.6.2")
}

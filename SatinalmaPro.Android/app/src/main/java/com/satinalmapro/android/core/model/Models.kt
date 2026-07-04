package com.satinalmapro.android.core.model

data class UserProfile(
    val uid: String,
    val email: String,
    val fullName: String,
    val role: String,
    val active: Boolean = true,
    val site: String? = null,
    val phone: String? = null
)

data class UpdateManifest(
    val version: String = "",
    val build: Int = 0,
    val downloadUrlApk: String = "",
    val notes: String = ""
)

data class AppNotification(
    val id: String,
    val title: String,
    val message: String,
    val type: String,
    val time: String,
    val requestId: String? = null,
    val route: String? = null,
    val read: Boolean = false
)

data class MenuItem(
    val title: String,
    val route: String,
    val group: String? = null
)

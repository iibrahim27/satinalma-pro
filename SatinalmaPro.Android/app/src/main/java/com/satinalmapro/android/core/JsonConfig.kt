package com.satinalmapro.android.core

import com.google.gson.Gson
import com.google.gson.GsonBuilder

/** Masaüstü Firestore JSON'u camelCase — Kotlin alan adlarıyla birebir eşleşir. */
object JsonConfig {
    val gson: Gson = GsonBuilder()
        .serializeNulls()
        .create()
}

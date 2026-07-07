package com.satinalmapro.android.services

import android.util.Log
import com.google.firebase.auth.FirebaseAuth
import kotlinx.coroutines.suspendCancellableCoroutine
import kotlin.coroutines.resume
import kotlin.coroutines.resumeWithException

/**
 * REST tabanlı oturum ile Firebase Auth SDK oturumunu senkronize eder.
 * [FcmSubscriptionHelper] ve Firestore Security Rules için SDK oturumu gerekir.
 */
object FirebaseAuthBridge {

    private const val TAG = "FirebaseAuthBridge"

    suspend fun signIn(email: String, password: String) {
        val trimmedEmail = email.trim()
        if (trimmedEmail.isBlank() || password.isBlank()) {
            throw IllegalArgumentException("E-posta veya şifre boş olamaz")
        }

        suspendCancellableCoroutine { continuation ->
            FirebaseAuth.getInstance()
                .signInWithEmailAndPassword(trimmedEmail, password)
                .addOnSuccessListener { result ->
                    val uid = result.user?.uid.orEmpty()
                    Log.i(TAG, "Firebase Auth SDK oturumu açıldı uid=$uid")
                    continuation.resume(Unit)
                }
                .addOnFailureListener { error ->
                    Log.e(TAG, "Firebase Auth SDK giriş hatası", error)
                    continuation.resumeWithException(error)
                }
        }
    }

    fun signOut() {
        runCatching {
            FirebaseAuth.getInstance().signOut()
            Log.i(TAG, "Firebase Auth SDK oturumu kapatıldı")
        }.onFailure { error ->
            Log.w(TAG, "Firebase Auth SDK çıkış hatası", error)
        }
    }

    fun currentUid(): String? = FirebaseAuth.getInstance().currentUser?.uid

    fun hasMatchingSession(expectedUid: String?): Boolean {
        if (expectedUid.isNullOrBlank()) return false
        return FirebaseAuth.getInstance().currentUser?.uid == expectedUid
    }
}

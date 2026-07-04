package com.satinalmapro.android.core

import java.io.IOException
import java.net.ConnectException
import java.net.SocketTimeoutException
import java.net.UnknownHostException
import javax.net.ssl.SSLException

object NetworkError {
    fun translate(message: String?): String {
        val mesaj = message.orEmpty()
        if (mesaj.isBlank()) return "Bağlantı hatası."

        if (mesaj.contains("quota", true) || mesaj.contains("RESOURCE_EXHAUSTED", true)) {
            return "Firebase günlük okuma kotası doldu. Birkaç saat sonra tekrar deneyin."
        }

        if (isNetworkRelated(mesaj)) {
            return "İnternet bağlantısı kurulamadı. Wi‑Fi veya mobil veriyi kontrol edip tekrar deneyin."
        }

        if (mesaj.contains("INVALID_LOGIN_CREDENTIALS", true)
            || mesaj.contains("EMAIL_NOT_FOUND", true)
            || mesaj.contains("INVALID_PASSWORD", true)
        ) {
            return "E-posta veya şifre hatalı."
        }

        if (mesaj.contains("USER_DISABLED", true)) {
            return "Hesabınız devre dışı bırakılmış."
        }

        if (mesaj.contains("Kullanıcı profili bulunamadı", true)) {
            return "Kullanıcı profili bulunamadı. Masaüstünden kullanıcı oluşturulmalıdır."
        }

        if (mesaj.contains("İndirme başarısız", true)) {
            return "Güncelleme indirilemedi. İnternet bağlantınızı kontrol edip tekrar deneyin."
        }

        if (mesaj.contains("Sürüm bilgisi alınamadı", true)) {
            return "Güncelleme sunucusuna ulaşılamadı. Daha sonra tekrar deneyin."
        }

        return mesaj
    }

    fun translate(error: Throwable): String = translate(error.message ?: error.javaClass.simpleName)

    fun isNetworkRelated(message: String?): Boolean {
        val mesaj = message.orEmpty()
        return mesaj.contains("connection", true)
            || mesaj.contains("network", true)
            || mesaj.contains("SERVICE_NOT_AVAILABLE", true)
            || mesaj.contains("Unable to resolve host", true)
            || mesaj.contains("No address associated with hostname", true)
            || mesaj.contains("timed out", true)
            || mesaj.contains("timeout", true)
            || mesaj.contains("Failed to connect", true)
    }

    fun isNetworkRelated(error: Throwable): Boolean =
        error is UnknownHostException
            || error is ConnectException
            || error is SocketTimeoutException
            || error is SSLException
            || error is IOException
            || isNetworkRelated(error.message)
}

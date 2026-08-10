package com.satinalmapro.android.core

import android.content.Context
import android.net.ConnectivityManager
import android.net.Network
import android.net.NetworkCapabilities
import android.net.NetworkRequest
import java.util.concurrent.CopyOnWriteArrayList
import java.util.concurrent.atomic.AtomicBoolean

object NetworkMonitor {
    private val onlineListeners = CopyOnWriteArrayList<() -> Unit>()
    private val registered = AtomicBoolean(false)

    fun isOnline(context: Context): Boolean {
        val manager = context.getSystemService(Context.CONNECTIVITY_SERVICE) as? ConnectivityManager
            ?: return true
        val network = manager.activeNetwork ?: return false
        val caps = manager.getNetworkCapabilities(network) ?: return false
        return caps.hasCapability(NetworkCapabilities.NET_CAPABILITY_INTERNET)
    }

    /**
     * İnternet geri gelince bir kez (veya her bağlanışta) dinleyici çağrılır.
     * Stok bekleyen yazmalarının anında gönderilmesi için.
     */
    fun registerOnlineListener(context: Context, listener: () -> Unit) {
        onlineListeners.add(listener)
        ensureCallback(context.applicationContext)
    }

    private fun ensureCallback(appContext: Context) {
        if (!registered.compareAndSet(false, true)) return
        val manager = appContext.getSystemService(Context.CONNECTIVITY_SERVICE) as? ConnectivityManager
            ?: return
        val request = NetworkRequest.Builder()
            .addCapability(NetworkCapabilities.NET_CAPABILITY_INTERNET)
            .build()
        manager.registerNetworkCallback(request, object : ConnectivityManager.NetworkCallback() {
            override fun onAvailable(network: Network) {
                onlineListeners.forEach { runCatching { it.invoke() } }
            }

            override fun onCapabilitiesChanged(network: Network, networkCapabilities: NetworkCapabilities) {
                if (networkCapabilities.hasCapability(NetworkCapabilities.NET_CAPABILITY_INTERNET)
                    && networkCapabilities.hasCapability(NetworkCapabilities.NET_CAPABILITY_VALIDATED)
                ) {
                    onlineListeners.forEach { runCatching { it.invoke() } }
                }
            }
        })
    }
}

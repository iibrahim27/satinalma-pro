package com.satinalmapro.android.core.model

data class UygulamaAyarlar(
    val firmaAdi: String = "",
    val malzemeKategorileri: List<String> = emptyList(),
    val malzemeBirimleri: List<String> = emptyList()
) {
    companion object {
        val varsayilanBirimler = listOf("Adet", "Ton", "Kg", "Lt", "m", "m²", "m³")
        val varsayilanKategoriler = listOf("Genel", "İnşaat", "Elektrik", "Mekanik")
    }
}

data class ManagedUser(
    val uid: String = "",
    val email: String = "",
    val fullName: String = "",
    val role: String = "",
    val active: Boolean = true,
    val site: String = ""
)

package com.satinalmayonetici.android.data

data class ModulYetki(
    val modul: String,
    val okuma: Boolean = false,
    val yazma: Boolean = false,
    val sekmeler: List<String> = emptyList()
)

object ModulKatalogu {
    val tum = listOf(
        "Alınan Malzemeler",
        "Stok Yönetimi",
        "Agrega",
        "Çimento",
        "Akaryakıt Takip",
        "Araç Filo Takip",
        "Finansman Raporlama",
        "Satınalma",
        "Raporlamalar",
        "Ayarlar"
    )

    private val sekmeler = mapOf(
        "Stok Yönetimi" to listOf("Stok Durumu", "Stok Hareketleri", "Stok Girişi", "Stok Çıkışı", "Stok Sayım"),
        "Ayarlar" to listOf("Genel", "Satınalma", "Malzeme Kategorileri", "Birim Terimleri", "Araç Filo", "Veri Dosyaları", "Yedekleme"),
        "Raporlamalar" to listOf("Modül Özeti", "Grup Özeti", "Detay"),
        "Finansman Raporlama" to listOf("Modül", "Nakit Akışı", "Vadeler", "Grup", "Hareketler"),
        "Satınalma" to listOf(
            "Taleplerim", "Gelen Talepler", "Onay Bekleyen", "Teklif Bekleyen", "Teklif Girişi",
            "Karşılaştırma", "Teklif Onay", "Onaylanan Talepler", "Geçmiş Talepler",
            "Geçmiş Teklifli Onaylar", "Alınan Malzemeler", "Red Talepler"
        )
    )

    fun sekmeleriAl(modul: String): List<String> = sekmeler[modul].orEmpty()

    val roller = listOf("Admin", "Yönetim", "Satınalma", "Şef", "Saha", "Atölye", "Depo")

    fun normalizeRol(rol: String?): String {
        val r = rol?.trim().orEmpty()
        if (r.isBlank()) return "Saha"
        if (r.equals("Okuma", true)) return "Saha"
        if (r.equals("Şantiye", true) || r.equals("Santiye", true)) return "Şef"
        if (r.equals("Yonetim", true)) return "Yönetim"
        if (r.equals("Satinalma", true)) return "Satınalma"
        if (r.equals("Sef", true)) return "Şef"
        if (r.equals("Atolye", true)) return "Atölye"
        return roller.firstOrNull { it.equals(r, true) } ?: "Saha"
    }

    fun masaustuModulleri(rol: String?): List<String> {
        val n = normalizeRol(rol)
        val ops = listOf(
            "Alınan Malzemeler", "Stok Yönetimi", "Agrega", "Çimento", "Akaryakıt Takip",
            "Araç Filo Takip", "Finansman Raporlama", "Satınalma", "Raporlamalar"
        )
        return when (n) {
            "Admin", "Yönetim", "Şef" -> ops
            "Satınalma" -> ops + "Ayarlar"
            "Saha" -> listOf("Satınalma", "Stok Yönetimi")
            "Depo", "Atölye" -> listOf("Stok Yönetimi")
            else -> emptyList()
        }
    }

    fun varsayilanSatinalmaSekmeler(rol: String?): List<String> {
        val n = normalizeRol(rol)
        val all = sekmeleriAl("Satınalma")
        return when (n) {
            "Admin", "Satınalma" -> all
            "Yönetim" -> all.filter {
                it in listOf(
                    "Onay Bekleyen", "Teklif Onay", "Onaylanan Talepler", "Geçmiş Talepler",
                    "Geçmiş Teklifli Onaylar", "Red Talepler", "Karşılaştırma"
                )
            }
            "Şef" -> listOf("Taleplerim", "Gelen Talepler", "Onay Bekleyen", "Onaylanan Talepler", "Alınan Malzemeler")
            "Saha" -> listOf("Taleplerim", "Onay Bekleyen", "Onaylanan Talepler")
            else -> emptyList()
        }
    }

    fun yazmaAtanabilir(rol: String?, modul: String): Boolean {
        val n = normalizeRol(rol)
        val adminOrSat = n == "Admin" || n == "Satınalma"
        if (modul.equals("Satınalma", true)) {
            return adminOrSat || n == "Yönetim" || n == "Şef" || n == "Saha"
        }
        return adminOrSat
    }

    fun rolVarsayilanYetkiler(rol: String?): List<ModulYetki> {
        val n = normalizeRol(rol)
        val mods = masaustuModulleri(n).toSet()
        return tum.map { modul ->
            val okuma = mods.contains(modul)
            val yazma = okuma && yazmaAtanabilir(n, modul)
            val sekmeler = when {
                !okuma -> emptyList()
                modul.equals("Satınalma", true) -> varsayilanSatinalmaSekmeler(n)
                else -> sekmeleriAl(modul)
            }
            ModulYetki(modul, okuma, yazma, sekmeler)
        }.filter { it.okuma || it.yazma }
    }
}

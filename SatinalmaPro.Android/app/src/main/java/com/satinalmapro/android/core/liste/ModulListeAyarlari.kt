package com.satinalmapro.android.core.liste

/** Modül tabloları için ince ayar tercihleri. */
data class ModulListeAyarlari(
    val dense: Boolean = false,
    val pageSize: Int = 50,
    val groupField: String? = null,
    val hiddenColumnIds: Set<String> = emptySet(),
    val fullscreen: Boolean = false
)

data class ModulTabloKolon(
    val id: String,
    val label: String,
    val weight: Float
)

data class ModulGrupSecenegi(
    val label: String,
    val field: String
)

fun <T> paginateList(items: List<T>, page: Int, pageSize: Int): List<T> {
    if (pageSize <= 0 || items.isEmpty()) return items
    val start = (page.coerceAtLeast(1) - 1) * pageSize
    if (start >= items.size) return emptyList()
    return items.drop(start).take(pageSize)
}

fun totalPages(count: Int, pageSize: Int): Int =
    if (count == 0 || pageSize <= 0) 1 else (count + pageSize - 1) / pageSize

fun groupedSortedList(
    items: List<Map<String, String>>,
    groupField: String?,
    dateField: String = "tarih"
): List<Map<String, String>> {
    if (groupField.isNullOrBlank()) {
        return items.sortedByDescending { it[dateField].orEmpty() }
    }
    return items.sortedWith(
        compareBy<Map<String, String>> { it[groupField].orEmpty().lowercase() }
            .thenByDescending { it[dateField].orEmpty() }
    )
}

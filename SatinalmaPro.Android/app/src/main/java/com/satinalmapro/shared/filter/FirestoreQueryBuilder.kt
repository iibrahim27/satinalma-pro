package com.satinalmapro.shared.filter

import com.google.firebase.firestore.FirebaseFirestore
import com.google.firebase.firestore.Query

/**
 * [FirestoreFilterSpec] → Firebase Firestore [Query] dönüştürücü.
 */
object FirestoreQueryBuilder {

    fun build(db: FirebaseFirestore, spec: FirestoreFilterSpec): Query {
        var query: Query = db.collection(spec.collection)

        when (spec.statusIn.size) {
            0 -> Unit
            1 -> query = query.whereEqualTo("status", spec.statusIn.first())
            else -> query = query.whereIn("status", spec.statusIn)
        }

        if (!spec.requesterUidEquals.isNullOrBlank()) {
            query = query.whereEqualTo("requesterUid", spec.requesterUidEquals)
        }

        spec.orderBy.forEach { order ->
            query = if (order.descending) {
                query.orderBy(order.field, Query.Direction.DESCENDING)
            } else {
                query.orderBy(order.field, Query.Direction.ASCENDING)
            }
        }

        return query
    }

    fun buildForTab(
        db: FirebaseFirestore,
        tab: ProcurementTab,
        role: String?,
        currentUid: String?
    ): Query? {
        val spec = TabFilterManager.getQuerySpec(tab, role, currentUid) ?: return null
        return build(db, spec)
    }
}

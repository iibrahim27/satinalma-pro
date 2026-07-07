using System.Text.Json;
using SatinalmaPro.Shared.Procurement;

namespace SatinalmaPro.Services.Firebase;

/// <summary>Firestore REST <c>:runQuery</c> gövdesi oluşturur.</summary>
internal static class FirestoreStructuredQueryOlusturucu
{
    public static object Olustur(FirestoreFilterSpec spec, int limit = 500)
    {
        var filtreler = new List<object>();

        if (spec.StatusIn.Count == 1)
            filtreler.Add(StringFiltre("status", "EQUAL", spec.StatusIn[0]));

        else if (spec.StatusIn.Count > 1)
        {
            filtreler.Add(new
            {
                fieldFilter = new
                {
                    field = new { fieldPath = "status" },
                    op = "IN",
                    value = new
                    {
                        arrayValue = new
                        {
                            values = spec.StatusIn
                                .Select(s => new { stringValue = s })
                                .ToArray()
                        }
                    }
                }
            });
        }

        if (!string.IsNullOrWhiteSpace(spec.RequesterUidEquals))
            filtreler.Add(StringFiltre("requesterUid", "EQUAL", spec.RequesterUidEquals));

        if (spec.RequiresReturnFlag)
            filtreler.Add(BoolFiltre("hasReturnFlag", "EQUAL", true));

        object? where = filtreler.Count switch
        {
            0 => null,
            1 => filtreler[0],
            _ => new
            {
                compositeFilter = new
                {
                    op = "AND",
                    filters = filtreler
                }
            }
        };

        var orderBy = spec.OrderBy.Select(o => new
        {
            field = new { fieldPath = o.Field },
            direction = o.Descending ? "DESCENDING" : "ASCENDING"
        }).ToArray();

        var structuredQuery = new Dictionary<string, object?>
        {
            ["from"] = new[] { new { collectionId = spec.Collection } },
            ["limit"] = limit
        };

        if (where is not null)
            structuredQuery["where"] = where;

        if (orderBy.Length > 0)
            structuredQuery["orderBy"] = orderBy;

        return new { structuredQuery };
    }

    public static string JsonOlustur(FirestoreFilterSpec spec, int limit = 500) =>
        JsonSerializer.Serialize(Olustur(spec, limit));

    private static object StringFiltre(string alan, string op, string deger) => new
    {
        fieldFilter = new
        {
            field = new { fieldPath = alan },
            op,
            value = new { stringValue = deger }
        }
    };

    private static object BoolFiltre(string alan, string op, bool deger) => new
    {
        fieldFilter = new
        {
            field = new { fieldPath = alan },
            op,
            value = new { booleanValue = deger }
        }
    };
}

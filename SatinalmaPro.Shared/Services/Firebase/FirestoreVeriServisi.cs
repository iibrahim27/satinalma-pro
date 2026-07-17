using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SatinalmaPro.Shared;
using SatinalmaPro.Shared.Helpers;
using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Shared.Services.Firebase;

public sealed class FirestoreVeriServisi
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(45) };

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly FirebaseAyarlar _ayarlar;
    private readonly FirebaseAuthServisi _auth;

    public FirestoreVeriServisi(FirebaseAyarlar ayarlar, FirebaseAuthServisi auth)
    {
        _ayarlar = ayarlar;
        _auth = auth;
    }

    private string Kok => $"https://firestore.googleapis.com/v1/projects/{_ayarlar.ProjectId}/databases/(default)/documents";

    public async Task<string?> BelgeJsonOkuAsync(string yol, CancellationToken iptal = default)
    {
        var (json, _) = await BelgeOkuAsync(yol, iptal);
        return json;
    }

    public async Task<(string? json, DateTime? guncelleme)> BelgeOkuAsync(string yol, CancellationToken iptal = default)
    {
        var token = await _auth.GecerliTokenAlAsync(iptal);
        using var istek = new HttpRequestMessage(HttpMethod.Get, $"{Kok}/{yol}");
        istek.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var yanit = await Http.SendAsync(istek, iptal);
        if (yanit.StatusCode == System.Net.HttpStatusCode.NotFound)
            return (null, null);

        var govde = await yanit.Content.ReadAsStringAsync(iptal);
        if (!yanit.IsSuccessStatusCode)
            throw new InvalidOperationException(FirestoreHataMesaji(govde));

        using var belge = JsonDocument.Parse(govde);
        var alanlar = belge.RootElement.GetProperty("fields");
        var json = alanlar.TryGetProperty("json", out var j) && j.TryGetProperty("stringValue", out var s)
            ? s.GetString()
            : null;

        DateTime? guncelleme = null;
        if (alanlar.TryGetProperty("updatedAt", out var t) && t.TryGetProperty("stringValue", out var ts) &&
            DateTime.TryParse(ts.GetString(), out var dt))
            guncelleme = dt.ToUniversalTime();

        return (json, guncelleme);
    }

    public async Task BelgeJsonYazAsync(string yol, string json, string? guncelleyenUid, CancellationToken iptal = default)
    {
        var token = await _auth.GecerliTokenAlAsync(iptal);
        var govde = new
        {
            fields = new Dictionary<string, object>
            {
                ["json"] = new { stringValue = json },
                ["updatedAt"] = new { stringValue = DateTime.UtcNow.ToString("o") },
                ["updatedBy"] = new { stringValue = guncelleyenUid ?? "" }
            }
        };

        using var patch = new HttpRequestMessage(HttpMethod.Patch,
            $"{Kok}/{yol}?updateMask.fieldPaths=json&updateMask.fieldPaths=updatedAt&updateMask.fieldPaths=updatedBy")
        {
            Content = new StringContent(JsonSerializer.Serialize(govde), Encoding.UTF8, "application/json")
        };
        patch.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var yanit = await Http.SendAsync(patch, iptal);
        if (yanit.IsSuccessStatusCode)
            return;

        // Nested yol: tenants/{id}/veri/{doc} — son segment documentId, üst yol parent.
        var (parentPath, documentId) = BelgeYoluAyir(yol);
        using var olustur = new HttpRequestMessage(HttpMethod.Post, $"{Kok}/{parentPath}?documentId={Uri.EscapeDataString(documentId)}")
        {
            Content = new StringContent(JsonSerializer.Serialize(govde), Encoding.UTF8, "application/json")
        };
        olustur.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        yanit = await Http.SendAsync(olustur, iptal);
        var metin = await yanit.Content.ReadAsStringAsync(iptal);
        if (!yanit.IsSuccessStatusCode)
            throw new InvalidOperationException(FirestoreHataMesaji(metin));
    }

    /// <summary>
    /// tenants/TID/veri/satinalma_talepler → parent=tenants/TID/veri, id=satinalma_talepler
    /// </summary>
    internal static (string ParentPath, string DocumentId) BelgeYoluAyir(string yol)
    {
        var parcalar = yol.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parcalar.Length < 2)
            throw new ArgumentException($"Geçersiz Firestore yolu: {yol}");
        return (string.Join('/', parcalar.Take(parcalar.Length - 1)), parcalar[^1]);
    }

    public async Task<KullaniciProfili?> KullaniciOkuAsync(string uid, CancellationToken iptal = default)
    {
        var token = await _auth.GecerliTokenAlAsync(iptal);
        using var istek = new HttpRequestMessage(HttpMethod.Get, $"{Kok}/{FirestoreYollari.User(uid)}");
        istek.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var yanit = await Http.SendAsync(istek, iptal);
        if (yanit.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        var govde = await yanit.Content.ReadAsStringAsync(iptal);
        if (!yanit.IsSuccessStatusCode)
            throw new InvalidOperationException(FirestoreHataMesaji(govde));

        using var belge = JsonDocument.Parse(govde);
        return KullaniciyiParseEt(uid, belge.RootElement.GetProperty("fields"));
    }

    public async Task KullaniciFcmTokenGuncelleAsync(string uid, string? fcmToken, CancellationToken iptal = default)
    {
        var token = await _auth.GecerliTokenAlAsync(iptal);
        var govde = new
        {
            fields = new Dictionary<string, object>
            {
                ["fcmToken"] = new { stringValue = fcmToken ?? "" }
            }
        };

        using var patch = new HttpRequestMessage(HttpMethod.Patch,
            $"{Kok}/{FirestoreYollari.User(uid)}?updateMask.fieldPaths=fcmToken")
        {
            Content = new StringContent(JsonSerializer.Serialize(govde), Encoding.UTF8, "application/json")
        };
        patch.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await Http.SendAsync(patch, iptal);
    }

    public async Task<List<KullaniciProfili>> RolKullanicilariOkuAsync(string rol, CancellationToken iptal = default)
    {
        var tum = await TumKullanicilariOkuAsync(iptal);
        rol = KullaniciRolleri.Normalize(rol);
        return tum.Where(k => KullaniciRolleri.Normalize(k.Rol) == rol && k.Aktif).ToList();
    }

    public async Task<List<KullaniciProfili>> TumKullanicilariOkuAsync(CancellationToken iptal = default)
    {
        var token = await _auth.GecerliTokenAlAsync(iptal);
        using var istek = new HttpRequestMessage(HttpMethod.Get, $"{Kok}/{FirestoreYollari.Users()}");
        istek.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var yanit = await Http.SendAsync(istek, iptal);
        var govde = await yanit.Content.ReadAsStringAsync(iptal);
        if (!yanit.IsSuccessStatusCode)
            throw new InvalidOperationException(FirestoreHataMesaji(govde));

        using var belge = JsonDocument.Parse(govde);
        var liste = new List<KullaniciProfili>();
        if (!belge.RootElement.TryGetProperty("documents", out var belgeler))
            return liste;

        foreach (var doc in belgeler.EnumerateArray())
        {
            var ad = doc.GetProperty("name").GetString() ?? "";
            var docUid = ad.Split('/').Last();
            liste.Add(KullaniciyiParseEt(docUid, doc.GetProperty("fields")));
        }

        return liste;
    }

    public async Task<List<NotificationInboxEntry>> InboxOkuAsync(string uid, int limit = 50, CancellationToken iptal = default)
    {
        var token = await _auth.GecerliTokenAlAsync(iptal);
        using var istek = new HttpRequestMessage(HttpMethod.Get,
            $"{Kok}/{FirestoreYollari.UserNotificationInbox(uid)}?pageSize={limit}");
        istek.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var yanit = await Http.SendAsync(istek, iptal);
        if (yanit.StatusCode == System.Net.HttpStatusCode.NotFound)
            return [];

        var govde = await yanit.Content.ReadAsStringAsync(iptal);
        if (!yanit.IsSuccessStatusCode)
            return [];

        using var belge = JsonDocument.Parse(govde);
        var liste = new List<NotificationInboxEntry>();
        if (!belge.RootElement.TryGetProperty("documents", out var belgeler))
            return liste;

        foreach (var doc in belgeler.EnumerateArray())
        {
            var ad = doc.GetProperty("name").GetString() ?? "";
            var docId = ad.Split('/').Last();
            var alanlar = doc.GetProperty("fields");
            liste.Add(InboxParseEt(docId, alanlar));
        }

        return liste.OrderByDescending(e => e.CreatedAt).Take(limit).ToList();
    }

    public async Task InboxOkunduIsaretleAsync(string uid, string inboxDocId, CancellationToken iptal = default)
    {
        var token = await _auth.GecerliTokenAlAsync(iptal);
        var govde = new
        {
            fields = new Dictionary<string, object>
            {
                ["isRead"] = new { booleanValue = true },
                ["readAt"] = new { timestampValue = DateTime.UtcNow.ToString("o") }
            }
        };

        using var patch = new HttpRequestMessage(HttpMethod.Patch,
            $"{Kok}/{FirestoreYollari.UserNotificationInbox(uid)}/{inboxDocId}?updateMask.fieldPaths=isRead&updateMask.fieldPaths=readAt")
        {
            Content = new StringContent(JsonSerializer.Serialize(govde), Encoding.UTF8, "application/json")
        };
        patch.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await Http.SendAsync(patch, iptal);
    }

    public async Task InboxArsivleAsync(string uid, string inboxDocId, CancellationToken iptal = default)
    {
        var token = await _auth.GecerliTokenAlAsync(iptal);
        var now = DateTime.UtcNow.ToString("o");
        var govde = new
        {
            fields = new Dictionary<string, object>
            {
                ["isRead"] = new { booleanValue = true },
                ["isArchived"] = new { booleanValue = true },
                ["dismissedAt"] = new { timestampValue = now },
                ["archivedAt"] = new { timestampValue = now }
            }
        };

        using var patch = new HttpRequestMessage(HttpMethod.Patch,
            $"{Kok}/{FirestoreYollari.UserNotificationInbox(uid)}/{inboxDocId}" +
            "?updateMask.fieldPaths=isRead&updateMask.fieldPaths=isArchived" +
            "&updateMask.fieldPaths=dismissedAt&updateMask.fieldPaths=archivedAt")
        {
            Content = new StringContent(JsonSerializer.Serialize(govde), Encoding.UTF8, "application/json")
        };
        patch.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await Http.SendAsync(patch, iptal);
    }

    public async Task InboxEkleBearerIleAsync(
        string bearerToken,
        string uid,
        string docId,
        BildirimKaydi bildirim,
        CancellationToken iptal = default)
    {
        if (string.IsNullOrWhiteSpace(uid) || string.IsNullOrWhiteSpace(docId))
            return;

        var now = DateTime.UtcNow.ToString("o");
        var talepId = bildirim.TalepId?.ToString() ?? "";
        var govde = new
        {
            fields = new Dictionary<string, object>
            {
                ["title"] = new { stringValue = bildirim.Baslik },
                ["baslik"] = new { stringValue = bildirim.Baslik },
                ["message"] = new { stringValue = bildirim.Mesaj },
                ["mesaj"] = new { stringValue = bildirim.Mesaj },
                ["tip"] = new { stringValue = bildirim.Tip },
                ["type"] = new { stringValue = bildirim.Tip },
                ["talepId"] = new { stringValue = talepId },
                ["entityId"] = new { stringValue = talepId },
                ["entityType"] = new { stringValue = "talep" },
                ["eventCode"] = new { stringValue = bildirim.EventCode ?? bildirim.Tip },
                ["hedefRol"] = new { stringValue = bildirim.HedefRol ?? "" },
                ["targetRole"] = new { stringValue = bildirim.HedefRol ?? "" },
                ["hedefUid"] = new { stringValue = bildirim.HedefUid ?? "" },
                ["targetUid"] = new { stringValue = bildirim.HedefUid ?? "" },
                ["olusturanUid"] = new { stringValue = bildirim.OlusturanUid },
                ["createdBy"] = new { stringValue = bildirim.OlusturanUid },
                ["isRead"] = new { booleanValue = false },
                ["okundu"] = new { booleanValue = false },
                ["createdAt"] = new { timestampValue = now },
                ["guncellemeUtc"] = new { integerValue = bildirim.GuncellemeUtc.ToString() }
            }
        };

        using var istek = new HttpRequestMessage(HttpMethod.Post,
            $"{Kok}/{FirestoreYollari.UserNotificationInbox(uid)}?documentId={Uri.EscapeDataString(docId)}")
        {
            Content = new StringContent(JsonSerializer.Serialize(govde), Encoding.UTF8, "application/json")
        };
        istek.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        var yanit = await Http.SendAsync(istek, iptal);
        if (!yanit.IsSuccessStatusCode && yanit.StatusCode != System.Net.HttpStatusCode.Conflict)
            throw new InvalidOperationException(await yanit.Content.ReadAsStringAsync(iptal));
    }

    private static NotificationInboxEntry InboxParseEt(string docId, JsonElement alanlar) => new()
    {
        DocId = docId,
        EventCode = AlanOku(alanlar, "eventCode") ?? "",
        Category = AlanOku(alanlar, "category") ?? "",
        Type = AlanOku(alanlar, "type") ?? "",
        Tip = AlanOku(alanlar, "tip") ?? "",
        Priority = AlanOku(alanlar, "priority") ?? "",
        Title = AlanOku(alanlar, "title") ?? AlanOku(alanlar, "baslik") ?? "",
        Baslik = AlanOku(alanlar, "baslik") ?? "",
        Message = AlanOku(alanlar, "message") ?? AlanOku(alanlar, "mesaj") ?? "",
        Mesaj = AlanOku(alanlar, "mesaj") ?? "",
        EntityType = AlanOku(alanlar, "entityType") ?? "",
        EntityId = AlanOku(alanlar, "entityId") ?? AlanOku(alanlar, "talepId") ?? "",
        TalepId = AlanOku(alanlar, "talepId") ?? "",
        DeepLink = AlanOku(alanlar, "deepLink") ?? "",
        DesktopRoute = AlanOku(alanlar, "desktopRoute") ?? "",
        Module = AlanOku(alanlar, "module") ?? "",
        Screen = AlanOku(alanlar, "screen") ?? "",
        Action = AlanOku(alanlar, "action") ?? "",
        HedefRol = AlanOku(alanlar, "hedefRol") ?? "",
        TargetRole = AlanOku(alanlar, "targetRole") ?? "",
        HedefUid = AlanOku(alanlar, "hedefUid") ?? "",
        TargetUid = AlanOku(alanlar, "targetUid") ?? "",
        OlusturanUid = AlanOku(alanlar, "olusturanUid") ?? "",
        CreatedBy = AlanOku(alanlar, "createdBy") ?? "",
        IsRead = alanlar.TryGetProperty("isRead", out var r) && r.TryGetProperty("booleanValue", out var rb) && rb.GetBoolean(),
        IsArchived = alanlar.TryGetProperty("isArchived", out var a) && a.TryGetProperty("booleanValue", out var ab) && ab.GetBoolean(),
        DismissedAt = TimestampOku(alanlar, "dismissedAt"),
        CreatedAt = TimestampOku(alanlar, "createdAt")
    };

    private static DateTime? TimestampOku(JsonElement alanlar, string ad)
    {
        if (!alanlar.TryGetProperty(ad, out var alan))
            return null;

        if (alan.TryGetProperty("timestampValue", out var ts) &&
            DateTime.TryParse(ts.GetString(), out var dt))
            return dt.ToUniversalTime();

        if (alan.TryGetProperty("stringValue", out var s) &&
            DateTime.TryParse(s.GetString(), out var ds))
            return ds.ToUniversalTime();

        return null;
    }

    private static KullaniciProfili KullaniciyiParseEt(string uid, JsonElement alanlar) => new()
    {
        Uid = uid,
        Eposta = AlanOku(alanlar, "eposta") ?? "",
        AdSoyad = AlanOku(alanlar, "adSoyad") ?? "",
        Rol = KullaniciRolleri.Normalize(AlanOku(alanlar, "rol")),
        Aktif = !alanlar.TryGetProperty("aktif", out var a) || a.TryGetProperty("booleanValue", out var b) && b.GetBoolean(),
        Saha = AlanOku(alanlar, "saha"),
        FcmToken = AlanOku(alanlar, "fcmToken")
    };

    private static string? AlanOku(JsonElement alanlar, string ad) =>
        alanlar.TryGetProperty(ad, out var alan) && alan.TryGetProperty("stringValue", out var s)
            ? s.GetString()
            : null;

    private static string FirestoreHataMesaji(string json)
    {
        try
        {
            using var belge = JsonDocument.Parse(json);
            if (belge.RootElement.TryGetProperty("error", out var hata) &&
                hata.TryGetProperty("message", out var mesaj))
                return AgHataMesaji.Turkcele(mesaj.GetString());
        }
        catch { /* yoksay */ }

        return "Firestore bağlantı hatası.";
    }
}

using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SatinalmaPro.Models;
using SatinalmaPro.Shared;
using SatinalmaPro.Shared.Helpers;
using SatinalmaPro.Shared.SaaS;
using SatinalmaPro.Shared.Procurement;
using SatinalmaPro.Shared.Procurement.Detail;
using NotificationInboxEntry = SatinalmaPro.Shared.Models.NotificationInboxEntry;

namespace SatinalmaPro.Services.Firebase;

public sealed class FirestoreVeriServisi
{
    private static readonly HttpClient Http = OlusturHttp();

    private static readonly JsonSerializerOptions YetkiJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static HttpClient OlusturHttp()
    {
        var istemci = new HttpClient { Timeout = TimeSpan.FromSeconds(45) };
        return istemci;
    }
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
        var token = await _auth.GecerliTokenAlAsync(iptal);
        using var istek = new HttpRequestMessage(HttpMethod.Get, $"{Kok}/{yol}");
        istek.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var yanit = await Http.SendAsync(istek, iptal);
        if (yanit.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        var govde = await yanit.Content.ReadAsStringAsync(iptal);
        if (!yanit.IsSuccessStatusCode)
            throw new InvalidOperationException(FirestoreHataMesaji(govde));

        using var belge = JsonDocument.Parse(govde);
        if (!belge.RootElement.TryGetProperty("fields", out var alanlar) ||
            !alanlar.TryGetProperty("json", out var jsonAlan) ||
            !jsonAlan.TryGetProperty("stringValue", out var deger))
            return null;

        return deger.GetString();
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

        using var istek = new HttpRequestMessage(HttpMethod.Patch, $"{Kok}/{yol}?updateMask.fieldPaths=json&updateMask.fieldPaths=updatedAt&updateMask.fieldPaths=updatedBy")
        {
            Content = new StringContent(JsonSerializer.Serialize(govde), Encoding.UTF8, "application/json")
        };
        istek.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var yanit = await Http.SendAsync(istek, iptal);
        if (yanit.IsSuccessStatusCode)
            return;

        // Nested yol: tenants/{id}/veri/{doc} — yalnızca son segment documentId, üst yol parent.
        var (parentPath, documentId) = FirestoreBelgeYoluAyir(yol);
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
    /// (Eski Split[0]/Split[1] hatası tenant belgesinin üzerine yazıyordu.)
    /// </summary>
    internal static (string ParentPath, string DocumentId) FirestoreBelgeYoluAyir(string yol)
    {
        var parcalar = yol.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parcalar.Length < 2)
            throw new ArgumentException($"Geçersiz Firestore yolu: {yol}");
        var documentId = parcalar[^1];
        var parentPath = string.Join('/', parcalar.Take(parcalar.Length - 1));
        return (parentPath, documentId);
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
        var alanlar = belge.RootElement.GetProperty("fields");
        return KullaniciyiParseEt(uid, alanlar);
    }

    public async Task KullaniciKaydetAsync(KullaniciProfili profil, CancellationToken iptal = default)
    {
        if (string.IsNullOrWhiteSpace(profil.Uid))
            throw new InvalidOperationException("Kullanıcı kimliği (Uid) boş.");

        var token = await _auth.GecerliTokenAlAsync(iptal);
        var modulDegerleri = profil.Moduller
            .Select(m => (object)new { stringValue = m })
            .ToArray();
        var yetkiJson = JsonSerializer.Serialize(profil.ModulYetkileri, YetkiJson);

        var fields = new Dictionary<string, object>
        {
            ["tenantId"] = new { stringValue = profil.TenantId ?? KiracıOturumu.TenantId ?? "" },
            ["kullaniciAdi"] = new { stringValue = profil.KullaniciAdi ?? "" },
            ["eposta"] = new { stringValue = profil.Eposta ?? "" },
            ["adSoyad"] = new { stringValue = profil.AdSoyad ?? "" },
            ["rol"] = new { stringValue = KullaniciRolleri.Normalize(profil.Rol) },
            ["aktif"] = new { booleanValue = profil.Aktif },
            ["saha"] = new { stringValue = profil.Saha ?? "" },
            ["moduller"] = DiziAlaniOlustur(modulDegerleri),
            ["modulYetkileriJson"] = new { stringValue = yetkiJson }
        };

        var govde = new { fields };
        var jsonGovde = JsonSerializer.Serialize(govde);

        var patchUrl =
            $"{Kok}/{FirestoreYollari.User(profil.Uid)}?updateMask.fieldPaths=eposta&updateMask.fieldPaths=kullaniciAdi&updateMask.fieldPaths=tenantId&updateMask.fieldPaths=adSoyad&updateMask.fieldPaths=rol&updateMask.fieldPaths=aktif&updateMask.fieldPaths=saha&updateMask.fieldPaths=moduller&updateMask.fieldPaths=modulYetkileriJson";

        using var patch = new HttpRequestMessage(HttpMethod.Patch, patchUrl)
        {
            Content = new StringContent(jsonGovde, Encoding.UTF8, "application/json")
        };
        patch.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var yanit = await Http.SendAsync(patch, iptal);
        if (yanit.IsSuccessStatusCode)
            return;

        var hataMetni = await yanit.Content.ReadAsStringAsync(iptal);

        if (yanit.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            using var olustur = new HttpRequestMessage(HttpMethod.Post, $"{Kok}/{FirestoreYollari.Users()}?documentId={profil.Uid}")
            {
                Content = new StringContent(jsonGovde, Encoding.UTF8, "application/json")
            };
            olustur.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            yanit = await Http.SendAsync(olustur, iptal);
            hataMetni = await yanit.Content.ReadAsStringAsync(iptal);
            if (yanit.IsSuccessStatusCode)
                return;
        }

        throw new InvalidOperationException(
            $"Kullanıcı kaydedilemedi ({(int)yanit.StatusCode}): {FirestoreHataMesaji(hataMetni)}");
    }

    private static object DiziAlaniOlustur(object[] degerler) =>
        degerler.Length > 0
            ? new { arrayValue = new { values = degerler } }
            : new { arrayValue = new { } };

    public async Task<List<KullaniciProfili>> TumKullanicilariOkuAsync(CancellationToken iptal = default)
    {
        var token = await _auth.GecerliTokenAlAsync(iptal);
        return await TumKullanicilariBearerIleOkuAsync(token, iptal);
    }

    /// <summary>Service account token ile tüm kullanıcıları okur (FCM fan-out).</summary>
    public async Task<List<KullaniciProfili>> TumKullanicilariBearerIleOkuAsync(string bearerToken, CancellationToken iptal = default)
    {
        using var istek = new HttpRequestMessage(HttpMethod.Get, $"{Kok}/{FirestoreYollari.Users()}");
        istek.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        var yanit = await Http.SendAsync(istek, iptal);
        var govde = await yanit.Content.ReadAsStringAsync(iptal);
        if (!yanit.IsSuccessStatusCode)
            return [];

        using var belge = JsonDocument.Parse(govde);
        var liste = new List<KullaniciProfili>();
        if (!belge.RootElement.TryGetProperty("documents", out var belgeler))
            return liste;

        foreach (var doc in belgeler.EnumerateArray())
        {
            var ad = doc.GetProperty("name").GetString() ?? "";
            var uid = ad.Split('/').Last();
            var alanlar = doc.GetProperty("fields");
            liste.Add(KullaniciyiParseEt(uid, alanlar));
        }

        return liste.OrderBy(k => k.AdSoyad).ToList();
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
            liste.Add(InboxParseEt(docId, doc.GetProperty("fields")));
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

    public async Task InboxTumunuOkunduIsaretleAsync(string uid, CancellationToken iptal = default)
    {
        var inbox = await InboxOkuAsync(uid, 200, iptal);
        foreach (var kayit in inbox.Where(k => !k.IsRead))
        {
            try
            {
                await InboxOkunduIsaretleAsync(uid, kayit.DocId, iptal);
            }
            catch
            {
                // tek kayıt hatası diğerlerini engellemesin
            }
        }
    }

    public async Task InboxEkleAsync(string uid, string docId, BildirimKaydi bildirim, CancellationToken iptal = default)
    {
        if (string.IsNullOrWhiteSpace(uid) || string.IsNullOrWhiteSpace(docId))
            return;

        var token = await _auth.GecerliTokenAlAsync(iptal);
        await InboxEkleBearerIleAsync(token, uid, docId, bildirim, iptal);
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
                ["olusturanUid"] = new { stringValue = bildirim.OlusturanUid ?? "" },
                ["createdBy"] = new { stringValue = bildirim.OlusturanUid ?? "" },
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
        await Http.SendAsync(istek, iptal);
    }

    public async Task InboxSilAsync(string uid, string inboxDocId, CancellationToken iptal = default)
    {
        var token = await _auth.GecerliTokenAlAsync(iptal);
        using var istek = new HttpRequestMessage(HttpMethod.Delete, $"{Kok}/{FirestoreYollari.UserNotificationInbox(uid)}/{inboxDocId}");
        istek.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await Http.SendAsync(istek, iptal);
    }

    public async Task InboxTemizleAsync(string uid, CancellationToken iptal = default)
    {
        for (var guard = 0; guard < 20; guard++)
        {
            var inbox = await InboxOkuAsync(uid, 200, iptal);
            var arsivlenecek = inbox.Where(k => !k.IsDismissed).ToList();
            if (arsivlenecek.Count == 0)
                break;

            foreach (var kayit in arsivlenecek)
            {
                try
                {
                    await InboxArsivleAsync(uid, kayit.DocId, iptal);
                }
                catch
                {
                    // tek kayıt hatası diğerlerini engellemesin
                }
            }

            if (inbox.Count < 200)
                break;
        }
    }

    private static NotificationInboxEntry InboxParseEt(string docId, JsonElement alanlar) => new()
    {
        DocId = docId,
        EventCode = AlanOku(alanlar, "eventCode") ?? "",
        Category = AlanOku(alanlar, "category") ?? "",
        Type = AlanOku(alanlar, "type") ?? "",
        Priority = AlanOku(alanlar, "priority") ?? "",
        Title = AlanOku(alanlar, "title") ?? "",
        Message = AlanOku(alanlar, "message") ?? "",
        EntityType = AlanOku(alanlar, "entityType") ?? "",
        EntityId = AlanOku(alanlar, "entityId") ?? "",
        DeepLink = AlanOku(alanlar, "deepLink") ?? "",
        DesktopRoute = AlanOku(alanlar, "desktopRoute") ?? "",
        Module = AlanOku(alanlar, "module") ?? "",
        Screen = AlanOku(alanlar, "screen") ?? "",
        Action = AlanOku(alanlar, "action") ?? "",
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

    private static KullaniciProfili KullaniciyiParseEt(string uid, JsonElement alanlar)
    {
        var profil = new KullaniciProfili
        {
            Uid = uid,
            TenantId = AlanOku(alanlar, "tenantId") ?? KiracıOturumu.TenantId ?? "",
            KullaniciAdi = AlanOku(alanlar, "kullaniciAdi") ?? "",
            Eposta = AlanOku(alanlar, "eposta") ?? "",
            AdSoyad = AlanOku(alanlar, "adSoyad") ?? "",
            Rol = KullaniciRolleri.Normalize(AlanOku(alanlar, "rol")),
            Aktif = !alanlar.TryGetProperty("aktif", out var a) || a.TryGetProperty("booleanValue", out var b) && b.GetBoolean(),
            Saha = AlanOku(alanlar, "saha"),
            Moduller = DiziOku(alanlar, "moduller")
        };

        profil.FcmToken = AlanOku(alanlar, "fcmToken");

        var yetkiJson = AlanOku(alanlar, "modulYetkileriJson");
        if (!string.IsNullOrWhiteSpace(yetkiJson))
        {
            try
            {
                profil.ModulYetkileri = JsonSerializer.Deserialize<List<ModulYetkiKaydi>>(yetkiJson, YetkiJson) ?? [];
            }
            catch
            {
                profil.ModulYetkileri = [];
            }
        }

        return profil;
    }

    private static string? AlanOku(JsonElement alanlar, string ad)
    {
        if (!alanlar.TryGetProperty(ad, out var alan))
            return null;
        if (alan.TryGetProperty("stringValue", out var s))
            return s.GetString();
        return null;
    }

    private static List<string> DiziOku(JsonElement alanlar, string ad)
    {
        var liste = new List<string>();
        if (!alanlar.TryGetProperty(ad, out var alan) ||
            !alan.TryGetProperty("arrayValue", out var dizi) ||
            !dizi.TryGetProperty("values", out var degerler))
            return liste;

        foreach (var deger in degerler.EnumerateArray())
        {
            if (deger.TryGetProperty("stringValue", out var metin) &&
                !string.IsNullOrWhiteSpace(metin.GetString()))
                liste.Add(metin.GetString()!);
        }

        return liste;
    }

    private static string FirestoreHataMesaji(string json)
    {
        try
        {
            using var belge = JsonDocument.Parse(json);
            if (belge.RootElement.TryGetProperty("error", out var hata) &&
                hata.TryGetProperty("message", out var mesaj))
                return AgHataMesaji.Turkcele(mesaj.GetString());
        }
        catch
        {
            // yoksay
        }

        return "Firestore bağlantı hatası.";
    }

    /// <summary>
    /// Enterprise <c>procurement_requests</c> koleksiyonunda structured query çalıştırır.
    /// Belge kimliklerini (GUID string) döner.
    /// </summary>
    public async Task<List<string>> ProcurementRequestIdleriSorgulaAsync(
        FirestoreFilterSpec spec,
        CancellationToken iptal = default)
    {
        if (!string.Equals(spec.Collection, "procurement_requests", StringComparison.Ordinal))
            return [];

        var token = await _auth.GecerliTokenAlAsync(iptal);
        var tenantId = KiracıOturumu.ZorunluTenantId();
        var structuredQuery = FirestoreStructuredQueryOlusturucu.StructuredQuery(spec);
        var govde = new
        {
            parent = $"{Kok}/{FirestoreYollari.TenantKok(tenantId)}",
            structuredQuery
        };

        using var istek = new HttpRequestMessage(
            HttpMethod.Post,
            $"{Kok}:runQuery")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(govde),
                Encoding.UTF8,
                "application/json")
        };
        istek.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var yanit = await Http.SendAsync(istek, iptal);
        var metin = await yanit.Content.ReadAsStringAsync(iptal);
        if (!yanit.IsSuccessStatusCode)
            throw new InvalidOperationException(FirestoreHataMesaji(metin));

        using var belge = JsonDocument.Parse(metin);
        var idler = new List<string>();

        foreach (var satir in belge.RootElement.EnumerateArray())
        {
            if (!satir.TryGetProperty("document", out var doc))
                continue;

            var ad = doc.GetProperty("name").GetString() ?? "";
            var docId = ad.Split('/').LastOrDefault();
            if (!string.IsNullOrWhiteSpace(docId))
                idler.Add(docId);
        }

        return idler;
    }

    /// <summary>
    /// Enterprise <c>procurement_requests/{id}</c> belgesinde workflow alanlarını günceller.
    /// </summary>
    public async Task ProcurementRequestAlanlariGuncelleAsync(
        string requestId,
        PurchaseRequestFirestorePatch patch,
        CancellationToken iptal = default)
    {
        if (string.IsNullOrWhiteSpace(requestId))
            return;

        var token = await _auth.GecerliTokenAlAsync(iptal);
        var fields = new Dictionary<string, object>();

        if (!string.IsNullOrWhiteSpace(patch.Status))
            fields["status"] = new { stringValue = patch.Status };

        if (!string.IsNullOrWhiteSpace(patch.Priority))
            fields["priority"] = new { stringValue = patch.Priority };

        if (patch.ApprovedQuoteId is not null)
            fields["approvedQuoteId"] = new { stringValue = patch.ApprovedQuoteId };

        if (patch.QuoteRevisionNote is not null)
            fields["quoteRevisionNote"] = new { stringValue = patch.QuoteRevisionNote };

        if (patch.RejectionReason is not null)
            fields["rejectionReason"] = new { stringValue = patch.RejectionReason };

        fields["updatedAtUtc"] = new { integerValue = patch.UpdatedAtUtcMs.ToString() };
        fields["updatedAt"] = new { timestampValue = DateTime.UtcNow.ToString("o") };

        var mask = string.Join("&", fields.Keys.Select(k => $"updateMask.fieldPaths={k}"));
        var govde = new { fields };

        using var istek = new HttpRequestMessage(
            HttpMethod.Patch,
            $"{Kok}/{FirestoreYollari.ProcurementRequests()}/{requestId}?{mask}")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(govde),
                Encoding.UTF8,
                "application/json")
        };
        istek.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var yanit = await Http.SendAsync(istek, iptal);
        if (yanit.IsSuccessStatusCode)
            return;

        var metin = await yanit.Content.ReadAsStringAsync(iptal);
        if (yanit.StatusCode == System.Net.HttpStatusCode.NotFound)
            return;

        throw new InvalidOperationException(FirestoreHataMesaji(metin));
    }
}

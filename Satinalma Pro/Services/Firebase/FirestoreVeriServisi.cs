using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SatinalmaPro.Models;
using SatinalmaPro.Shared.Helpers;

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

        // İlk kayıt
        using var olustur = new HttpRequestMessage(HttpMethod.Post, $"{Kok}/{yol.Split('/')[0]}?documentId={yol.Split('/')[1]}")
        {
            Content = new StringContent(JsonSerializer.Serialize(govde), Encoding.UTF8, "application/json")
        };
        olustur.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        yanit = await Http.SendAsync(olustur, iptal);
        var metin = await yanit.Content.ReadAsStringAsync(iptal);
        if (!yanit.IsSuccessStatusCode)
            throw new InvalidOperationException(FirestoreHataMesaji(metin));
    }

    public async Task<KullaniciProfili?> KullaniciOkuAsync(string uid, CancellationToken iptal = default)
    {
        var token = await _auth.GecerliTokenAlAsync(iptal);
        using var istek = new HttpRequestMessage(HttpMethod.Get, $"{Kok}/users/{uid}");
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
            $"{Kok}/users/{profil.Uid}?updateMask.fieldPaths=eposta&updateMask.fieldPaths=adSoyad&updateMask.fieldPaths=rol&updateMask.fieldPaths=aktif&updateMask.fieldPaths=saha&updateMask.fieldPaths=moduller&updateMask.fieldPaths=modulYetkileriJson";

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
            using var olustur = new HttpRequestMessage(HttpMethod.Post, $"{Kok}/users?documentId={profil.Uid}")
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
        using var istek = new HttpRequestMessage(HttpMethod.Get, $"{Kok}/users");
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
            var uid = ad.Split('/').Last();
            var alanlar = doc.GetProperty("fields");
            liste.Add(KullaniciyiParseEt(uid, alanlar));
        }

        return liste.OrderBy(k => k.AdSoyad).ToList();
    }

    private static KullaniciProfili KullaniciyiParseEt(string uid, JsonElement alanlar)
    {
        var profil = new KullaniciProfili
        {
            Uid = uid,
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
}

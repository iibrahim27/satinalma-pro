using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using SatinalmaPro.Models;
using SatinalmaPro.Shared.SaaS;

namespace SatinalmaPro.Services;

/// <summary>
/// Sunucu tarafı (Cloud Function) kiracı operasyonel veri sıfırlaması.
/// İstemci birleştirmesi / offline cache yarışlarından bağımsız otorite.
/// </summary>
public static class KiraciVeriSifirlamaServisi
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromMinutes(9) };

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public sealed record Sonuc(
        long VeriSifirlamaUtc,
        int UsersProcessed,
        int InboxesCleared);

    public static async Task<Sonuc> SifirlaAsync(CancellationToken iptal = default)
    {
        if (!OturumYoneticisi.GirisYapildi || OturumYoneticisi.Auth is null)
            throw new InvalidOperationException("Buluta sıfırlama için oturum gerekli.");

        if (!KullaniciYetkileri.Duzenleyebilir)
            throw new InvalidOperationException("Sistemi sıfırlamak için düzenleme yetkisi gerekli.");

        FirebaseAyarDeposu.Yukle();
        var projectId = FirebaseAyarDeposu.Ayarlar.ProjectId?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(projectId))
            throw new InvalidOperationException("Firebase proje kimliği yapılandırılmamış.");

        var token = await OturumYoneticisi.Auth.GecerliTokenAlAsync(iptal);
        var tenantId = KiracıOturumu.ZorunluTenantId();
        var url =
            $"https://europe-west1-{projectId}.cloudfunctions.net/resetTenantOperationalData";

        using var istek = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(new { data = new { tenantId } }, options: Json)
        };
        istek.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var yanit = await Http.SendAsync(istek, iptal);
        var govde = await yanit.Content.ReadAsStringAsync(iptal);
        if (!yanit.IsSuccessStatusCode)
            throw new InvalidOperationException(HataMesaji(yanit.StatusCode, govde));

        using var belge = JsonDocument.Parse(govde);
        if (!belge.RootElement.TryGetProperty("result", out var result))
            throw new InvalidOperationException("Sıfırlama yanıtı geçersiz.");

        var utc = result.TryGetProperty("veriSifirlamaUtc", out var u) ? u.GetInt64() : 0L;
        if (utc <= 0)
            throw new InvalidOperationException("Sıfırlama damgası alınamadı.");

        var users = result.TryGetProperty("usersProcessed", out var up) ? up.GetInt32() : 0;
        var inboxes = result.TryGetProperty("inboxesCleared", out var ic) ? ic.GetInt32() : 0;
        return new Sonuc(utc, users, inboxes);
    }

    private static string HataMesaji(System.Net.HttpStatusCode kod, string govde)
    {
        if ((int)kod == 404 || govde.Contains("Page not found", StringComparison.OrdinalIgnoreCase))
            return "Sıfırlama sunucu fonksiyonu bulunamadı. Firebase Functions deploy gerekli (resetTenantOperationalData).";

        try
        {
            using var belge = JsonDocument.Parse(govde);
            if (belge.RootElement.TryGetProperty("error", out var err) &&
                err.TryGetProperty("message", out var mesaj))
                return mesaj.GetString() ?? govde;
        }
        catch
        {
            // ham gövde
        }

        return string.IsNullOrWhiteSpace(govde) ? $"Sıfırlama başarısız ({(int)kod})" : govde;
    }
}

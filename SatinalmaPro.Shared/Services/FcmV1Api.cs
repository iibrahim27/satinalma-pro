using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace SatinalmaPro.Shared.Services;

/// <summary>
/// FCM HTTP v1 — Service Account JSON ile push gönderimi (Legacy Server key yerine).
/// </summary>
public static class FcmV1Api
{
    /// <summary>Android bildirim kanalı — SatinalmaFirebaseMessagingService ile aynı.</summary>
    public const string AndroidKanalId = "satinalma_pro";

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(25) };
    private static string? _onbellekToken;
    private static DateTime _tokenBitis = DateTime.MinValue;
    private static readonly SemaphoreSlim Kilit = new(1, 1);

    public static bool ServiceAccountMevcut(string? jsonYolu) =>
        !string.IsNullOrWhiteSpace(jsonYolu) && File.Exists(jsonYolu);

    public static async Task TokenaGonderAsync(
        string serviceAccountJsonYolu,
        string projectId,
        string cihazToken,
        string baslik,
        string mesaj,
        IReadOnlyDictionary<string, string>? veri = null,
        CancellationToken iptal = default)
    {
        var erisim = await ErisimTokeniAlAsync(serviceAccountJsonYolu, iptal);
        var data = new Dictionary<string, string>(veri ?? new Dictionary<string, string>())
        {
            ["title"] = baslik,
            ["body"] = mesaj
        };

        // Yalnızca data + HIGH priority: arka planda OnMessageReceived her zaman tetiklenir (notification bloğu geciktirir).
        var govde = new
        {
            message = new
            {
                token = cihazToken,
                data,
                android = new
                {
                    priority = "HIGH",
                    ttl = "3600s",
                    direct_boot_ok = true
                }
            }
        };

        var url = $"https://fcm.googleapis.com/v1/projects/{projectId}/messages:send";
        using var istek = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(govde)
        };
        istek.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", erisim);

        var yanit = await Http.SendAsync(istek, iptal);
        yanit.EnsureSuccessStatusCode();
    }

    private static async Task<string> ErisimTokeniAlAsync(string jsonYolu, CancellationToken iptal)
    {
        if (_onbellekToken is not null && DateTime.UtcNow < _tokenBitis.AddMinutes(-2))
            return _onbellekToken;

        await Kilit.WaitAsync(iptal);
        try
        {
            if (_onbellekToken is not null && DateTime.UtcNow < _tokenBitis.AddMinutes(-2))
                return _onbellekToken;

            using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(jsonYolu, iptal));
            var kok = doc.RootElement;
            var email = kok.GetProperty("client_email").GetString()
                        ?? throw new InvalidOperationException("Service account: client_email yok.");
            var privateKey = kok.GetProperty("private_key").GetString()
                             ?? throw new InvalidOperationException("Service account: private_key yok.");

            var jwt = JwtOlustur(email, privateKey);
            var tokenYanit = await Http.PostAsync(
                "https://oauth2.googleapis.com/token",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer",
                    ["assertion"] = jwt
                }),
                iptal);

            tokenYanit.EnsureSuccessStatusCode();
            using var tokenDoc = JsonDocument.Parse(await tokenYanit.Content.ReadAsStringAsync(iptal));
            _onbellekToken = tokenDoc.RootElement.GetProperty("access_token").GetString()
                             ?? throw new InvalidOperationException("OAuth token alınamadı.");
            var sure = tokenDoc.RootElement.TryGetProperty("expires_in", out var exp) ? exp.GetInt32() : 3600;
            _tokenBitis = DateTime.UtcNow.AddSeconds(sure);
            return _onbellekToken;
        }
        finally
        {
            Kilit.Release();
        }
    }

    private static string JwtOlustur(string clientEmail, string privateKeyPem)
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(privateKeyPem);

        var simdi = DateTime.UtcNow;
        var credentials = new SigningCredentials(
            new RsaSecurityKey(rsa) { KeyId = "fcm-v1" },
            SecurityAlgorithms.RsaSha256);

        var token = new JwtSecurityToken(
            issuer: clientEmail,
            audience: "https://oauth2.googleapis.com/token",
            claims: null,
            notBefore: simdi,
            expires: simdi.AddMinutes(55),
            signingCredentials: credentials);

        token.Payload["scope"] = "https://www.googleapis.com/auth/firebase.messaging";

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

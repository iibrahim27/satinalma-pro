using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using SatinalmaPro.Shared.SaaS;

namespace SatinalmaYonetici.Services;

public sealed class PlatformOturum
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(45) };
    private string? _refreshToken;
    private string? _idToken;
    private long _tokenExpiryMs;
    private string? _eposta;

    public PlatformYonetimServisi Platform { get; private set; } = null!;
    public bool Yapilandirildi { get; private set; }
    public string ProjectId { get; private set; } = "";
    public string ApiKey { get; private set; } = "";
    public string? KayitliEposta => PlatformOturumDeposu.Oku()?.Eposta;

    public PlatformOturum()
    {
        var yol = Path.Combine(AppContext.BaseDirectory, "firebase_ayarlar.json");
        if (!File.Exists(yol))
            return;

        using var belge = JsonDocument.Parse(File.ReadAllText(yol));
        ProjectId = belge.RootElement.GetProperty("projectId").GetString() ?? "";
        ApiKey = belge.RootElement.GetProperty("apiKey").GetString() ?? "";
        if (string.IsNullOrWhiteSpace(ProjectId) || string.IsNullOrWhiteSpace(ApiKey))
            return;

        Platform = new PlatformYonetimServisi(ProjectId);
        Platform.TokenSaglayiciAyarla(GecerliTokenAlAsync);
        Yapilandirildi = true;
    }

    public async Task<bool> OtomatikGirisDeneAsync()
    {
        if (!Yapilandirildi)
            return false;

        var paket = PlatformOturumDeposu.Oku();
        if (paket is null)
            return false;

        try
        {
            _refreshToken = paket.RefreshToken;
            _eposta = paket.Eposta;
            _idToken = null;
            _tokenExpiryMs = 0;
            await GecerliTokenAlAsync();
            return true;
        }
        catch
        {
            PlatformOturumDeposu.Sil();
            CikisYap(oturumuSil: false);
            return false;
        }
    }

    public async Task GirisYapAsync(string eposta, string sifre, bool beniHatirla = false)
    {
        if (!Yapilandirildi)
            throw new InvalidOperationException("firebase_ayarlar.json yapılandırılmamış.");

        var yanit = await Http.PostAsJsonAsync(
            $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={ApiKey}",
            new { email = eposta.Trim(), password = sifre, returnSecureToken = true });

        var json = await yanit.Content.ReadAsStringAsync();
        if (!yanit.IsSuccessStatusCode)
            throw new InvalidOperationException("E-posta veya şifre hatalı.");

        using var belge = JsonDocument.Parse(json);
        _idToken = belge.RootElement.GetProperty("idToken").GetString();
        _refreshToken = belge.RootElement.GetProperty("refreshToken").GetString();
        _eposta = eposta.Trim();
        var expiresIn = belge.RootElement.TryGetProperty("expiresIn", out var ex)
            ? int.TryParse(ex.GetString(), out var sec) ? sec : 3600
            : 3600;
        _tokenExpiryMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + (expiresIn - 60) * 1000L;
        Platform.OturumTokenAyarla(_idToken!);

        if (beniHatirla && !string.IsNullOrWhiteSpace(_refreshToken))
            PlatformOturumDeposu.Kaydet(_refreshToken!, _eposta!, true);
        else
            PlatformOturumDeposu.Sil();
    }

    public async Task<string> GecerliTokenAlAsync(CancellationToken iptal = default)
    {
        if (!string.IsNullOrWhiteSpace(_idToken) &&
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() < _tokenExpiryMs)
            return _idToken;

        if (string.IsNullOrWhiteSpace(_refreshToken))
            throw new InvalidOperationException("Oturum süresi doldu. Yeniden giriş yapın.");

        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = _refreshToken
        };

        using var istek = new HttpRequestMessage(HttpMethod.Post,
            $"https://securetoken.googleapis.com/v1/token?key={ApiKey}")
        {
            Content = new FormUrlEncodedContent(form)
        };

        var yanit = await Http.SendAsync(istek, iptal);
        var json = await yanit.Content.ReadAsStringAsync(iptal);
        if (!yanit.IsSuccessStatusCode)
            throw new InvalidOperationException("Oturum yenilenemedi. Yeniden giriş yapın.");

        using var belge = JsonDocument.Parse(json);
        _idToken = belge.RootElement.GetProperty("id_token").GetString();
        _refreshToken = belge.RootElement.GetProperty("refresh_token").GetString();
        var expiresIn = belge.RootElement.TryGetProperty("expires_in", out var ex)
            ? ex.GetString()
            : "3600";
        var saniye = int.TryParse(expiresIn, out var s) ? s : 3600;
        _tokenExpiryMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + (saniye - 60) * 1000L;
        Platform.OturumTokenAyarla(_idToken!);

        if (!string.IsNullOrWhiteSpace(_refreshToken) && PlatformOturumDeposu.Oku() is { BeniHatirla: true })
            PlatformOturumDeposu.Kaydet(_refreshToken!, _eposta ?? "", true);

        return _idToken!;
    }

    public void CikisYap(bool oturumuSil = true)
    {
        _idToken = null;
        _refreshToken = null;
        _tokenExpiryMs = 0;
        _eposta = null;
        if (oturumuSil)
            PlatformOturumDeposu.Sil();
    }
}

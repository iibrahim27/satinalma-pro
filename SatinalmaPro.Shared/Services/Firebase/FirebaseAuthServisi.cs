using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Shared.Services.Firebase;

public sealed class FirebaseAuthServisi
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(45) };
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly FirebaseAyarlar _ayarlar;
    private string? _idToken;
    private string? _refreshToken;
    private DateTime _tokenBitis = DateTime.MinValue;

    public FirebaseAuthServisi(FirebaseAyarlar ayarlar) => _ayarlar = ayarlar;

    public string? IdToken => _idToken;
    public string? Uid { get; private set; }
    public string? Eposta { get; private set; }
    public bool OturumAcik => !string.IsNullOrEmpty(_idToken) && DateTime.UtcNow < _tokenBitis;

    public async Task GirisYapAsync(string eposta, string sifre, CancellationToken iptal = default)
    {
        var yanit = await KimlikIstegiAsync(
            $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={_ayarlar.ApiKey}",
            new { email = eposta.Trim(), password = sifre, returnSecureToken = true },
            iptal);
        OturumuUygula(yanit);
    }

    public async Task<string> GecerliTokenAlAsync(CancellationToken iptal = default)
    {
        if (OturumAcik)
            return _idToken!;

        if (string.IsNullOrEmpty(_refreshToken))
            throw new InvalidOperationException("Oturum süresi doldu. Tekrar giriş yapın.");

        var icerik = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("refresh_token", _refreshToken)
        ]);

        var yanit = await Http.PostAsync(
            $"https://securetoken.googleapis.com/v1/token?key={_ayarlar.ApiKey}",
            icerik,
            iptal);

        var json = await yanit.Content.ReadAsStringAsync(iptal);
        if (!yanit.IsSuccessStatusCode)
            throw new InvalidOperationException(FirebaseHataMesaji(json));

        using var belge = JsonDocument.Parse(json);
        _idToken = belge.RootElement.GetProperty("id_token").GetString();
        _refreshToken = belge.RootElement.GetProperty("refresh_token").GetString();
        var saniye = int.Parse(belge.RootElement.GetProperty("expires_in").GetString() ?? "3600");
        _tokenBitis = DateTime.UtcNow.AddSeconds(saniye - 60);
        return _idToken!;
    }

    public void OturumuKaydet(Action<string> yaz, bool beniHatirla = true)
    {
        if (string.IsNullOrEmpty(_refreshToken))
            return;

        var paket = new OturumPaketi
        {
            RefreshToken = _refreshToken,
            Uid = Uid,
            Eposta = Eposta,
            BeniHatirla = beniHatirla
        };
        yaz(JsonSerializer.Serialize(paket, Json));
    }

    public async Task<bool> KayitliOturumuDeneAsync(Func<string?> oku, CancellationToken iptal = default)
    {
        var metin = oku();
        if (string.IsNullOrWhiteSpace(metin))
            return false;

        try
        {
            var paket = JsonSerializer.Deserialize<OturumPaketi>(metin, Json);
            if (paket is null || string.IsNullOrEmpty(paket.RefreshToken) || !paket.BeniHatirla)
                return false;

            _refreshToken = paket.RefreshToken;
            Uid = paket.Uid;
            Eposta = paket.Eposta;
            await GecerliTokenAlAsync(iptal);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void OturumuKapat()
    {
        _idToken = null;
        _refreshToken = null;
        _tokenBitis = DateTime.MinValue;
        Uid = null;
        Eposta = null;
    }

    public void RefreshTokenAyarla(string refreshToken, string? uid, string? eposta)
    {
        _refreshToken = refreshToken;
        Uid = uid;
        Eposta = eposta;
        _idToken = null;
        _tokenBitis = DateTime.MinValue;
    }

    public async Task SifreDegistirAsync(string mevcutSifre, string yeniSifre, CancellationToken iptal = default)
    {
        if (string.IsNullOrWhiteSpace(Eposta))
            throw new InvalidOperationException("Oturum bilgisi bulunamadı.");

        await GirisYapAsync(Eposta, mevcutSifre, iptal);
        var token = await GecerliTokenAlAsync(iptal);

        var yanit = await KimlikIstegiAsync(
            $"https://identitytoolkit.googleapis.com/v1/accounts:update?key={_ayarlar.ApiKey}",
            new { idToken = token, password = yeniSifre, returnSecureToken = true },
            iptal);
        OturumuUygula(yanit);
    }

    private async Task<JsonElement> KimlikIstegiAsync(string url, object govde, CancellationToken iptal)
    {
        var yanit = await Http.PostAsJsonAsync(url, govde, Json, iptal);
        var json = await yanit.Content.ReadAsStringAsync(iptal);
        if (!yanit.IsSuccessStatusCode)
            throw new InvalidOperationException(FirebaseHataMesaji(json));

        var belge = JsonDocument.Parse(json);
        return belge.RootElement.Clone();
    }

    private void OturumuUygula(JsonElement yanit)
    {
        _idToken = yanit.GetProperty("idToken").GetString();
        _refreshToken = yanit.GetProperty("refreshToken").GetString();
        Uid = yanit.GetProperty("localId").GetString();
        Eposta = yanit.TryGetProperty("email", out var mail) ? mail.GetString() : null;
        var saniye = int.Parse(yanit.GetProperty("expiresIn").GetString() ?? "3600");
        _tokenBitis = DateTime.UtcNow.AddSeconds(saniye - 60);
    }

    private static string FirebaseHataMesaji(string json)
    {
        try
        {
            using var belge = JsonDocument.Parse(json);
            if (belge.RootElement.TryGetProperty("error", out var hata) &&
                hata.TryGetProperty("message", out var mesaj))
            {
                return mesaj.GetString() switch
                {
                    "EMAIL_NOT_FOUND" => "E-posta bulunamadı.",
                    "INVALID_PASSWORD" => "Şifre hatalı.",
                    "INVALID_LOGIN_CREDENTIALS" => "E-posta veya şifre hatalı.",
                    var m => m ?? "Firebase kimlik doğrulama hatası."
                };
            }
        }
        catch { /* yoksay */ }

        return "Firebase bağlantı hatası.";
    }

    private sealed class OturumPaketi
    {
        public string? RefreshToken { get; set; }
        public string? Uid { get; set; }
        public string? Eposta { get; set; }
        public bool BeniHatirla { get; set; }
    }
}

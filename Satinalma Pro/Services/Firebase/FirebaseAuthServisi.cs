using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SatinalmaPro.Models;
using SatinalmaPro.Shared.SaaS;

namespace SatinalmaPro.Services.Firebase;

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

    public async Task<string> KullaniciOlusturAsync(string eposta, string sifre, CancellationToken iptal = default)
    {
        // signUp yeni oturum açmasın — admin oturumunu koru
        var adminRefresh = _refreshToken;
        var adminToken = _idToken;
        var adminUid = Uid;
        var adminEposta = Eposta;
        var adminBitis = _tokenBitis;

        try
        {
            var yanit = await KimlikIstegiAsync(
                $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={_ayarlar.ApiKey}",
                new { email = eposta.Trim(), password = sifre, returnSecureToken = false },
                iptal);

            return yanit.GetProperty("localId").GetString()
                   ?? throw new InvalidOperationException("Kullanıcı oluşturulamadı.");
        }
        finally
        {
            _refreshToken = adminRefresh;
            _idToken = adminToken;
            Uid = adminUid;
            Eposta = adminEposta;
            _tokenBitis = adminBitis;
        }
    }

    public async Task AdminSifreSifirMailiGonderAsync(string eposta, CancellationToken iptal = default)
    {
        await SifreSifirlamaEpostasiGonderAsync(eposta, iptal);
    }

    public async Task SifreSifirlamaEpostasiGonderAsync(string eposta, CancellationToken iptal = default)
    {
        var yanit = await Http.PostAsJsonAsync(
            $"https://identitytoolkit.googleapis.com/v1/accounts:sendOobCode?key={_ayarlar.ApiKey}",
            new { requestType = "PASSWORD_RESET", email = eposta.Trim() },
            Json,
            iptal);

        var json = await yanit.Content.ReadAsStringAsync(iptal);
        if (!yanit.IsSuccessStatusCode)
            throw new InvalidOperationException(SifreSifirHataMesaji(json));
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

    public void OturumuKaydet(string dosyaYolu, bool beniHatirla = true, string? tenantId = null, string? tenantAd = null, string? kullaniciAdi = null)
    {
        if (string.IsNullOrEmpty(_refreshToken))
            return;

        var paket = new OturumPaketi
        {
            RefreshToken = _refreshToken,
            Uid = Uid,
            Eposta = Eposta,
            TenantId = tenantId,
            TenantAd = tenantAd,
            KullaniciAdi = kullaniciAdi,
            BeniHatirla = beniHatirla
        };
        File.WriteAllText(dosyaYolu, JsonSerializer.Serialize(paket, Json));
    }

    public async Task<bool> KayitliOturumuDeneAsync(string dosyaYolu, CancellationToken iptal = default)
    {
        if (!File.Exists(dosyaYolu))
            return false;

        try
        {
            var paket = JsonSerializer.Deserialize<OturumPaketi>(File.ReadAllText(dosyaYolu), Json);
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

    public void OturumuSaaSDenUygula(SaaSLoginSonucu sonuc)
    {
        _idToken = sonuc.IdToken;
        _refreshToken = sonuc.RefreshToken;
        Uid = sonuc.Uid;
        Eposta = sonuc.Eposta;
        _tokenBitis = DateTime.UtcNow.AddSeconds(sonuc.ExpiresIn - 60);
    }

    public void RefreshTokenAyarla(string refreshToken, string? uid, string? eposta)
    {
        _refreshToken = refreshToken;
        Uid = uid;
        Eposta = eposta;
        _idToken = null;
        _tokenBitis = DateTime.MinValue;
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
                    "EMAIL_EXISTS" => "Bu e-posta zaten kayıtlı.",
                    "WEAK_PASSWORD : Password should be at least 6 characters" => "Şifre en az 6 karakter olmalı.",
                    var m => m ?? "Firebase kimlik doğrulama hatası."
                };
            }
        }
        catch
        {
            // yoksay
        }

        return "Firebase bağlantı hatası.";
    }

    private static string SifreSifirHataMesaji(string json)
    {
        try
        {
            using var belge = JsonDocument.Parse(json);
            if (belge.RootElement.TryGetProperty("error", out var hata) &&
                hata.TryGetProperty("message", out var mesaj))
            {
                return mesaj.GetString() switch
                {
                    "EMAIL_NOT_FOUND" => "Bu e-posta adresi kayıtlı değil.",
                    "INVALID_EMAIL" => "Geçersiz e-posta adresi.",
                    _ => FirebaseHataMesaji(json)
                };
            }
        }
        catch
        {
            // yoksay
        }

        return "Şifre sıfırlama e-postası gönderilemedi.";
    }

    private sealed class OturumPaketi
    {
        public string? RefreshToken { get; set; }
        public string? Uid { get; set; }
        public string? Eposta { get; set; }
        public string? TenantId { get; set; }
        public string? TenantAd { get; set; }
        public string? KullaniciAdi { get; set; }
        public bool BeniHatirla { get; set; }
    }
}

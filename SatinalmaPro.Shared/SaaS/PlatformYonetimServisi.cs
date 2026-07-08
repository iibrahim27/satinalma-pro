using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SatinalmaPro.Shared.SaaS;

public sealed class PlatformYonetimServisi
{
  private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(60) };
  private static readonly JsonSerializerOptions Json = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
  };

  private readonly string _projectId;
  private readonly string _region;
  private string? _idToken;
  private Func<CancellationToken, Task<string>>? _tokenAl;

  public PlatformYonetimServisi(string projectId, string region = "europe-west1")
  {
    _projectId = projectId.Trim();
    _region = region.Trim();
  }

  public void OturumTokenAyarla(string idToken) => _idToken = idToken;

  /// <summary>Her istek öncesi güncel Firebase idToken alır (yenileme dahil).</summary>
  public void TokenSaglayiciAyarla(Func<CancellationToken, Task<string>> tokenAl) => _tokenAl = tokenAl;

  public async Task BootstrapAdminAsync(CancellationToken iptal = default) =>
    await CagirAsync("platformBootstrapAdmin", new { }, iptal);

  public async Task<(int Imported, int Skipped, int Total)> LegacyKullanicilariAktarAsync(
    string tenantId,
    CancellationToken iptal = default)
  {
    var sonuc = await CagirAsync("platformImportLegacyUsers", new { tenantId }, iptal);
    return (
      sonuc.TryGetProperty("imported", out var i) ? i.GetInt32() : 0,
      sonuc.TryGetProperty("skipped", out var s) ? s.GetInt32() : 0,
      sonuc.TryGetProperty("total", out var t) ? t.GetInt32() : 0
    );
  }

  public async Task<List<KiracıKaydi>> FirmalariListeleAsync(CancellationToken iptal = default)
  {
    var sonuc = await CagirAsync("platformListTenants", new { }, iptal);
    return JsonSerializer.Deserialize<List<KiracıKaydi>>(sonuc.GetRawText(), Json) ?? [];
  }

  public async Task<KiracıKaydi> FirmaKaydetAsync(KiracıKaydi firma, CancellationToken iptal = default)
  {
    var sonuc = await CagirAsync("platformSaveTenant", new
    {
      id = firma.Id,
      kod = firma.Kod,
      ad = firma.Ad,
      aktif = firma.Aktif
    }, iptal);
    return JsonSerializer.Deserialize<KiracıKaydi>(sonuc.GetRawText(), Json)
           ?? throw new InvalidOperationException("Firma kaydı okunamadı.");
  }

  public async Task<List<PlatformKullaniciKaydi>> KullanicilariListeleAsync(string tenantId, CancellationToken iptal = default)
  {
    var sonuc = await CagirAsync("platformListTenantUsers", new { tenantId }, iptal);
    return JsonSerializer.Deserialize<List<PlatformKullaniciKaydi>>(sonuc.GetRawText(), Json) ?? [];
  }

  public async Task<PlatformKullaniciKaydi> KullaniciKaydetAsync(
    string tenantId,
    PlatformKullaniciKaydi kullanici,
    string? sifre,
    CancellationToken iptal = default)
  {
    var sonuc = await CagirAsync("platformSaveTenantUser", new
    {
      tenantId,
      uid = kullanici.Uid,
      kullaniciAdi = kullanici.KullaniciAdi,
      eposta = kullanici.Eposta,
      adSoyad = kullanici.AdSoyad,
      rol = kullanici.Rol,
      saha = kullanici.Saha,
      aktif = kullanici.Aktif,
      sifre
    }, iptal);
    return JsonSerializer.Deserialize<PlatformKullaniciKaydi>(sonuc.GetRawText(), Json)
           ?? throw new InvalidOperationException("Kullanıcı kaydı okunamadı.");
  }

  private async Task<JsonElement> CagirAsync(string ad, object data, CancellationToken iptal)
  {
    var token = _tokenAl is not null
      ? await _tokenAl(iptal)
      : _idToken;

    if (string.IsNullOrWhiteSpace(token))
      throw new InvalidOperationException("Platform oturumu yok. Yeniden giriş yapın.");

    _idToken = token;

    using var istek = new HttpRequestMessage(HttpMethod.Post, CallableUrl(ad))
    {
      Content = JsonContent.Create(new { data }, options: Json)
    };
    istek.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    var yanit = await Http.SendAsync(istek, iptal);
    var govde = await yanit.Content.ReadAsStringAsync(iptal);
    if (!yanit.IsSuccessStatusCode)
      throw new InvalidOperationException(CallableHataMesaji(yanit.StatusCode, govde, ad));

    using var belge = JsonDocument.Parse(govde);
    return belge.RootElement.GetProperty("result").Clone();
  }

  private string CallableUrl(string name) =>
    $"https://{_region}-{_projectId}.cloudfunctions.net/{name}";

  private static string CallableHataMesaji(System.Net.HttpStatusCode kod, string govde, string fonksiyon)
  {
    if ((int)kod == 401)
      return "Oturum süresi doldu veya geçersiz. Çıkış yapıp yeniden giriş yapın.";

    if ((int)kod == 404 || govde.Contains("Page not found", StringComparison.OrdinalIgnoreCase))
      return $"Sunucu fonksiyonu bulunamadı ({fonksiyon}). Firebase Functions deploy edilmemiş olabilir. "
           + "Proje kökünde .\\deploy-firebase.ps1 çalıştırın.";

    try
    {
      using var belge = JsonDocument.Parse(govde);
      if (belge.RootElement.TryGetProperty("error", out var err) &&
          err.TryGetProperty("message", out var mesaj))
      {
        var m = mesaj.GetString() ?? "";
        return m switch
        {
          "Platform yöneticisi yetkisi gerekli." => m,
          "Platform yöneticisi zaten tanımlı." => m,
          "Firma bulunamadı." => m,
          _ when m.Length > 0 && m.Length < 200 => m,
          _ => "İşlem başarısız."
        };
      }
    }
    catch
    {
      // yoksay
    }

    return $"Sunucu hatası ({(int)kod}). Functions deploy durumunu kontrol edin.";
  }
}

public sealed class KiracıKaydi
{
  public string Id { get; set; } = "";
  public string Kod { get; set; } = "";
  public string Ad { get; set; } = "";
  public bool Aktif { get; set; } = true;
}

public sealed class PlatformKullaniciKaydi
{
  public string Uid { get; set; } = "";
  public string KullaniciAdi { get; set; } = "";
  public string Eposta { get; set; } = "";
  public string AdSoyad { get; set; } = "";
  public string Rol { get; set; } = "Saha";
  public string Saha { get; set; } = "";
  public bool Aktif { get; set; } = true;
}

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SatinalmaPro.Shared.SaaS;

public sealed class SaaSAuthServisi
{
  private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(45) };
  private static readonly JsonSerializerOptions Json = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
  };

  private readonly string _projectId;
  private readonly string _region;

  public SaaSAuthServisi(string projectId, string region = "europe-west1")
  {
    _projectId = projectId.Trim();
    _region = region.Trim();
  }

  private string CallableUrl(string name) =>
    $"https://{_region}-{_projectId}.cloudfunctions.net/{name}";

  public async Task<SaaSLoginSonucu> GirisYapAsync(
    string kullaniciAdi,
    string sifre,
    CancellationToken iptal = default)
  {
    var hata = KullaniciAdiYardimcisi.DogrulaVeyaHata(kullaniciAdi);
    if (hata is not null)
      throw new InvalidOperationException(hata);

    if (string.IsNullOrWhiteSpace(sifre))
      throw new InvalidOperationException("Şifre zorunludur.");

  var yanit = await Http.PostAsJsonAsync(
      CallableUrl("loginWithUsername"),
      new { data = new { username = KullaniciAdiYardimcisi.Normallestir(kullaniciAdi), password = sifre } },
      Json,
      iptal);

    var govde = await yanit.Content.ReadAsStringAsync(iptal);
    if (!yanit.IsSuccessStatusCode)
      throw new InvalidOperationException(CallableHataMesaji(govde));

    using var belge = JsonDocument.Parse(govde);
    if (!belge.RootElement.TryGetProperty("result", out var sonuc))
      throw new InvalidOperationException("Sunucu yanıtı geçersiz.");

    return new SaaSLoginSonucu
    {
      IdToken = sonuc.GetProperty("idToken").GetString() ?? "",
      RefreshToken = sonuc.GetProperty("refreshToken").GetString() ?? "",
      Uid = sonuc.GetProperty("uid").GetString() ?? "",
      TenantId = sonuc.GetProperty("tenantId").GetString() ?? "",
      TenantAd = sonuc.TryGetProperty("tenantAd", out var ta) ? ta.GetString() : null,
      Eposta = sonuc.TryGetProperty("eposta", out var ep) ? ep.GetString() : null,
      KullaniciAdi = sonuc.TryGetProperty("kullaniciAdi", out var ka) ? ka.GetString() : null,
      ExpiresIn = sonuc.TryGetProperty("expiresIn", out var ex) ? ex.GetInt32() : 3600,
      Lisans = LisansOku(sonuc)
    };
  }

  private static KiracıLisansi? LisansOku(JsonElement sonuc)
  {
    if (!sonuc.TryGetProperty("lisans", out var l) || l.ValueKind != JsonValueKind.Object)
      return null;

    DateTime? ParseUtc(string? s) =>
      DateTime.TryParse(s, null, System.Globalization.DateTimeStyles.RoundtripKind, out var d)
        ? d.ToUniversalTime()
        : null;

    return new KiracıLisansi
    {
      Tip = l.TryGetProperty("tip", out var tip) ? tip.GetString() ?? LisansTipleri.Deneme : LisansTipleri.Deneme,
      BaslangicUtc = ParseUtc(l.TryGetProperty("baslangicUtc", out var b) ? b.GetString() : null),
      BitisUtc = ParseUtc(l.TryGetProperty("bitisUtc", out var bi) ? bi.GetString() : null),
      Aktif = !l.TryGetProperty("aktif", out var a) || a.ValueKind != JsonValueKind.False,
      KalanGun = l.TryGetProperty("kalanGun", out var k) && k.ValueKind == JsonValueKind.Number ? k.GetInt32() : null,
      SuresiDoldu = l.TryGetProperty("suresiDoldu", out var s) && s.ValueKind == JsonValueKind.True
    };
  }

  public async Task SifreSifirlaAsync(string kullaniciAdi, CancellationToken iptal = default)
  {
    var hata = KullaniciAdiYardimcisi.DogrulaVeyaHata(kullaniciAdi);
    if (hata is not null)
      throw new InvalidOperationException(hata);

    var yanit = await Http.PostAsJsonAsync(
      CallableUrl("passwordResetByUsername"),
      new { data = new { username = KullaniciAdiYardimcisi.Normallestir(kullaniciAdi) } },
      Json,
      iptal);

    var govde = await yanit.Content.ReadAsStringAsync(iptal);
    if (!yanit.IsSuccessStatusCode)
      throw new InvalidOperationException(CallableHataMesaji(govde));
  }

  private static string CallableHataMesaji(string json)
  {
    if (json.Contains("Page not found", StringComparison.OrdinalIgnoreCase) ||
        json.Contains("404", StringComparison.OrdinalIgnoreCase) && json.Contains("<html", StringComparison.OrdinalIgnoreCase))
      return "Sunucu fonksiyonu bulunamadı. Firebase Functions henüz deploy edilmemiş olabilir.";

    try
    {
      using var belge = JsonDocument.Parse(json);
      if (belge.RootElement.TryGetProperty("error", out var err))
      {
        if (err.TryGetProperty("message", out var mesaj))
          return MesajCevir(mesaj.GetString() ?? "Giriş başarısız.");
        if (err.TryGetProperty("status", out var status))
          return MesajCevir(status.GetString() ?? "Giriş başarısız.");
      }
    }
    catch
    {
      // yoksay
    }

    return "Sunucu bağlantı hatası.";
  }

  private static string MesajCevir(string mesaj) => mesaj switch
  {
    "INVALID_LOGIN" => "Kullanıcı adı veya şifre hatalı.",
    "USER_INACTIVE" => "Hesabınız pasif durumda.",
    "USER_NOT_FOUND" => "Kullanıcı adı veya şifre hatalı.",
    "TENANT_INACTIVE" => "Firma hesabı pasif durumda.",
    "LICENSE_EXPIRED" => "Firma lisans süresi dolmuş. Giriş yapılamaz. Platform yöneticinize başvurun.",
    "PLATFORM_ADMIN_LOGIN" => "Platform yöneticisi SatınalmaPro'ya firma olarak giriş yapamaz. Satınalma Yönetici uygulamasını kullanın.",
    "AUTH_CONFIG_MISSING" => "Sunucu giriş ayarı eksik (WEB_API_KEY). Yöneticiye bildirin.",
    _ when mesaj.Contains("NOT_FOUND", StringComparison.OrdinalIgnoreCase) => "Kullanıcı adı veya şifre hatalı.",
    _ => mesaj
  };
}

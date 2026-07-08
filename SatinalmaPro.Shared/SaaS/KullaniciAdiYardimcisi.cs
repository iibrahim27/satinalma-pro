using System.Text.RegularExpressions;

namespace SatinalmaPro.Shared.SaaS;

public static partial class KullaniciAdiYardimcisi
{
  public const int MinUzunluk = 3;
  public const int MaxUzunluk = 32;

  [GeneratedRegex("^[a-z0-9][a-z0-9._-]{2,31}$", RegexOptions.CultureInvariant)]
  private static partial Regex GecerliDesen();

  public static string Normallestir(string? kullaniciAdi)
  {
    if (string.IsNullOrWhiteSpace(kullaniciAdi))
      return "";

    var tr = kullaniciAdi.Trim().ToLowerInvariant();
    tr = tr
      .Replace('ı', 'i')
      .Replace('ğ', 'g')
      .Replace('ü', 'u')
      .Replace('ş', 's')
      .Replace('ö', 'o')
      .Replace('ç', 'c');

    return tr;
  }

  public static bool GecerliMi(string? kullaniciAdi)
  {
    var n = Normallestir(kullaniciAdi);
    return n.Length >= MinUzunluk && n.Length <= MaxUzunluk && GecerliDesen().IsMatch(n);
  }

  public static string? DogrulaVeyaHata(string? kullaniciAdi)
  {
    if (string.IsNullOrWhiteSpace(kullaniciAdi))
      return "Kullanıcı adı zorunludur.";

    var n = Normallestir(kullaniciAdi);
    if (n.Length < MinUzunluk || n.Length > MaxUzunluk)
      return $"Kullanıcı adı {MinUzunluk}-{MaxUzunluk} karakter olmalıdır.";

    if (!GecerliDesen().IsMatch(n))
      return "Kullanıcı adı yalnızca küçük harf, rakam, nokta, tire ve alt çizgi içerebilir.";

    return null;
  }
}

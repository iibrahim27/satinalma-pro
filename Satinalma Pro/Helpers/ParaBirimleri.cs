namespace SatinalmaPro.Helpers;

public static class ParaBirimleri
{
    public const string Try = "TRY";
    public const string Usd = "USD";
    public const string Eur = "EUR";

    public static IReadOnlyList<string> Liste { get; } = [Try, Usd, Eur];

    public static bool TryMi(string? paraBirimi) =>
        string.IsNullOrWhiteSpace(paraBirimi) ||
        string.Equals(paraBirimi.Trim(), Try, StringComparison.OrdinalIgnoreCase);

    public static string Sembol(string? paraBirimi) => (paraBirimi ?? Try).Trim().ToUpperInvariant() switch
    {
        Usd => "$",
        Eur => "€",
        _ => "₺"
    };

    public static decimal TlCevir(decimal birimFiyat, string? paraBirimi, decimal usdKuru, decimal eurKuru) =>
        (paraBirimi ?? Try).Trim().ToUpperInvariant() switch
        {
            Usd when usdKuru > 0 => Math.Round(birimFiyat * usdKuru, 4),
            Eur when eurKuru > 0 => Math.Round(birimFiyat * eurKuru, 4),
            _ => birimFiyat
        };

    public static string BirimFiyatGosterim(decimal birimFiyat, string? paraBirimi, decimal usdKuru, decimal eurKuru)
    {
        var pb = string.IsNullOrWhiteSpace(paraBirimi) ? Try : paraBirimi.Trim().ToUpperInvariant();
        var sembol = Sembol(pb);
        if (TryMi(pb) || birimFiyat == 0)
            return $"{birimFiyat:N2} {sembol}";

        var tl = TlCevir(birimFiyat, pb, usdKuru, eurKuru);
        return $"{birimFiyat:N2} {sembol} ({tl:N2} ₺)";
    }
}

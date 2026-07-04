namespace SatinalmaPro.Shared.Models;

public static class ParaBirimleri
{
    public const string Try = "TRY";
    public const string Usd = "USD";
    public const string Eur = "EUR";

    public static readonly string[] Tum = [Try, Usd, Eur];

    public static string Sembol(string? kod) => kod?.ToUpperInvariant() switch
    {
        Usd => "$",
        Eur => "€",
        _ => "₺"
    };
}

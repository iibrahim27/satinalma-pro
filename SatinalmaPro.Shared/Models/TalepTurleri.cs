namespace SatinalmaPro.Shared.Models;

public static class TalepTurleri
{
    public const string Acil = "Acil";
    public const string Normal = "Normal";

    public static IReadOnlyList<string> Tum { get; } = [Acil, Normal];

    public static string GorunenAd(string tur) => tur switch
    {
        Acil => "Acil",
        Normal => "Normal",
        _ => tur
    };

    public static string TurkceAd(string tur) => tur switch
    {
        Acil => "Acil Talep",
        Normal => "Normal Talep",
        _ => tur
    };
}

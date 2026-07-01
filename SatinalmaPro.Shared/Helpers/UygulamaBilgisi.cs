namespace SatinalmaPro.Shared.Helpers;

public static class UygulamaBilgisi
{
    public const string Ad = "Satınalma Pro";
    public const string Gelistirici = "İ:PEKBALCI";
    public const string Yazar = "İ:PEKBALCI";

    public static string AltBilgiMetni(string surum) =>
        $"{Ad} v{surum}  ·  Geliştirici: {Gelistirici}  ·  Yazar: {Yazar}";
}

using SatinalmaPro.Models;

namespace SatinalmaPro.Services;

public static class StokBelgeNoUretici
{
    public static string SonrakiGirisBelgeNo() => SonrakiBelgeNo("GR");

    public static string SonrakiCikisBelgeNo() => SonrakiBelgeNo("CK");

    private static string SonrakiBelgeNo(string kod)
    {
        var yil = DateTime.Now.Year;
        var onEk = $"{kod}-{yil}-";
        var sira = ModulVeriDeposu.StokHareketleri
            .Select(h => h.BelgeNo)
            .Where(b => b.StartsWith(onEk, StringComparison.OrdinalIgnoreCase))
            .Select(SiraNumarasiAl)
            .DefaultIfEmpty(0)
            .Max();

        return $"{onEk}{sira + 1:D3}";
    }

    private static int SiraNumarasiAl(string belgeNo)
    {
        var sonTire = belgeNo.LastIndexOf('-');
        if (sonTire < 0 || sonTire >= belgeNo.Length - 1)
            return 0;

        return int.TryParse(belgeNo[(sonTire + 1)..], out var no) ? no : 0;
    }
}

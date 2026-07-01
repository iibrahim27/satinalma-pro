namespace SatinalmaPro.Shared.Helpers;

/// <summary>Masaüstü ve mobil güncelleme karşılaştırması — tek kaynak.</summary>
public static class SurumYardimcisi
{
    public static bool TryParse(string? metin, out Version surum)
    {
        surum = new Version(0, 0);
        if (string.IsNullOrWhiteSpace(metin))
            return false;

        metin = metin.Trim();
        var parcalar = metin.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parcalar.Length == 0)
            return false;

        try
        {
            var major = int.Parse(parcalar[0]);
            var minor = parcalar.Length > 1 ? int.Parse(parcalar[1]) : 0;
            var build = parcalar.Length > 2 ? int.Parse(parcalar[2]) : 0;
            var revision = parcalar.Length > 3 ? int.Parse(parcalar[3]) : 0;
            surum = new Version(major, minor, build, revision);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool AyniSurum(string? a, string? b) =>
        TryParse(a, out var sa) && TryParse(b, out var sb) && sa == sb;

    /// <summary>Uzak sürüm veya build daha yeniyse güncelleme gerekir.</summary>
    public static bool GuncellemeGerekli(string uzakSurum, int uzakBuild, string yerelSurum, int yerelBuild)
    {
        if (!TryParse(uzakSurum, out var uzak) || !TryParse(yerelSurum, out var yerel))
            return false;

        var surumFarki = uzak.CompareTo(yerel);
        if (surumFarki > 0)
            return true;
        if (surumFarki < 0)
            return false;

        if (uzakBuild <= 0)
            return false;

        return yerelBuild > 0
            ? uzakBuild > yerelBuild
            : false;
    }
}

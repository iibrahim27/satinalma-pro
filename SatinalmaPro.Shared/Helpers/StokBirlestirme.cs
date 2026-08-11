using System.Globalization;
using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Shared.Helpers;

public static class StokBirlestirme
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
    private static readonly string[] TarihFormatlari =
    [
        "dd.MM.yyyy HH:mm:ss", "dd.MM.yyyy HH:mm", "dd.MM.yyyy",
        "d.M.yyyy HH:mm", "d.M.yyyy", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd"
    ];

    /// <summary>
    /// Malzeme+depo anahtarına göre birleştir.
    /// Daha yeni SonGuncelleme kazanır; eşitlikte yerel kazanır.
    /// Miktar karşılaştırması yok — çıkış (düşük miktar) geri alınmasın.
    /// </summary>
    public static List<StokKaydi> Birlestir(IEnumerable<StokKaydi> yerel, IEnumerable<StokKaydi> bulut)
    {
        var sozluk = new Dictionary<string, StokKaydi>(StringComparer.OrdinalIgnoreCase);

        foreach (var kayit in bulut)
            Ekle(sozluk, kayit, yerelKayit: false);

        foreach (var kayit in yerel)
            Ekle(sozluk, kayit, yerelKayit: true);

        return sozluk.Values
            .OrderBy(s => s.MalzemeAdi, StringComparer.OrdinalIgnoreCase)
            .ThenBy(s => s.DepoSaha, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static List<StokHareketKaydi> HareketleriBirlestir(
        IEnumerable<StokHareketKaydi> yerel,
        IEnumerable<StokHareketKaydi> bulut)
    {
        var sozluk = new Dictionary<Guid, StokHareketKaydi>();

        foreach (var kayit in bulut)
            sozluk[kayit.Id] = kayit;

        foreach (var kayit in yerel)
            sozluk[kayit.Id] = kayit;

        return sozluk.Values
            .OrderByDescending(h => TarihOku(h.Tarih))
            .ThenBy(h => h.MalzemeAdi, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void Ekle(Dictionary<string, StokKaydi> sozluk, StokKaydi kayit, bool yerelKayit)
    {
        var anahtar = Anahtar(kayit);
        if (string.IsNullOrWhiteSpace(anahtar) || anahtar == "|")
            return;

        if (!sozluk.TryGetValue(anahtar, out var mevcut))
        {
            sozluk[anahtar] = kayit;
            return;
        }

        var adayT = TarihOku(kayit.SonGuncelleme);
        var mevT = TarihOku(mevcut.SonGuncelleme);
        if (adayT > mevT || (adayT == mevT && yerelKayit))
            sozluk[anahtar] = kayit;
    }

    private static string Anahtar(StokKaydi kayit) =>
        $"{kayit.MalzemeAdi?.Trim()}|{kayit.DepoSaha?.Trim()}";

    private static DateTime TarihOku(string? metin)
    {
        if (string.IsNullOrWhiteSpace(metin))
            return DateTime.MinValue;

        var temiz = metin.Trim();
        if (DateTime.TryParseExact(temiz, TarihFormatlari, Tr, DateTimeStyles.None, out var dt))
            return dt;
        if (DateTime.TryParse(temiz, Tr, DateTimeStyles.None, out dt))
            return dt;
        return DateTime.TryParse(temiz, out dt) ? dt : DateTime.MinValue;
    }
}

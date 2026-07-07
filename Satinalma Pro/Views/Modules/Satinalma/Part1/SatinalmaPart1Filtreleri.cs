using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;
using SatinalmaPro.Shared.Procurement;

namespace SatinalmaPro.Views.Modules.Satinalma.Part1;

/// <summary>
/// Satınalma Part1 route → enterprise status filtreleri.
/// Görsel katman değişmez; yalnızca grid veri kaynağı buradan beslenir.
/// </summary>
public static class SatinalmaPart1Filtreleri
{
    private static string? AktifRol => OturumYoneticisi.AktifKullanici?.Rol;
    private static string? AktifUid => OturumYoneticisi.AktifKullanici?.Uid;

    public static bool GelenTalepler(SatinalmaTalep talep) =>
        Eslesir(SatinalmaRoutes.YonetimGelenTalepler, talep);

    public static bool YonetimTeklifBekleyen(SatinalmaTalep talep) =>
        Eslesir(SatinalmaRoutes.YonetimTeklifBekleyen, talep);

    public static bool YonetimTeklifGirilen(SatinalmaTalep talep) =>
        Eslesir(SatinalmaRoutes.YonetimTeklifGirilen, talep);

    public static bool TeklifIstenen(SatinalmaTalep talep) =>
        Eslesir(SatinalmaRoutes.SatinalmaTeklifIstenen, talep);

    public static bool Karsilastirma(SatinalmaTalep talep) =>
        Eslesir(SatinalmaRoutes.SatinalmaKarsilastirma, talep);

    public static bool TeklifDuzeltme(SatinalmaTalep talep) =>
        Eslesir(SatinalmaRoutes.SatinalmaTeklifDuzeltme, talep);

    public static bool SatinalmaTeklifGirilen(SatinalmaTalep talep) =>
        Eslesir(SatinalmaRoutes.SatinalmaTeklifGirilen, talep);

    public static bool SatinalmaOnaylanan(SatinalmaTalep talep) =>
        Eslesir(SatinalmaRoutes.SatinalmaOnaylanan, talep);

    public static bool SatinalmaSiparisVerilen(SatinalmaTalep talep) =>
        Eslesir(SatinalmaRoutes.SatinalmaSiparis, talep);

    public static bool SatinalmaMalKabulEdilmis(SatinalmaTalep talep) =>
        Eslesir(SatinalmaRoutes.SatinalmaMalKabul, talep);

    public static bool MalKabulTamam(SatinalmaTalep talep) =>
        ProcurementTalepAdapter.ResolveStatus(talep)
            .Equals(ProcurementStatus.Completed, StringComparison.OrdinalIgnoreCase);

    public static string MalKabulOzeti(SatinalmaTalep talep)
    {
        var kalemler = MalKabulKalemleri(talep);
        if (kalemler.Count == 0)
            return "—";

        if (kalemler.All(k => k.SiparisTamamlandi || k.KabulEdilenMiktar >= k.Miktar - 0.0001))
            return "Tamamlandı";

        if (kalemler.Any(k => k.KabulEdilenMiktar > 0.0001))
            return "Kısmi";

        return "Bekliyor";
    }

    private static List<SatinalmaTalepKalemi> MalKabulKalemleri(SatinalmaTalep talep)
    {
        talep.Kalemler ??= [];
        var teklifsiz = talep.TeklifsizYonetimOnayi && !talep.HerhangiKalemOnayli;
        return talep.Kalemler
            .Where(k => !string.IsNullOrWhiteSpace(k.Malzeme))
            .Where(k => teklifsiz || k.OnaylananTeklifId != null)
            .ToList();
    }

    public static bool Taleplerim(SatinalmaTalep talep) =>
        Eslesir(SatinalmaRoutes.Taleplerim, talep);

    public static bool OnayBekleyenTalep(SatinalmaTalep talep) =>
        Eslesir(SatinalmaPart1Menusu.SatinalmaOnayBekleyen, talep);

    public static bool OnaylananTaleplerSaha(SatinalmaTalep talep) =>
        Eslesir(SatinalmaRoutes.OnaylananTaleplerSaha, talep);

    public static Func<SatinalmaTalep, bool>? Filtre(string route) =>
        DesktopRoleTabManager.GetDataFilter(route, AktifRol, AktifUid);

    public static bool YonetimOnaylananTeklifler(SatinalmaTalep talep) =>
        Eslesir(SatinalmaRoutes.YonetimOnaylananTeklifler, talep);

    public static bool YonetimDirekOnaylanan(SatinalmaTalep talep) =>
        Eslesir(SatinalmaRoutes.YonetimDirekOnaylanan, talep);

    public static bool YonetimRedVerilen(SatinalmaTalep talep) =>
        Eslesir(SatinalmaRoutes.YonetimRedVerilen, talep);

    public static bool YonetimGecmis(SatinalmaTalep talep) =>
        Eslesir(SatinalmaRoutes.YonetimGecmis, talep);

    public static bool YonetimOnayGecmisi(SatinalmaTalep talep) =>
        Eslesir(SatinalmaRoutes.YonetimOnayGecmisi, talep);

    public static int Sayaç(string route) =>
        RozetSayilari([route]).GetValueOrDefault(route);

    public static Dictionary<string, int> RozetSayilari(IReadOnlyList<string> routes) =>
        DesktopRoleTabManager.CountBadges(routes, AktifRol, AktifUid);

    public static List<SatinalmaTalep> SiraliListe(string route, IEnumerable<SatinalmaTalep> kaynak) =>
        DesktopRoleTabManager.FilterAndSort(route, kaynak, AktifRol, AktifUid);

    private static bool Eslesir(string route, SatinalmaTalep talep)
    {
        var filtre = DesktopRoleTabManager.GetDataFilter(route, AktifRol, AktifUid);
        return filtre?.Invoke(talep) == true;
    }
}

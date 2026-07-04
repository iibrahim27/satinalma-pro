using SatinalmaPro.Models;

namespace SatinalmaPro.Helpers;

public static class SatinalmaTalepSenkronYardimcisi
{
    public static void SilindiIsaretle(Guid talepId, SatinalmaAyarlar ayarlar)
    {
        ayarlar.SilinenTalepIdleri ??= [];
        if (!ayarlar.SilinenTalepIdleri.Contains(talepId))
            ayarlar.SilinenTalepIdleri.Add(talepId);
    }

    public static List<Guid> SilinenleriBirlestir(IEnumerable<Guid>? a, IEnumerable<Guid>? b) =>
        (a ?? []).Union(b ?? []).Distinct().ToList();

    public static HashSet<Guid> SilinenKumesi(IEnumerable<Guid>? silinenIdler) =>
        silinenIdler?.ToHashSet() ?? [];

    public static void SilinenleriListedenCikar(IList<Models.SatinalmaTalep> talepler, IEnumerable<Guid>? silinenIdler)
    {
        var silinen = SilinenKumesi(silinenIdler);
        if (silinen.Count == 0)
            return;

        for (var i = talepler.Count - 1; i >= 0; i--)
        {
            if (silinen.Contains(talepler[i].Id))
                talepler.RemoveAt(i);
        }
    }
}

using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Shared.Helpers;

/// <summary>Bulut birleştirmede silme ve düzenleme çakışmalarını çözer.</summary>
public static class SatinalmaTalepSenkronYardimcisi
{
    public static void Dokun(SatinalmaTalep talep) =>
        talep.GuncellemeUtc = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    public static void SilindiIsaretle(Guid talepId, SatinalmaAyarlar ayarlar)
    {
        ayarlar.SilinenTalepIdleri ??= [];
        if (!ayarlar.SilinenTalepIdleri.Contains(talepId))
            ayarlar.SilinenTalepIdleri.Add(talepId);
    }

    public static List<Guid> SilinenleriBirlestir(IEnumerable<Guid>? a, IEnumerable<Guid>? b) =>
        (a ?? []).Union(b ?? []).Distinct().ToList();

    public static void AyarlariBirlestir(SatinalmaAyarlar hedef, SatinalmaAyarlar kaynak)
    {
        hedef.SilinenTalepIdleri = SilinenleriBirlestir(hedef.SilinenTalepIdleri, kaynak.SilinenTalepIdleri);
        hedef.SonTalepSira = Math.Max(hedef.SonTalepSira, kaynak.SonTalepSira);
        hedef.SonSiparisSira = Math.Max(hedef.SonSiparisSira, kaynak.SonSiparisSira);
        hedef.SonIadeSira = Math.Max(hedef.SonIadeSira, kaynak.SonIadeSira);
        hedef.VeriSifirlamaUtc = Math.Max(hedef.VeriSifirlamaUtc, kaynak.VeriSifirlamaUtc);

        if (string.IsNullOrWhiteSpace(hedef.FirmaAdi) && !string.IsNullOrWhiteSpace(kaynak.FirmaAdi))
            hedef.FirmaAdi = kaynak.FirmaAdi;

        if (hedef.VarsayilanUsdKuru <= 0 && kaynak.VarsayilanUsdKuru > 0)
            hedef.VarsayilanUsdKuru = kaynak.VarsayilanUsdKuru;

        if (hedef.VarsayilanEurKuru <= 0 && kaynak.VarsayilanEurKuru > 0)
            hedef.VarsayilanEurKuru = kaynak.VarsayilanEurKuru;

        SatinalmaAyarlarSenkronYardimcisi.MetinAlanlariniBirlestir(hedef, kaynak);
    }

    public static HashSet<Guid> SilinenKumesi(IEnumerable<Guid>? silinenIdler) =>
        silinenIdler?.ToHashSet() ?? [];

    public static void SilinenleriListedenCikar(IList<SatinalmaTalep> talepler, IEnumerable<Guid>? silinenIdler)
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

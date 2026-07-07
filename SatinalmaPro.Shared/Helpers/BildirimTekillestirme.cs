using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Shared.Helpers;

/// <summary>Aynı talep+tip+hedef için yinelenen bildirimleri birleştirir.</summary>
public static class BildirimTekillestirme
{
    public static List<BildirimKaydi> Tekille(IEnumerable<BildirimKaydi> kaynak)
    {
        return kaynak
            .GroupBy(BildirimMantikAnahtari.Olustur)
            .Select(BirlestirGrup)
            .OrderByDescending(b => b.GuncellemeUtc)
            .ThenByDescending(b => b.OlusturmaTarihi, StringComparer.Ordinal)
            .ToList();
    }

    private static BildirimKaydi BirlestirGrup(IEnumerable<BildirimKaydi> grup)
    {
        var sirali = grup
            .OrderByDescending(b => b.Okundu)
            .ThenByDescending(b => b.GuncellemeUtc)
            .ToList();

        var birincil = Kopyala(sirali[0]);
        foreach (var diger in sirali.Skip(1))
        {
            birincil.Okundu = birincil.Okundu || diger.Okundu;
            birincil.GuncellemeUtc = Math.Max(birincil.GuncellemeUtc, diger.GuncellemeUtc);

            if (string.IsNullOrWhiteSpace(birincil.InboxDocId) && !string.IsNullOrWhiteSpace(diger.InboxDocId))
                birincil.InboxDocId = diger.InboxDocId;
            if (string.IsNullOrWhiteSpace(birincil.Baslik))
                birincil.Baslik = diger.Baslik;
            if (string.IsNullOrWhiteSpace(birincil.Mesaj))
                birincil.Mesaj = diger.Mesaj;
        }

        return birincil;
    }

    private static BildirimKaydi Kopyala(BildirimKaydi kaynak) => new()
    {
        Id = kaynak.Id,
        Baslik = kaynak.Baslik,
        Mesaj = kaynak.Mesaj,
        Tip = kaynak.Tip,
        TalepId = kaynak.TalepId,
        HedefRol = kaynak.HedefRol,
        HedefUid = kaynak.HedefUid,
        OlusturanUid = kaynak.OlusturanUid,
        OlusturanAd = kaynak.OlusturanAd,
        OlusturmaTarihi = kaynak.OlusturmaTarihi,
        Okundu = kaynak.Okundu,
        GuncellemeUtc = kaynak.GuncellemeUtc,
        InboxDocId = kaynak.InboxDocId,
        DeepLink = kaynak.DeepLink,
        EventCode = kaynak.EventCode,
        DesktopRoute = kaynak.DesktopRoute
    };
}

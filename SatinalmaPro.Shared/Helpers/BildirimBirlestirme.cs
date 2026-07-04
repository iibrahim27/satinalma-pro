using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Shared.Helpers;

/// <summary>Firestore bildirim listesi birleştirme — last-write-wins yerine kayıt bazlı birleşim.</summary>
public static class BildirimBirlestirme
{
    public static List<BildirimKaydi> Birlestir(
        IEnumerable<BildirimKaydi> yerel,
        IEnumerable<BildirimKaydi> bulut)
    {
        var sozluk = new Dictionary<Guid, BildirimKaydi>();

        foreach (var b in bulut)
            sozluk[b.Id] = b;

        foreach (var b in yerel)
        {
            if (!sozluk.TryGetValue(b.Id, out var mevcut))
            {
                sozluk[b.Id] = b;
                continue;
            }

            sozluk[b.Id] = BirlestirIkili(mevcut, b);
        }

        return sozluk.Values
            .OrderByDescending(b => b.GuncellemeUtc)
            .ThenByDescending(b => b.OlusturmaTarihi, StringComparer.Ordinal)
            .ToList();
    }

    public static void Dokun(BildirimKaydi bildirim) =>
        bildirim.GuncellemeUtc = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    private static BildirimKaydi BirlestirIkili(BildirimKaydi a, BildirimKaydi b)
    {
        var birincil = a.GuncellemeUtc >= b.GuncellemeUtc ? Kopyala(a) : Kopyala(b);
        var ikincil = a.GuncellemeUtc >= b.GuncellemeUtc ? b : a;

        birincil.Okundu = a.Okundu || b.Okundu;
        birincil.GuncellemeUtc = Math.Max(a.GuncellemeUtc, b.GuncellemeUtc);

        if (string.IsNullOrWhiteSpace(birincil.Baslik))
            birincil.Baslik = ikincil.Baslik;
        if (string.IsNullOrWhiteSpace(birincil.Mesaj))
            birincil.Mesaj = ikincil.Mesaj;

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

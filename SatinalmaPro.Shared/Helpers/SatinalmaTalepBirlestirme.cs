using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Shared.Helpers;

/// <summary>
/// Bulut ve yerel talep kayıtlarını birleştirir — silinen kayıtlar geri gelmez, en güncel kazanır.
/// </summary>
public static class SatinalmaTalepBirlestirme
{
    public static List<SatinalmaTalep> Birlestir(
        IEnumerable<SatinalmaTalep> yerel,
        IEnumerable<SatinalmaTalep> bulut,
        IEnumerable<Guid>? silinenIdler = null)
    {
        var silinen = SatinalmaTalepSenkronYardimcisi.SilinenKumesi(silinenIdler);
        var sozluk = new Dictionary<Guid, SatinalmaTalep>();

        foreach (var talep in yerel.Where(t => !silinen.Contains(t.Id)))
            sozluk[talep.Id] = talep;

        foreach (var talep in bulut.Where(t => !silinen.Contains(t.Id)))
        {
            if (!sozluk.TryGetValue(talep.Id, out var mevcut))
            {
                sozluk[talep.Id] = talep;
                continue;
            }

            sozluk[talep.Id] = DahaGuncelKayit(mevcut, talep);
        }

        return sozluk.Values.ToList();
    }

    private static SatinalmaTalep DahaGuncelKayit(SatinalmaTalep a, SatinalmaTalep b)
    {
        var kazanan = KazananKayit(a, b);
        var diger = ReferenceEquals(kazanan, a) ? b : a;
        // Kazanan daha yeniyse teklif listesi otoriterdir; silinen teklif eski buluttan geri gelmez.
        var kazananDahaYeni = kazanan.GuncellemeUtc > diger.GuncellemeUtc;
        SurecDurumunuBirlestir(kazanan, diger);
        kazanan.GuncellemeUtc = Math.Max(kazanan.GuncellemeUtc, diger.GuncellemeUtc);
        TeklifleriBirlestir(kazanan, diger, kazananDahaYeni);
        return kazanan;
    }

    /// <summary>
    /// Bulut birleştirmede «Hazırlanıyor» kaydı «İmza Sürecinde» gönderimini ezmesin.
    /// Red yalnızca daha yeni bilinçli red kaydıysa uygulanır.
    /// </summary>
    private static void SurecDurumunuBirlestir(SatinalmaTalep hedef, SatinalmaTalep kaynak)
    {
        if (ReferenceEquals(hedef, kaynak))
            return;

        // Daha yeni kazanan kaydın durumu korunur — «Yeniden Gönder» sonrası eski bulut aşaması ezmez.
        if (hedef.GuncellemeUtc > kaynak.GuncellemeUtc)
            return;

        if (kaynak.Durum == SatinalmaTalepDurumlari.Reddedildi
            && hedef.Durum != SatinalmaTalepDurumlari.Reddedildi
            && kaynak.GuncellemeUtc > hedef.GuncellemeUtc)
        {
            hedef.Durum = kaynak.Durum;
            return;
        }

        if (hedef.Durum == SatinalmaTalepDurumlari.Reddedildi
            && kaynak.Durum != SatinalmaTalepDurumlari.Reddedildi)
            return;

        var hedefAsama = SatinalmaTalepDurumlari.SurecAsamaSkoru(hedef.Durum);
        var kaynakAsama = SatinalmaTalepDurumlari.SurecAsamaSkoru(kaynak.Durum);
        if (kaynakAsama > hedefAsama)
            hedef.Durum = kaynak.Durum;

        if (SatinalmaTalepYardimcisi.GercekTeklifVar(hedef) && kaynakAsama < hedefAsama)
            return;
    }

    private static SatinalmaTalep KazananKayit(SatinalmaTalep a, SatinalmaTalep b)
    {
        if (a.GuncellemeUtc != b.GuncellemeUtc)
        {
            if (a.GuncellemeUtc <= 0 && b.GuncellemeUtc > 0)
                return b;
            if (b.GuncellemeUtc <= 0 && a.GuncellemeUtc > 0)
                return a;
            return b.GuncellemeUtc > a.GuncellemeUtc ? b : a;
        }

        var skorA = Skor(a);
        var skorB = Skor(b);
        if (skorB > skorA)
            return b;
        if (skorA > skorB)
            return a;

        var asamaA = SatinalmaTalepDurumlari.SurecAsamaSkoru(a.Durum);
        var asamaB = SatinalmaTalepDurumlari.SurecAsamaSkoru(b.Durum);
        if (asamaB != asamaA)
            return asamaB > asamaA ? b : a;

        var teklifA = a.Teklifler?.Count ?? 0;
        var teklifB = b.Teklifler?.Count ?? 0;
        if (teklifB != teklifA)
            return teklifB > teklifA ? b : a;

        return TarihSira(b.Tarih) >= TarihSira(a.Tarih) ? b : a;
    }

    private static void TeklifleriBirlestir(SatinalmaTalep hedef, SatinalmaTalep kaynak, bool kazananDahaYeni)
    {
        if (ReferenceEquals(hedef, kaynak))
            return;

        hedef.Teklifler ??= [];
        if (ReferenceEquals(hedef.Teklifler, kaynak.Teklifler))
            return;

        foreach (var teklif in (kaynak.Teklifler ?? []).ToList())
        {
            if (!SatinalmaTalepYardimcisi.GercekTeklifVar(teklif))
                continue;

            var mevcut = hedef.Teklifler.FirstOrDefault(t => t.Id == teklif.Id);
            if (mevcut is null)
            {
                if (!kazananDahaYeni)
                    hedef.Teklifler.Add(teklif);
                continue;
            }

            if (TeklifDolulukSkoru(teklif) > TeklifDolulukSkoru(mevcut))
            {
                var idx = hedef.Teklifler.IndexOf(mevcut);
                if (idx >= 0)
                    hedef.Teklifler[idx] = teklif;
                else
                {
                    hedef.Teklifler.Remove(mevcut);
                    hedef.Teklifler.Add(teklif);
                }
            }
        }
    }

    private static int TeklifDolulukSkoru(SatinalmaTeklif teklif)
    {
        var skor = 0;
        if (!string.IsNullOrWhiteSpace(teklif.FirmaAdi))
            skor += 4;
        skor += (teklif.Fiyatlar?.Count(f => f.BirimFiyat > 0) ?? 0) * 3;
        if (teklif.GenelToplam > 0)
            skor += 5;
        return skor;
    }

    private static int Skor(SatinalmaTalep talep)
    {
        var skor = SatinalmaTalepDurumlari.SurecAsamaSkoru(talep.Durum);
        if (!string.IsNullOrWhiteSpace(talep.TalepNo))
            skor += 4;
        if (talep.Durum != SatinalmaTalepDurumlari.Taslak)
            skor += 8;
        skor += (talep.Kalemler?.Count ?? 0) * 3;
        skor += (talep.Teklifler?.Count ?? 0) * 5;
        if (talep.HerhangiKalemOnayli)
            skor += 10;
        if (!string.IsNullOrWhiteSpace(talep.YonetimOnaylayanUid))
            skor += 6;
        if (!string.IsNullOrWhiteSpace(talep.SiparisNo) || talep.FirmaSiparisNolari?.Count > 0)
            skor += 8;
        return skor;
    }

    private static DateTime TarihSira(string? tarih)
    {
        if (string.IsNullOrWhiteSpace(tarih))
            return DateTime.MinValue;

        if (DateTime.TryParse(tarih, out var dt))
            return dt;

        return DateTime.MinValue;
    }
}

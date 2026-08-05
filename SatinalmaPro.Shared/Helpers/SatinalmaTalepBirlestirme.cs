using SatinalmaPro.Shared.Models;
using SatinalmaPro.Shared.Procurement;

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
    /// Süreç durumu birleştirme: ileri aşama her zaman kazanır.
    /// Masaüstü «Dokun» ile daha yeni UTC ama İmza/Hazırlanıyor kalan kayıt,
    /// Android onay / teklif iste / red kararını ezemez.
    /// </summary>
    private static void SurecDurumunuBirlestir(SatinalmaTalep hedef, SatinalmaTalep kaynak)
    {
        if (ReferenceEquals(hedef, kaynak))
            return;

        BirlestirKararAlanlari(hedef, kaynak);

        var hedefAsama = SatinalmaTalepDurumlari.SurecAsamaSkoru(hedef.Durum);
        var kaynakAsama = SatinalmaTalepDurumlari.SurecAsamaSkoru(kaynak.Durum);

        // Red hedefte kaldıysa yalnızca daha yeni ve daha ileri aşama (nadir yeniden açılış) ezebilir.
        if (hedef.Durum == SatinalmaTalepDurumlari.Reddedildi
            && kaynak.Durum != SatinalmaTalepDurumlari.Reddedildi)
        {
            if (kaynak.GuncellemeUtc > hedef.GuncellemeUtc && kaynakAsama > hedefAsama)
                UygulaSurecDurumu(hedef, kaynak);
            return;
        }

        // Yönetim «teklif iste» / «revizeye gönder»: Teklif Girişi kazanır.
        if (TeklifIstemeGecisiMi(hedef, kaynak))
        {
            UygulaSurecDurumu(hedef, kaynak);
            return;
        }

        // Revize / teklif iste sonrası Teklif Girişi'ni stale Yönetim Onayına geri çekme.
        if (TeklifIstemeKorumaMi(hedef, kaynak))
            return;

        // Sipariş / onay geri alma: stale «Sipariş Oluşturuldu» skorla geri alma.
        if (SiparisGeriAlKorumaMi(hedef, kaynak))
            return;

        // Geri alınmış kopya kazanan değilse (UTC eşit / skor) Sipariş'ten düşür.
        if (SiparisGeriAlGecisiMi(hedef, kaynak))
        {
            UygulaSurecDurumu(hedef, kaynak);
            return;
        }

        // Teklifler yönetime gönderildi: Karşılaştırma → Yonetim Onayında (teklifli, daha yeni).
        if (TeklifYonetimIncelemeGecisiMi(hedef, kaynak))
        {
            UygulaSurecDurumu(hedef, kaynak);
            return;
        }

        // İleri aşama — UTC'den bağımsız (Android kararları masaüstü stale İmza'yı geçer).
        if (kaynakAsama > hedefAsama)
        {
            UygulaSurecDurumu(hedef, kaynak);
            return;
        }

        // Aynı aşama, kaynak daha yeni: durum metni / status hizası.
        if (kaynakAsama == hedefAsama
            && kaynak.GuncellemeUtc > hedef.GuncellemeUtc
            && !string.Equals(kaynak.Durum, hedef.Durum, StringComparison.Ordinal))
        {
            UygulaSurecDurumu(hedef, kaynak);
        }
    }

    private static bool TeklifIstemeGecisiMi(SatinalmaTalep hedef, SatinalmaTalep kaynak)
    {
        if (kaynak.Durum != SatinalmaTalepDurumlari.TeklifGirisi)
            return false;
        if (hedef.Durum is not (SatinalmaTalepDurumlari.YonetimOnayinda
            or SatinalmaTalepDurumlari.ImzaSurecinde
            or SatinalmaTalepDurumlari.Hazirlaniyor
            or SatinalmaTalepDurumlari.Karsilastirma))
            return false;

        if (!SatinalmaTalepYardimcisi.GercekTeklifVar(hedef))
            return true;

        if (!RevizeTeklifGirisiMi(kaynak))
            return false;
        if (hedef.Durum == SatinalmaTalepDurumlari.YonetimOnayinda
            && hedef.GuncellemeUtc > kaynak.GuncellemeUtc
            && string.IsNullOrWhiteSpace(hedef.TeklifDuzeltmeNotu)
            && SatinalmaTalepYardimcisi.GercekTeklifVar(hedef))
            return false;
        return true;
    }

    private static bool TeklifIstemeKorumaMi(SatinalmaTalep hedef, SatinalmaTalep kaynak)
    {
        if (kaynak.Durum is not (SatinalmaTalepDurumlari.YonetimOnayinda
            or SatinalmaTalepDurumlari.ImzaSurecinde
            or SatinalmaTalepDurumlari.Hazirlaniyor))
            return false;

        // Revize: Teklif Girişi / Karşılaştırma + not — skor ile yönetime geri alma.
        if ((hedef.Durum is SatinalmaTalepDurumlari.TeklifGirisi
                or SatinalmaTalepDurumlari.Karsilastirma)
            && !string.IsNullOrWhiteSpace(hedef.TeklifDuzeltmeNotu))
            return true;

        if (hedef.Durum == SatinalmaTalepDurumlari.TeklifGirisi && RevizeTeklifGirisiMi(hedef))
            return true;

        if (hedef.Durum != SatinalmaTalepDurumlari.TeklifGirisi)
            return false;

        return !SatinalmaTalepYardimcisi.GercekTeklifVar(kaynak);
    }

    /// <summary>
    /// Siparişi/onayı geri alınmış kayıt: kaynak hâlâ Sipariş Oluşturuldu ise skor ile geri alma.
    /// Yalnız UTC — stale Status ile yeni siparişi engelleme.
    /// </summary>
    private static bool SiparisGeriAlKorumaMi(SatinalmaTalep hedef, SatinalmaTalep kaynak)
    {
        if (kaynak.Durum != SatinalmaTalepDurumlari.SiparisOlusturuldu)
            return false;
        if (hedef.Durum is not (SatinalmaTalepDurumlari.Onaylandi
            or SatinalmaTalepDurumlari.Karsilastirma
            or SatinalmaTalepDurumlari.TeklifGirisi))
            return false;

        return hedef.GuncellemeUtc >= kaynak.GuncellemeUtc;
    }

    private static bool SiparisGeriAlGecisiMi(SatinalmaTalep hedef, SatinalmaTalep kaynak)
    {
        if (hedef.Durum != SatinalmaTalepDurumlari.SiparisOlusturuldu)
            return false;
        if (kaynak.Durum is not (SatinalmaTalepDurumlari.Onaylandi
            or SatinalmaTalepDurumlari.Karsilastirma
            or SatinalmaTalepDurumlari.TeklifGirisi))
            return false;

        return kaynak.GuncellemeUtc >= hedef.GuncellemeUtc;
    }

    private static bool TeklifYonetimIncelemeGecisiMi(SatinalmaTalep hedef, SatinalmaTalep kaynak)
    {
        if (kaynak.Durum != SatinalmaTalepDurumlari.YonetimOnayinda)
            return false;
        if (hedef.Durum != SatinalmaTalepDurumlari.Karsilastirma)
            return false;
        if (RevizeTeklifGirisiMi(hedef) || !string.IsNullOrWhiteSpace(hedef.TeklifDuzeltmeNotu))
            return false;
        if (!SatinalmaTalepYardimcisi.GercekTeklifVar(kaynak)
            && !SatinalmaTalepYardimcisi.GercekTeklifVar(hedef))
            return false;

        return kaynak.GuncellemeUtc >= hedef.GuncellemeUtc;
    }

    private static bool RevizeTeklifGirisiMi(SatinalmaTalep t) =>
        t.Durum == SatinalmaTalepDurumlari.TeklifGirisi
        && (!string.IsNullOrWhiteSpace(t.TeklifDuzeltmeNotu)
            || (SatinalmaTalepYardimcisi.GercekTeklifVar(t)
                && string.Equals(t.Status, ProcurementStatus.QuoteRequested, StringComparison.OrdinalIgnoreCase)));

    private static void UygulaSurecDurumu(SatinalmaTalep hedef, SatinalmaTalep kaynak)
    {
        hedef.Durum = kaynak.Durum;
        if (!string.IsNullOrWhiteSpace(kaynak.Status))
            hedef.Status = kaynak.Status;

        hedef.TeklifsizYonetimOnayi = kaynak.TeklifsizYonetimOnayi;
        hedef.YonetimOnayKilitli = kaynak.YonetimOnayKilitli;

        if (!string.IsNullOrWhiteSpace(kaynak.RedGerekcesi))
            hedef.RedGerekcesi = kaynak.RedGerekcesi;
        if (kaynak.OnaylananTeklifId is { } onayId)
            hedef.OnaylananTeklifId = onayId;
        else if (kaynak.Durum == SatinalmaTalepDurumlari.TeklifGirisi)
            hedef.OnaylananTeklifId = null;

        if (!string.IsNullOrWhiteSpace(kaynak.YonetimOnaylayanUid))
        {
            hedef.YonetimOnaylayanUid = kaynak.YonetimOnaylayanUid;
            hedef.YonetimOnaylayanAd = kaynak.YonetimOnaylayanAd;
            hedef.YonetimOnaylayanEposta = kaynak.YonetimOnaylayanEposta;
            hedef.YonetimOnayTarihi = kaynak.YonetimOnayTarihi;
        }

        if (!string.IsNullOrWhiteSpace(kaynak.TeklifDuzeltmeNotu))
            hedef.TeklifDuzeltmeNotu = kaynak.TeklifDuzeltmeNotu;
        else if (kaynak.Durum == SatinalmaTalepDurumlari.YonetimOnayinda)
            hedef.TeklifDuzeltmeNotu = "";
    }

    private static void BirlestirKararAlanlari(SatinalmaTalep hedef, SatinalmaTalep kaynak)
    {
        // Onay kilidi burada OR ile yapışkan kalmasın — SurecDurumu uygular.
        if (kaynak.TeklifsizYonetimOnayi)
            hedef.TeklifsizYonetimOnayi = true;
        if (string.IsNullOrWhiteSpace(hedef.RedGerekcesi)
            && !string.IsNullOrWhiteSpace(kaynak.RedGerekcesi))
            hedef.RedGerekcesi = kaynak.RedGerekcesi;
        if (hedef.OnaylananTeklifId is null && kaynak.OnaylananTeklifId is { } id)
            hedef.OnaylananTeklifId = id;
        if (string.IsNullOrWhiteSpace(hedef.YonetimOnaylayanUid)
            && !string.IsNullOrWhiteSpace(kaynak.YonetimOnaylayanUid))
        {
            hedef.YonetimOnaylayanUid = kaynak.YonetimOnaylayanUid;
            hedef.YonetimOnaylayanAd = kaynak.YonetimOnaylayanAd;
            hedef.YonetimOnaylayanEposta = kaynak.YonetimOnaylayanEposta;
            hedef.YonetimOnayTarihi = kaynak.YonetimOnayTarihi;
        }
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

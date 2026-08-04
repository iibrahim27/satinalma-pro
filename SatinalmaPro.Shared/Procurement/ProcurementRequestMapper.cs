using SatinalmaPro.Shared.Helpers;
using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Shared.Procurement;

public static class ProcurementRequestMapper
{
    public static ProcurementRequestSnapshot FromTalep(SatinalmaTalep talep) => new()
    {
        Id = talep.Id.ToString(),
        Status = ProcurementStatusResolver.Resolve(talep),
        RequesterUid = talep.OlusturanUid ?? "",
        Priority = ProcurementPriority.FromRequestType(talep.TalepTuru),
        RequestType = talep.TalepTuru ?? "Normal"
    };

    public static bool HasReturnFlag(SatinalmaTalep talep) => talep.HasReturnFlag;
}

/// <summary>
/// Legacy <see cref="SatinalmaTalep.Durum"/> + teklif durumundan enterprise status türetir.
/// Sekme filtreleri için <see cref="SatinalmaTalep.Durum"/> kaynağıdır; eski/stale
/// <c>status</c> alanı (ör. quote_requested kalmışken Durum=Karşılaştırma) yok sayılır.
/// </summary>
public static class ProcurementStatusResolver
{
    public static string Resolve(SatinalmaTalep talep)
    {
        // Durum boşsa (nadir) kayıtlı Status'e düş.
        if (string.IsNullOrWhiteSpace(talep.Durum) && !string.IsNullOrWhiteSpace(talep.Status))
            return ProcurementStatus.Normalize(talep.Status);

        if (talep.Durum == SatinalmaTalepDurumlari.Taslak)
            return ProcurementStatus.Draft;

        if (talep.Durum == SatinalmaTalepDurumlari.Reddedildi)
            return ProcurementStatus.Rejected;

        if (talep.Durum == SatinalmaTalepDurumlari.SiparisOlusturuldu)
            return MalKabulTamamlandi(talep) ? ProcurementStatus.Completed : ProcurementStatus.Ordered;

        if (talep.Durum == SatinalmaTalepDurumlari.Onaylandi)
            return ProcurementStatus.Approved;

        if (talep.Durum == SatinalmaTalepDurumlari.Karsilastirma)
        {
            // Eski revize kayıtları (Karşılaştırma + düzeltme notu) Teklif İstemi'nde kalsın.
            if (SatinalmaTalepYardimcisi.TeklifDuzeltmeBekliyor(talep))
                return ProcurementStatus.QuoteRequested;
            return ProcurementStatus.Comparison;
        }

        if (talep.Durum == SatinalmaTalepDurumlari.TeklifGirisi)
        {
            if (SatinalmaTalepYardimcisi.TeklifDuzeltmeBekliyor(talep)
                || SatinalmaTalepKuyrugu.YonetimTeklifBekleyen(talep))
                return ProcurementStatus.QuoteRequested;
            return ProcurementStatus.QuoteEntry;
        }

        if (talep.Durum == SatinalmaTalepDurumlari.YonetimOnayinda
            && (talep.Teklifler?.Count ?? 0) > 0
            && !talep.HerhangiKalemOnayli)
            return ProcurementStatus.ManagementQuoteReview;

        // Kalem/teklif onayı yapılmış ama Durum güncellenmemiş kayıtlar.
        if (talep.Durum == SatinalmaTalepDurumlari.YonetimOnayinda
            && (talep.HerhangiKalemOnayli || talep.TeklifsizYonetimOnayi || talep.YonetimOnayKilitli))
            return ProcurementStatus.Approved;

        if (talep.Durum is SatinalmaTalepDurumlari.Hazirlaniyor
            or SatinalmaTalepDurumlari.ImzaSurecinde
            or SatinalmaTalepDurumlari.YonetimOnayinda)
            return ProcurementStatus.Submitted;

        return ProcurementStatus.Normalize(talep.Durum);
    }

    /// <summary>
    /// Status ileri aşamadaysa stale Durum'u yükseltir; sonra Durum→Status hizalar.
    /// Android kararları Status'te kalıp Durum İmza'da takılı kaldığında masaüstü etiketi düzelir.
    /// </summary>
    public static bool SenkronizeEt(SatinalmaTalep talep)
    {
        var degisti = DurumuStatusTenYukselt(talep);
        var dogru = Resolve(talep);
        if (!string.Equals(talep.Status, dogru, StringComparison.OrdinalIgnoreCase))
        {
            talep.Status = dogru;
            degisti = true;
        }
        return degisti;
    }

    /// <summary>
    /// Kayıtlı enterprise Status, legacy Durum'dan ileriyse Durum'u yükselt.
    /// Stale Status (ör. quote_requested + Durum=Karşılaştırma) geri çekmez.
    /// </summary>
    public static bool DurumuStatusTenYukselt(SatinalmaTalep talep)
    {
        if (string.IsNullOrWhiteSpace(talep.Status))
            return false;

        var status = ProcurementStatus.Normalize(talep.Status);

        // quote_requested skoru YonetimOnayinda'dan düşük; teklifsiz yönetim→teklif iste özel geçişi.
        if ((status is ProcurementStatus.QuoteRequested or ProcurementStatus.QuoteEntry)
            && (talep.Durum is SatinalmaTalepDurumlari.YonetimOnayinda
                or SatinalmaTalepDurumlari.ImzaSurecinde
                or SatinalmaTalepDurumlari.Hazirlaniyor)
            && !SatinalmaTalepYardimcisi.GercekTeklifVar(talep))
        {
            talep.Durum = SatinalmaTalepDurumlari.TeklifGirisi;
            return true;
        }

        var durumAsama = SatinalmaTalepDurumlari.SurecAsamaSkoru(talep.Durum);
        var statusAsama = StatusAsamaSkoru(status);
        if (statusAsama <= durumAsama)
            return false;

        var yeniDurum = status switch
        {
            ProcurementStatus.Rejected => SatinalmaTalepDurumlari.Reddedildi,
            ProcurementStatus.Approved => SatinalmaTalepDurumlari.Onaylandi,
            ProcurementStatus.Ordered or ProcurementStatus.Completed => SatinalmaTalepDurumlari.SiparisOlusturuldu,
            ProcurementStatus.Comparison => SatinalmaTalepDurumlari.Karsilastirma,
            ProcurementStatus.QuoteRequested or ProcurementStatus.QuoteEntry => SatinalmaTalepDurumlari.TeklifGirisi,
            ProcurementStatus.ManagementQuoteReview => SatinalmaTalepDurumlari.YonetimOnayinda,
            _ => null
        };

        if (yeniDurum is null || string.Equals(talep.Durum, yeniDurum, StringComparison.Ordinal))
            return false;

        talep.Durum = yeniDurum;
        return true;
    }

    private static int StatusAsamaSkoru(string status) => status switch
    {
        ProcurementStatus.Completed => 95,
        ProcurementStatus.Ordered => 90,
        ProcurementStatus.Approved => 70,
        ProcurementStatus.Rejected => 65,
        ProcurementStatus.ManagementQuoteReview => 60,
        ProcurementStatus.Comparison => 50,
        ProcurementStatus.QuoteEntry => 40,
        ProcurementStatus.QuoteRequested => 40,
        ProcurementStatus.Submitted => 30,
        ProcurementStatus.Draft => 0,
        _ => 0
    };

    public static bool SenkronizeEt(IEnumerable<SatinalmaTalep> talepler)
    {
        var degisti = false;
        foreach (var t in talepler)
            degisti |= SenkronizeEt(t);
        return degisti;
    }

    private static bool MalKabulTamamlandi(SatinalmaTalep talep)
    {
        talep.Kalemler ??= [];
        var kalemler = talep.Kalemler.Where(k => !string.IsNullOrWhiteSpace(k.Malzeme)).ToList();
        if (kalemler.Count == 0)
            return false;

        return kalemler.All(k =>
            k.SiparisTamamlandi || k.KabulEdilenMiktar >= k.Miktar - 0.0001);
    }
}

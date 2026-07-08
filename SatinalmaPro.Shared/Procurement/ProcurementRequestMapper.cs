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
            return ProcurementStatus.Comparison;

        if (talep.Durum == SatinalmaTalepDurumlari.TeklifGirisi)
        {
            return SatinalmaTalepKuyrugu.YonetimTeklifBekleyen(talep)
                ? ProcurementStatus.QuoteRequested
                : ProcurementStatus.QuoteEntry;
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
    /// Durum'dan türetilen Status'ü talep üzerine yazar.
    /// Eski kayıtların sekmelerde kaybolmasını önler; buluta bir sonraki kayıtta yansır.
    /// </summary>
    public static bool SenkronizeEt(SatinalmaTalep talep)
    {
        var dogru = Resolve(talep);
        if (string.Equals(talep.Status, dogru, StringComparison.OrdinalIgnoreCase))
            return false;
        talep.Status = dogru;
        return true;
    }

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

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
/// Firestore <c>status</c> alanı varsa önceliklidir.
/// </summary>
public static class ProcurementStatusResolver
{
    public static string Resolve(SatinalmaTalep talep)
    {
        if (!string.IsNullOrWhiteSpace(talep.Status))
            return ProcurementStatus.Normalize(talep.Status);

        if (talep.Durum == SatinalmaTalepDurumlari.Taslak)
            return ProcurementStatus.Draft;

        if (talep.Durum == SatinalmaTalepDurumlari.Reddedildi)
            return ProcurementStatus.Rejected;

        if (talep.Durum == SatinalmaTalepDurumlari.SiparisOlusturuldu)
            return ProcurementStatus.Ordered;

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

        if (talep.Durum is SatinalmaTalepDurumlari.Hazirlaniyor
            or SatinalmaTalepDurumlari.ImzaSurecinde
            or SatinalmaTalepDurumlari.YonetimOnayinda)
            return ProcurementStatus.Submitted;

        if (talep.Durum == SatinalmaTalepDurumlari.SiparisOlusturuldu
            && MalKabulTamamlandi(talep))
            return ProcurementStatus.Completed;

        return ProcurementStatus.Normalize(talep.Durum);
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

using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Shared.Procurement;

namespace SatinalmaPro.Helpers;

/// <summary>
/// Masaüstü <see cref="SatinalmaTalep"/> için enterprise status çözümlemesi.
/// </summary>
public static class ProcurementTalepAdapter
{
    public static string ResolveStatus(SatinalmaTalep talep)
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

        if (talep.Durum == SatinalmaTalepDurumlari.SiparisOlusturuldu && MalKabulTamam(talep))
            return ProcurementStatus.Completed;

        return ProcurementStatus.Normalize(talep.Durum);
    }

    public static string EffectivePriority(SatinalmaTalep talep)
    {
        if (!string.IsNullOrWhiteSpace(talep.Priority)
            && !talep.Priority.Equals(ProcurementPriority.Normal, StringComparison.OrdinalIgnoreCase))
            return talep.Priority;

        return ProcurementPriority.FromRequestType(talep.TalepTuru);
    }

    public static bool HasReturn(SatinalmaTalep talep) => talep.HasReturnFlag;

    public static ProcurementRequestSnapshot ToSnapshot(SatinalmaTalep talep) => new()
    {
        Id = talep.Id.ToString(),
        Status = ResolveStatus(talep),
        RequesterUid = talep.OlusturanUid ?? "",
        Priority = EffectivePriority(talep),
        RequestType = talep.TalepTuru ?? "Normal"
    };

    private static bool MalKabulTamam(SatinalmaTalep talep)
    {
        var kalemler = (talep.Kalemler ?? [])
            .Where(k => !string.IsNullOrWhiteSpace(k.Malzeme))
            .ToList();

        if (kalemler.Count == 0)
            return false;

        return kalemler.All(k =>
            k.SiparisTamamlandi || k.KabulEdilenMiktar >= k.Miktar - 0.0001);
    }
}

using SatinalmaPro.Models;
using SatinalmaPro.Shared.Procurement;

namespace SatinalmaPro.Helpers;

/// <summary>
/// Masaüstü <see cref="SatinalmaTalep"/> için enterprise status.
/// Sekme filtreleri Durum kaynaklıdır; stale Status yok sayılır.
/// </summary>
public static class ProcurementTalepAdapter
{
    public static string ResolveStatus(SatinalmaTalep talep)
    {
        if (string.IsNullOrWhiteSpace(talep.Durum) && !string.IsNullOrWhiteSpace(talep.Status))
            return ProcurementStatus.Normalize(talep.Status);

        if (talep.Durum == SatinalmaTalepDurumlari.Taslak)
            return ProcurementStatus.Draft;

        if (talep.Durum == SatinalmaTalepDurumlari.Reddedildi)
            return ProcurementStatus.Rejected;

        if (talep.Durum == SatinalmaTalepDurumlari.SiparisOlusturuldu)
            return MalKabulTamam(talep) ? ProcurementStatus.Completed : ProcurementStatus.Ordered;

        if (talep.Durum == SatinalmaTalepDurumlari.Onaylandi)
            return ProcurementStatus.Approved;

        if (talep.Durum == SatinalmaTalepDurumlari.Karsilastirma)
        {
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

        // Revize notu varken stale Yönetim Durum → quote_requested (senkron Durum'u da çeker).
        if (talep.Durum == SatinalmaTalepDurumlari.YonetimOnayinda
            && !string.IsNullOrWhiteSpace(talep.TeklifDuzeltmeNotu)
            && SatinalmaTalepYardimcisi.GercekTeklifVar(talep)
            && !talep.HerhangiKalemOnayli
            && !talep.YonetimOnayKilitli)
            return ProcurementStatus.QuoteRequested;

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

    public static bool StatusSenkronizeEt(SatinalmaTalep talep)
    {
        // Status ileri ise stale Durum'u yükselt; sonra Durum→Status hizala.
        var degisti = DurumuStatusTenYukselt(talep);
        var dogru = ResolveStatus(talep);
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

        // quote_requested skoru YonetimOnayinda'dan düşük; teklif iste / revize yine Teklif Girişi.
        if ((status is ProcurementStatus.QuoteRequested or ProcurementStatus.QuoteEntry)
            && (talep.Durum is SatinalmaTalepDurumlari.YonetimOnayinda
                or SatinalmaTalepDurumlari.ImzaSurecinde
                or SatinalmaTalepDurumlari.Hazirlaniyor)
            && (!SatinalmaTalepYardimcisi.GercekTeklifVar(talep)
                || !string.IsNullOrWhiteSpace(talep.TeklifDuzeltmeNotu)))
        {
            talep.Durum = SatinalmaTalepDurumlari.TeklifGirisi;
            return true;
        }

        // Stale management_quote_review + revize notu → yönetime yükseltme.
        if (status == ProcurementStatus.ManagementQuoteReview
            && !string.IsNullOrWhiteSpace(talep.TeklifDuzeltmeNotu)
            && !talep.YonetimOnayKilitli)
        {
            if (talep.Durum is SatinalmaTalepDurumlari.TeklifGirisi
                or SatinalmaTalepDurumlari.Karsilastirma)
                return false;
            if (talep.Durum is SatinalmaTalepDurumlari.YonetimOnayinda
                or SatinalmaTalepDurumlari.ImzaSurecinde
                or SatinalmaTalepDurumlari.Hazirlaniyor)
            {
                talep.Durum = SatinalmaTalepDurumlari.TeklifGirisi;
                return true;
            }
        }

        var durumAsama = SatinalmaPro.Shared.Models.SatinalmaTalepDurumlari.SurecAsamaSkoru(talep.Durum);
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

    public static bool StatusSenkronizeEt(IEnumerable<SatinalmaTalep> talepler)
    {
        var degisti = false;
        foreach (var t in talepler)
            degisti |= StatusSenkronizeEt(t);
        return degisti;
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

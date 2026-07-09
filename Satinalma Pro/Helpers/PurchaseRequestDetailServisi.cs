using SatinalmaPro.Models;
using SatinalmaPro.Services;
using SatinalmaPro.Services.Firebase;
using SatinalmaPro.Shared.Procurement;
using SatinalmaPro.Shared.Procurement.Detail;

namespace SatinalmaPro.Helpers;

/// <summary>
/// Talep detay aksiyonlarını modele uygular, yerel/bulut kaydeder ve enterprise Firestore alanlarını günceller.
/// </summary>
public static class PurchaseRequestDetailServisi
{
    public static PurchaseRequestDetailUiState UiDurumu(
        SatinalmaTalep talep,
        string? rol,
        PurchaseRequestDetailScreen? screen = null) =>
        PurchaseRequestDetailPresenter.BuildUiState(PaylasimaCevir(talep), rol, screen);

    public static IReadOnlyList<PurchaseRequestQuoteRow> TeklifSatirlari(SatinalmaTalep talep, string? rol) =>
        PurchaseRequestDetailPresenter.BuildQuoteRows(PaylasimaCevir(talep), rol);

    public static async Task UygulaAsync(
        SatinalmaTalep talep,
        PurchaseRequestDetailAction action,
        string? rol,
        string? quoteId = null,
        string? not = null,
        CancellationToken iptal = default)
    {
        var mutation = PurchaseRequestDetailPresenter.CreateMutation(
                action, PaylasimaCevir(talep), rol, quoteId, not)
            ?? throw new InvalidOperationException("Bu aksiyon şu an uygulanamaz.");

        MutasyonuModeleUygula(talep, mutation, rol);
        await KaydetVeEnterpriseGuncelleAsync(talep, mutation, iptal);
        await AksiyonSonrasiBildirimlerAsync(talep, action, not);
    }

    internal static void MutasyonuModeleUygula(
        SatinalmaTalep talep,
        PurchaseRequestDetailMutation mutation,
        string? rol)
    {
        talep.Status = mutation.NewStatus;
        talep.Priority = PurchaseRequestDetailPresenter.ResolvePriority(PaylasimaCevir(talep));

        if (!string.IsNullOrWhiteSpace(mutation.NewLegacyDurum))
            talep.Durum = mutation.NewLegacyDurum;

        if (mutation.TeklifsizYonetimOnayi)
            talep.TeklifsizYonetimOnayi = true;

        talep.YonetimOnayKilitli = mutation.YonetimOnayKilitli;

        if (mutation.RejectionReason is not null)
            talep.RedGerekcesi = mutation.RejectionReason;

        if (mutation.QuoteCorrectionNote is not null)
            talep.TeklifDuzeltmeNotu = mutation.QuoteCorrectionNote;

        if (mutation.ClearApprovedQuote)
            talep.OnaylananTeklifId = null;

        talep.Kalemler ??= [];
        talep.Teklifler ??= [];

        if (mutation.ClearLineItemApprovals)
        {
            foreach (var kalem in talep.Kalemler)
                kalem.OnaylananTeklifId = null;
        }

        if (!string.IsNullOrWhiteSpace(mutation.ApprovedQuoteId)
            && Guid.TryParse(mutation.ApprovedQuoteId, out var onayTeklifId))
        {
            talep.OnaylananTeklifId = onayTeklifId;
            talep.YonetimOnerilenTeklifId = onayTeklifId;

            if (mutation.ApplyQuoteToAllLineItems)
            {
                foreach (var kalem in talep.Kalemler)
                    kalem.OnaylananTeklifId = onayTeklifId;
            }

            foreach (var teklif in talep.Teklifler)
                teklif.Onaylandi = teklif.Id == onayTeklifId;
        }

        if (mutation.NewStatus == ProcurementStatus.Approved
            && PurchaseRequestDetailPresenter.CanManagementDecide(rol))
        {
            YonetimOnayKaydiUygula(talep);
        }

        if (mutation.NewStatus == ProcurementStatus.QuoteRequested)
        {
            talep.TeklifsizYonetimOnayi = false;
            talep.YonetimOnayKilitli = false;
        }

        talep.GuncellemeUtc = mutation.UpdatedAtUtcMs;
        SatinalmaTalepYardimcisi.Dokun(talep);
    }

    private static void YonetimOnayKaydiUygula(SatinalmaTalep talep)
    {
        var k = OturumYoneticisi.AktifKullanici;
        talep.YonetimOnaylayanUid = k?.Uid ?? "";
        talep.YonetimOnaylayanAd = k?.AdSoyad ?? "";
        talep.YonetimOnaylayanEposta = k?.Eposta ?? "";
        talep.YonetimOnayTarihi = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
    }

    private static async Task KaydetVeEnterpriseGuncelleAsync(
        SatinalmaTalep talep,
        PurchaseRequestDetailMutation mutation,
        CancellationToken iptal)
    {
        SatinalmaDepo.Kaydet();
        await SatinalmaKayitYardimcisi.BulutaHemenGonderAsync();

        if (!OturumYoneticisi.BulutAktif || OturumYoneticisi.Firestore is null)
            return;

        try
        {
            var patch = PurchaseRequestFirestorePatch.FromMutation(
                mutation,
                PurchaseRequestDetailPresenter.ResolvePriority(PaylasimaCevir(talep)));

            await OturumYoneticisi.Firestore.ProcurementRequestAlanlariGuncelleAsync(
                talep.Id.ToString(),
                patch,
                iptal);
        }
        catch (Exception ex)
        {
            HataGunlugu.Kaydet(ex, "PurchaseRequestDetailServisi.EnterpriseGuncelle");
        }
    }

    private static async Task AksiyonSonrasiBildirimlerAsync(
        SatinalmaTalep talep,
        PurchaseRequestDetailAction action,
        string? not)
    {
        try
        {
            switch (action)
            {
                case PurchaseRequestDetailAction.DirectApprove:
                case PurchaseRequestDetailAction.ApproveQuote:
                    await SatinalmaBildirimleri.OnaylandiBildirimleriGonderAsync(talep);
                    break;
                case PurchaseRequestDetailAction.StartQuoteProcess:
                    await SatinalmaBildirimleri.TeklifIstendiAsync(talep);
                    if (!string.IsNullOrWhiteSpace(talep.OlusturanUid))
                        await SatinalmaBildirimleri.TeklifIstendiOlusturucuyaAsync(talep);
                    break;
                case PurchaseRequestDetailAction.RejectRequest:
                case PurchaseRequestDetailAction.RejectEntireRequest:
                    await SatinalmaBildirimleri.ReddedildiAsync(talep, not ?? "");
                    break;
                case PurchaseRequestDetailAction.SendQuotesForRevision:
                    await SatinalmaBildirimleri.TeklifDuzeltmeyeGonderildiAsync(talep, not ?? "");
                    break;
            }

            await BildirimYoneticisi.GecersizleriOkunduYapAsync();
        }
        catch (Exception ex)
        {
            HataGunlugu.Kaydet(ex, "PurchaseRequestDetailServisi.Bildirim");
        }
    }

    private static SatinalmaPro.Shared.Models.SatinalmaTalep PaylasimaCevir(SatinalmaTalep talep)
    {
        // CamelCase + case-sensitive deserialize Durum/Status kaybediyordu → butonlar Collapsed.
        // DesktopRoleTabManager.TalepPaylasimaCevir ile aynı kural.
        var opts = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        var json = System.Text.Json.JsonSerializer.Serialize(talep);
        return System.Text.Json.JsonSerializer.Deserialize<SatinalmaPro.Shared.Models.SatinalmaTalep>(json, opts)
               ?? new SatinalmaPro.Shared.Models.SatinalmaTalep();
    }
}

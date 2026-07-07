using SatinalmaPro.Shared.Helpers;
using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Shared.Procurement.Detail;

/// <summary>
/// Talep detay ekranı — durum, öncelik ve role göre buton görünürlüğü.
/// Masaüstü ve Android ortak presenter.
/// </summary>
public static class PurchaseRequestDetailPresenter
{
    private static readonly IReadOnlyDictionary<PurchaseRequestDetailAction, string> DefaultLabels =
        new Dictionary<PurchaseRequestDetailAction, string>
        {
            [PurchaseRequestDetailAction.DirectApprove] = "Direkt Onay Ver",
            [PurchaseRequestDetailAction.RejectRequest] = "Talebi Reddet",
            [PurchaseRequestDetailAction.StartQuoteProcess] = "Teklif Sürecini Başlat",
            [PurchaseRequestDetailAction.ApproveQuote] = "Bu Firmayı Onayla",
            [PurchaseRequestDetailAction.RejectEntireRequest] = "Talebi Komple Reddet",
            [PurchaseRequestDetailAction.SendQuotesForRevision] = "Teklifleri Revizeye Gönder"
        };

    public static PurchaseRequestDetailScreen ResolveScreen(SatinalmaTalep talep)
    {
        var status = ProcurementStatusResolver.Resolve(talep);
        return status.Equals(ProcurementStatus.ManagementQuoteReview, StringComparison.OrdinalIgnoreCase)
            ? PurchaseRequestDetailScreen.ManagementQuoteReview
            : PurchaseRequestDetailScreen.ManagementSubmittedReview;
    }

    public static string ResolvePriority(SatinalmaTalep talep)
    {
        if (!string.IsNullOrWhiteSpace(talep.Priority)
            && !talep.Priority.Equals(ProcurementPriority.Normal, StringComparison.OrdinalIgnoreCase))
            return ProcurementPriority.Normalize(talep.Priority);

        return ProcurementPriority.FromRequestType(talep.TalepTuru);
    }

    public static PurchaseRequestDetailUiState BuildUiState(
        SatinalmaTalep talep,
        string? role,
        PurchaseRequestDetailScreen? screen = null)
    {
        var resolvedScreen = screen ?? ResolveScreen(talep);
        var status = ProcurementStatusResolver.Resolve(talep);
        var priority = ResolvePriority(talep);
        var canManage = CanManagementDecide(role);

        var actions = new List<PurchaseRequestDetailAction>();
        var labels = new Dictionary<PurchaseRequestDetailAction, string>(DefaultLabels);

        if (canManage && resolvedScreen == PurchaseRequestDetailScreen.ManagementSubmittedReview
            && status.Equals(ProcurementStatus.Submitted, StringComparison.OrdinalIgnoreCase))
        {
            var urgent = priority.Equals(ProcurementPriority.Urgent, StringComparison.OrdinalIgnoreCase);

            if (urgent)
            {
                actions.Add(PurchaseRequestDetailAction.DirectApprove);
                actions.Add(PurchaseRequestDetailAction.RejectRequest);
            }
            else
            {
                actions.Add(PurchaseRequestDetailAction.StartQuoteProcess);
                labels[PurchaseRequestDetailAction.RejectRequest] = "Talebi Reddet";
                actions.Add(PurchaseRequestDetailAction.RejectRequest);
            }
        }

        var showQuotes = false;
        var showPerQuoteApprove = false;

        if (canManage && resolvedScreen == PurchaseRequestDetailScreen.ManagementQuoteReview
            && status.Equals(ProcurementStatus.ManagementQuoteReview, StringComparison.OrdinalIgnoreCase))
        {
            showQuotes = (talep.Teklifler?.Count ?? 0) > 0;
            showPerQuoteApprove = showQuotes && !talep.YonetimOnayKilitli && !talep.HerhangiKalemOnayli;
            actions.Add(PurchaseRequestDetailAction.RejectEntireRequest);
            actions.Add(PurchaseRequestDetailAction.SendQuotesForRevision);
        }

        return new PurchaseRequestDetailUiState
        {
            RequestId = talep.Id.ToString(),
            Status = status,
            Priority = priority,
            Screen = resolvedScreen,
            VisibleActions = actions,
            ActionLabels = labels,
            ShowQuotesList = showQuotes,
            ShowPerQuoteApproveButtons = showPerQuoteApprove,
            RequiresRejectionReason = true,
            RequiresRevisionNote = true
        };
    }

    public static IReadOnlyList<PurchaseRequestQuoteRow> BuildQuoteRows(
        SatinalmaTalep talep,
        string? role)
    {
        var ui = BuildUiState(talep, role, PurchaseRequestDetailScreen.ManagementQuoteReview);
        if (!ui.ShowPerQuoteApproveButtons)
            return [];

        talep.Teklifler ??= [];
        return talep.Teklifler
            .Where(t => !string.IsNullOrWhiteSpace(t.FirmaAdi)
                        || t.Fiyatlar?.Any(f => f.BirimFiyat > 0) == true)
            .Select(t => new PurchaseRequestQuoteRow
            {
                QuoteId = t.Id.ToString(),
                FirmName = string.IsNullOrWhiteSpace(t.FirmaAdi) ? "—" : t.FirmaAdi,
                CanApprove = ui.ShowPerQuoteApproveButtons
            })
            .ToList();
    }

    public static PurchaseRequestDetailMutation? CreateMutation(
        PurchaseRequestDetailAction action,
        SatinalmaTalep talep,
        string? role,
        string? quoteId = null,
        string? note = null)
    {
        var ui = BuildUiState(talep, role);
        if (!ui.VisibleActions.Contains(action)
            && action != PurchaseRequestDetailAction.ApproveQuote)
            return null;

        if (action == PurchaseRequestDetailAction.ApproveQuote)
        {
            if (string.IsNullOrWhiteSpace(quoteId))
                return null;
            if (!ui.ShowPerQuoteApproveButtons)
                return null;
            return PurchaseRequestDetailMutation.ApproveQuote(quoteId);
        }

        return action switch
        {
            PurchaseRequestDetailAction.DirectApprove => PurchaseRequestDetailMutation.DirectApprove(),
            PurchaseRequestDetailAction.RejectRequest or PurchaseRequestDetailAction.RejectEntireRequest
                => PurchaseRequestDetailMutation.Reject(note ?? ""),
            PurchaseRequestDetailAction.StartQuoteProcess => PurchaseRequestDetailMutation.StartQuoteProcess(),
            PurchaseRequestDetailAction.SendQuotesForRevision
                => PurchaseRequestDetailMutation.SendForRevision(note ?? ""),
            _ => null
        };
    }

    public static bool CanManagementDecide(string? role)
    {
        var normalized = KullaniciRolleri.Normalize(role);
        return normalized is KullaniciRolleri.Admin or KullaniciRolleri.Yonetim;
    }
}

using SatinalmaPro.Shared.Helpers;
using SatinalmaPro.Shared.Models;
using SatinalmaPro.Shared.Services.Firebase;

namespace SatinalmaPro.Shared.Services;

public sealed class BildirimInboxServisi
{
    private readonly FirestoreVeriServisi _firestore;

    public BildirimInboxServisi(FirestoreVeriServisi firestore) => _firestore = firestore;

    public Task<List<NotificationInboxEntry>> InboxOkuAsync(string uid, int limit = 50, CancellationToken iptal = default) =>
        _firestore.InboxOkuAsync(uid, limit, iptal);

    public Task InboxOkunduIsaretleAsync(string uid, string inboxDocId, CancellationToken iptal = default) =>
        _firestore.InboxOkunduIsaretleAsync(uid, inboxDocId, iptal);

    public static BildirimKaydi InboxtenBildirimeDonustur(NotificationInboxEntry e)
    {
        var entityOrTalep = !string.IsNullOrWhiteSpace(e.EntityId) ? e.EntityId : e.TalepId;
        Guid? talepId = Guid.TryParse(entityOrTalep, out var id) ? id : null;
        var tip = CozLegacyTip(e.EventCode, e.Tip, e.Type);

        return new BildirimKaydi
        {
            Id = BildirimMantikAnahtari.InboxDocIddenGuid(e.DocId),
            Baslik = FirstNonEmpty(e.Title, e.Baslik),
            Mesaj = FirstNonEmpty(e.Message, e.Mesaj),
            Tip = tip,
            TalepId = talepId,
            HedefRol = FirstNonEmpty(e.HedefRol, e.TargetRole),
            HedefUid = FirstNonEmpty(e.HedefUid, e.TargetUid),
            OlusturanUid = FirstNonEmpty(e.OlusturanUid, e.CreatedBy),
            OlusturmaTarihi = e.CreatedAt?.ToLocalTime().ToString("dd.MM.yyyy HH:mm") ?? "",
            Okundu = e.IsRead,
            Arsivlendi = e.IsDismissed,
            GuncellemeUtc = e.CreatedAt.HasValue
                ? new DateTimeOffset(e.CreatedAt.Value).ToUnixTimeMilliseconds()
                : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            InboxDocId = e.DocId,
            DeepLink = e.DeepLink,
            EventCode = e.EventCode,
            DesktopRoute = e.DesktopRoute
        };
    }

    /// <summary>Event kodunu legacy tip sabitine çevirir (filtreleme / toast).</summary>
    public static string EventCodeToLegacyTipPublic(string? eventCode) =>
        EventCodeToLegacyTip(eventCode ?? "");

    private static string CozLegacyTip(string? eventCode, string? tip, string? type)
    {
        var fromEvent = EventCodeToLegacyTip(eventCode ?? "");
        if (BildirimFiltreleme.TalepBaglantiliMi(fromEvent))
            return BildirimRolPolitikasi.NormalizeTip(fromEvent);

        var raw = FirstNonEmpty(tip, type);
        if (BildirimFiltreleme.TalepBaglantiliMi(raw))
            return BildirimRolPolitikasi.NormalizeTip(raw);

        // type alanı APPROVAL/INFO olabilir — event'ten gelen (veya noktalı kod) kalsın.
        if (string.Equals(raw, "APPROVAL", StringComparison.OrdinalIgnoreCase)
            || string.Equals(raw, "INFO", StringComparison.OrdinalIgnoreCase)
            || string.Equals(raw, "TASK", StringComparison.OrdinalIgnoreCase)
            || string.Equals(raw, "WARNING", StringComparison.OrdinalIgnoreCase)
            || string.Equals(raw, "REMINDER", StringComparison.OrdinalIgnoreCase)
            || string.Equals(raw, "URGENT", StringComparison.OrdinalIgnoreCase)
            || string.Equals(raw, "CRITICAL", StringComparison.OrdinalIgnoreCase))
            return string.IsNullOrWhiteSpace(fromEvent) ? raw : fromEvent;

        return string.IsNullOrWhiteSpace(raw) ? fromEvent : BildirimRolPolitikasi.NormalizeTip(raw);
    }

    private static string EventCodeToLegacyTip(string eventCode) => eventCode switch
    {
        "talep.yonetime_gonderildi" or "talep.olusturuldu"
            or "talep.sla_yaklasiyor" or "talep.sla_asildi" => BildirimTipleri.YonetimeGonderildi,
        "teklif.istendi" => BildirimTipleri.TeklifIstendi,
        "teklif.yonetime_gonderildi" => BildirimTipleri.TeklifOnayda,
        "teklif.duzeltme_istendi" => BildirimTipleri.TeklifDuzeltmeIstendi,
        "talep.onaylandi" => BildirimTipleri.Onaylandi,
        "talep.reddedildi" => BildirimTipleri.Reddedildi,
        "siparis.olusturuldu" => BildirimTipleri.SiparisOlusturuldu,
        "depo.mal_kabul_yapildi" => BildirimTipleri.MalKabulEdildi,
        _ => eventCode
    };

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var v in values)
        {
            if (!string.IsNullOrWhiteSpace(v))
                return v.Trim();
        }

        return "";
    }
}

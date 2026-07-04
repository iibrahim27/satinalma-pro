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
        Guid? talepId = Guid.TryParse(e.EntityId, out var id) ? id : null;
        var tip = EventCodeToLegacyTip(e.EventCode);

        return new BildirimKaydi
        {
            Id = Guid.TryParse(e.DocId, out var docGuid) ? docGuid : Guid.NewGuid(),
            Baslik = e.Title,
            Mesaj = e.Message,
            Tip = tip,
            TalepId = talepId,
            OlusturmaTarihi = e.CreatedAt?.ToLocalTime().ToString("dd.MM.yyyy HH:mm") ?? "",
            Okundu = e.IsRead,
            GuncellemeUtc = e.CreatedAt.HasValue
                ? new DateTimeOffset(e.CreatedAt.Value).ToUnixTimeMilliseconds()
                : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            InboxDocId = e.DocId,
            DeepLink = e.DeepLink,
            EventCode = e.EventCode,
            DesktopRoute = e.DesktopRoute
        };
    }

    private static string EventCodeToLegacyTip(string eventCode) => eventCode switch
    {
        "talep.yonetime_gonderildi" => BildirimTipleri.YonetimeGonderildi,
        "teklif.istendi" => BildirimTipleri.TeklifIstendi,
        "teklif.yonetime_gonderildi" => BildirimTipleri.TeklifOnayda,
        "teklif.duzeltme_istendi" => BildirimTipleri.TeklifDuzeltmeIstendi,
        "talep.onaylandi" => BildirimTipleri.Onaylandi,
        "talep.reddedildi" => BildirimTipleri.Reddedildi,
        "siparis.olusturuldu" => BildirimTipleri.SiparisOlusturuldu,
        "depo.mal_kabul_yapildi" => BildirimTipleri.MalKabulEdildi,
        _ => eventCode
    };
}

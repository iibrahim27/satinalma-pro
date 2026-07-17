using SatinalmaPro.Shared.Models;
using SatinalmaPro.Shared.Helpers;
using SatinalmaPro.Shared.Services.Firebase;

namespace SatinalmaPro.Shared.Services;

/// <summary>
/// FCM push — önce v1 (Service Account), yoksa Legacy Server key.
/// </summary>
public sealed class FcmPushServisi
{
    private readonly FirebaseAyarlar _ayarlar;
    private readonly FirestoreVeriServisi _firestore;
    private readonly Func<string?>? _serviceAccountYolu;

    public FcmPushServisi(
        FirebaseAyarlar ayarlar,
        FirestoreVeriServisi firestore,
        Func<string?>? serviceAccountYolu = null)
    {
        _ayarlar = ayarlar;
        _firestore = firestore;
        _serviceAccountYolu = serviceAccountYolu;
    }

    public bool Aktif =>
        FcmV1Api.ServiceAccountMevcut(CozumleServiceAccountYolu()) ||
        !string.IsNullOrWhiteSpace(_ayarlar.FcmServerKey);

    public async Task BildirimGonderAsync(BildirimKaydi bildirim, CancellationToken iptal = default)
    {
        if (!Aktif)
            return;

        var hedefler = await HedefleriAlAsync(bildirim, iptal);
        var saYolu = CozumleServiceAccountYolu();
        var v1 = FcmV1Api.ServiceAccountMevcut(saYolu);
        var servisTokeni = v1
            ? await FcmV1Api.ServiceAccountErisimTokeniAlAsync(saYolu!, iptal)
            : null;
        var inboxDocId = BildirimMantikAnahtari.Olustur(bildirim);

        foreach (var hedef in hedefler)
        {
            try
            {
                var veri = BildirimRotaServisi.FcmVeri(bildirim, hedef.Rol);
                if (v1)
                {
                    await _firestore.InboxEkleBearerIleAsync(
                        servisTokeni!,
                        hedef.Uid,
                        inboxDocId,
                        bildirim,
                        iptal);
                    veri["inboxDocId"] = inboxDocId;
                    await FcmV1Api.TokenaGonderAsync(
                        saYolu!,
                        _ayarlar.ProjectId,
                        hedef.Token,
                        bildirim.Baslik,
                        bildirim.Mesaj,
                        veri,
                        iptal);
                }
                else
                    await LegacyTokenaGonderAsync(hedef.Token, bildirim, hedef.Rol, iptal);
            }
            catch
            {
                // tek token hatası diğerlerini durdurmasın
            }
        }
    }

    private string? CozumleServiceAccountYolu()
    {
        var ozel = _serviceAccountYolu?.Invoke();
        if (FcmV1Api.ServiceAccountMevcut(ozel))
            return ozel;

        if (FcmV1Api.ServiceAccountMevcut(_ayarlar.FcmServiceAccountYolu))
            return _ayarlar.FcmServiceAccountYolu;

        return null;
    }

    private sealed record FcmHedef(string Uid, string Token, string Rol);

    private async Task<List<FcmHedef>> HedefleriAlAsync(BildirimKaydi bildirim, CancellationToken iptal)
    {
        if (!string.IsNullOrWhiteSpace(bildirim.HedefUid))
        {
            if (string.Equals(bildirim.HedefUid, bildirim.OlusturanUid, StringComparison.OrdinalIgnoreCase))
                return [];

            var profil = await _firestore.KullaniciOkuAsync(bildirim.HedefUid, iptal);
            if (string.IsNullOrWhiteSpace(profil?.FcmToken))
                return [];

            return [new FcmHedef(profil.Uid, profil.FcmToken, profil.Rol ?? "")];
        }

        if (!string.IsNullOrWhiteSpace(bildirim.HedefRol))
        {
            var kullanicilar = await _firestore.RolKullanicilariOkuAsync(bildirim.HedefRol, iptal);
            return kullanicilar
                .Where(k => k.Uid != bildirim.OlusturanUid && !string.IsNullOrWhiteSpace(k.FcmToken))
                .Select(k => new FcmHedef(k.Uid, k.FcmToken!, k.Rol ?? ""))
                .GroupBy(h => h.Uid)
                .Select(g => g.First())
                .ToList();
        }

        return [];
    }

    private async Task LegacyTokenaGonderAsync(string token, BildirimKaydi bildirim, string rol, CancellationToken iptal)
    {
        if (string.IsNullOrWhiteSpace(_ayarlar.FcmServerKey))
            return;

        var veri = new Dictionary<string, string>(BildirimRotaServisi.FcmVeri(bildirim, rol))
        {
            ["title"] = bildirim.Baslik,
            ["body"] = bildirim.Mesaj
        };
        var govde = new
        {
            to = token,
            priority = "high",
            data = veri
        };

        using var istek = new HttpRequestMessage(HttpMethod.Post, "https://fcm.googleapis.com/fcm/send")
        {
            Content = System.Net.Http.Json.JsonContent.Create(govde)
        };
        istek.Headers.TryAddWithoutValidation("Authorization", $"key={_ayarlar.FcmServerKey}");

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        var yanit = await http.SendAsync(istek, iptal);
        yanit.EnsureSuccessStatusCode();
    }
}

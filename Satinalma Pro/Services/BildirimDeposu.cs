using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SatinalmaPro.Models;
using SatinalmaPro.Services.Firebase;
using SatinalmaPro.Shared.Helpers;
using SatinalmaPro.Shared.Services;

namespace SatinalmaPro.Services;

public static class BildirimDeposu
{
    private const string FirestoreYol = "veri/bildirimler";

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static List<BildirimKaydi> Bildirimler { get; } = [];

    private static DateTime? _sonYukleme;
    private static readonly TimeSpan YuklemeBekleme = TimeSpan.FromSeconds(12);

    public static async Task YukleAsync(bool zorla = false, CancellationToken iptal = default)
    {
        if (!OturumYoneticisi.GirisYapildi || OturumYoneticisi.Firestore is null)
            return;

        if (!zorla
            && _sonYukleme.HasValue
            && DateTime.Now - _sonYukleme.Value < YuklemeBekleme)
            return;

        var json = await OturumYoneticisi.Firestore.BelgeJsonOkuAsync(FirestoreYol, iptal);
        Bildirimler.Clear();
        if (string.IsNullOrWhiteSpace(json))
        {
            _sonYukleme = DateTime.Now;
            return;
        }

        Bildirimler.AddRange(JsonSerializer.Deserialize<List<BildirimKaydi>>(json, Json) ?? []);
        _sonYukleme = DateTime.Now;
    }

    public static async Task KaydetAsync(CancellationToken iptal = default)
    {
        if (!OturumYoneticisi.GirisYapildi || OturumYoneticisi.Firestore is null)
            return;

        var json = JsonSerializer.Serialize(Bildirimler, Json);
        await OturumYoneticisi.Firestore.BelgeJsonYazAsync(
            FirestoreYol, json, OturumYoneticisi.Auth?.Uid, iptal);
    }

    public static async Task EkleAsync(BildirimKaydi bildirim, CancellationToken iptal = default)
    {
        await YukleAsync(zorla: true, iptal);
        Bildirimler.Insert(0, bildirim);
        await KaydetAsync(iptal);
        await FcmPushGonderAsync(bildirim, iptal);
    }

    private static async Task FcmPushGonderAsync(BildirimKaydi bildirim, CancellationToken iptal)
    {
        if (OturumYoneticisi.Firestore is null)
            return;

        var saYolu = FirebaseAyarDeposu.FcmServiceAccountMevcut
            ? FirebaseAyarDeposu.FcmServiceAccountCalismaYolu
            : null;
        var legacyKey = FirebaseAyarDeposu.Ayarlar.FcmServerKey;
        var v1 = FcmV1Api.ServiceAccountMevcut(saYolu);

        if (!v1 && string.IsNullOrWhiteSpace(legacyKey))
            return;

        try
        {
            var hedefler = await HedefleriAlAsync(bildirim, iptal);
            var projectId = FirebaseAyarDeposu.Ayarlar.ProjectId;
            var mobil = MobilBildirim(bildirim);

            foreach (var hedef in hedefler)
            {
                var veri = BildirimRotaServisi.FcmVeri(mobil, hedef.Rol);
                if (v1)
                {
                    await FcmV1Api.TokenaGonderAsync(
                        saYolu!,
                        projectId,
                        hedef.Token,
                        bildirim.Baslik,
                        bildirim.Mesaj,
                        veri,
                        iptal);
                }
                else
                {
                    var govde = new
                    {
                        to = hedef.Token,
                        priority = "high",
                        notification = new
                        {
                            title = bildirim.Baslik,
                            body = bildirim.Mesaj,
                            sound = "default"
                        },
                        data = new Dictionary<string, string>(veri)
                    };

                    using var istek = new HttpRequestMessage(HttpMethod.Post, "https://fcm.googleapis.com/fcm/send")
                    {
                        Content = JsonContent.Create(govde)
                    };
                    istek.Headers.TryAddWithoutValidation("Authorization", $"key={legacyKey}");
                    using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
                    await http.SendAsync(istek, iptal);
                }
            }
        }
        catch
        {
            // push isteğe bağlı
        }
    }

    private sealed record FcmHedef(string Token, string Rol);

    private static async Task<List<FcmHedef>> HedefleriAlAsync(BildirimKaydi bildirim, CancellationToken iptal)
    {
        if (OturumYoneticisi.Firestore is null)
            return [];

        if (!string.IsNullOrWhiteSpace(bildirim.HedefUid))
        {
            var profil = await OturumYoneticisi.Firestore.KullaniciOkuAsync(bildirim.HedefUid, iptal);
            if (string.IsNullOrWhiteSpace(profil?.FcmToken))
                return [];

            return [new FcmHedef(profil.FcmToken, profil.Rol ?? "")];
        }

        if (string.IsNullOrWhiteSpace(bildirim.HedefRol))
            return [];

        var tum = await OturumYoneticisi.Firestore.TumKullanicilariOkuAsync(iptal);
        var rol = KullaniciRolleri.Normalize(bildirim.HedefRol);
        return tum
            .Where(k => k.Aktif && k.Uid != bildirim.OlusturanUid &&
                        KullaniciRolleri.Normalize(k.Rol) == rol &&
                        !string.IsNullOrWhiteSpace(k.FcmToken))
            .Select(k => new FcmHedef(k.FcmToken!, k.Rol ?? ""))
            .GroupBy(h => h.Token)
            .Select(g => g.First())
            .ToList();
    }

    private static SatinalmaPro.Shared.Models.BildirimKaydi MobilBildirim(BildirimKaydi b) => new()
    {
        Id = b.Id,
        Baslik = b.Baslik,
        Mesaj = b.Mesaj,
        Tip = b.Tip,
        TalepId = b.TalepId,
        HedefRol = b.HedefRol,
        HedefUid = b.HedefUid,
        OlusturanUid = b.OlusturanUid,
        OlusturanAd = b.OlusturanAd,
        OlusturmaTarihi = b.OlusturmaTarihi,
        Okundu = b.Okundu
    };
}

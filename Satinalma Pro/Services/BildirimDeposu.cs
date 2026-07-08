using System.Threading;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services.Firebase;
using SatinalmaPro.Shared.Helpers;
using SatinalmaPro.Shared.Services;
using SharedBildirim = SatinalmaPro.Shared.Models.BildirimKaydi;

namespace SatinalmaPro.Services;

public static class BildirimDeposu
{
    private const string FirestoreYol = "veri/bildirimler";

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly SemaphoreSlim BulutYazmaKilidi = new(1, 1);

    public static List<BildirimKaydi> Bildirimler { get; } = [];

    /// <summary>Liste üzerinde güvenli okuma için anlık kopya.</summary>
    public static List<BildirimKaydi> AnlikListe()
    {
        lock (Bildirimler)
            return Bildirimler.ToList();
    }

    public static void Sil(Func<BildirimKaydi, bool> predicate)
    {
        lock (Bildirimler)
            Bildirimler.RemoveAll(b => predicate(b));
    }

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
        var bulut = Deserialize(json);
        var inbox = await InboxYukleAsync(iptal);
        if (inbox.Count > 0)
            bulut = InboxIleBirlestir(bulut, inbox, OturumYoneticisi.AktifKullanici);

        var yerel = AnlikListe().Select(ToShared).ToList();
        var birlesik = BildirimTekillestirme.Tekille(
            BildirimBirlestirme.Birlestir(yerel, bulut));

        await BulutYazmaKilidi.WaitAsync(iptal);
        try
        {
            lock (Bildirimler)
            {
                Bildirimler.Clear();
                Bildirimler.AddRange(birlesik.Select(FromShared));
            }
            _sonYukleme = DateTime.Now;
        }
        finally
        {
            BulutYazmaKilidi.Release();
        }
    }

    public static async Task KaydetAsync(CancellationToken iptal = default)
    {
        if (!OturumYoneticisi.GirisYapildi || OturumYoneticisi.Firestore is null)
            return;

        await BulutYazmaKilidi.WaitAsync(iptal);
        try
        {
            var bulutJson = await OturumYoneticisi.Firestore.BelgeJsonOkuAsync(FirestoreYol, iptal);
            var bulut = Deserialize(bulutJson);
            var yerel = AnlikListe().Select(ToShared).ToList();
            var birlesik = BildirimTekillestirme.Tekille(
                BildirimBirlestirme.Birlestir(yerel, bulut));

            lock (Bildirimler)
            {
                Bildirimler.Clear();
                Bildirimler.AddRange(birlesik.Select(FromShared));
            }

            await YerelListeyiBulutaYazAsync(iptal);
        }
        finally
        {
            BulutYazmaKilidi.Release();
        }
    }

    /// <summary>Silme / toplu okundu sonrası yerel listeyi buluta yazar; silinen kayıtları buluttan geri yüklemez.</summary>
    public static async Task KaydetYerelAsync(CancellationToken iptal = default)
    {
        if (!OturumYoneticisi.GirisYapildi || OturumYoneticisi.Firestore is null)
            return;

        await BulutYazmaKilidi.WaitAsync(iptal);
        try
        {
            await YerelListeyiBulutaYazAsync(iptal);
            _sonYukleme = DateTime.Now;
        }
        finally
        {
            BulutYazmaKilidi.Release();
        }
    }

    public static async Task GecersizleriSilAsync(CancellationToken iptal = default)
    {
        if (!OturumYoneticisi.GirisYapildi || OturumYoneticisi.Firestore is null)
            return;

        SatinalmaDepo.Yukle();
        await YukleAsync(zorla: true, iptal);

        var degisti = false;
        lock (Bildirimler)
        {
            var kalacak = Bildirimler
                .Where(b => MasaustuBildirimFiltreleme.GecerliMi(b, SatinalmaDepo.Talepler))
                .ToList();
            degisti = kalacak.Count != Bildirimler.Count;
            if (degisti)
            {
                Bildirimler.Clear();
                Bildirimler.AddRange(kalacak);
            }
        }

        if (degisti)
            await KaydetYerelAsync(iptal);

        await InboxGecersizleriSilAsync(iptal);
    }

    private static async Task InboxGecersizleriSilAsync(CancellationToken iptal)
    {
        var uid = OturumYoneticisi.Auth?.Uid;
        if (string.IsNullOrWhiteSpace(uid) || OturumYoneticisi.Firestore is null)
            return;

        for (var guard = 0; guard < 20; guard++)
        {
            var inbox = await OturumYoneticisi.Firestore.InboxOkuAsync(uid, 200, iptal);
            if (inbox.Count == 0)
                break;

            foreach (var kayit in inbox)
            {
                var bildirim = BildirimInboxServisi.InboxtenBildirimeDonustur(kayit);
                if (MasaustuBildirimFiltreleme.GecerliMi(FromShared(bildirim), SatinalmaDepo.Talepler))
                    continue;

                try
                {
                    await OturumYoneticisi.Firestore.InboxArsivleAsync(uid, kayit.DocId, iptal);
                }
                catch
                {
                    // tek kayıt hatası diğerlerini engellemesin
                }
            }

            if (inbox.Count < 200)
                break;
        }
    }

    public static async Task InboxTumunuOkunduAsync(CancellationToken iptal = default)
    {
        var uid = OturumYoneticisi.Auth?.Uid;
        if (string.IsNullOrWhiteSpace(uid) || OturumYoneticisi.Firestore is null)
            return;

        await OturumYoneticisi.Firestore.InboxTumunuOkunduIsaretleAsync(uid, iptal);
    }

    public static async Task InboxTemizleAsync(CancellationToken iptal = default)
    {
        var uid = OturumYoneticisi.Auth?.Uid;
        if (string.IsNullOrWhiteSpace(uid) || OturumYoneticisi.Firestore is null)
            return;

        SatinalmaDepo.Yukle();
        var kullanici = OturumYoneticisi.AktifKullanici;

        for (var guard = 0; guard < 20; guard++)
        {
            var inbox = await OturumYoneticisi.Firestore.InboxOkuAsync(uid, 200, iptal);
            var arsivlenecek = inbox.Where(k => !k.IsDismissed).ToList();
            if (kullanici is not null)
            {
                arsivlenecek = arsivlenecek.Where(k =>
                {
                    var bildirim = FromShared(BildirimInboxServisi.InboxtenBildirimeDonustur(k));
                    return MasaustuBildirimFiltreleme.KullaniciyaMi(bildirim, kullanici)
                        && !MasaustuBildirimFiltreleme.Temizlenmemeli(bildirim, SatinalmaDepo.Talepler);
                }).ToList();
            }

            if (arsivlenecek.Count == 0)
                break;

            foreach (var kayit in arsivlenecek)
            {
                try
                {
                    await OturumYoneticisi.Firestore.InboxArsivleAsync(uid, kayit.DocId, iptal);
                }
                catch
                {
                    // tek kayıt hatası diğerlerini engellemesin
                }
            }

            if (inbox.Count < 200)
                break;
        }
    }

    private static async Task YerelListeyiBulutaYazAsync(CancellationToken iptal)
    {
        if (OturumYoneticisi.Firestore is null)
            return;

        var json = JsonSerializer.Serialize(AnlikListe(), Json);
        await OturumYoneticisi.Firestore.BelgeJsonYazAsync(
            FirestoreYol, json, OturumYoneticisi.Auth?.Uid, iptal);

        await InboxOkunduSenkronizeEtAsync(iptal);
    }

    public static async Task<bool> EkleAsync(BildirimKaydi bildirim, CancellationToken iptal = default)
    {
        if (!BildirimRolPolitikasi.KayitGonderilmeli(bildirim.HedefRol, bildirim.HedefUid, bildirim.OlusturanUid))
            return false;

        bildirim.GuncellemeUtc = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await YukleAsync(zorla: true, iptal);

        var mevcut = MevcutOkunmamis(bildirim);
        if (mevcut is not null)
        {
            var yenidenAktif = mevcut.Okundu;
            mevcut.Baslik = bildirim.Baslik;
            mevcut.Mesaj = bildirim.Mesaj;
            mevcut.Okundu = false;
            mevcut.GuncellemeUtc = bildirim.GuncellemeUtc;
            await KaydetAsync(iptal);
            if (yenidenAktif)
                await FcmPushGonderAsync(mevcut, iptal);
            return false;
        }

        lock (Bildirimler)
            Bildirimler.Insert(0, bildirim);
        await KaydetAsync(iptal);
        await FcmPushGonderAsync(bildirim, iptal);
        return true;
    }

    public static async Task CokluEkleAsync(IReadOnlyList<BildirimKaydi> bildirimler, CancellationToken iptal = default)
    {
        var gecerli = bildirimler
            .Where(b => BildirimRolPolitikasi.KayitGonderilmeli(b.HedefRol, b.HedefUid, b.OlusturanUid))
            .ToList();
        if (gecerli.Count == 0)
            return;

        foreach (var b in gecerli)
            b.GuncellemeUtc = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        await YukleAsync(zorla: true, iptal);

        var yeniKayitlar = new List<BildirimKaydi>();
        lock (Bildirimler)
        {
            foreach (var b in gecerli)
            {
                var mevcut = MevcutOkunmamis(b);
                if (mevcut is not null)
                {
                    var yenidenAktif = mevcut.Okundu;
                    mevcut.Baslik = b.Baslik;
                    mevcut.Mesaj = b.Mesaj;
                    mevcut.Okundu = false;
                    mevcut.GuncellemeUtc = b.GuncellemeUtc;
                    if (yenidenAktif)
                        yeniKayitlar.Add(mevcut);
                    continue;
                }

                Bildirimler.Insert(0, b);
                yeniKayitlar.Add(b);
            }
        }

        await KaydetAsync(iptal);

        foreach (var b in yeniKayitlar)
            await FcmPushGonderAsync(b, iptal);
    }

    private static BildirimKaydi? MevcutOkunmamis(BildirimKaydi bildirim)
    {
        var anahtar = BildirimMantikAnahtari.Olustur(ToShared(bildirim));
        return AnlikListe()
            .FirstOrDefault(b => BildirimMantikAnahtari.Olustur(ToShared(b)) == anahtar);
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
            var adminToken = v1
                ? await FcmV1Api.ServiceAccountErisimTokeniAlAsync(saYolu!, iptal)
                : null;

            var hedefler = await HedefleriAlAsync(bildirim, adminToken, iptal);
            if (hedefler.Count == 0)
            {
                HataGunlugu.Kaydet(
                    new InvalidOperationException(
                        $"FCM hedef bulunamadı tip={bildirim.Tip} rol={bildirim.HedefRol} uid={bildirim.HedefUid}"),
                    "FCM.HedefYok");
                return;
            }

            var projectId = FirebaseAyarDeposu.Ayarlar.ProjectId;
            var mobil = ToShared(bildirim);
            var inboxDocId = BildirimMantikAnahtari.Olustur(mobil);

            foreach (var hedef in hedefler)
            {
                var veri = BildirimRotaServisi.FcmVeri(mobil, hedef.Rol);
                if (!string.IsNullOrWhiteSpace(hedef.Uid))
                {
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(adminToken))
                        {
                            await OturumYoneticisi.Firestore.InboxEkleBearerIleAsync(
                                adminToken, hedef.Uid, inboxDocId, bildirim, iptal);
                        }
                        else
                        {
                            await OturumYoneticisi.Firestore.InboxEkleAsync(
                                hedef.Uid, inboxDocId, bildirim, iptal);
                        }
                    }
                    catch
                    {
                        // inbox isteğe bağlı
                    }
                }

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
                    var legacyVeri = new Dictionary<string, string>(veri)
                    {
                        ["title"] = bildirim.Baslik,
                        ["body"] = bildirim.Mesaj
                    };
                    var govde = new
                    {
                        to = hedef.Token,
                        priority = "high",
                        data = legacyVeri
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
        catch (Exception ex)
        {
            HataGunlugu.Kaydet(ex, $"FCM push başarısız tip={bildirim.Tip} hedefRol={bildirim.HedefRol}");
        }
    }

    private sealed record FcmHedef(string Token, string Rol, string Uid);

    private static async Task<List<FcmHedef>> HedefleriAlAsync(
        BildirimKaydi bildirim,
        string? adminToken,
        CancellationToken iptal)
    {
        if (OturumYoneticisi.Firestore is null)
            return [];

        if (!string.IsNullOrWhiteSpace(bildirim.HedefUid))
        {
            KullaniciProfili? profil;
            if (!string.IsNullOrWhiteSpace(adminToken))
            {
                var kullanicilar = await OturumYoneticisi.Firestore.TumKullanicilariBearerIleOkuAsync(adminToken, iptal);
                profil = kullanicilar.FirstOrDefault(k => k.Uid == bildirim.HedefUid);
            }
            else
                profil = await OturumYoneticisi.Firestore.KullaniciOkuAsync(bildirim.HedefUid, iptal);

            if (string.IsNullOrWhiteSpace(profil?.FcmToken))
                return [];

            return [new FcmHedef(profil.FcmToken, profil.Rol ?? "", profil.Uid)];
        }

        if (string.IsNullOrWhiteSpace(bildirim.HedefRol))
            return [];

        List<KullaniciProfili> tum;
        if (!string.IsNullOrWhiteSpace(adminToken))
            tum = await OturumYoneticisi.Firestore.TumKullanicilariBearerIleOkuAsync(adminToken, iptal);
        else
            tum = await OturumYoneticisi.Firestore.TumKullanicilariOkuAsync(iptal);

        var rol = KullaniciRolleri.Normalize(bildirim.HedefRol);
        return tum
            .Where(k => k.Aktif && k.Uid != bildirim.OlusturanUid &&
                        KullaniciRolleri.Normalize(k.Rol) == rol &&
                        !string.IsNullOrWhiteSpace(k.FcmToken))
            .Select(k => new FcmHedef(k.FcmToken!, k.Rol ?? "", k.Uid))
            .GroupBy(h => h.Token)
            .Select(g => g.First())
            .ToList();
    }

    private static List<SharedBildirim> Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        return JsonSerializer.Deserialize<List<SharedBildirim>>(json, Json) ?? [];
    }

    private static SharedBildirim ToShared(BildirimKaydi b) => new()
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
        Okundu = b.Okundu,
        GuncellemeUtc = b.GuncellemeUtc,
        InboxDocId = b.InboxDocId,
        DeepLink = b.DeepLink,
        EventCode = b.EventCode,
        DesktopRoute = b.DesktopRoute
    };

    private static BildirimKaydi FromShared(SharedBildirim b) => new()
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
        Okundu = b.Okundu,
        GuncellemeUtc = b.GuncellemeUtc,
        InboxDocId = b.InboxDocId,
        DeepLink = b.DeepLink,
        EventCode = b.EventCode,
        DesktopRoute = b.DesktopRoute
    };

    private static async Task<List<SharedBildirim>> InboxYukleAsync(CancellationToken iptal)
    {
        var uid = OturumYoneticisi.Auth?.Uid;
        if (string.IsNullOrWhiteSpace(uid) || OturumYoneticisi.Firestore is null)
            return [];

        try
        {
            var inbox = await OturumYoneticisi.Firestore.InboxOkuAsync(uid, 50, iptal);
            return inbox
                .Where(e => !e.IsDismissed)
                .Select(BildirimInboxServisi.InboxtenBildirimeDonustur)
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private static List<SharedBildirim> InboxIleBirlestir(
        List<SharedBildirim> legacy,
        List<SharedBildirim> inbox,
        KullaniciProfili? kullanici)
    {
        var sharedKullanici = kullanici is null
            ? null
            : new SatinalmaPro.Shared.Models.KullaniciProfili
            {
                Uid = kullanici.Uid,
                Rol = kullanici.Rol,
                Aktif = kullanici.Aktif
            };

        var sonuc = new List<SharedBildirim>(inbox);
        foreach (var l in legacy)
        {
            if (inbox.Any(i => BildirimMantikAnahtari.Olustur(i) == BildirimMantikAnahtari.Olustur(l)))
                continue;
            if (sharedKullanici is not null && !BildirimFiltreleme.KullaniciyaMi(l, sharedKullanici))
                continue;
            sonuc.Add(l);
        }

        return BildirimTekillestirme.Tekille(sonuc);
    }

    private static async Task InboxOkunduSenkronizeEtAsync(CancellationToken iptal)
    {
        var uid = OturumYoneticisi.Auth?.Uid;
        if (string.IsNullOrWhiteSpace(uid) || OturumYoneticisi.Firestore is null)
            return;

        foreach (var b in AnlikListe().Where(x => x.Okundu && !string.IsNullOrWhiteSpace(x.InboxDocId)))
        {
            try
            {
                await OturumYoneticisi.Firestore.InboxOkunduIsaretleAsync(uid, b.InboxDocId!, iptal);
            }
            catch
            {
                // inbox senkronu isteğe bağlı
            }
        }
    }
}

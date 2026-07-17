using System.Threading;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services.Firebase;
using SatinalmaPro.Shared;
using SatinalmaPro.Shared.Helpers;
using SatinalmaPro.Shared.Services;
using SharedBildirim = SatinalmaPro.Shared.Models.BildirimKaydi;

namespace SatinalmaPro.Services;

public static class BildirimDeposu
{
    private static string FirestoreYol => FirestoreYollari.Bildirimler();

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

    /// <summary>Firma değişiminde / çıkışta bildirim belleğini temizler.</summary>
    public static void KiraciDegisti()
    {
        lock (Bildirimler)
            Bildirimler.Clear();
        _sonYukleme = null;
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
        var yerelPaylasilan = AnlikListe()
            .Where(b => string.IsNullOrWhiteSpace(b.InboxDocId))
            .Select(ToShared)
            .ToList();
        var paylasilan = BildirimBirlestirme.Birlestir(yerelPaylasilan, bulut);
        var inbox = await InboxYukleAsync(iptal);
        var birlesik = BildirimTekillestirme.Tekille(
            BildirimBirlestirme.Birlestir(inbox, paylasilan));

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
            await InboxOkunduSenkronizeEtAsync(iptal);
            var bulutJson = await OturumYoneticisi.Firestore.BelgeJsonOkuAsync(FirestoreYol, iptal);
            var bulut = Deserialize(bulutJson);
            var yerel = AnlikListe()
                .Where(b => string.IsNullOrWhiteSpace(b.InboxDocId))
                .Select(ToShared)
                .ToList();
            var birlesik = BildirimTekillestirme.Tekille(
            BildirimBirlestirme.Birlestir(yerel, bulut));
            await YerelListeyiBulutaYazAsync(birlesik, iptal);

            var inbox = await InboxYukleAsync(iptal);
            var gorunum = BildirimTekillestirme.Tekille(
                BildirimBirlestirme.Birlestir(inbox, birlesik));
            lock (Bildirimler)
            {
                Bildirimler.Clear();
                Bildirimler.AddRange(gorunum.Select(FromShared));
            }
        }
        finally
        {
            BulutYazmaKilidi.Release();
        }
    }

    /// <summary>Silme / toplu okundu sonrası yerel listeyi buluta yazar; silinen kayıtları buluttan geri yüklemez.</summary>
    public static async Task KaydetYerelAsync(CancellationToken iptal = default)
    {
        await KaydetAsync(iptal);
    }

    public static async Task GecersizleriSilAsync(CancellationToken iptal = default)
    {
        if (!OturumYoneticisi.GirisYapildi || OturumYoneticisi.Firestore is null)
            return;

        // Çağıran taraf talepleri/bildirimleri yüklemiş olmalı; burada yeniden Yukle
        // geçmiş inbox kayıtlarını tekrar içeri alıp toast/liste yağmuruna yol açıyordu.

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

    private static async Task YerelListeyiBulutaYazAsync(
        IEnumerable<SharedBildirim> paylasilanKayitlar,
        CancellationToken iptal)
    {
        if (OturumYoneticisi.Firestore is null)
            return;

        // Legacy blob şişmesin: inbox-only / okunmuş / geçersizleri budayıp boyut sınırla.
        var kayitlar = paylasilanKayitlar
            .OrderByDescending(b => b.GuncellemeUtc)
            .Take(150)
            .ToList();
        var json = JsonSerializer.Serialize(kayitlar, Json);
        const int maxBytes = 900_000;
        while (Encoding.UTF8.GetByteCount(json) > maxBytes && kayitlar.Count > 10)
        {
            kayitlar.RemoveAt(kayitlar.Count - 1);
            json = JsonSerializer.Serialize(kayitlar, Json);
        }

        await OturumYoneticisi.Firestore.BelgeJsonYazAsync(
            FirestoreYol, json, OturumYoneticisi.Auth?.Uid, iptal);

    }

    /// <summary>
    /// veri/bildirimler belgesi Firestore ~1MB alan sınırına takılmasın diye
    /// inbox kopyalarını ve işlemi bitenleri bellekten düşürür.
    /// </summary>
    private static void BudamayiUygula()
    {
        lock (Bildirimler)
        {
            var talepler = SatinalmaDepo.Talepler;
            var kalacak = Bildirimler
                .Where(b =>
                {
                    // Inbox asıl kaynak; legacy blob'a yalnızca talep bağlı iş akışı kalsın.
                    if (!string.IsNullOrWhiteSpace(b.InboxDocId) && b.TalepId is null)
                        return false;
                    if (b.Okundu && !MasaustuBildirimFiltreleme.Temizlenmemeli(b, talepler))
                        return false;
                    if (!MasaustuBildirimFiltreleme.GecerliMi(b, talepler)
                        && !MasaustuBildirimFiltreleme.Temizlenmemeli(b, talepler))
                        return false;
                    return true;
                })
                .OrderByDescending(b => b.GuncellemeUtc)
                .Take(150)
                .ToList();

            if (kalacak.Count == Bildirimler.Count)
                return;

            Bildirimler.Clear();
            Bildirimler.AddRange(kalacak);
        }
    }

    private static void BudamayiSikistir(int maxBytes)
    {
        lock (Bildirimler)
        {
            while (Bildirimler.Count > 10)
            {
                var json = JsonSerializer.Serialize(Bildirimler.ToList(), Json);
                if (Encoding.UTF8.GetByteCount(json) <= maxBytes)
                    break;

                // En eski okunmuşları at; yoksa en eskileri at.
                var silinecek = Bildirimler
                    .OrderByDescending(b => b.Okundu)
                    .ThenBy(b => b.GuncellemeUtc)
                    .FirstOrDefault();
                if (silinecek is null)
                    break;
                Bildirimler.Remove(silinecek);
            }
        }
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
            // Okunmuş bildirim bir daha asla gönderilmez / yeniden açılmaz.
            if (mevcut.Okundu)
                return false;

            mevcut.Baslik = bildirim.Baslik;
            mevcut.Mesaj = bildirim.Mesaj;
            mevcut.GuncellemeUtc = bildirim.GuncellemeUtc;
            await KaydetAsync(iptal);
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
                    // Okunmuş → bir daha push yok; okunmamış → yalnızca metin güncelle.
                    if (!mevcut.Okundu)
                    {
                        mevcut.Baslik = b.Baslik;
                        mevcut.Mesaj = b.Mesaj;
                        mevcut.GuncellemeUtc = b.GuncellemeUtc;
                    }
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
        var fcmHazir = v1 || !string.IsNullOrWhiteSpace(legacyKey);

        try
        {
            var adminToken = v1
                ? await FcmV1Api.ServiceAccountErisimTokeniAlAsync(saYolu!, iptal)
                : null;

            // Inbox için token şart değil; FCM için token gerekir.
            var hedefler = await HedefleriAlAsync(bildirim, adminToken, fcmZorunlu: false, iptal);
            if (hedefler.Count == 0)
            {
                HataGunlugu.Kaydet(
                    new InvalidOperationException(
                        $"Bildirim hedefi bulunamadı tip={bildirim.Tip} rol={bildirim.HedefRol} uid={bildirim.HedefUid}"),
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
                        veri["inboxDocId"] = inboxDocId;
                    }
                    catch (Exception ex)
                    {
                        HataGunlugu.Kaydet(ex, $"Inbox yazılamadı uid={hedef.Uid}");
                    }
                }

                if (!fcmHazir || string.IsNullOrWhiteSpace(hedef.Token))
                    continue;

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
        bool fcmZorunlu,
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

            if (profil is null)
                return [];

            if (fcmZorunlu && string.IsNullOrWhiteSpace(profil.FcmToken))
                return [];

            return [new FcmHedef(profil.FcmToken ?? "", profil.Rol ?? "", profil.Uid)];
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
            .Where(k => k.Aktif
                && !string.Equals(k.Uid, bildirim.OlusturanUid, StringComparison.OrdinalIgnoreCase)
                && KullaniciRolleri.Normalize(k.Rol) == rol
                && (!fcmZorunlu || !string.IsNullOrWhiteSpace(k.FcmToken)))
            .Select(k => new FcmHedef(k.FcmToken ?? "", k.Rol ?? "", k.Uid))
            .GroupBy(h => h.Uid)
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
        Arsivlendi = b.Arsivlendi,
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
        Arsivlendi = b.Arsivlendi,
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

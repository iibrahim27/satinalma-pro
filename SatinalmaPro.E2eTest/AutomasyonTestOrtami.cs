using SatinalmaPro.Shared.Helpers;
using SatinalmaPro.Shared.Models;
using SatinalmaPro.Shared.Procurement;
using SatinalmaPro.Shared.Procurement.Detail;
using SatinalmaPro.Shared.Services;

namespace SatinalmaPro.E2eTest;

/// <summary>
/// PurchaseModuleAutomationTest için bellek içi Firestore + FCM + stok simülasyonu.
/// </summary>
public sealed class AutomasyonTestOrtami
{
    public FcmTopicKayitcisi Fcm { get; } = new();
    public List<SatinalmaTalep> Talepler { get; } = [];
    public List<BildirimKaydi> Bildirimler { get; } = [];
    public Dictionary<string, decimal> Stocks { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<StockMovementKaydi> StockMovements { get; } = [];
    public Dictionary<string, List<EnterpriseQuoteKaydi>> Quotes { get; } = new(StringComparer.OrdinalIgnoreCase);

    public SatinalmaAyarlar Ayarlar { get; set; } = new();
    public KullaniciProfili? AktifKullanici { get; set; }

    public static readonly KullaniciProfili Saha = BellekTestOrtami.Saha;
    public static readonly KullaniciProfili Sef = BellekTestOrtami.Sef;
    public static readonly KullaniciProfili Yonetim = BellekTestOrtami.Yonetim;
    public static readonly KullaniciProfili Satinalma = BellekTestOrtami.Satinalma;
    public static readonly KullaniciProfili Depo = BellekTestOrtami.Depo;

    public static readonly KullaniciProfili Atolye = new()
    {
        Uid = "e2e-atolye-uid",
        AdSoyad = "E2E Atölye",
        Rol = KullaniciRolleri.Atolye,
        Eposta = "e2e-atolye@test.local",
        Aktif = true
    };

    public void OturumAc(KullaniciProfili user)
    {
        AktifKullanici = user;
        Fcm.OturumAc(user);
    }

    public SatinalmaTalep GuncelTalep(Guid id) => Talepler.First(t => t.Id == id);

    public void Kaydet(SatinalmaTalep talep)
    {
        SatinalmaTalepSenkronYardimcisi.Dokun(talep);
        Talepler.RemoveAll(t => t.Id == talep.Id);
        Talepler.Insert(0, talep);
    }

    public IEnumerable<SatinalmaTalep> RouteTalepleri(string route, KullaniciProfili user) =>
        Talepler.Where(t => ProcurementRouteMatcher.Matches(route, t, user.Rol, user.Uid));

    public bool SekmeGorunur(string route, KullaniciProfili user) =>
        ProcurementRouteMatcher.IsRouteVisibleForRole(route, user.Rol);

    public SatinalmaTalep TalepOlustur(
        KullaniciProfili olusturan,
        string oncelik = ProcurementPriority.Normal,
        string malzeme = "E2E Otomasyon Malzeme",
        double miktar = 10)
    {
        OturumAc(olusturan);
        var kalem = new SatinalmaTalepKalemi
        {
            Id = Guid.NewGuid(),
            SiraNo = 1,
            Malzeme = malzeme,
            Miktar = miktar,
            Birim = "Adet",
            Aciklama = BellekTestOrtami.TestEtiketi
        };

        var talepTuru = oncelik.Equals(ProcurementPriority.Urgent, StringComparison.OrdinalIgnoreCase)
            ? TalepTurleri.Acil
            : TalepTurleri.Normal;

        var talep = new SatinalmaTalep
        {
            Id = Guid.NewGuid(),
            TalepNo = YeniTalepNo(),
            Tarih = DateTime.Now.ToString("dd.MM.yyyy"),
            TalepEden = olusturan.AdSoyad,
            OlusturanUid = olusturan.Uid,
            OlusturanRol = olusturan.Rol,
            SantiyeAdi = olusturan.Saha ?? "Test Şantiye",
            TalepAciklamasi = BellekTestOrtami.TestEtiketi,
            TalepTuru = talepTuru,
            Priority = oncelik,
            Status = ProcurementStatus.Submitted,
            Durum = SatinalmaTalepDurumlari.ImzaSurecinde,
            Kalemler = [kalem]
        };

        Kaydet(talep);
        foreach (var (hedefRol, _) in BildirimRolPolitikasi.YonetimeGonderildiHedefleri())
            BildirimVeFcm(BildirimTipleri.YonetimeGonderildi, talep, hedefRol: hedefRol);
        return talep;
    }

    public void DetayAksiyonUygula(
        SatinalmaTalep talep,
        PurchaseRequestDetailAction action,
        KullaniciProfili user,
        string? quoteId = null,
        string? not = null)
    {
        OturumAc(user);
        var mutation = PurchaseRequestDetailPresenter.CreateMutation(action, talep, user.Rol, quoteId, not)
            ?? throw new InvalidOperationException($"Aksiyon uygulanamaz: {action}");

        MutasyonUygula(talep, mutation, user);
        Kaydet(talep);
        AksiyonSonrasiBildirim(talep, action, not);
    }

    public void MutasyonUygula(SatinalmaTalep talep, PurchaseRequestDetailMutation mutation, KullaniciProfili user)
    {
        talep.Status = mutation.NewStatus;
        talep.Priority = PurchaseRequestDetailPresenter.ResolvePriority(talep);

        if (!string.IsNullOrWhiteSpace(mutation.NewLegacyDurum))
            talep.Durum = mutation.NewLegacyDurum;

        talep.TeklifsizYonetimOnayi = mutation.TeklifsizYonetimOnayi;
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

        if (mutation.NewStatus == ProcurementStatus.Approved)
        {
            talep.YonetimOnaylayanUid = user.Uid;
            talep.YonetimOnaylayanAd = user.AdSoyad;
            talep.YonetimOnayTarihi = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
        }

        if (mutation.NewStatus == ProcurementStatus.QuoteRequested)
        {
            talep.TeklifsizYonetimOnayi = false;
            talep.YonetimOnayKilitli = false;
        }

        talep.GuncellemeUtc = mutation.UpdatedAtUtcMs;
    }

    public SatinalmaTeklif EnterpriseTeklifEkle(
        SatinalmaTalep talep,
        string firma,
        double birimFiyat,
        KullaniciProfili user)
    {
        OturumAc(user);
        var teklif = new SatinalmaTeklif
        {
            Id = Guid.NewGuid(),
            FirmaAdi = firma,
            Marka = "E2E",
            VadeGunu = 30,
            TeslimSuresi = "7 gün",
            OdemeSekli = "Havale"
        };

        foreach (var kalem in talep.Kalemler)
        {
            teklif.Fiyatlar.Add(new SatinalmaTeklifFiyati
            {
                KalemId = kalem.Id,
                BirimFiyat = (decimal)birimFiyat,
                KdvOrani = 20
            });
        }

        teklif.FiyatlariHesapla(talep.Kalemler);
        talep.Teklifler.Add(teklif);

        var quoteDoc = new EnterpriseQuoteKaydi
        {
            QuoteId = teklif.Id.ToString(),
            FirmName = firma,
            LinePrices = talep.Kalemler.Select(k => new EnterpriseLinePriceKaydi
            {
                LineItemId = kalemId(k),
                UnitPrice = birimFiyat
            }).ToList()
        };

        if (!Quotes.TryGetValue(talep.Id.ToString(), out var liste))
        {
            liste = [];
            Quotes[talep.Id.ToString()] = liste;
        }
        liste.Add(quoteDoc);

        talep.Status = ProcurementStatus.QuoteEntry;
        talep.Durum = SatinalmaTalepDurumlari.TeklifGirisi;
        Kaydet(talep);
        return teklif;

        static string kalemId(SatinalmaTalepKalemi k) => k.Id.ToString();
    }

    public void YonetimeTeklifGonder(SatinalmaTalep talep, KullaniciProfili user)
    {
        OturumAc(user);
        if (talep.Teklifler.Count == 0)
            throw new InvalidOperationException("Teklif yok");

        foreach (var t in talep.Teklifler)
            if (t.GenelToplam <= 0)
                throw new InvalidOperationException($"Geçersiz teklif: {t.FirmaAdi}");

        var oneri = talep.OnerilenTeklif() ?? throw new InvalidOperationException("Öneri yok");
        if (!talep.SatinalmaOnerisiElleSecildi)
            talep.YonetimOnerilenTeklifId = oneri.Id;

        talep.Status = ProcurementStatus.ManagementQuoteReview;
        talep.Durum = SatinalmaTalepDurumlari.YonetimOnayinda;
        Kaydet(talep);
        foreach (var (hedefRol, _) in BildirimRolPolitikasi.TeklifOnaydaHedefleri())
            BildirimVeFcm(BildirimTipleri.TeklifOnayda, talep, hedefRol: hedefRol);
    }

    public void SiparisOlustur(SatinalmaTalep talep, KullaniciProfili user)
    {
        OturumAc(user);
        talep.Status = ProcurementStatus.Ordered;
        talep.Durum = SatinalmaTalepDurumlari.SiparisOlusturuldu;
        talep.SiparisNo = YeniSiparisNo();
        Kaydet(talep);
        BildirimVeFcm(BildirimTipleri.SiparisOlusturuldu, talep, hedefRol: KullaniciRolleri.Depo);
        BildirimVeFcm(BildirimTipleri.SiparisOlusturuldu, talep, hedefUid: talep.OlusturanUid);
    }

    public void DepoMalKabulTamamla(SatinalmaTalep talep)
    {
        OturumAc(Depo);
        foreach (var kalem in talep.Kalemler)
        {
            var onceki = Stocks.GetValueOrDefault(kalem.Malzeme);
            var yeni = onceki + (decimal)kalem.Miktar;
            Stocks[kalem.Malzeme] = yeni;

            StockMovements.Add(new StockMovementKaydi
            {
                Id = Guid.NewGuid().ToString(),
                Type = "IN",
                MaterialName = kalem.Malzeme,
                Quantity = kalem.Miktar,
                RequestId = talep.Id.ToString(),
                CreatedByUid = Depo.Uid
            });

            kalem.KabulEdilenMiktar = kalem.Miktar;
            kalem.SiparisTamamlandi = true;
        }

        talep.Status = ProcurementStatus.Completed;
        Kaydet(talep);
        BildirimVeFcm(BildirimTipleri.MalKabulEdildi, talep, hedefRol: KullaniciRolleri.Depo);
        BildirimVeFcm(BildirimTipleri.MalKabulEdildi, talep, hedefUid: talep.OlusturanUid);
    }

    public FirestoreGuvenlikSimulasyonu.IslemSonucu StockMovementYazmayiDene(KullaniciProfili user)
    {
        OturumAc(user);
        var sonuc = FirestoreGuvenlikSimulasyonu.StockMovementOlustur(user.Rol);
        if (sonuc == FirestoreGuvenlikSimulasyonu.IslemSonucu.IzinVerildi)
        {
            StockMovements.Add(new StockMovementKaydi
            {
                Id = Guid.NewGuid().ToString(),
                Type = "IN",
                MaterialName = "Yetkisiz Test",
                Quantity = 1,
                RequestId = "",
                CreatedByUid = user.Uid
            });
        }

        Console.WriteLine(sonuc == FirestoreGuvenlikSimulasyonu.IslemSonucu.PermissionDenied
            ? $"[GÜVENLİK] {user.Rol} stock_movements yazma REDDEDİLDİ (Permission Denied)"
            : $"[GÜVENLİK] {user.Rol} stock_movements yazma izin verildi");

        return sonuc;
    }

    public FirestoreGuvenlikSimulasyonu.IslemSonucu QuotesOkumayiDene(KullaniciProfili user, string requestId)
    {
        OturumAc(user);
        var sonuc = FirestoreGuvenlikSimulasyonu.ProcurementQuotesOku(user.Rol);
        Console.WriteLine(sonuc == FirestoreGuvenlikSimulasyonu.IslemSonucu.PermissionDenied
            ? $"[GÜVENLİK] {user.Rol} procurement_requests/{requestId}/quotes okuma REDDEDİLDİ"
            : $"[GÜVENLİK] {user.Rol} quotes okuma izin verildi");
        return sonuc;
    }

    public PurchaseRequestDetailUiState UiDurumu(SatinalmaTalep talep, KullaniciProfili user) =>
        PurchaseRequestDetailPresenter.BuildUiState(talep, user.Rol);

    public string YeniTalepNo()
    {
        Ayarlar.SonTalepSira++;
        return $"TLP-{DateTime.Now.Year}-{Ayarlar.SonTalepSira:D4}";
    }

    public string YeniSiparisNo()
    {
        Ayarlar.SonSiparisSira++;
        return $"SIP-{DateTime.Now.Year}-{Ayarlar.SonSiparisSira:D4}";
    }

    private void BildirimVeFcm(
        string tip,
        SatinalmaTalep talep,
        string? hedefRol = null,
        string? hedefUid = null,
        string? ek = null)
    {
        var (baslik, mesaj) = BildirimMetniOlusturucu.Olustur(tip, talep, ek: ek);
        Bildirimler.Insert(0, new BildirimKaydi
        {
            Id = Guid.NewGuid(),
            Tip = tip,
            Baslik = baslik,
            Mesaj = mesaj,
            TalepId = talep.Id,
            HedefRol = hedefRol,
            HedefUid = hedefUid ?? "",
            OlusturanUid = AktifKullanici?.Uid ?? "",
            OlusturanAd = AktifKullanici?.AdSoyad ?? ""
        });

        if (!string.IsNullOrWhiteSpace(hedefRol))
            Fcm.PushRol(hedefRol, tip, talep.Id, mesaj);
        else if (!string.IsNullOrWhiteSpace(hedefUid))
            Fcm.PushUid(hedefUid, tip, talep.Id, mesaj);
    }

    private void AksiyonSonrasiBildirim(SatinalmaTalep talep, PurchaseRequestDetailAction action, string? not)
    {
        switch (action)
        {
            case PurchaseRequestDetailAction.DirectApprove:
            case PurchaseRequestDetailAction.ApproveQuote:
                BildirimVeFcm(BildirimTipleri.Onaylandi, talep, hedefRol: KullaniciRolleri.Satinalma);
                BildirimVeFcm(BildirimTipleri.Onaylandi, talep, hedefUid: talep.OlusturanUid);
                break;
            case PurchaseRequestDetailAction.StartQuoteProcess:
                BildirimVeFcm(BildirimTipleri.TeklifIstendi, talep, hedefRol: KullaniciRolleri.Satinalma);
                if (!string.IsNullOrWhiteSpace(talep.OlusturanUid))
                    BildirimVeFcm(BildirimTipleri.TeklifIstendi, talep, hedefUid: talep.OlusturanUid);
                break;
            case PurchaseRequestDetailAction.RejectRequest:
            case PurchaseRequestDetailAction.RejectEntireRequest:
                BildirimVeFcm(BildirimTipleri.Reddedildi, talep, hedefRol: KullaniciRolleri.Satinalma);
                BildirimVeFcm(BildirimTipleri.Reddedildi, talep, hedefUid: talep.OlusturanUid);
                break;
            case PurchaseRequestDetailAction.SendQuotesForRevision:
                BildirimVeFcm(BildirimTipleri.TeklifDuzeltmeIstendi, talep, hedefRol: KullaniciRolleri.Satinalma);
                break;
        }
    }

    public void Temizle()
    {
        Talepler.Clear();
        Bildirimler.Clear();
        Stocks.Clear();
        StockMovements.Clear();
        Quotes.Clear();
        Ayarlar = new SatinalmaAyarlar();
        AktifKullanici = null;
        Fcm.Temizle();
    }
}

public sealed class StockMovementKaydi
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public double Quantity { get; set; }
    public string RequestId { get; set; } = "";
    public string CreatedByUid { get; set; } = "";
}

public sealed class EnterpriseQuoteKaydi
{
    public string QuoteId { get; set; } = "";
    public string FirmName { get; set; } = "";
    public List<EnterpriseLinePriceKaydi> LinePrices { get; set; } = [];
}

public sealed class EnterpriseLinePriceKaydi
{
    public string LineItemId { get; set; } = "";
    public double UnitPrice { get; set; }
}

using SatinalmaPro.Shared.Helpers;
using SatinalmaPro.Shared.Models;
using SatinalmaPro.Shared.Services;

namespace SatinalmaPro.E2eTest;

/// <summary>Firebase olmadan bellek içi satınalma + bildirim ortamı.</summary>
public sealed class BellekTestOrtami
{
    public const string TestEtiketi = "[E2E-TEST]";

    public List<SatinalmaTalep> Talepler { get; } = [];
    public List<BildirimKaydi> Bildirimler { get; } = [];
    public SatinalmaAyarlar Ayarlar { get; set; } = new();
    public KullaniciProfili? AktifKullanici { get; set; }

    public static readonly KullaniciProfili Saha = new()
    {
        Uid = "e2e-saha-uid",
        AdSoyad = "E2E Saha Kullanıcı",
        Rol = KullaniciRolleri.Saha,
        Eposta = "e2e-saha@test.local",
        Aktif = true,
        Saha = "Test Şantiye"
    };

    public static readonly KullaniciProfili Sef = new()
    {
        Uid = "e2e-sef-uid",
        AdSoyad = "E2E Şef Kullanıcı",
        Rol = KullaniciRolleri.Sef,
        Eposta = "e2e-sef@test.local",
        Aktif = true,
        Saha = "Test Şantiye"
    };

    public static readonly KullaniciProfili Yonetim = new()
    {
        Uid = "e2e-yonetim-uid",
        AdSoyad = "E2E Yönetim",
        Rol = KullaniciRolleri.Yonetim,
        Eposta = "e2e-yonetim@test.local",
        Aktif = true
    };

    public static readonly KullaniciProfili Satinalma = new()
    {
        Uid = "e2e-satinalma-uid",
        AdSoyad = "E2E Satınalma",
        Rol = KullaniciRolleri.Satinalma,
        Eposta = "e2e-satinalma@test.local",
        Aktif = true
    };

    public static readonly KullaniciProfili Depo = new()
    {
        Uid = "e2e-depo-uid",
        AdSoyad = "E2E Depo",
        Rol = KullaniciRolleri.Depo,
        Eposta = "e2e-depo@test.local",
        Aktif = true
    };

    public void SetUser(KullaniciProfili user) => AktifKullanici = user;

    public SatinalmaTalep GuncelTalep(Guid id) =>
        Talepler.First(t => t.Id == id);

    public void Kaydet(SatinalmaTalep talep)
    {
        SatinalmaTalepSenkronYardimcisi.Dokun(talep);
        Talepler.RemoveAll(t => t.Id == talep.Id);
        Talepler.Insert(0, talep);
    }

    public void BildirimEkle(BildirimKaydi kayit)
    {
        BildirimBirlestirme.Dokun(kayit);
        Bildirimler.Insert(0, kayit);
    }

    public void BildirimEkle(string tip, SatinalmaTalep talep, string? hedefRol = null, string? hedefUid = null, string? ek = null)
    {
        var (baslik, mesaj) = BildirimMetniOlusturucu.Olustur(tip, talep, ek: ek);
        BildirimEkle(new BildirimKaydi
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
    }

    public IEnumerable<BildirimKaydi> KullaniciBildirimleri(KullaniciProfili user) =>
        BildirimFiltreleme.KullaniciBildirimleri(Bildirimler, user);

    public string YeniTalepNo()
    {
        Ayarlar.SonTalepSira++;
        var yil = DateTime.Now.Year;
        return $"TLP-{yil}-{Ayarlar.SonTalepSira:D4}";
    }

    public string YeniSiparisNo()
    {
        Ayarlar.SonSiparisSira++;
        var yil = DateTime.Now.Year;
        return $"SIP-{yil}-{Ayarlar.SonSiparisSira:D4}";
    }

    public SatinalmaTalep SahaTalepOlustur(KullaniciProfili saha, string malzeme = "E2E Test Çimento", double miktar = 10)
    {
        var kalem = new SatinalmaTalepKalemi
        {
            Id = Guid.NewGuid(),
            SiraNo = 1,
            Malzeme = malzeme,
            Miktar = miktar,
            Birim = "Torba",
            Aciklama = TestEtiketi
        };
        var talep = new SatinalmaTalep
        {
            Id = Guid.NewGuid(),
            TalepNo = YeniTalepNo(),
            Tarih = DateTime.Now.ToString("dd.MM.yyyy"),
            TalepEden = saha.AdSoyad,
            OlusturanUid = saha.Uid,
            OlusturanRol = saha.Rol,
            SantiyeAdi = saha.Saha ?? "Test Şantiye",
            TalepAciklamasi = TestEtiketi,
            TalepTuru = TalepTurleri.Normal,
            Durum = SatinalmaTalepDurumlari.ImzaSurecinde,
            Kalemler = [kalem]
        };
        Kaydet(talep);
        SetUser(saha);
        BildirimEkle(BildirimTipleri.YonetimeGonderildi, talep, hedefRol: KullaniciRolleri.Yonetim);
        return talep;
    }

    public void YonetimTeklifIste(SatinalmaTalep talep)
    {
        SetUser(Yonetim);
        talep.Durum = SatinalmaTalepDurumlari.TeklifGirisi;
        Kaydet(talep);
        BildirimEkle(BildirimTipleri.TeklifIstendi, talep, hedefRol: KullaniciRolleri.Satinalma);
        BildirimEkle(BildirimTipleri.TeklifIstendi, talep, hedefUid: talep.OlusturanUid);
    }

    public SatinalmaTeklif TeklifEkle(SatinalmaTalep talep, string firma, double birimFiyat)
    {
        SetUser(Satinalma);
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
        talep.Durum = SatinalmaTalepDurumlari.Karsilastirma;
        if (!talep.SatinalmaOnerisiElleSecildi)
            talep.YonetimOnerilenTeklifId = talep.EnDusukFiyatliTeklif()?.Id;
        Kaydet(talep);
        return teklif;
    }

    public void YonetimeTeklifGonder(SatinalmaTalep talep)
    {
        SetUser(Satinalma);
        foreach (var t in talep.Teklifler)
            if (t.GenelToplam <= 0) throw new InvalidOperationException($"Geçersiz teklif: {t.FirmaAdi}");
        var oneri = talep.OnerilenTeklif() ?? throw new InvalidOperationException("Öneri yok");
        if (!talep.SatinalmaOnerisiElleSecildi)
            talep.YonetimOnerilenTeklifId = oneri.Id;
        talep.Durum = SatinalmaTalepDurumlari.YonetimOnayinda;
        Kaydet(talep);
        BildirimEkle(BildirimTipleri.TeklifOnayda, talep, hedefRol: KullaniciRolleri.Yonetim);
    }

    public void YonetimTeklifOnayla(SatinalmaTalep talep, Guid teklifId)
    {
        SetUser(Yonetim);
        foreach (var k in talep.Kalemler)
            k.OnaylananTeklifId = teklifId;
        foreach (var t in talep.Teklifler)
            t.Onaylandi = t.Id == teklifId;
        talep.OnaylananTeklifId = teklifId;
        talep.YonetimOnayKilitli = true;
        talep.Durum = SatinalmaTalepDurumlari.Onaylandi;
        talep.FirmaSiparisNolari[teklifId] = YeniSiparisNo();
        talep.SiparisNo = talep.FirmaSiparisNolari[teklifId];
        Kaydet(talep);
        BildirimEkle(BildirimTipleri.Onaylandi, talep, hedefRol: KullaniciRolleri.Satinalma);
        BildirimEkle(BildirimTipleri.Onaylandi, talep, hedefUid: talep.OlusturanUid);
    }

    public void SiparisVer(SatinalmaTalep talep)
    {
        SetUser(Satinalma);
        talep.Durum = SatinalmaTalepDurumlari.SiparisOlusturuldu;
        Kaydet(talep);
        BildirimEkle(BildirimTipleri.SiparisOlusturuldu, talep, hedefRol: KullaniciRolleri.Satinalma);
        BildirimEkle(BildirimTipleri.SiparisOlusturuldu, talep, hedefUid: talep.OlusturanUid);
    }

    public void MalKabul(SatinalmaTalep talep, Guid kalemId, double miktar)
    {
        SetUser(Satinalma);
        var kalem = talep.Kalemler.First(k => k.Id == kalemId);
        kalem.KabulEdilenMiktar += miktar;
        kalem.SiparisTamamlandi = kalem.KabulEdilenMiktar >= kalem.Miktar;
        Kaydet(talep);
        BildirimEkle(BildirimTipleri.MalKabulEdildi, talep, hedefRol: KullaniciRolleri.Satinalma);
        BildirimEkle(BildirimTipleri.MalKabulEdildi, talep, hedefRol: KullaniciRolleri.Depo);
        BildirimEkle(BildirimTipleri.MalKabulEdildi, talep, hedefUid: talep.OlusturanUid);
    }

    public void Temizle()
    {
        Talepler.Clear();
        Bildirimler.Clear();
        Ayarlar = new SatinalmaAyarlar();
        AktifKullanici = null;
    }
}

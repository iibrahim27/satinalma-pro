using System.Windows.Media;
using SatinalmaPro.Models;
using SatinalmaPro.Models.SatinalmaMerkezi;
using SharedTalepDurumlari = SatinalmaPro.Shared.Models.SatinalmaTalepDurumlari;
using SharedTalepTurleri = SatinalmaPro.Shared.Models.TalepTurleri;

namespace SatinalmaPro.Services;

/// <summary>SatinalmaDepo taleplerini Satınalma Merkezi UI modellerine dönüştürür.</summary>
public static class SatinalmaMerkeziVeriServisi
{
    public static bool FirebaseVerisiKullanilabilir =>
        OturumYoneticisi.BulutAktif && OturumYoneticisi.GirisYapildi;

    public static async Task VerileriHazirlaAsync(CancellationToken iptal = default)
    {
        SatinalmaDepo.Yukle();
        if (FirebaseVerisiKullanilabilir)
            await IadeDeposu.YukleAsync(iptal).ConfigureAwait(false);
    }

    public static void VerileriHazirla()
    {
        VerileriHazirlaAsync().GetAwaiter().GetResult();
    }

    public static IReadOnlyList<KpiKartModel> KpiKartlari()
    {
        if (!FirebaseVerisiKullanilabilir)
            return SatinalmaMerkeziMockServisi.KpiKartlari();

        var t = SatinalmaDepo.Talepler;
        return
        [
            Kpi("Bekleyen Talepler", t.Count(x => Bekleyen(x)).ToString(), "#F59E0B", "bekleyen"),
            Kpi("Teklif Hazırlanacak", t.Count(x => x.Durum is SharedTalepDurumlari.TeklifGirisi or SharedTalepDurumlari.Karsilastirma).ToString(), "#7C3AED", "teklif"),
            Kpi("Yönetim Onayı Bekleyen", t.Count(x => x.Durum == SharedTalepDurumlari.YonetimOnayinda).ToString(), "#2563EB", "onay"),
            Kpi("Sipariş Oluşturulacak", t.Count(x => x.Durum == SharedTalepDurumlari.Onaylandi).ToString(), "#0891B2", "siparis"),
            Kpi("Siparişe Dönüşen", t.Count(x => x.Durum == SharedTalepDurumlari.SiparisOlusturuldu).ToString(), "#0D9488", "teslimat"),
            Kpi("Reddedilen", t.Count(x => x.Durum == SharedTalepDurumlari.Reddedildi).ToString(), "#DC2626", "iade"),
            Kpi("Toplam Talep", t.Count.ToString(), "#059669", "bugun"),
            Kpi("Acil Talepler", t.Count(x => x.TalepTuru == SharedTalepTurleri.Acil).ToString(), "#EA580C", "kismi")
        ];
    }

    public static IReadOnlyList<TalepSatirModel> Talepler() =>
        FirebaseVerisiKullanilabilir
            ? SatinalmaDepo.Talepler.Select(TalepSatir).OrderByDescending(x => x.TalepNo).ToList()
            : SatinalmaMerkeziMockServisi.Talepler();

    public static TalepDetayModel TalepDetay(Guid id)
    {
        if (FirebaseVerisiKullanilabilir)
        {
            var talep = SatinalmaDepo.Talepler.FirstOrDefault(t => t.Id == id);
            if (talep is not null)
                return DetayOlustur(talep);
        }

        return SatinalmaMerkeziMockServisi.TalepDetay(id);
    }

    public static IReadOnlyList<SiparisSatirModel> Siparisler()
    {
        if (!FirebaseVerisiKullanilabilir)
            return SatinalmaMerkeziMockServisi.Siparisler();

        return SatinalmaDepo.Talepler
            .Where(t => t.Durum == SharedTalepDurumlari.SiparisOlusturuldu && !string.IsNullOrWhiteSpace(t.SiparisNo))
            .Select(SiparisSatir)
            .ToList();
    }

    public static IReadOnlyList<SiparisSatirModel> BeklenenSiparisler() =>
        Siparisler().Where(s => s.Durum is "Sevkiyatta" or "Bekleniyor" or "Kısmi Teslim").ToList();

    public static IReadOnlyList<YapilacakIsModel> YapilacakIsler()
    {
        if (!FirebaseVerisiKullanilabilir)
            return SatinalmaMerkeziMockServisi.YapilacakIsler();

        return SatinalmaDepo.Talepler.Where(Bekleyen).Take(8).Select(t => new YapilacakIsModel
        {
            Baslik = DurumBaslik(t.Durum),
            Aciklama = t.TalepNo,
            Oncelik = t.TalepTuru == SharedTalepTurleri.Acil ? "Acil" : "Normal",
            OncelikRenk = t.TalepTuru == SharedTalepTurleri.Acil ? Brush("#DC2626") : Brush("#F59E0B"),
            IlgiliNo = t.TalepNo
        }).ToList();
    }

    public static IReadOnlyList<SonHareketModel> SonHareketler() =>
        FirebaseVerisiKullanilabilir
            ? SatinalmaDepo.Talepler.OrderByDescending(t => t.GuncellemeUtc).Take(6).Select(t => new SonHareketModel
            {
                Mesaj = $"{t.TalepNo} — {t.Durum}",
                Kullanici = t.TalepEden,
                Zaman = t.Tarih
            }).ToList()
            : SatinalmaMerkeziMockServisi.SonHareketler();

    public static IReadOnlyList<BildirimModel> Bildirimler()
    {
        if (!FirebaseVerisiKullanilabilir)
            return SatinalmaMerkeziMockServisi.Bildirimler();

        return BildirimDeposu.Bildirimler.Take(20).Select(b => new BildirimModel
        {
            Baslik = b.Baslik,
            Mesaj = b.Mesaj,
            Zaman = b.OlusturmaTarihi,
            Okundu = b.Okundu
        }).ToList();
    }

    public static IReadOnlyList<DepoTakipSatirModel> DepoTakip()
    {
        if (!FirebaseVerisiKullanilabilir)
            return SatinalmaMerkeziMockServisi.DepoTakip();

        var satirlar = new List<DepoTakipSatirModel>();
        foreach (var t in SatinalmaDepo.Talepler.Where(t => t.Durum == SharedTalepDurumlari.SiparisOlusturuldu))
        {
            foreach (var k in t.Kalemler.Where(k => k.OnaylananTeklifId != null))
            {
                var siparisMiktar = (decimal)k.Miktar;
                var teslim = (decimal)k.KabulEdilenMiktar;
                var kalan = Math.Max(0, siparisMiktar - teslim);
                var (durum, renk) = KalemTeslimDurumu(k);
                satirlar.Add(new DepoTakipSatirModel
                {
                    Malzeme = k.Malzeme,
                    SiparisMiktari = siparisMiktar,
                    TeslimAlinan = teslim,
                    Kalan = kalan,
                    Eksik = kalan,
                    Fazla = Math.Max(0, teslim - siparisMiktar),
                    Durum = durum,
                    DurumRenk = Brush(renk)
                });
            }
        }

        return satirlar;
    }

    public static IReadOnlyList<TedarikciPerformansModel> TedarikciPerformans()
    {
        if (!FirebaseVerisiKullanilabilir)
            return SatinalmaMerkeziMockServisi.TedarikciPerformans();

        var gruplar = SatinalmaDepo.Talepler
            .Where(t => t.Durum == SharedTalepDurumlari.SiparisOlusturuldu && t.OnaylananTeklif is not null)
            .GroupBy(t => t.OnaylananTeklif!.FirmaAdi, StringComparer.OrdinalIgnoreCase);

        return gruplar.Select(g =>
        {
            var siparisSayisi = g.Count();
            var toplam = g.Sum(t => t.OnaylananTeklif?.GenelToplam ?? 0m);
            var tamTeslim = g.Count(t => t.Kalemler.Where(k => k.OnaylananTeklifId != null)
                .All(k => k.SiparisTamamlandi || k.KabulEdilenMiktar >= k.Miktar - 0.0001));
            var kismi = g.Count() - tamTeslim;
            var puan = siparisSayisi == 0 ? 0 : (int)Math.Round(100.0 * tamTeslim / siparisSayisi);
            return new TedarikciPerformansModel
            {
                Firma = g.Key,
                ToplamSiparis = siparisSayisi,
                ToplamTutar = toplam,
                ZamanindaTeslim = tamTeslim,
                EksikTeslim = kismi,
                Iade = 0,
                Kalite = puan,
                OrtTeslimSuresi = "—",
                PerformansPuani = puan
            };
        }).OrderByDescending(x => x.ToplamSiparis).ToList();
    }

    public static IReadOnlyList<IadeSatirModel> Iadeler()
    {
        if (!FirebaseVerisiKullanilabilir)
            return SatinalmaMerkeziMockServisi.Iadeler();

        var satirlar = IadeDeposu.MerkeziSatirlar();
        return satirlar.Count > 0 ? satirlar : [];
    }

    public static IReadOnlyList<TamamlananSatirModel> Tamamlananlar()
    {
        if (!FirebaseVerisiKullanilabilir)
            return SatinalmaMerkeziMockServisi.Tamamlananlar();

        var liste = new List<TamamlananSatirModel>();
        foreach (var t in SatinalmaDepo.Talepler.Where(t => t.Durum == SharedTalepDurumlari.SiparisOlusturuldu))
        {
            var onayli = t.Kalemler.Where(k => k.OnaylananTeklifId != null).ToList();
            if (onayli.Count == 0)
                continue;

            if (onayli.All(k => k.SiparisTamamlandi || k.KabulEdilenMiktar >= k.Miktar - 0.0001))
            {
                liste.Add(new TamamlananSatirModel
                {
                    KayitNo = t.SiparisNo.Length > 0 ? t.SiparisNo : t.TalepNo,
                    Tip = "Sipariş",
                    Santiye = SantiyeMetni(t),
                    Firma = t.OnaylananTeklif?.FirmaAdi ?? "—",
                    Tutar = (t.OnaylananTeklif?.GenelToplam ?? 0m).ToString("N2") + " ₺",
                    TamamlanmaTarihi = t.YonetimOnayTarihi.Length > 0 ? t.YonetimOnayTarihi : t.Tarih,
                    Durum = "Tam Teslim"
                });
            }
        }

        foreach (var t in SatinalmaDepo.Talepler.Where(t => t.Durum == SharedTalepDurumlari.Onaylandi))
        {
            liste.Add(new TamamlananSatirModel
            {
                KayitNo = t.TalepNo,
                Tip = "Talep",
                Santiye = SantiyeMetni(t),
                Firma = "—",
                Tutar = "—",
                TamamlanmaTarihi = t.Tarih,
                Durum = "Siparişe Dönüştü"
            });
        }

        return liste;
    }

    private static bool Bekleyen(SatinalmaTalep t) =>
        t.Durum is not SharedTalepDurumlari.Taslak
            and not SharedTalepDurumlari.SiparisOlusturuldu
            and not SharedTalepDurumlari.Reddedildi;

    private static KpiKartModel Kpi(string baslik, string deger, string renk, string anahtar) =>
        new() { Baslik = baslik, Deger = deger, Renk = Brush(renk), FiltreAnahtar = anahtar };

    private static SolidColorBrush Brush(string hex) =>
        (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;

    private static string SantiyeMetni(SatinalmaTalep t) =>
        !string.IsNullOrWhiteSpace(t.SantiyeAdi) ? t.SantiyeAdi
        : !string.IsNullOrWhiteSpace(t.OlusturanRol) ? t.OlusturanRol
        : t.TalepEden;

    private static (string durum, string renk) KalemTeslimDurumu(SatinalmaTalepKalemi k)
    {
        if (k.SiparisTamamlandi || k.KabulEdilenMiktar >= k.Miktar - 0.0001)
            return ("Tam Teslim", "#059669");
        if (k.KabulEdilenMiktar > 0)
            return ("Kısmi Teslim", "#EA580C");
        return ("Bekleniyor", "#F59E0B");
    }

    private static (string durum, string renk) SiparisDurum(SatinalmaTalep t)
    {
        var onayli = t.Kalemler.Where(k => k.OnaylananTeklifId != null).ToList();
        if (onayli.Count == 0)
            return ("Sevkiyatta", "#2563EB");
        if (onayli.All(k => k.SiparisTamamlandi || k.KabulEdilenMiktar >= k.Miktar - 0.0001))
            return ("Tam Teslim", "#059669");
        if (onayli.Any(k => k.KabulEdilenMiktar > 0))
            return ("Kısmi Teslim", "#EA580C");
        return ("Sevkiyatta", "#2563EB");
    }

    private static TalepSatirModel TalepSatir(SatinalmaTalep t) => new()
    {
        Id = t.Id,
        TalepNo = t.TalepNo,
        TalepTarihi = t.Tarih,
        Santiye = SantiyeMetni(t),
        TalepEden = t.TalepEden,
        Oncelik = t.TalepTuru,
        Durum = UiDurum(t.Durum),
        YonetimKarari = t.YonetimOnayKilitli ? "Onaylandı" : "Beklemede",
        SonIslemTarihi = t.Tarih,
        DurumRenk = DurumRenk(t.Durum),
        OncelikRenk = t.TalepTuru == SharedTalepTurleri.Acil ? Brush("#DC2626") : Brush("#64748B")
    };

    private static SiparisSatirModel SiparisSatir(SatinalmaTalep t)
    {
        var toplam = t.OnaylananTeklif?.GenelToplam ?? 0m;
        var (durum, renk) = SiparisDurum(t);
        return new SiparisSatirModel
        {
            Id = t.Id,
            SiparisNo = t.SiparisNo,
            Firma = t.OnaylananTeklif?.FirmaAdi ?? "—",
            TalepNo = t.TalepNo,
            Santiye = SantiyeMetni(t),
            ToplamTutar = toplam,
            SiparisTarihi = t.YonetimOnayTarihi.Length > 0 ? t.YonetimOnayTarihi : t.Tarih,
            Durum = durum,
            DurumRenk = Brush(renk)
        };
    }

    private static TalepDetayModel DetayOlustur(SatinalmaTalep t) => new()
    {
        Id = t.Id,
        TalepNo = t.TalepNo,
        Santiye = SantiyeMetni(t),
        TalepEden = t.TalepEden,
        Tarih = t.Tarih,
        Oncelik = t.TalepTuru,
        Durum = UiDurum(t.Durum),
        YonetimKarari = t.YonetimOnayKilitli ? "Onaylandı" : "Beklemede",
        Aciklama = t.TalepAciklamasi,
        Malzemeler = t.Kalemler.Select(k => new DetayMalzemeModel
        {
            Ad = k.Malzeme,
            Miktar = k.Miktar.ToString("N2"),
            Birim = k.Birim
        }).ToList(),
        Teklifler = t.Teklifler.Select(teklif => new TeklifSatirModel
        {
            Firma = teklif.FirmaAdi,
            Marka = teklif.Marka,
            BirimFiyat = teklif.Fiyatlar.FirstOrDefault()?.BirimFiyat ?? 0,
            Iskonto = 0,
            Kdv = (decimal)teklif.KdvOrani,
            Toplam = teklif.GenelToplam,
            TeslimSuresi = teklif.TeslimSuresi,
            Vade = $"{teklif.VadeGunu} gün",
            TeklifTarihi = t.Tarih,
            Dosya = "—",
            Durum = teklif.Onaylandi ? "Onaylı" : "Bekliyor"
        }).ToList()
    };

    private static string UiDurum(string durum) => durum switch
    {
        SharedTalepDurumlari.YonetimOnayinda => "Onay Bekleyen",
        SharedTalepDurumlari.TeklifGirisi => "Teklif Bekleyen",
        SharedTalepDurumlari.Karsilastirma => "Teklif Hazırlanıyor",
        SharedTalepDurumlari.Onaylandi => "Onaylandı",
        SharedTalepDurumlari.SiparisOlusturuldu => "Siparişe Dönüşen",
        SharedTalepDurumlari.Reddedildi => "Reddedildi",
        _ => "Bekleyen"
    };

    private static Brush DurumRenk(string durum) => durum switch
    {
        SharedTalepDurumlari.Reddedildi => Brush("#DC2626"),
        SharedTalepDurumlari.Onaylandi => Brush("#059669"),
        SharedTalepDurumlari.SiparisOlusturuldu => Brush("#2563EB"),
        SharedTalepDurumlari.YonetimOnayinda => Brush("#2563EB"),
        SharedTalepDurumlari.TeklifGirisi or SharedTalepDurumlari.Karsilastirma => Brush("#7C3AED"),
        _ => Brush("#F59E0B")
    };

    private static string DurumBaslik(string durum) => durum switch
    {
        SharedTalepDurumlari.YonetimOnayinda => "Yönetim onayı bekleniyor",
        SharedTalepDurumlari.TeklifGirisi => "Teklif girilecek",
        SharedTalepDurumlari.Onaylandi => "Sipariş oluşturulacak",
        _ => "Talep takibi"
    };
}

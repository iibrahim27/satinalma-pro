using SatinalmaPro.Models;
using SatinalmaPro.Shared.Services;
using SharedBildirimTipleri = SatinalmaPro.Shared.Models.BildirimTipleri;

namespace SatinalmaPro.Services;

public static class SatinalmaBildirimleri
{
    private static string Simdi() => DateTime.Now.ToString("dd.MM.yyyy HH:mm");

    private static (string uid, string ad) Olusturan() =>
        (OturumYoneticisi.Auth?.Uid ?? "", OturumYoneticisi.AktifKullanici?.AdSoyad ?? "");

    private static (string Baslik, string Mesaj) Metin(string tip, SatinalmaTalep talep, string? firmaAdi = null, string? ek = null)
    {
        var malzemeler = talep.Kalemler?.OrderBy(k => k.SiraNo).Select(k => k.Malzeme);
        return BildirimMetniOlusturucu.Olustur(tip, talep.TalepNo, talep.TalepEden, talep.TalepAciklamasi, malzemeler, firmaAdi, ek);
    }

    private static BildirimKaydi Kayit(
        SatinalmaTalep talep,
        string tip,
        string baslik,
        string mesaj,
        string? hedefRol = null,
        string? hedefUid = null) =>
        new()
        {
            Baslik = baslik,
            Mesaj = mesaj,
            Tip = tip,
            TalepId = talep.Id,
            HedefRol = hedefRol,
            HedefUid = hedefUid,
            OlusturanUid = Olusturan().uid,
            OlusturanAd = Olusturan().ad,
            OlusturmaTarihi = Simdi(),
            GuncellemeUtc = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

    public static Task YonetimeGonderildiAsync(SatinalmaTalep talep)
    {
        var (baslik, mesaj) = Metin(SharedBildirimTipleri.YonetimeGonderildi, talep);
        return BildirimDeposu.CokluEkleAsync(
        [
            Kayit(talep, SharedBildirimTipleri.YonetimeGonderildi, baslik, mesaj, hedefRol: KullaniciRolleri.Yonetim),
            Kayit(talep, SharedBildirimTipleri.YonetimeGonderildi, baslik, mesaj, hedefRol: KullaniciRolleri.Satinalma)
        ]);
    }

    public static Task TeklifIstendiAsync(SatinalmaTalep talep)
    {
        var (baslik, mesaj) = Metin(SharedBildirimTipleri.TeklifIstendi, talep);
        return BildirimYoneticisi.EkleAsync(Kayit(talep, SharedBildirimTipleri.TeklifIstendi, baslik, mesaj, hedefRol: KullaniciRolleri.Satinalma));
    }

    public static Task TeklifOnaydaAsync(SatinalmaTalep talep)
    {
        var (baslik, mesaj) = Metin(SharedBildirimTipleri.TeklifOnayda, talep);
        return BildirimYoneticisi.EkleAsync(Kayit(talep, SharedBildirimTipleri.TeklifOnayda, baslik, mesaj, hedefRol: KullaniciRolleri.Yonetim));
    }

    public static Task TeklifDuzeltmeyeGonderildiAsync(SatinalmaTalep talep, string? not = null)
    {
        var (baslik, mesaj) = Metin(SharedBildirimTipleri.TeklifDuzeltmeIstendi, talep, ek: not);
        return BildirimYoneticisi.EkleAsync(Kayit(talep, SharedBildirimTipleri.TeklifDuzeltmeIstendi, baslik, mesaj, hedefRol: KullaniciRolleri.Satinalma));
    }

    public static Task OnaylandiAsync(SatinalmaTalep talep, string? firmaAdi = null, string? hedefRol = null, string? hedefUid = null)
    {
        var (baslik, mesaj) = Metin(SharedBildirimTipleri.Onaylandi, talep, firmaAdi);
        return BildirimYoneticisi.EkleAsync(Kayit(talep, SharedBildirimTipleri.Onaylandi, baslik, mesaj, hedefRol, hedefUid));
    }

    public static Task ReddedildiAsync(SatinalmaTalep talep, string gerekce)
    {
        var (baslik, mesaj) = Metin(SharedBildirimTipleri.Reddedildi, talep, ek: gerekce);
        return BildirimYoneticisi.EkleAsync(Kayit(talep, SharedBildirimTipleri.Reddedildi, baslik, mesaj, hedefUid: talep.OlusturanUid));
    }

    public static Task SiparisOlusturulduAsync(SatinalmaTalep talep)
    {
        var ek = string.IsNullOrWhiteSpace(talep.SiparisNo) ? null : $"Sipariş No: {talep.SiparisNo}";
        var (baslik, mesaj) = Metin(SharedBildirimTipleri.SiparisOlusturuldu, talep, ek: ek);
        var kayitlar = new List<BildirimKaydi>
        {
            Kayit(talep, SharedBildirimTipleri.SiparisOlusturuldu, baslik, mesaj, hedefRol: KullaniciRolleri.Satinalma)
        };
        if (!string.IsNullOrWhiteSpace(talep.OlusturanUid))
            kayitlar.Add(Kayit(talep, SharedBildirimTipleri.SiparisOlusturuldu, baslik, mesaj, hedefUid: talep.OlusturanUid));

        return BildirimDeposu.CokluEkleAsync(kayitlar);
    }

    public static Task MalKabulEdildiAsync(SatinalmaTalep talep, string? malzemeOzeti = null)
    {
        var (baslik, mesaj) = Metin(SharedBildirimTipleri.MalKabulEdildi, talep, ek: malzemeOzeti);
        var kayitlar = new List<BildirimKaydi>
        {
            Kayit(talep, SharedBildirimTipleri.MalKabulEdildi, baslik, mesaj, hedefRol: KullaniciRolleri.Satinalma),
            Kayit(talep, SharedBildirimTipleri.MalKabulEdildi, baslik, mesaj, hedefRol: KullaniciRolleri.Depo)
        };
        if (!string.IsNullOrWhiteSpace(talep.OlusturanUid))
            kayitlar.Add(Kayit(talep, SharedBildirimTipleri.MalKabulEdildi, baslik, mesaj, hedefUid: talep.OlusturanUid));

        return BildirimDeposu.CokluEkleAsync(kayitlar);
    }
}

public static class TalepTurleri
{
    public const string Acil = "Acil";
    public const string Normal = "Normal";

    public static string GorunenAd(string tur) => tur switch
    {
        Acil => "Acil",
        Normal => "Normal",
        _ => string.IsNullOrWhiteSpace(tur) ? Normal : tur
    };
}

using SatinalmaPro.Models;

using SatinalmaPro.Shared.Helpers;
using SatinalmaPro.Shared.Services;

using SharedBildirimTipleri = SatinalmaPro.Shared.Models.BildirimTipleri;



namespace SatinalmaPro.Services;



public static class SatinalmaBildirimleri

{

    private static string Simdi() => DateTime.Now.ToString("dd.MM.yyyy HH:mm");



    private static (string uid, string ad) Olusturan() =>

        (OturumYoneticisi.Auth?.Uid ?? "", OturumYoneticisi.AktifKullanici?.AdSoyad ?? "");



    private static (string Baslik, string Mesaj) Metin(
        string tip,
        SatinalmaTalep talep,
        string? firmaAdi = null,
        string? ek = null,
        string? onaylayanRol = null)
    {
        var malzemeler = talep.Kalemler?.OrderBy(k => k.SiraNo).Select(k => k.Malzeme);
        return BildirimMetniOlusturucu.Olustur(
            tip, talep.TalepNo, talep.TalepEden, talep.TalepAciklamasi, malzemeler, firmaAdi, ek, onaylayanRol);
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
        return BildirimYoneticisi.CokluEkleAsync(
            BildirimRolPolitikasi.YonetimeGonderildiHedefleri()
                .Select(h => Kayit(talep, SharedBildirimTipleri.YonetimeGonderildi, baslik, mesaj, h.HedefRol, h.HedefUid))
                .ToList());
    }



    public static Task TeklifIstendiAsync(SatinalmaTalep talep)
    {
        var (baslik, mesaj) = Metin(SharedBildirimTipleri.TeklifIstendi, talep);
        return BildirimYoneticisi.EkleAsync(Kayit(talep, SharedBildirimTipleri.TeklifIstendi, baslik, mesaj, hedefRol: KullaniciRolleri.Satinalma));
    }

    public static Task TeklifIstendiOlusturucuyaAsync(SatinalmaTalep talep)
    {
        var onaylayanRol = KullaniciRolleri.Normalize(OturumYoneticisi.AktifKullanici?.Rol);
        var (baslik, mesaj) = Metin(
            SharedBildirimTipleri.TeklifIstendi,
            talep,
            ek: OnayBildirimYardimcisi.TeklifIstemeBildirimEk(onaylayanRol));
        return BildirimYoneticisi.EkleAsync(
            Kayit(talep, SharedBildirimTipleri.TeklifIstendi, baslik, mesaj, hedefUid: talep.OlusturanUid));
    }


    public static Task TeklifOnaydaAsync(SatinalmaTalep talep)
    {
        var (baslik, mesaj) = Metin(SharedBildirimTipleri.TeklifOnayda, talep);
        return BildirimYoneticisi.CokluEkleAsync(
            BildirimRolPolitikasi.TeklifOnaydaHedefleri()
                .Select(h => Kayit(talep, SharedBildirimTipleri.TeklifOnayda, baslik, mesaj, h.HedefRol, h.HedefUid))
                .ToList());
    }

    public static Task TeklifDuzeltmeyeGonderildiAsync(SatinalmaTalep talep, string? not = null)
    {
        var (baslik, mesaj) = Metin(SharedBildirimTipleri.TeklifDuzeltmeIstendi, talep, ek: not);
        return BildirimYoneticisi.EkleAsync(Kayit(talep, SharedBildirimTipleri.TeklifDuzeltmeIstendi, baslik, mesaj, hedefRol: KullaniciRolleri.Satinalma));
    }



    public static Task OnaylandiAsync(
        SatinalmaTalep talep,
        string? firmaAdi = null,
        string? hedefRol = null,
        string? hedefUid = null,
        string? onaylayanRol = null)
    {
        onaylayanRol ??= KullaniciRolleri.Normalize(OturumYoneticisi.AktifKullanici?.Rol);
        var (baslik, mesaj) = Metin(SharedBildirimTipleri.Onaylandi, talep, firmaAdi, onaylayanRol: onaylayanRol);
        return BildirimYoneticisi.EkleAsync(
            Kayit(talep, SharedBildirimTipleri.Onaylandi, baslik, mesaj, hedefRol, hedefUid));
    }

    public static async Task OnaylandiBildirimleriGonderAsync(SatinalmaTalep talep, string? firmaAdi = null)
    {
        var onaylayanRol = KullaniciRolleri.Normalize(OturumYoneticisi.AktifKullanici?.Rol);
        foreach (var (hedefRol, hedefUid) in OnayBildirimYardimcisi.OnaylandiHedefleri(talep.OlusturanUid, onaylayanRol))
        {
            try
            {
                await OnaylandiAsync(talep, firmaAdi, hedefRol, hedefUid, onaylayanRol);
            }
            catch (Exception ex)
            {
                HataGunlugu.Kaydet(ex, "SatinalmaBildirimleri.Onaylandi");
            }
        }
    }


    public static Task ReddedildiAsync(SatinalmaTalep talep, string gerekce)
    {
        var (baslik, mesaj) = Metin(SharedBildirimTipleri.Reddedildi, talep, ek: gerekce);
        var actorUid = Olusturan().uid;
        return BildirimYoneticisi.CokluEkleAsync(
            BildirimRolPolitikasi.ReddedildiHedefleri(talep.OlusturanUid, actorUid)
                .Select(h => Kayit(talep, SharedBildirimTipleri.Reddedildi, baslik, mesaj, h.HedefRol, h.HedefUid))
                .ToList());
    }



    public static Task SiparisOlusturulduAsync(SatinalmaTalep talep)
    {
        var ek = string.IsNullOrWhiteSpace(talep.SiparisNo) ? null : $"Sipariş No: {talep.SiparisNo}";
        var (baslik, mesaj) = Metin(SharedBildirimTipleri.SiparisOlusturuldu, talep, ek: ek);
        var actorUid = Olusturan().uid;
        return BildirimYoneticisi.CokluEkleAsync(
            BildirimRolPolitikasi.SiparisOlusturulduHedefleri(talep.OlusturanUid, actorUid)
                .Select(h => Kayit(talep, SharedBildirimTipleri.SiparisOlusturuldu, baslik, mesaj, h.HedefRol, h.HedefUid))
                .ToList());
    }



    public static Task MalKabulEdildiAsync(SatinalmaTalep talep, string? malzemeOzeti = null)
    {
        var (baslik, mesaj) = Metin(SharedBildirimTipleri.MalKabulEdildi, talep, ek: malzemeOzeti);
        var actorUid = Olusturan().uid;
        return BildirimYoneticisi.CokluEkleAsync(
            BildirimRolPolitikasi.MalKabulEdildiHedefleri(talep.OlusturanUid, actorUid)
                .Select(h => Kayit(talep, SharedBildirimTipleri.MalKabulEdildi, baslik, mesaj, h.HedefRol, h.HedefUid))
                .ToList());
    }

}


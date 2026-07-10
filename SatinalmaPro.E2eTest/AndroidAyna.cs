using SatinalmaPro.Shared.Helpers;
using SatinalmaPro.Shared.Models;
using SatinalmaPro.Shared.Services;

namespace SatinalmaPro.E2eTest;

/// <summary>Android TalepKuyrugu + BildirimRota mantığının test karşılaştırması için sadeleştirilmiş ayna.</summary>
public static class AndroidAyna
{
    public static bool SatinalmaSiparisListede(SatinalmaTalep t, IEnumerable<OnaylananMalzemeSatiri> malzemeler)
    {
        if (t.Durum != SatinalmaTalepDurumlari.SiparisOlusturuldu) return false;
        if (!(t.HerhangiKalemOnayli || t.TeklifsizYonetimOnayi)) return false;
        var kalemler = malzemeler.Where(m => m.TalepId == t.Id).ToList();
        if (kalemler.Count == 0) return false;
        return kalemler.Any(m => m.KalanMiktar > 0.0001);
    }

    public static bool SatinalmaMalKabulListede(SatinalmaTalep t, IEnumerable<OnaylananMalzemeSatiri> malzemeler)
    {
        if (t.Durum != SatinalmaTalepDurumlari.SiparisOlusturuldu) return false;
        if (!(t.HerhangiKalemOnayli || t.TeklifsizYonetimOnayi)) return false;
        var kalemler = malzemeler.Where(m => m.TalepId == t.Id).ToList();
        return kalemler.Count > 0 && kalemler.All(m => m.KalanMiktar <= 0.0001);
    }

    public static bool MasaustuSiparisListede(SatinalmaTalep t, IEnumerable<OnaylananMalzemeSatiri> malzemeler)
    {
        // SiparisVerilenTalepListesiView: SatinalmaSiparisVerilen && !MalKabulTamam
        if (t.Durum != SatinalmaTalepDurumlari.SiparisOlusturuldu) return false;
        if (!(t.HerhangiKalemOnayli || t.TeklifsizYonetimOnayi)) return false;
        var kalemler = malzemeler.Where(m => m.TalepId == t.Id).ToList();
        return kalemler.Count > 0 && kalemler.Any(m => m.KalanMiktar > 0.0001);
    }

    public static string AndroidBildirimRoute(string tip, Guid? talepId, string? rol)
    {
        rol = KullaniciRolleri.Normalize(rol);
        if (talepId is not null && tip == BildirimTipleri.Reddedildi)
            return rol == KullaniciRolleri.Yonetim ? "red-talepler" : $"talep-detay?id={talepId}";

        return tip switch
        {
            BildirimTipleri.YonetimeGonderildi => "gelen-talepler",
            BildirimTipleri.TeklifIstendi when rol == KullaniciRolleri.Yonetim => "gelen-talepler",
            BildirimTipleri.TeklifIstendi when talepId is not null => $"teklif-gir?id={talepId}",
            BildirimTipleri.TeklifIstendi => "satinalma-teklif-istenen",
            BildirimTipleri.TeklifDuzeltmeIstendi when talepId is not null => $"satinalma-teklif-duzeltme?id={talepId}",
            BildirimTipleri.TeklifDuzeltmeIstendi => "satinalma-teklif-duzeltme",
            BildirimTipleri.TeklifOnayda when talepId is not null => $"teklif-onay-detay?id={talepId}",
            BildirimTipleri.TeklifOnayda => "yonetim-teklif-girilen",
            BildirimTipleri.Onaylandi when talepId is null && rol == KullaniciRolleri.Yonetim => "gecmis-talepler",
            BildirimTipleri.Onaylandi when talepId is null && rol == KullaniciRolleri.Satinalma => "satinalma-onaylanan",
            BildirimTipleri.Onaylandi when talepId is null => "bildirimler",
            BildirimTipleri.Onaylandi when rol == KullaniciRolleri.Satinalma => $"talep-detay?id={talepId}&view=onaylanan",
            BildirimTipleri.Onaylandi => $"talep-detay?id={talepId}",
            BildirimTipleri.SiparisOlusturuldu when talepId is not null => $"talep-detay?id={talepId}&view=siparis",
            BildirimTipleri.SiparisOlusturuldu when rol is KullaniciRolleri.Satinalma or KullaniciRolleri.Admin => "satinalma-siparis",
            BildirimTipleri.SiparisOlusturuldu => "onaylanan-malzemeler?section=siparis",
            BildirimTipleri.MalKabulEdildi when rol == KullaniciRolleri.Depo => "stok-durum",
            BildirimTipleri.MalKabulEdildi when talepId is not null && rol is KullaniciRolleri.Satinalma or KullaniciRolleri.Admin => $"talep-detay?id={talepId}&view=malkabul",
            BildirimTipleri.MalKabulEdildi when talepId is not null => $"talep-detay?id={talepId}&view=malkabul",
            BildirimTipleri.MalKabulEdildi when rol is KullaniciRolleri.Satinalma or KullaniciRolleri.Admin => "satinalma-mal-kabul",
            BildirimTipleri.MalKabulEdildi => "onaylanan-malzemeler?section=malkabul",
            BildirimTipleri.Reddedildi when rol == KullaniciRolleri.Yonetim => "red-talepler",
            BildirimTipleri.Reddedildi when talepId is not null => $"talep-detay?id={talepId}",
            _ => talepId is not null ? $"talep-detay?id={talepId}" : "bildirimler"
        };
    }

    public static bool AndroidCanAccess(string route, string rol)
    {
        var baseRoute = route.Split('?')[0];
        var satinalmaMenus = new HashSet<string>
        {
            "dashboard", "yeni-talep", "taleplerim", "gelen-talepler",
            "satinalma-teklif-istenen", "yonetim-teklif-girilen", "satinalma-teklif-girilen", "satinalma-teklif-duzeltme",
            "teklif-karsilastirma",
            "teklifsiz-firma-fiyat", "satinalma-onaylanan", "satinalma-siparis", "satinalma-mal-kabul",
            "stok-durum", "stok-hareket", "stok-giris", "stok-cikis", "stok-sayim", "raporlar", "bildirimler", "profil"
        };
        if (satinalmaMenus.Contains(baseRoute)) return true;
        return baseRoute switch
        {
            "teklif-gir" => true,
            "teklif-onay-detay" => true,
            "onaylanan-malzemeler" => true,
            "talep-detay" => true,
            _ => false
        };
    }
}

public static class MasaustuPart1Ayna
{
    public static bool TalepListede(SatinalmaTalep t, string route, string? uid, string? ad, string? rol)
    {
        rol = KullaniciRolleri.Normalize(rol);
        return route switch
        {
            "satinalma-teklif-istenen" => SatinalmaTalepKuyrugu.SatinalmaTeklifIstenen(t),
            "satinalma-teklif-girilen" => SatinalmaTalepYardimcisi.TeklifYonetimOnayiBekliyor(t),
            "satinalma-karsilastirma" => SatinalmaTalepKuyrugu.SatinalmaKarsilastirma(t),
            "satinalma-onaylanan" => t.Durum == SatinalmaTalepDurumlari.Onaylandi
                && (t.HerhangiKalemOnayli || t.TeklifsizYonetimOnayi || t.YonetimOnayKilitli),
            "gelen-talepler" => SatinalmaTalepKuyrugu.YonetimTalepler(t),
            "yonetim-teklif-girilen" => SatinalmaTalepKuyrugu.YonetimTeklifler(t),
            "taleplerim" => SatinalmaTalepKuyrugu.KayitliTalep(t),
            _ => false
        };
    }
}


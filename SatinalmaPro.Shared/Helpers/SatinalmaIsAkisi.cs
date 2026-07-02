using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Shared.Helpers;

/// <summary>
/// Satınalma iş akışı kuralları — masaüstü ve mobil ortak doğrulama.
/// </summary>
public static class SatinalmaIsAkisi
{
    public static bool ImzayaGonderilebilir(SatinalmaTalep talep) =>
        SatinalmaTalepYardimcisi.GonderimOncesiDuzenlenebilir(talep)
        && talep.Kalemler?.Any(k => !string.IsNullOrWhiteSpace(k.Malzeme)) == true;

    public static bool TeklifEklenebilir(SatinalmaTalep talep, KullaniciProfili? kullanici = null) =>
        TeklifEklenebilir(
            talep.Durum,
            talep.TalepTuru,
            talep.YonetimOnayKilitli,
            talep.OlusturanRol,
            kullanici?.Rol,
            kullanici?.Aktif ?? false);

    public static bool TeklifEklenebilir(
        string durum,
        string talepTuru,
        bool yonetimOnayKilitli,
        string olusturanRol,
        string? kullaniciRol,
        bool kullaniciAktif)
    {
        if (talepTuru == TalepTurleri.Acil || yonetimOnayKilitli)
            return false;

        if (durum is SatinalmaTalepDurumlari.TeklifGirisi or SatinalmaTalepDurumlari.Karsilastirma)
            return true;

        // Yönetime gönderilmiş ama onaylanmamış — satınalma düzeltebilir
        if (durum == SatinalmaTalepDurumlari.YonetimOnayinda
            && kullaniciAktif
            && SatinalmaRoluMu(kullaniciRol)
            && !yonetimOnayKilitli)
            return true;

        // Satınalma: yönetime gönderilmiş talebe onay beklemeden teklif girebilir
        if (durum == SatinalmaTalepDurumlari.ImzaSurecinde
            && kullaniciAktif
            && SatinalmaRoluMu(kullaniciRol))
            return true;

        return SatinalmaIcTeklifEklenebilir(durum, olusturanRol, kullaniciRol, kullaniciAktif);
    }

    private static bool SatinalmaRoluMu(string? rol)
    {
        var n = KullaniciRolleri.Normalize(rol);
        return n == KullaniciRolleri.Satinalma || KullaniciRolleri.AdminMi(rol);
    }

    /// <summary>Yalnızca satınalma: kayıt sonrası yönetime göndermeden teklif girişi.</summary>
    public static bool SatinalmaIcTeklifEklenebilir(SatinalmaTalep talep, KullaniciProfili? kullanici) =>
        SatinalmaIcTeklifEklenebilir(talep.Durum, talep.OlusturanRol, kullanici?.Rol, kullanici?.Aktif ?? false);

    public static bool SatinalmaIcTeklifEklenebilir(
        string durum,
        string olusturanRol,
        string? kullaniciRol,
        bool kullaniciAktif)
    {
        if (!kullaniciAktif)
            return false;

        if (KullaniciRolleri.Normalize(kullaniciRol) != KullaniciRolleri.Satinalma
            && !KullaniciRolleri.AdminMi(kullaniciRol))
            return false;

        return !string.IsNullOrWhiteSpace(olusturanRol)
               && KullaniciRolleri.Normalize(olusturanRol) == KullaniciRolleri.Satinalma
               && durum is SatinalmaTalepDurumlari.Hazirlaniyor
                   or SatinalmaTalepDurumlari.ImzaSurecinde
                   or SatinalmaTalepDurumlari.Karsilastirma;
    }

    public static bool YonetimeTeklifGonderilebilir(SatinalmaTalep talep) =>
        SatinalmaTalepKuyrugu.SatinalmaKarsilastirma(talep)
        && (talep.Teklifler?.Count ?? 0) > 0;

    /// <summary>Yönetime daha önce iletilmiş talep/teklif için yeniden bildirim gönderilebilir.</summary>
    public static bool YonetimeYenidenGonderebilir(SatinalmaTalep talep)
    {
        if (talep.Durum is SatinalmaTalepDurumlari.Reddedildi
            or SatinalmaTalepDurumlari.Onaylandi
            or SatinalmaTalepDurumlari.SiparisOlusturuldu
            or SatinalmaTalepDurumlari.Taslak)
            return false;

        if (talep.HerhangiKalemOnayli)
            return false;

        if (talep.TeklifsizYonetimOnayi && talep.YonetimOnayKilitli)
            return false;

        var teklifVar = (talep.Teklifler?.Count ?? 0) > 0;
        if (teklifVar)
            return talep.Durum is SatinalmaTalepDurumlari.TeklifGirisi
                or SatinalmaTalepDurumlari.Karsilastirma
                or SatinalmaTalepDurumlari.YonetimOnayinda
                or SatinalmaTalepDurumlari.ImzaSurecinde
                or SatinalmaTalepDurumlari.Hazirlaniyor;

        return talep.Durum is SatinalmaTalepDurumlari.ImzaSurecinde
            or SatinalmaTalepDurumlari.YonetimOnayinda
            or SatinalmaTalepDurumlari.Hazirlaniyor
            or SatinalmaTalepDurumlari.TeklifGirisi;
    }

    public static bool YonetimKararBekliyor(SatinalmaTalep talep) =>
        SatinalmaTalepKuyrugu.YonetimKararBekleyen(talep);

    public static bool AcilOnaylanabilir(SatinalmaTalep talep) =>
        talep.TalepTuru == TalepTurleri.Acil
        && talep.Durum == SatinalmaTalepDurumlari.ImzaSurecinde;

    public static bool TeklifIstenebilir(SatinalmaTalep talep) =>
        TeklifIstenebilir(talep.TalepTuru, talep.Durum, SatinalmaTalepYardimcisi.TeklifYonetimOnayiBekliyor(talep));

    public static bool TeklifIstenebilir(string talepTuru, string durum, bool teklifYonetimOnayiBekliyor) =>
        talepTuru != TalepTurleri.Acil
        && (durum == SatinalmaTalepDurumlari.ImzaSurecinde
            || (durum == SatinalmaTalepDurumlari.YonetimOnayinda && !teklifYonetimOnayiBekliyor));

    public static bool DirektOnaylanabilir(SatinalmaTalep talep) => TeklifIstenebilir(talep);

    /// <summary>Teklif veya kalem değişikliği sonrası yönetim kuyruğundan çekilir — yeniden gönderim gerekir.</summary>
    public static void TeklifRevizyonuBaslat(SatinalmaTalep talep)
    {
        if (talep.Durum == SatinalmaTalepDurumlari.YonetimOnayinda)
            talep.Durum = SatinalmaTalepDurumlari.Karsilastirma;
    }

    public static bool TeklifDuzenlemeDevamEdiyor(SatinalmaTalep talep) =>
        SatinalmaTalepYardimcisi.TeklifDuzenlemeDevamEdiyor(talep);

    public static string TeklifEklemeEngelMesaji(SatinalmaTalep talep, KullaniciProfili? kullanici = null) =>
        TeklifEklemeEngelMesaji(
            talep.Durum,
            talep.TalepTuru,
            talep.YonetimOnayKilitli,
            talep.OlusturanRol,
            kullanici?.Rol);

    public static string TeklifEklemeEngelMesaji(
        string durum,
        string talepTuru,
        bool yonetimOnayKilitli,
        string olusturanRol,
        string? kullaniciRol)
    {
        if (!string.IsNullOrWhiteSpace(olusturanRol)
            && KullaniciRolleri.Normalize(olusturanRol) == KullaniciRolleri.Satinalma
            && durum == SatinalmaTalepDurumlari.Hazirlaniyor
            && !string.IsNullOrWhiteSpace(kullaniciRol)
            && KullaniciRolleri.Normalize(kullaniciRol) != KullaniciRolleri.Satinalma
            && !KullaniciRolleri.AdminMi(kullaniciRol))
            return "Bu talebe yalnızca oluşturan satınalma teklif girebilir.";

        return durum switch
    {
        SatinalmaTalepDurumlari.Taslak or SatinalmaTalepDurumlari.Hazirlaniyor =>
            !string.IsNullOrWhiteSpace(olusturanRol)
            && KullaniciRolleri.Normalize(olusturanRol) == KullaniciRolleri.Satinalma
                ? "Satınalma talebi: önce kaydedin, teklifleri girin, ardından yönetime gönderin."
                : "Talep henüz onaylanmadı. Yönetim veya satınalma «Teklif İste» demeden teklif girilemez.",
        SatinalmaTalepDurumlari.ImzaSurecinde =>
            SatinalmaRoluMu(kullaniciRol)
                ? "Teklifleri girebilirsiniz; tamamlayınca karşılaştırmadan yönetime gönderin."
                : "Yönetim veya satınalma henüz «Teklif İste» demedi. Onaylanmayan talebe teklif girilmez.",
        SatinalmaTalepDurumlari.Reddedildi =>
            "Reddedilmiş talebe teklif eklenemez.",
        _ when talepTuru == TalepTurleri.Acil =>
            "Acil taleplerde teklif alınmaz.",
        _ when yonetimOnayKilitli =>
            "Onay kilitli talebe teklif eklenemez.",
        _ =>
            "Bu aşamada teklif eklenemez."
    };
    }
}

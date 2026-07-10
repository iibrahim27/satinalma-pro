using SatinalmaPro.Models;

namespace SatinalmaPro.Helpers;

/// <summary>Masaüstü — Shared ile aynı kuyruk kuralları.</summary>
public static class SatinalmaTalepKuyrugu
{
    public static bool Taslak(SatinalmaTalep t) =>
        t.Durum == SatinalmaTalepDurumlari.Taslak;

    /// <summary>Kalıcı kayıt — boş taslak hariç tüm talepler listede görünür.</summary>
    public static bool KayitliTalep(SatinalmaTalep t) =>
        t.Durum != SatinalmaTalepDurumlari.Taslak
        || SatinalmaTalepYardimcisi.IcerikVar(t);

    public static bool Reddedildi(SatinalmaTalep t) =>
        t.Durum == SatinalmaTalepDurumlari.Reddedildi;

    public static bool OnayBekleyen(SatinalmaTalep t) =>
        t.Durum is SatinalmaTalepDurumlari.Hazirlaniyor
            or SatinalmaTalepDurumlari.ImzaSurecinde
            or SatinalmaTalepDurumlari.YonetimOnayinda;

    public static bool OnayBekleyenListede(SatinalmaTalep t, bool talepSahibiModu) =>
        OnayBekleyen(t)
        && (talepSahibiModu || !SatinalmaTeklifGirisiAktif(t));

    public static bool Onaylanmis(SatinalmaTalep t) =>
        t.Durum is SatinalmaTalepDurumlari.Onaylandi or SatinalmaTalepDurumlari.SiparisOlusturuldu
        && (t.HerhangiKalemOnayli || t.TeklifsizYonetimOnayi || t.YonetimOnayKilitli);

    /// <summary>Tüm yönetim onayları — teklifsiz/teklifli, sipariş ve mal kabul sonrası dahil.</summary>
    public static bool YonetimOnayGecmisinde(SatinalmaTalep t) =>
        !Reddedildi(t)
        && (Onaylanmis(t) || OnaylananTalep(t) || YonetimGecmisTalep(t) || YonetimGecmisTeklifli(t));

    public static bool YonetimTalepler(SatinalmaTalep t) =>
        TeklifsizYonetimTalebi(t)
        && t.Durum is SatinalmaTalepDurumlari.ImzaSurecinde or SatinalmaTalepDurumlari.YonetimOnayinda;

    public static bool TeklifsizYonetimTalebi(SatinalmaTalep t) =>
        t.Teklifler is not { Count: > 0 }
        || !t.Teklifler.Any(te =>
            !string.IsNullOrWhiteSpace(te.FirmaAdi)
            || te.Fiyatlar?.Any(f => f.BirimFiyat > 0) == true);

    public static bool YonetimKararBekleyen(SatinalmaTalep t) =>
        YonetimTalepler(t) || YonetimTeklifler(t);

    public static bool YonetimTeklifBekleyen(SatinalmaTalep t) =>
        t.Durum == SatinalmaTalepDurumlari.TeklifGirisi
        && !SatinalmaTalepYardimcisi.GercekTeklifVar(t);

    public static bool YonetimTeklifler(SatinalmaTalep t) =>
        SatinalmaTalepYardimcisi.YonetimTeklifKarariBekliyor(t);

    public static bool YonetimOnaylanan(SatinalmaTalep t) =>
        t.Durum == SatinalmaTalepDurumlari.Onaylandi
        && (YonetimOnayKaydiVar(t) || TeklifOnayKanıtıVar(t));

    public static bool OnaylananTalep(SatinalmaTalep t) =>
        t.TeklifsizYonetimOnayi
        && (t.Durum is SatinalmaTalepDurumlari.Onaylandi or SatinalmaTalepDurumlari.SiparisOlusturuldu
            || t.TeklifsizFirmaFiyatBekliyor);

    public static bool OnaylananTeklif(SatinalmaTalep t) =>
        !t.TeklifsizYonetimOnayi
        && ((t.Durum == SatinalmaTalepDurumlari.Onaylandi && TeklifOnayKanıtıVar(t))
            || YonetimGecmis(t));

    public static bool YonetimGecmisTalep(SatinalmaTalep t) =>
        t.TeklifsizYonetimOnayi
        && t.YonetimOnayKilitli
        && t.Durum != SatinalmaTalepDurumlari.Reddedildi
        && t.Durum != SatinalmaTalepDurumlari.ImzaSurecinde
        && t.Durum != SatinalmaTalepDurumlari.TeklifGirisi
        && t.Durum != SatinalmaTalepDurumlari.Karsilastirma;

    public static bool YonetimGecmisTeklifli(SatinalmaTalep t) =>
        !t.TeklifsizYonetimOnayi
        && TeklifOnayKanıtıVar(t)
        && t.Durum is SatinalmaTalepDurumlari.Onaylandi or SatinalmaTalepDurumlari.SiparisOlusturuldu
        && !SatinalmaTalepYardimcisi.TeklifYonetimOnayiBekliyor(t);

    public static bool YonetimGecmis(SatinalmaTalep t, string? onaylayanUid = null)
    {
        if (t.Durum != SatinalmaTalepDurumlari.SiparisOlusturuldu || t.TeklifsizYonetimOnayi)
            return false;

        if (!TeklifOnayKanıtıVar(t))
            return false;

        return onaylayanUid is null || t.YonetimOnaylayanUid == onaylayanUid;
    }

    public static bool SatinalmaTeklifGirisi(SatinalmaTalep t) =>
        SatinalmaTeklifGirisi(
            t.Durum,
            t.OlusturanRol,
            SatinalmaTalepYardimcisi.GercekTeklifSayisi(t),
            t.YonetimOnayKilitli,
            t.TalepTuru);

    public static bool SatinalmaTeklifGirisi(
        string durum,
        string olusturanRol,
        int teklifSayisi,
        bool yonetimOnayKilitli,
        string talepTuru) =>
        (durum == SatinalmaTalepDurumlari.TeklifGirisi
         && !yonetimOnayKilitli
         && talepTuru != TalepTurleri.Acil)
        || (durum == SatinalmaTalepDurumlari.Karsilastirma && !yonetimOnayKilitli)
        || (durum == SatinalmaTalepDurumlari.ImzaSurecinde
            && teklifSayisi == 0
            && !yonetimOnayKilitli
            && talepTuru != TalepTurleri.Acil)
        || SatinalmaTalepYardimcisi.SatinalmaIcTeklifGirisi(
            durum, olusturanRol, teklifSayisi, yonetimOnayKilitli, talepTuru);

    public static bool SatinalmaTeklifIstenen(SatinalmaTalep t) =>
        SatinalmaTeklifGirisi(t) && !SatinalmaTalepYardimcisi.TeklifYonetimOnayiBekliyor(t);

    public static bool SatinalmaKarsilastirma(SatinalmaTalep t) =>
        ((t.Durum == SatinalmaTalepDurumlari.Karsilastirma
          || (t.Durum == SatinalmaTalepDurumlari.TeklifGirisi && (t.Teklifler?.Count ?? 0) > 0))
         && !SatinalmaTalepYardimcisi.TeklifYonetimOnayiBekliyor(t))
        || ((t.Teklifler?.Count ?? 0) > 0
            && !t.YonetimOnayKilitli
            && t.TalepTuru != TalepTurleri.Acil
            && t.Durum is SatinalmaTalepDurumlari.Karsilastirma
                or SatinalmaTalepDurumlari.Hazirlaniyor
                or SatinalmaTalepDurumlari.ImzaSurecinde
            && !SatinalmaTalepYardimcisi.TeklifYonetimOnayiBekliyor(t));

    /// <summary>Teklif girişi devam ediyor — yönetime gönderilene kadar (teklifsiz veya karşılaştırma aşaması).</summary>
    public static bool SatinalmaTeklifGirisiAktif(SatinalmaTalep t) =>
        SatinalmaTeklifGirisi(t)
        || SatinalmaKarsilastirma(t)
        || SatinalmaTeklifDuzenlemeDevamEdiyor(t);

    private static bool SatinalmaTeklifDuzenlemeDevamEdiyor(SatinalmaTalep t) =>
        SatinalmaTalepYardimcisi.TeklifDuzenlemeDevamEdiyor(t);

    public static bool SatinalmaTeklifsizFirmaFiyat(SatinalmaTalep t) =>
        t.TeklifsizFirmaFiyatBekliyor;

    public static bool OnaylananMalzeme(SatinalmaTalep t) =>
        t.HerhangiKalemOnayli;

    /// <summary>
    /// Taleplerim: talep açabilen roller tüm kayıtlı talepleri görür (süreç takibi).
    /// Düzenleme/silme sahiplik ile sınırlıdır — SatinalmaTalepYetkileri.
    /// </summary>
    public static bool TaleplerimListesindeGoster(
        SatinalmaTalep t,
        string? uid,
        string? adSoyad,
        string? rol = null,
        Guid? korunanTaslakId = null) =>
        KayitliTalep(t) || (korunanTaslakId.HasValue && t.Id == korunanTaslakId.Value);

    public static bool SahaModu(string? rol) =>
        !KullaniciRolleri.AdminMi(rol)
        && KullaniciRolleri.Normalize(rol) is not (KullaniciRolleri.Satinalma or KullaniciRolleri.Yonetim);

    public static bool KullanicininTalebi(SatinalmaTalep t, string? uid, string? adSoyad) =>
        SatinalmaTalepSahiplikYardimcisi.KullanicininTalebi(t, uid, adSoyad);

    private static bool YonetimOnayKaydiVar(SatinalmaTalep t) =>
        t.YonetimOnayKilitli
        || t.TeklifsizYonetimOnayi
        || !string.IsNullOrWhiteSpace(t.YonetimOnaylayanUid)
        || !string.IsNullOrWhiteSpace(t.YonetimOnayTarihi);

    private static bool TeklifOnayKanıtıVar(SatinalmaTalep t) =>
        t.HerhangiKalemOnayli
        || t.OnaylananTeklifId != null
        || t.Teklifler?.Any(teklif => teklif.Onaylandi) == true;
}

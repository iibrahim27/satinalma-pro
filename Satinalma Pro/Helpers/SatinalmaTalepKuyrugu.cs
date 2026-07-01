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
            or SatinalmaTalepDurumlari.TeklifGirisi
            or SatinalmaTalepDurumlari.Karsilastirma
            or SatinalmaTalepDurumlari.YonetimOnayinda;

    public static bool Onaylanmis(SatinalmaTalep t) =>
        t.Durum is SatinalmaTalepDurumlari.Onaylandi or SatinalmaTalepDurumlari.SiparisOlusturuldu
        && (t.HerhangiKalemOnayli || t.TeklifsizYonetimOnayi || t.YonetimOnayKilitli);

    public static bool YonetimTalepler(SatinalmaTalep t) =>
        (t.Teklifler?.Count ?? 0) == 0
        && t.Durum is SatinalmaTalepDurumlari.ImzaSurecinde or SatinalmaTalepDurumlari.YonetimOnayinda;

    public static bool YonetimKararBekleyen(SatinalmaTalep t) =>
        YonetimTalepler(t) || YonetimTeklifler(t);

    public static bool YonetimTeklifBekleyen(SatinalmaTalep t) =>
        t.Durum == SatinalmaTalepDurumlari.TeklifGirisi
        && (t.Teklifler?.Count ?? 0) == 0;

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
        t.Durum == SatinalmaTalepDurumlari.TeklifGirisi
        && (t.Teklifler?.Count ?? 0) == 0
        && !t.YonetimOnayKilitli;

    public static bool SatinalmaKarsilastirma(SatinalmaTalep t) =>
        (t.Durum == SatinalmaTalepDurumlari.Karsilastirma
         || (t.Durum == SatinalmaTalepDurumlari.TeklifGirisi && (t.Teklifler?.Count ?? 0) > 0))
        && !SatinalmaTalepYardimcisi.TeklifYonetimOnayiBekliyor(t)
        && !t.YonetimOnayKilitli;

    public static bool SatinalmaTeklifsizFirmaFiyat(SatinalmaTalep t) =>
        t.TeklifsizFirmaFiyatBekliyor;

    public static bool OnaylananMalzeme(SatinalmaTalep t) =>
        t.HerhangiKalemOnayli;

    public static bool TaleplerimListesindeGoster(SatinalmaTalep t, string? uid, string? adSoyad) =>
        KayitliTalep(t) || KullanicininTalebi(t, uid, adSoyad);

    public static bool KullanicininTalebi(SatinalmaTalep t, string? uid, string? adSoyad) =>
        (!string.IsNullOrWhiteSpace(uid) && t.OlusturanUid == uid)
        || (!string.IsNullOrWhiteSpace(adSoyad) && t.TalepEden == adSoyad);

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

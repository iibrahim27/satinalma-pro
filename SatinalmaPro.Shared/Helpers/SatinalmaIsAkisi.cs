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

    public static bool TeklifEklenebilir(SatinalmaTalep talep) =>
        talep.Durum is SatinalmaTalepDurumlari.TeklifGirisi or SatinalmaTalepDurumlari.Karsilastirma
        && !talep.YonetimOnayKilitli
        && talep.TalepTuru != TalepTurleri.Acil;

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
        talep.TalepTuru != TalepTurleri.Acil
        && (talep.Durum == SatinalmaTalepDurumlari.ImzaSurecinde
            || (talep.Durum == SatinalmaTalepDurumlari.YonetimOnayinda
                && !SatinalmaTalepYardimcisi.TeklifYonetimOnayiBekliyor(talep)));

    public static bool DirektOnaylanabilir(SatinalmaTalep talep) => TeklifIstenebilir(talep);

    public static string TeklifEklemeEngelMesaji(SatinalmaTalep talep) => talep.Durum switch
    {
        SatinalmaTalepDurumlari.Taslak or SatinalmaTalepDurumlari.Hazirlaniyor =>
            "Talep henüz yönetime gönderilmedi. Önce «İmzaya Gönder» yapılmalıdır.",
        SatinalmaTalepDurumlari.ImzaSurecinde =>
            "Yönetim henüz «Teklif İste» demedi. Teklif girişi yönetim onayından sonra başlar.",
        SatinalmaTalepDurumlari.Reddedildi =>
            "Reddedilmiş talebe teklif eklenemez.",
        _ when talep.TalepTuru == TalepTurleri.Acil =>
            "Acil taleplerde teklif alınmaz.",
        _ when talep.YonetimOnayKilitli =>
            "Onay kilitli talebe teklif eklenemez.",
        _ =>
            "Bu aşamada teklif eklenemez."
    };
}

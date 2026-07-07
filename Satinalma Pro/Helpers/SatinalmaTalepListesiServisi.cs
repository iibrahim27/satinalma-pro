using SatinalmaPro.Models;
using SatinalmaPro.Services;

namespace SatinalmaPro.Helpers;

/// <summary>
/// Satınalma modülü talep listesi — tek kaynak.
/// Veri silinmez; yalnızca boş taslaklar listede gösterilmez.
/// </summary>
public static class SatinalmaTalepListesiServisi
{
    public const string FiltreTumu = "tumu";
    public const string FiltreHazirlaniyor = "hazirlaniyor";
    public const string FiltreImza = "imza";
    public const string FiltreTeklifBekleyen = "teklif-bekleyen";
    public const string FiltreKarsilastirma = "karsilastirma";
    public const string FiltreYonetimOnay = "yonetim-onay";
    public const string FiltreOnaylandi = "onaylandi";
    public const string FiltreSiparis = "siparis";
    public const string FiltreRed = "red";

    public static IReadOnlyList<(string Id, string Etiket)> DurumFiltreleri { get; } =
    [
        (FiltreTumu, "Tüm talepler"),
        (FiltreHazirlaniyor, "Hazırlanıyor"),
        (FiltreImza, "İmza sürecinde"),
        (FiltreTeklifBekleyen, "Teklif bekleyen"),
        (FiltreKarsilastirma, "Karşılaştırma"),
        (FiltreYonetimOnay, "Yönetim onayında"),
        (FiltreOnaylandi, "Onaylandı"),
        (FiltreSiparis, "Sipariş oluşturuldu"),
        (FiltreRed, "Reddedildi")
    ];

    /// <summary>Depodaki her talep listede görünür (boş taslak dahil).</summary>
    public static bool ListedeGoster(SatinalmaTalep talep) => true;

    public static bool SahipFiltresineUygun(SatinalmaTalep talep, bool sadeceTalepModu) => true;

    public static bool DurumFiltresineUygun(SatinalmaTalep talep, string filtreId) => filtreId switch
    {
        FiltreTumu => true,
        FiltreHazirlaniyor => talep.Durum == SatinalmaTalepDurumlari.Hazirlaniyor,
        FiltreImza => talep.Durum == SatinalmaTalepDurumlari.ImzaSurecinde,
        FiltreTeklifBekleyen => talep.Durum == SatinalmaTalepDurumlari.TeklifGirisi
                                && (talep.Teklifler?.Count ?? 0) == 0,
        FiltreKarsilastirma => KarsilastirmaAsamasinda(talep),
        FiltreYonetimOnay => YonetimOnayAsamasinda(talep),
        FiltreOnaylandi => talep.Durum == SatinalmaTalepDurumlari.Onaylandi,
        FiltreSiparis => talep.Durum == SatinalmaTalepDurumlari.SiparisOlusturuldu,
        FiltreRed => talep.Durum == SatinalmaTalepDurumlari.Reddedildi,
        _ => true
    };

    public static bool AramayaUygun(SatinalmaTalep talep, string? arama)
    {
        if (string.IsNullOrWhiteSpace(arama))
            return true;

        var metin = string.Join(" ",
            talep.TalepNo,
            talep.TalepEden,
            talep.TalepAciklamasi,
            talep.Durum,
            talep.Tarih,
            talep.SiparisNo);

        return metin.Contains(arama.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public static bool Gorunur(SatinalmaTalep talep, string durumFiltresi, string? arama, bool sadeceTalepModu) =>
        ListedeGoster(talep)
        && SahipFiltresineUygun(talep, sadeceTalepModu)
        && DurumFiltresineUygun(talep, durumFiltresi)
        && AramayaUygun(talep, arama);

    public static string OnerilenDurumFiltresi(SatinalmaTalep talep)
    {
        if (talep.Durum == SatinalmaTalepDurumlari.Reddedildi)
            return FiltreRed;
        if (talep.Durum == SatinalmaTalepDurumlari.SiparisOlusturuldu)
            return FiltreSiparis;
        if (talep.Durum == SatinalmaTalepDurumlari.Onaylandi)
            return FiltreOnaylandi;
        if (YonetimOnayAsamasinda(talep))
            return FiltreYonetimOnay;
        if (KarsilastirmaAsamasinda(talep))
            return FiltreKarsilastirma;
        if (talep.Durum == SatinalmaTalepDurumlari.TeklifGirisi && (talep.Teklifler?.Count ?? 0) == 0)
            return FiltreTeklifBekleyen;
        if (talep.Durum == SatinalmaTalepDurumlari.ImzaSurecinde)
            return FiltreImza;
        if (talep.Durum == SatinalmaTalepDurumlari.Hazirlaniyor)
            return FiltreHazirlaniyor;

        return FiltreTumu;
    }

    private static bool KarsilastirmaAsamasinda(SatinalmaTalep talep)
    {
        if (talep.Durum == SatinalmaTalepDurumlari.Karsilastirma)
            return true;

        return talep.Durum == SatinalmaTalepDurumlari.TeklifGirisi
               && (talep.Teklifler?.Count ?? 0) > 0
               && !YonetimOnayAsamasinda(talep);
    }

    private static bool YonetimOnayAsamasinda(SatinalmaTalep talep) =>
        talep.Durum == SatinalmaTalepDurumlari.YonetimOnayinda
        || SatinalmaTalepYardimcisi.TeklifYonetimOnayiBekliyor(talep);
}

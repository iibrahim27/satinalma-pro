using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Shared.Helpers;

/// <summary>Kullanıcıya gösterilen tek tip talep/teklif durumları (6 adet).</summary>
public static class SatinalmaTalepDurumEtiketi
{
    public const string RedEdildi = "Red Edildi";
    public const string Onaylandi = "Onaylandı";
    public const string TeklifBekleniyor = "Teklif Bekleniyor";
    public const string TeklifOnaylandi = "Teklif Onaylandı";
    public const string Sipariste = "Siparişte";
    public const string DepoTeslimOldu = "Depo Teslim Oldu";

    public static readonly IReadOnlyList<string> Tum =
    [
        RedEdildi, Onaylandi, TeklifBekleniyor, TeklifOnaylandi, Sipariste, DepoTeslimOldu
    ];

    public static string Olustur(SatinalmaTalep talep, Func<Guid, Guid, bool>? stokAktarildiMi = null)
    {
        if (talep.Durum == SatinalmaTalepDurumlari.Reddedildi)
            return RedEdildi;

        if (DepoTeslimOlduMu(talep, stokAktarildiMi))
            return DepoTeslimOldu;

        if (talep.Durum == SatinalmaTalepDurumlari.SiparisOlusturuldu)
            return Sipariste;

        if (talep.HerhangiKalemOnayli
            || (talep.TeklifGirilmis && talep.YonetimOnayKilitli && !talep.TeklifsizYonetimOnayi))
            return TeklifOnaylandi;

        if (talep.Durum == SatinalmaTalepDurumlari.Onaylandi)
            return Onaylandi;

        return TeklifBekleniyor;
    }

    private static bool DepoTeslimOlduMu(SatinalmaTalep talep, Func<Guid, Guid, bool>? stokAktarildiMi)
    {
        var onayli = OnayliKalemler(talep);
        if (onayli.Count == 0)
            return false;

        if (!onayli.All(k => k.SiparisTamamlandi && k.KabulEdilenMiktar >= k.Miktar - 0.0001))
            return false;

        if (stokAktarildiMi is null)
            return talep.Durum == SatinalmaTalepDurumlari.SiparisOlusturuldu;

        return onayli.All(k => stokAktarildiMi(talep.Id, k.Id));
    }

    private static List<SatinalmaTalepKalemi> OnayliKalemler(SatinalmaTalep talep)
    {
        talep.Kalemler ??= [];
        if (talep.TeklifsizYonetimOnayi)
            return talep.Kalemler.Where(k => !string.IsNullOrWhiteSpace(k.Malzeme)).ToList();

        return talep.Kalemler.Where(k => k.OnaylananTeklifId != null).ToList();
    }
}

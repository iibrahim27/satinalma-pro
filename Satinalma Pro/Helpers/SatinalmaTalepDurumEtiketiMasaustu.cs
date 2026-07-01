using SatinalmaPro.Models;
using SatinalmaPro.Shared.Helpers;

namespace SatinalmaPro.Helpers;

public static class SatinalmaTalepDurumEtiketiMasaustu
{
    public static string Olustur(SatinalmaTalep talep, Func<Guid, Guid, bool>? stokAktarildiMi = null)
    {
        if (talep.Durum == SatinalmaTalepDurumlari.Reddedildi)
            return SatinalmaTalepDurumEtiketi.RedEdildi;

        if (DepoTeslimOlduMu(talep, stokAktarildiMi))
            return SatinalmaTalepDurumEtiketi.DepoTeslimOldu;

        if (talep.Durum == SatinalmaTalepDurumlari.SiparisOlusturuldu)
            return SatinalmaTalepDurumEtiketi.Sipariste;

        if (talep.HerhangiKalemOnayli
            || ((talep.Teklifler?.Count ?? 0) > 0 && talep.YonetimOnayKilitli && !talep.TeklifsizYonetimOnayi))
            return SatinalmaTalepDurumEtiketi.TeklifOnaylandi;

        if (talep.Durum == SatinalmaTalepDurumlari.Onaylandi)
            return SatinalmaTalepDurumEtiketi.Onaylandi;

        return SatinalmaTalepDurumEtiketi.TeklifBekleniyor;
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
        if (talep.TeklifsizYonetimOnayi)
            return talep.Kalemler.Where(k => !string.IsNullOrWhiteSpace(k.Malzeme)).ToList();

        return talep.Kalemler.Where(k => k.OnaylananTeklifId != null).ToList();
    }
}

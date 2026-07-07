using SatinalmaPro.Helpers;
using SatinalmaPro.Models;

namespace SatinalmaPro.Views.Modules.Satinalma.Part1;

/// <summary>Part 1 — kullanıcıya gösterilen talep/teklif durum metinleri.</summary>
public static class SatinalmaPart1DurumEtiketi
{
    public static string TalepDurumu(SatinalmaTalep talep)
    {
        if (talep.Durum == SatinalmaTalepDurumlari.Reddedildi)
            return "Red verildi";

        if (talep.Durum is SatinalmaTalepDurumlari.Onaylandi or SatinalmaTalepDurumlari.SiparisOlusturuldu
            && (talep.TeklifsizYonetimOnayi || talep.YonetimOnayKilitli || talep.HerhangiKalemOnayli))
            return "Onaylandı";

        if (talep.Durum == SatinalmaTalepDurumlari.TeklifGirisi
            && !SatinalmaTalepYardimcisi.GercekTeklifVar(talep))
            return "Satınalmadan teklif bekleniyor";

        if (talep.Durum is SatinalmaTalepDurumlari.ImzaSurecinde
            or SatinalmaTalepDurumlari.Hazirlaniyor
            or SatinalmaTalepDurumlari.YonetimOnayinda
            or SatinalmaTalepDurumlari.Taslak)
            return "Yönetim onayı bekliyor";

        if (talep.Durum == SatinalmaTalepDurumlari.Karsilastirma)
            return "Satınalmadan teklif bekleniyor";

        return "Yönetim onayı bekliyor";
    }

    public static string TeklifDurumu(SatinalmaTalep talep)
    {
        if (talep.Durum == SatinalmaTalepDurumlari.SiparisOlusturuldu)
        {
            if (talep.Kalemler?.Any(k => k.KabulEdilenMiktar > 0) == true
                && talep.Kalemler.All(k => k.SiparisTamamlandi || k.KabulEdilenMiktar >= k.Miktar - 0.0001))
                return "Mal kabul yapıldı";

            return "Siparişte";
        }

        if (!SatinalmaTalepYardimcisi.GercekTeklifVar(talep))
            return "—";

        if (talep.Durum == SatinalmaTalepDurumlari.Reddedildi)
            return "Red edildi";

        if (!string.IsNullOrWhiteSpace(talep.TeklifDuzeltmeNotu)
            && talep.Durum == SatinalmaTalepDurumlari.Karsilastirma)
            return "Yeniden teklif istendi";

        if (SatinalmaTalepYardimcisi.TeklifYonetimOnayiBekliyor(talep))
            return "Yönetim teklif değerlendirmede";

        if (talep.Durum == SatinalmaTalepDurumlari.Karsilastirma)
            return "Karşılaştırma inceleniyor";

        if (talep.HerhangiKalemOnayli || talep.OnaylananTeklifId != null)
            return "Teklif onaylandı";

        if (talep.Durum == SatinalmaTalepDurumlari.Onaylandi)
            return "Onaylandı";

        return "Karşılaştırma inceleniyor";
    }
}

using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Shared.Helpers;

/// <summary>Onaylı taleplerden mal kabul kuyruğu satırları üretir — teklifli ve teklifsiz onay.</summary>
public static class OnaylananMalzemeOlusturucu
{
    public static bool MalKabulTalep(SatinalmaTalep talep) =>
        talep.Durum is SatinalmaTalepDurumlari.Onaylandi or SatinalmaTalepDurumlari.SiparisOlusturuldu
        && (talep.HerhangiKalemOnayli || talep.TeklifsizYonetimOnayi);

    public static bool MalKabulBekleyen(OnaylananMalzemeSatiri satir) =>
        !satir.SiparisTamamlandi
        && satir.Durum is SatinalmaTalepDurumlari.Onaylandi or SatinalmaTalepDurumlari.SiparisOlusturuldu;

    public static bool SevkiyatTamamlanabilir(OnaylananMalzemeSatiri satir) =>
        !satir.SiparisTamamlandi
        && satir.KabulEdilenMiktar > 0.0001
        && satir.KabulEdilenMiktar < satir.SiparisMiktari - 0.0001
        && satir.Durum is SatinalmaTalepDurumlari.Onaylandi or SatinalmaTalepDurumlari.SiparisOlusturuldu;

    public static List<OnaylananMalzemeSatiri> Olustur(IEnumerable<SatinalmaTalep> talepler)
    {
        var liste = new List<OnaylananMalzemeSatiri>();

        foreach (var talep in talepler.Where(MalKabulTalep))
        {
            talep.FirmaSiparisNolari ??= [];
            var teklifsiz = talep.TeklifsizYonetimOnayi && !talep.HerhangiKalemOnayli;

            foreach (var kalem in talep.Kalemler
                         .Where(k => !string.IsNullOrWhiteSpace(k.Malzeme))
                         .Where(k => teklifsiz || k.OnaylananTeklifId != null)
                         .OrderBy(k => k.SiraNo))
            {
                if (kalem.OnaylananTeklifId is { } teklifId)
                {
                    var teklif = talep.KalemOnayTeklifi(kalem);
                    if (teklif is null)
                        continue;

                    teklif.FiyatlariHesapla(talep.Kalemler);
                    var fiyat = teklif.Fiyatlar.FirstOrDefault(f => f.KalemId == kalem.Id);
                    if (fiyat is null)
                        continue;

                    var siparisNo = talep.FirmaSiparisNolari.TryGetValue(teklif.Id, out var no)
                                    && !string.IsNullOrWhiteSpace(no)
                        ? no
                        : talep.SiparisNo;

                    liste.Add(Satir(talep, kalem, teklif.Id, teklif.FirmaAdi,
                        string.IsNullOrWhiteSpace(fiyat.Marka) ? teklif.Marka : fiyat.Marka,
                        fiyat.TlBirimFiyat(teklif.UsdKuru, teklif.EurKuru), fiyat.ToplamTutar,
                        teklif.VadeGunu, siparisNo));
                }
                else
                {
                    var siparisNo = string.IsNullOrWhiteSpace(talep.SiparisNo) ? talep.TalepNo : talep.SiparisNo;
                    liste.Add(Satir(talep, kalem, Guid.Empty, "", "", 0, 0, 0, siparisNo));
                }
            }
        }

        return liste;
    }

    private static OnaylananMalzemeSatiri Satir(
        SatinalmaTalep talep,
        SatinalmaTalepKalemi kalem,
        Guid teklifId,
        string firma,
        string marka,
        decimal birimFiyat,
        decimal toplamTutar,
        int vadeGunu,
        string siparisNo) => new()
    {
        TalepId = talep.Id,
        KalemId = kalem.Id,
        TeklifId = teklifId,
        TalepNo = talep.TalepNo,
        SiparisNo = siparisNo,
        Tarih = talep.Tarih,
        Durum = talep.Durum,
        Firma = firma,
        Marka = marka,
        Malzeme = kalem.Malzeme,
        SiparisMiktari = kalem.Miktar,
        KabulEdilenMiktar = kalem.KabulEdilenMiktar,
        SiparisTamamlandi = kalem.SiparisTamamlandi,
        Birim = kalem.Birim,
        BirimFiyati = birimFiyat,
        ToplamTutar = toplamTutar,
        KalemAciklamasi = kalem.Aciklama,
        VadeGunu = vadeGunu
    };
}

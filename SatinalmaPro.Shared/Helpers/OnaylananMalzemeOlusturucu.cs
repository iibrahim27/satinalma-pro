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
                         .Where(k => teklifsiz || KalemFirmaAtamaYardimcisi.OnayliMi(k))
                         .OrderBy(k => k.SiraNo))
            {
                if (teklifsiz && !KalemFirmaAtamaYardimcisi.OnayliMi(kalem))
                {
                    var siparisNo = string.IsNullOrWhiteSpace(talep.SiparisNo) ? talep.TalepNo : talep.SiparisNo;
                    liste.Add(Satir(talep, kalem, Guid.Empty, "", "", 0, 0, 0, siparisNo,
                        kalem.Miktar, kalem.KabulEdilenMiktar, kalem.SiparisTamamlandi));
                    continue;
                }

                foreach (var atama in KalemFirmaAtamaYardimcisi.EtkinAtamalar(kalem))
                {
                    var teklif = talep.Teklifler.FirstOrDefault(t => t.Id == atama.TeklifId);
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

                    var birim = fiyat.TlBirimFiyat(teklif.UsdKuru, teklif.EurKuru);
                    var toplam = Math.Round(birim * (decimal)atama.Miktar, 2);

                    liste.Add(Satir(talep, kalem, teklif.Id, teklif.FirmaAdi,
                        string.IsNullOrWhiteSpace(fiyat.Marka) ? teklif.Marka : fiyat.Marka,
                        birim, toplam,
                        teklif.VadeGunu, siparisNo,
                        atama.Miktar, atama.KabulEdilenMiktar, atama.SiparisTamamlandi));
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
        string siparisNo,
        double siparisMiktari,
        double kabulEdilenMiktar,
        bool siparisTamamlandi) => new()
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
        SiparisMiktari = siparisMiktari,
        KabulEdilenMiktar = kabulEdilenMiktar,
        SiparisTamamlandi = siparisTamamlandi,
        Birim = kalem.Birim,
        BirimFiyati = birimFiyat,
        ToplamTutar = toplamTutar,
        KalemAciklamasi = kalem.Aciklama,
        VadeGunu = vadeGunu
    };
}

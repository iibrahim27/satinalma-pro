using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;

namespace SatinalmaPro.Views.Modules.Satinalma.Part1;

public class SiparisVerilenTalepListeSatiri
{
    public SiparisVerilenTalepListeSatiri(
        SatinalmaTalep talep,
        string? kullaniciUid = null,
        string? kullaniciAd = null)
    {
        Talep = talep;
        BenimTalebim = SatinalmaTalepKuyrugu.KullanicininTalebi(talep, kullaniciUid, kullaniciAd);
        DurumEtiketi = SatinalmaPart1DurumEtiketi.TeklifDurumu(talep);
        MalKabulOzeti = SatinalmaPart1Filtreleri.MalKabulOzeti(talep);

        var firmalar = SatinalmaDepo.OnaylananMalzemeleriOlustur()
            .Where(s => s.TalepId == talep.Id)
            .Select(s => s.Firma)
            .Where(f => !string.IsNullOrWhiteSpace(f))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        FirmaOzet = firmalar.Count switch
        {
            0 => "—",
            1 => firmalar[0],
            _ => $"{firmalar[0]} (+{firmalar.Count - 1})"
        };

        SiparisNo = string.IsNullOrWhiteSpace(talep.SiparisNo) ? talep.TalepNo : talep.SiparisNo;
    }

    public SatinalmaTalep Talep { get; }
    public bool BenimTalebim { get; }
    public string TalepNo => Talep.TalepNo;
    public string Tarih => Talep.Tarih;
    public string TalepEden => Talep.TalepEden;
    public string SiparisNo { get; }
    public string FirmaOzet { get; }
    public string MalKabulOzeti { get; }
    public string DurumEtiketi { get; }
}

using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;

namespace SatinalmaPro.Views.Modules.Satinalma.Part1;

public class MalKabulTalepListeSatiri
{
    public MalKabulTalepListeSatiri(
        SatinalmaTalep talep,
        string? kullaniciUid = null,
        string? kullaniciAd = null)
    {
        Talep = talep;
        BenimTalebim = SatinalmaTalepKuyrugu.KullanicininTalebi(talep, kullaniciUid, kullaniciAd);

        var kalemler = SatinalmaDepo.OnaylananMalzemeleriOlustur()
            .Where(s => s.TalepId == talep.Id)
            .ToList();

        KalemSayisi = kalemler.Count;
        TamamlananKalem = kalemler.Count(s => s.KalanMiktar <= 0.0001);
        MalKabulOzeti = $"{TamamlananKalem}/{KalemSayisi} kalem";

        var firmalar = kalemler
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
    public int KalemSayisi { get; }
    public int TamamlananKalem { get; }
}

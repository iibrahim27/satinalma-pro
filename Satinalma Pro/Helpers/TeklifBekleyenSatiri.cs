using SatinalmaPro.Models;
using SatinalmaPro.Shared.Helpers;

namespace SatinalmaPro.Helpers;

public class TeklifBekleyenSatiri
{
    public TeklifBekleyenSatiri(SatinalmaTalep talep) => Talep = talep;

    public string TalepNo => Talep.TalepNo;
    public string Tarih => Talep.Tarih;
    public string TalepEden => Talep.TalepEden;
    public string KalemSayisiMetni => Talep.KalemSayisiMetni;
    public string DurumMetni => SatinalmaTalepDurumEtiketiMasaustu.Olustur(Talep);
    public string OnayDurumuMetni => DurumMetni;

    public SatinalmaTalep Talep { get; }

    public bool OnayRozetiGoster =>
        SatinalmaTalepDurumEtiketiMasaustu.Olustur(Talep) is SatinalmaTalepDurumEtiketi.TeklifOnaylandi
            or SatinalmaTalepDurumEtiketi.Onaylandi;

    public static bool KuyruktaGoster(SatinalmaTalep talep) =>
        SatinalmaTalepKuyrugu.SatinalmaTeklifGirisi(talep);
}

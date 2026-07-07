using SatinalmaPro.Models;

namespace SatinalmaPro.Views.Modules.Satinalma.Part1;

public sealed class TalepListeSatiriPart1
{
    public TalepListeSatiriPart1(SatinalmaTalep talep) => Talep = talep;

    public SatinalmaTalep Talep { get; }

    public string TalepNo => string.IsNullOrWhiteSpace(Talep.TalepNo) ? "—" : Talep.TalepNo;
    public string Tarih => Talep.Tarih;
    public string TalepEden => Talep.TalepEden;
    public string Oncelik => TalepTurleri.GorunenAd(Talep.TalepTuru);
    public string TalepDurumu => SatinalmaPart1DurumEtiketi.TalepDurumu(Talep);
    public string TeklifDurumu => SatinalmaPart1DurumEtiketi.TeklifDurumu(Talep);
}

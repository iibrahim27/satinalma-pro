using SatinalmaPro.Models;
using SatinalmaPro.Shared.Helpers;

namespace SatinalmaPro.Helpers;

public class OnayBekleyenSatiri
{
    public OnayBekleyenSatiri(SatinalmaTalep talep, Func<Guid, Guid, bool>? stokAktarildiMi = null)
    {
        Talep = talep;
        DurumEtiketi = SatinalmaTalepDurumEtiketiMasaustu.Olustur(talep, stokAktarildiMi);
    }

    public SatinalmaTalep Talep { get; }

    public string TalepNo => Talep.TalepNo;
    public string TalepTuru => Talep.TalepTuru;
    public string Tarih => Talep.Tarih;
    public string TalepEden => Talep.TalepEden;
    public string DurumEtiketi { get; }
}

using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;

namespace SatinalmaPro.Views.Modules.Satinalma.Part1;

public static class SatinalmaPart1OnayYardimcisi
{
    public static bool TeklifOnaylanabilir(SatinalmaTalep talep) =>
        KullaniciYetkileri.TeklifOnayVerebilir()
        && !talep.YonetimOnayKilitli
        && !talep.HerhangiKalemOnayli
        && SatinalmaTalepYardimcisi.GercekTeklifVar(talep)
        && talep.Durum is SatinalmaTalepDurumlari.Karsilastirma or SatinalmaTalepDurumlari.YonetimOnayinda;

    public static bool TalepKararVerilebilir(SatinalmaTalep talep) =>
        KullaniciYetkileri.YonetimKararVerebilir()
        && SatinalmaPart1Filtreleri.GelenTalepler(talep);
}

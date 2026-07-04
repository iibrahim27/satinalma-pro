using SatinalmaPro.Shared.Helpers;

namespace SatinalmaPro.Services;

public static class MalzemeAdiOneriServisi
{
    private static readonly object Kilit = new();
    private static List<string>? _onbellek;

    public static void OnbellekSifirla()
    {
        lock (Kilit)
            _onbellek = null;
    }

    public static IEnumerable<string> TumAdlar()
    {
        lock (Kilit)
        {
            if (_onbellek is not null)
                return _onbellek;

            ModulVeriDeposu.Yukle();
            SatinalmaDepo.Yukle();

            var liste = new List<string>();
            foreach (var kayit in ModulVeriDeposu.AlinanMalzemeler)
            {
                if (!string.IsNullOrWhiteSpace(kayit.MalzemeHizmet))
                    liste.Add(kayit.MalzemeHizmet.Trim());
            }

            foreach (var stok in ModulVeriDeposu.Stok)
            {
                if (!string.IsNullOrWhiteSpace(stok.MalzemeAdi))
                    liste.Add(stok.MalzemeAdi.Trim());
            }

            foreach (var talep in SatinalmaDepo.Talepler.ToList())
            {
                foreach (var kalem in talep.Kalemler ?? [])
                {
                    if (!string.IsNullOrWhiteSpace(kalem.Malzeme))
                        liste.Add(kalem.Malzeme.Trim());
                }
            }

            _onbellek = liste
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .ToList();
            return _onbellek;
        }
    }

    public static IEnumerable<string> Ara(string? arama) =>
        MalzemeAdiOneriYardimcisi.Filtrele(TumAdlar(), arama);
}

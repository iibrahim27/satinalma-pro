using System.Collections.ObjectModel;
using SatinalmaPro.Models;

namespace SatinalmaPro.Services;

public static class UygulamaVeriDeposu
{
    public static ObservableCollection<AlinanMalzemeKaydi> AlinanMalzemeler => ModulVeriDeposu.AlinanMalzemeler;
    public static ObservableCollection<StokKaydi> Stok => ModulVeriDeposu.Stok;
    public static ObservableCollection<StokHareketKaydi> StokHareketleri => ModulVeriDeposu.StokHareketleri;

    public static void OrnekVeriyiYukle() => ModulVeriDeposu.Yukle();
}

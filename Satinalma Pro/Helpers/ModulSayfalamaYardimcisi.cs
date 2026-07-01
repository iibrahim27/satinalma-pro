using System.ComponentModel;
using System.Windows.Data;
using SatinalmaPro.Views.Controls;

namespace SatinalmaPro.Helpers;

public static class ModulSayfalamaYardimcisi
{
    public static DateTime TarihSira(string? tarih) => TarihYardimcisi.SiralamaDegeri(tarih);

    public static void FiltreSonrasi<T>(
        ModulSayfalamaYoneticisi<T> sayfalama,
        ICollectionView gorunum,
        Func<T, DateTime> tarihSecici,
        SayfalamaCubugu? cubuk,
        bool ilkSayfayaDon = true)
    {
        gorunum.SortDescriptions.Clear();
        gorunum.Refresh();

        var filtrelenmis = FiltrelenmisListe<T>(gorunum);
        sayfalama.KaynakGuncelle(filtrelenmis, tarihSecici, ilkSayfayaDon);
        cubuk?.Guncelle(sayfalama.GuncelSayfa, sayfalama.ToplamSayfa, sayfalama.ToplamKayit);
    }

    public static List<T> FiltrelenmisListe<T>(ICollectionView gorunum)
    {
        var liste = new List<T>();
        foreach (var oge in gorunum)
        {
            if (oge is T kayit)
                liste.Add(kayit);
        }

        return liste;
    }

    public static void CubukBagla<T>(ModulSayfalamaYoneticisi<T> sayfalama, SayfalamaCubugu cubuk)
    {
        void CubukGuncelle() =>
            cubuk.Guncelle(sayfalama.GuncelSayfa, sayfalama.ToplamSayfa, sayfalama.ToplamKayit);

        cubuk.IlkTiklandi += () => { sayfalama.IlkSayfa(); CubukGuncelle(); };
        cubuk.OncekiTiklandi += () => { sayfalama.OncekiSayfa(); CubukGuncelle(); };
        cubuk.SonrakiTiklandi += () => { sayfalama.SonrakiSayfa(); CubukGuncelle(); };
        cubuk.SonTiklandi += () => { sayfalama.SonSayfa(); CubukGuncelle(); };
    }
}

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using SatinalmaPro.Views.Controls;

namespace SatinalmaPro.Helpers;

/// <summary>Modül tablolarında ortak ince ayar: gruplama, sayfalama, grup bilgisi.</summary>
public static class ErpModulTabloYardimcisi
{
    public static void SayfalamayiUygula<T>(
        ModulSayfalamaYoneticisi<T> sayfalama,
        ICollectionView gorunum,
        Func<T, DateTime> tarihSecici,
        SayfalamaCubugu? cubuk,
        string? grupAlani,
        Func<T, string>? grupAnahtari,
        bool ilkSayfayaDon = false)
    {
        gorunum.SortDescriptions.Clear();
        gorunum.Refresh();

        var filtrelenmis = ModulSayfalamaYardimcisi.FiltrelenmisListe<T>(gorunum);
        if (string.IsNullOrEmpty(grupAlani) || grupAnahtari is null)
        {
            sayfalama.KaynakGuncelle(filtrelenmis, tarihSecici, ilkSayfayaDon);
        }
        else
        {
            var sirali = filtrelenmis
                .OrderBy(grupAnahtari, StringComparer.OrdinalIgnoreCase)
                .ThenByDescending(tarihSecici)
                .ToList();
            sayfalama.SiraliKaynakGuncelle(sirali, ilkSayfayaDon);
        }

        cubuk?.Guncelle(sayfalama.GuncelSayfa, sayfalama.ToplamSayfa, sayfalama.ToplamKayit);
    }

    public static void SayfaBoyutuDegistir<T>(
        ModulSayfalamaYoneticisi<T> sayfalama,
        int boyut,
        Func<T, DateTime> tarihSecici,
        string? grupAlani,
        SayfalamaCubugu? cubuk)
    {
        if (string.IsNullOrEmpty(grupAlani))
            sayfalama.SayfaBoyutunuAyarla(boyut, tarihSecici);
        else
            sayfalama.SayfaBoyutunuDegistir(boyut, ilkSayfayaDon: true);

        cubuk?.Guncelle(sayfalama.GuncelSayfa, sayfalama.ToplamSayfa, sayfalama.ToplamKayit);
    }

    public static void GrupBilgiGuncelle(
        TextBlock? hedef,
        string? grupAlani,
        IReadOnlyList<(string Baslik, string Alan)> secenekler,
        IEnumerable<string> grupAnahtarlari)
    {
        if (hedef is null)
            return;

        if (string.IsNullOrEmpty(grupAlani))
        {
            hedef.Visibility = Visibility.Collapsed;
            return;
        }

        var baslik = secenekler.FirstOrDefault(g => g.Alan == grupAlani).Baslik;
        if (string.IsNullOrEmpty(baslik))
            baslik = grupAlani;

        var grupSayisi = grupAnahtarlari.Distinct(StringComparer.OrdinalIgnoreCase).Count();
        hedef.Text = $"· Gruplama: {baslik} ({grupSayisi} grup)";
        hedef.Visibility = Visibility.Visible;
    }

    public static void Kolonlar(DataGrid grid, Window? owner) =>
        ErpDataGridYardimcisi.KolonSeciminiGoster(grid, owner);

    public static void Grupla(
        FrameworkElement hedef,
        IReadOnlyList<(string Baslik, string Alan)> secenekler,
        string? aktifAlan,
        Action<string?> secildi) =>
        ErpDataGridYardimcisi.GruplaMenusunuGoster(hedef, secenekler, aktifAlan, secildi);

    public static void Yogun(DataGrid grid, ref bool yogun) =>
        ErpDataGridYardimcisi.YogunGorunumToggle(grid, ref yogun);

    public static void TamEkran(
        Grid anaGrid,
        UIElement tabloKart,
        int tabloSatir,
        int[] gizlenecekSatirlar,
        ref bool aktif,
        Button? btn = null) =>
        ErpDataGridYardimcisi.TabloTamEkranToggle(anaGrid, tabloKart, tabloSatir, gizlenecekSatirlar, ref aktif, btn);

    public static void FiltreOdakla(FrameworkElement filtreBaslik, Action? paneliAc = null)
    {
        paneliAc?.Invoke();
        ErpDataGridYardimcisi.FiltrePanelineOdakla(filtreBaslik);
    }
}

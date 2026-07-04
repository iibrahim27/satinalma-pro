using System.Collections.ObjectModel;

namespace SatinalmaPro.Helpers;

public sealed class ModulSayfalamaYoneticisi<T>
{
    private readonly int _sayfaBoyutu;

    public ModulSayfalamaYoneticisi(int? sayfaBoyutu = null) =>
        _sayfaBoyutu = sayfaBoyutu ?? ModulSayfalama.SayfaBoyutu;

    public ObservableCollection<T> SayfaKayitlari { get; } = [];

    public int GuncelSayfa { get; private set; } = 1;
    public int ToplamSayfa { get; private set; } = 1;
    public int ToplamKayit { get; private set; }
    public int SayfaBoyutu => _sayfaBoyutu;

    private List<T> _sirali = [];

    public void KaynakGuncelle(IEnumerable<T> kaynak, Func<T, DateTime> tarihSecici, bool ilkSayfayaDon = false)
    {
        if (ilkSayfayaDon)
            GuncelSayfa = 1;

        var indeksli = kaynak.Select((kayit, index) => (kayit, index)).ToList();
        _sirali = indeksli
            .OrderByDescending(x => tarihSecici(x.kayit))
            .ThenByDescending(x => x.index)
            .Select(x => x.kayit)
            .ToList();

        ToplamKayit = _sirali.Count;
        ToplamSayfa = ToplamKayit == 0 ? 1 : (int)Math.Ceiling(ToplamKayit / (double)_sayfaBoyutu);

        if (GuncelSayfa > ToplamSayfa)
            GuncelSayfa = ToplamSayfa;
        if (GuncelSayfa < 1)
            GuncelSayfa = 1;

        SayfayiYukle();
    }

    public void IlkSayfa() => SayfayaGit(1);
    public void OncekiSayfa() => SayfayaGit(GuncelSayfa - 1);
    public void SonrakiSayfa() => SayfayaGit(GuncelSayfa + 1);
    public void SonSayfa() => SayfayaGit(ToplamSayfa);

    public void SayfayaGit(int sayfa)
    {
        if (ToplamKayit == 0)
        {
            GuncelSayfa = 1;
            SayfaKayitlari.Clear();
            return;
        }

        GuncelSayfa = Math.Clamp(sayfa, 1, ToplamSayfa);
        SayfayiYukle();
    }

    public IReadOnlyList<T> TumKayitlar() => _sirali;

    private void SayfayiYukle()
    {
        SayfaKayitlari.Clear();
        if (ToplamKayit == 0)
            return;

        var baslangic = (GuncelSayfa - 1) * _sayfaBoyutu;
        foreach (var kayit in _sirali.Skip(baslangic).Take(_sayfaBoyutu))
            SayfaKayitlari.Add(kayit);
    }
}

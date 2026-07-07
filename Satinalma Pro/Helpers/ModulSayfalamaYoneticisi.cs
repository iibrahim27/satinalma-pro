using System.Collections.ObjectModel;

namespace SatinalmaPro.Helpers;

public sealed class ModulSayfalamaYoneticisi<T>
{
    private int _sayfaBoyutu;

    public ModulSayfalamaYoneticisi(int? sayfaBoyutu = null) =>
        _sayfaBoyutu = sayfaBoyutu ?? ModulSayfalama.SayfaBoyutu;

    public ObservableCollection<T> SayfaKayitlari { get; } = [];

    public int GuncelSayfa { get; private set; } = 1;
    public int ToplamSayfa { get; private set; } = 1;
    public int ToplamKayit { get; private set; }
    public int SayfaBoyutu => _sayfaBoyutu;

    private List<T> _sirali = [];
    private Func<T, DateTime>? _tarihSecici;

    public void SayfaBoyutunuAyarla(int yeniBoyut, Func<T, DateTime> tarihSecici)
    {
        _sayfaBoyutu = Math.Max(1, yeniBoyut);
        _tarihSecici = tarihSecici;
        KaynakGuncelle(_sirali, tarihSecici, ilkSayfayaDon: true);
    }

    /// <summary>Mevcut sıralamayı koruyarak sayfa boyutunu değiştirir (gruplama vb.).</summary>
    public void SayfaBoyutunuDegistir(int yeniBoyut, bool ilkSayfayaDon = true)
    {
        _sayfaBoyutu = Math.Max(1, yeniBoyut);
        SayfalamaMetaGuncelle(ilkSayfayaDon);
    }

    public void KaynakGuncelle(IEnumerable<T> kaynak, Func<T, DateTime> tarihSecici, bool ilkSayfayaDon = false)
    {
        var indeksli = kaynak.Select((kayit, index) => (kayit, index)).ToList();
        _sirali = indeksli
            .OrderByDescending(x => tarihSecici(x.kayit))
            .ThenByDescending(x => x.index)
            .Select(x => x.kayit)
            .ToList();

        SayfalamaMetaGuncelle(ilkSayfayaDon);
    }

    /// <summary>Önceden sıralanmış listeyi doğrudan uygular (gruplama vb.).</summary>
    public void SiraliKaynakGuncelle(IEnumerable<T> sirali, bool ilkSayfayaDon = false)
    {
        _sirali = sirali.ToList();
        SayfalamaMetaGuncelle(ilkSayfayaDon);
    }

    private void SayfalamaMetaGuncelle(bool ilkSayfayaDon)
    {
        if (ilkSayfayaDon)
            GuncelSayfa = 1;

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

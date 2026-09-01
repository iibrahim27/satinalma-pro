using System.Windows.Controls;
using SatinalmaPro.Models;

namespace SatinalmaPro.Services;

public static class MalzemeKategoriDeposu
{
    public static IReadOnlyList<string> Liste => UygulamaAyarDeposu.Ayarlar.MalzemeKategorileri;

    /// <summary>Boş liste bırakır — örnek kategori isimleri otomatik eklenmez.</summary>
    public static void VarsayilanlariHazirla()
    {
        // Bilerek boş: kullanıcı veya alınan malzeme kayıtları kategorileri oluşturur.
    }

    public static bool Ekle(string ad)
    {
        ad = ad.Trim();
        if (string.IsNullOrWhiteSpace(ad))
            return false;

        if (UygulamaAyarDeposu.Ayarlar.MalzemeKategorileri
            .Any(k => k.Equals(ad, StringComparison.OrdinalIgnoreCase)))
            return false;

        UygulamaAyarDeposu.Ayarlar.MalzemeKategorileri.Add(ad);
        UygulamaAyarDeposu.Kaydet();
        return true;
    }

    public static bool Sil(string ad)
    {
        var liste = UygulamaAyarDeposu.Ayarlar.MalzemeKategorileri;
        var bulunan = liste.FirstOrDefault(k => k.Equals(ad, StringComparison.OrdinalIgnoreCase));
        if (bulunan is null)
            return false;

        liste.Remove(bulunan);
        UygulamaAyarDeposu.Kaydet();
        return true;
    }

    /// <summary>Tüm kategori listesini temizler (veri sıfırlama).</summary>
    public static void TumunuTemizle()
    {
        UygulamaAyarDeposu.Ayarlar.MalzemeKategorileri.Clear();
        UygulamaAyarDeposu.Kaydet();
    }

    /// <summary>Kategoriye ait alınan malzeme kayıt sayısı (büyük/küçük harf duyarsız).</summary>
    public static int AlinanMalzemeKayitSayisi(string kategoriAdi)
    {
        if (string.IsNullOrWhiteSpace(kategoriAdi))
            return 0;

        ModulVeriDeposu.Yukle();
        return ModulVeriDeposu.AlinanMalzemeler.Count(k =>
            !string.IsNullOrWhiteSpace(k.Kategori)
            && k.Kategori.Trim().Equals(kategoriAdi.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Kategoriyi ayarlardan siler ve o kategoriye ait tüm alınan malzeme kayıtlarını kaldırır.
    /// </summary>
    /// <returns>Silinen alınan malzeme kayıt sayısı; kategori silinemediyse -1.</returns>
    public static int SilVeAlinanMalzemeKayitlariniTemizle(string ad)
    {
        ad = ad.Trim();
        if (string.IsNullOrWhiteSpace(ad))
            return -1;

        var liste = UygulamaAyarDeposu.Ayarlar.MalzemeKategorileri;
        var bulunan = liste.FirstOrDefault(k => k.Equals(ad, StringComparison.OrdinalIgnoreCase));
        if (bulunan is null)
            return -1;

        ModulVeriDeposu.Yukle();
        var silinecekler = ModulVeriDeposu.AlinanMalzemeler
            .Where(k => !string.IsNullOrWhiteSpace(k.Kategori)
                        && k.Kategori.Trim().Equals(bulunan, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var kayit in silinecekler)
            ModulVeriDeposu.AlinanMalzemeler.Remove(kayit);

        if (silinecekler.Count > 0)
            ModulVeriDeposu.KaydetAlinanMalzemeler();

        liste.Remove(bulunan);
        UygulamaAyarDeposu.Kaydet();
        return silinecekler.Count;
    }

    /// <summary>Birden fazla kategoriyi ayarlardan siler; kayıtları da temizler.</summary>
    public static (int silinenKategori, int silinenKayit) TopluSil(IEnumerable<string> adlar)
    {
        var benzersiz = adlar
            .Select(a => a.Trim())
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (benzersiz.Count == 0)
            return (0, 0);

        var toplamKayit = 0;
        var silinenKategori = 0;
        foreach (var ad in benzersiz)
        {
            var n = SilVeAlinanMalzemeKayitlariniTemizle(ad);
            if (n >= 0)
            {
                silinenKategori++;
                toplamKayit += n;
            }
        }

        BosKategorileriTemizle();
        return (silinenKategori, toplamKayit);
    }

    /// <summary>Ayarlar + alınan malzeme + stok kayıtlarındaki tüm kategoriler (form/combo).</summary>
    public static IEnumerable<string> TumListe()
    {
        ModulVeriDeposu.Yukle();

        var set = new HashSet<string>(Liste, StringComparer.OrdinalIgnoreCase);
        foreach (var k in ModulVeriDeposu.AlinanMalzemeler)
        {
            if (!string.IsNullOrWhiteSpace(k.Kategori))
                set.Add(k.Kategori.Trim());
        }

        foreach (var k in ModulVeriDeposu.Stok)
        {
            if (!string.IsNullOrWhiteSpace(k.Kategori))
                set.Add(k.Kategori.Trim());
        }

        return set.OrderBy(s => s, StringComparer.CurrentCultureIgnoreCase);
    }

    /// <summary>Yalnızca alınan malzeme kayıtlarından kategorileri ayarlara ekler; kullanılmayanları temizler.</summary>
    public static int KayitlardanSenkronizeEt()
    {
        UygulamaAyarDeposu.Yukle();
        ModulVeriDeposu.Yukle();

        var ayarlar = UygulamaAyarDeposu.Ayarlar.MalzemeKategorileri;
        var eklendi = 0;

        foreach (var kategori in ModulVeriDeposu.AlinanMalzemeler.Select(k => k.Kategori))
        {
            if (string.IsNullOrWhiteSpace(kategori))
                continue;

            var ad = kategori.Trim();
            if (ayarlar.Any(k => k.Equals(ad, StringComparison.OrdinalIgnoreCase)))
                continue;

            ayarlar.Add(ad);
            eklendi++;
        }

        if (eklendi > 0)
            UygulamaAyarDeposu.Kaydet();

        BosKategorileriTemizle();
        return eklendi;
    }

    /// <summary>Alınan malzemede kaydı kalmayan kategorileri ayarlardan kaldırır.</summary>
    public static int BosKategorileriTemizle()
    {
        UygulamaAyarDeposu.Yukle();
        ModulVeriDeposu.Yukle();

        var kullanilan = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var k in ModulVeriDeposu.AlinanMalzemeler)
        {
            if (!string.IsNullOrWhiteSpace(k.Kategori))
                kullanilan.Add(k.Kategori.Trim());
        }

        var liste = UygulamaAyarDeposu.Ayarlar.MalzemeKategorileri;
        var silinen = 0;
        for (var i = liste.Count - 1; i >= 0; i--)
        {
            if (kullanilan.Contains(liste[i]))
                continue;

            liste.RemoveAt(i);
            silinen++;
        }

        if (silinen > 0)
            UygulamaAyarDeposu.Kaydet();

        return silinen;
    }

    public static IEnumerable<string> FiltreIcinListe(IEnumerable<AlinanMalzemeKaydi> kayitlar) => TumListe();

    public static void ComboDoldur(ComboBox combo, string? secili = null)
    {
        combo.Items.Clear();
        foreach (var kategori in TumListe())
            combo.Items.Add(kategori);

        if (!string.IsNullOrWhiteSpace(secili))
        {
            var eslesen = combo.Items.Cast<object>()
                .Select(o => o?.ToString())
                .FirstOrDefault(k => k != null && k.Equals(secili, StringComparison.OrdinalIgnoreCase));
            if (eslesen is not null)
                combo.SelectedItem = eslesen;
            else
                combo.Text = secili;
        }
        else if (combo.Items.Count > 0)
        {
            combo.SelectedIndex = 0;
        }
    }
}

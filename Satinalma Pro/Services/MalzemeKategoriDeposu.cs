using System.Windows.Controls;
using SatinalmaPro.Models;

namespace SatinalmaPro.Services;

public static class MalzemeKategoriDeposu
{
    public static readonly string[] Varsayilanlar =
    [
        "Agrega", "Bağlayıcı", "Demir", "Hizmet", "Yakıt", "Malzeme", "Diğer"
    ];

    public static IReadOnlyList<string> Liste => UygulamaAyarDeposu.Ayarlar.MalzemeKategorileri;

    public static void VarsayilanlariHazirla()
    {
        if (UygulamaAyarDeposu.Ayarlar.MalzemeKategorileri.Count > 0)
            return;

        UygulamaAyarDeposu.Ayarlar.MalzemeKategorileri.AddRange(Varsayilanlar);
        UygulamaAyarDeposu.Kaydet();
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

        if (liste.Count <= 1)
            return false;

        liste.Remove(bulunan);
        UygulamaAyarDeposu.Kaydet();
        return true;
    }

    /// <summary>Ayarlar + alınan malzeme + stok kayıtlarındaki tüm kategoriler.</summary>
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

    /// <summary>Excel / kayıtlardaki yeni kategorileri ayarlara ekler.</summary>
    public static int KayitlardanSenkronizeEt()
    {
        UygulamaAyarDeposu.Yukle();
        ModulVeriDeposu.Yukle();

        var ayarlar = UygulamaAyarDeposu.Ayarlar.MalzemeKategorileri;
        var eklendi = 0;

        foreach (var kategori in ModulVeriDeposu.AlinanMalzemeler.Select(k => k.Kategori)
                     .Concat(ModulVeriDeposu.Stok.Select(k => k.Kategori)))
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

        return eklendi;
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

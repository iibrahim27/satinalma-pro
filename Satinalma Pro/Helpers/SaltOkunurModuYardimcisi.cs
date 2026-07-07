using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SatinalmaPro.Helpers;

/// <summary>Okuma izni — filtre, arama, PDF/Excel; yazma işlemleri kapalı.</summary>
public static class SaltOkunurModuYardimcisi
{
    private static readonly string[] YasakliAnahtarlar =
    [
        "sil", "duzenle", "düzenle", "kaydet", "ekle", "guncelle", "güncelle",
        "yukle", "yükle", "onayla", "reddet", "teklif", "siparis", "sipariş",
        "malkabul", "mal kabul", "stokgiris", "stokcikis", "stok giris", "stok cikis",
        "zimmet", "sevk", "gonder", "gönder", "pasif", "sifir", "sıfır",
        "faturala", "yenitalep", "yeni talep", "yeni arac", "yeni kayit",
        "temizle liste", "bulutayukle", "kullaniciyonet"
    ];

    private static readonly string[] IzinliAnahtarlar =
    [
        "pdf", "excel", "indir", "sablon", "şablon", "yazdir", "yazdır",
        "yenile", "filtre", "ara", "temizle", "sec", "seç", "detay",
        "goruntule", "görüntüle", "ilk", "onceki", "önceki", "sonraki", "son ",
        "nav", "kategori", "malzeme", "rapor", "disa", "dışa", "ozet", "özet",
        "goster", "göster", "listele", "ac", "aç", "yazdir", "export", "aktar pdf"
    ];

    public static void Uygula(DependencyObject kok) => Gez(kok);

    private static void Gez(DependencyObject dugum)
    {
        switch (dugum)
        {
            case Button btn when btn.Name is not ("BtnCikis" or "BtnHome"):
                btn.IsEnabled = !YazmaButonuMu(btn);
                break;
            case TextBox tb:
                tb.IsReadOnly = !FiltreMetinKutusuMu(tb);
                break;
            case PasswordBox pb:
                pb.IsEnabled = false;
                break;
            case ComboBox cb:
                cb.IsEnabled = FiltreComboMu(cb);
                break;
            case MenuItem menu when menu.Header is string baslik:
                menu.IsEnabled = !YazmaMenuMu(baslik);
                break;
            case DataGrid grid:
                grid.IsReadOnly = true;
                break;
        }

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(dugum); i++)
            Gez(VisualTreeHelper.GetChild(dugum, i));
    }

    private static bool YazmaMenuMu(string baslik)
    {
        var metin = baslik.ToLowerInvariant();
        return metin.Contains("sil") || metin.Contains("düzenle") || metin.Contains("duzenle")
            || metin.Contains("kaydet") || metin.Contains("ekle");
    }

    private static bool YazmaButonuMu(Button btn)
    {
        var metin = $"{btn.Name} {ButonMetni(btn)}".ToLowerInvariant();

        if (metin.Contains("excelyukle") || metin.Contains("excel yukle") || metin.Contains("excel yükle"))
            return true;

        if (metin.Contains("disa") || metin.Contains("dışa") || metin.Contains("sablon") || metin.Contains("şablon")
            || metin.Contains("pdf") || metin.Contains("indir") || metin.Contains("yazdir") || metin.Contains("yazdır"))
            return false;

        foreach (var anahtar in YasakliAnahtarlar)
        {
            if (metin.Contains(anahtar))
                return true;
        }

        foreach (var anahtar in IzinliAnahtarlar)
        {
            if (metin.Contains(anahtar))
                return false;
        }

        return true;
    }

    private static bool FiltreMetinKutusuMu(TextBox tb)
    {
        var ad = tb.Name ?? "";
        if (ad.Contains("Arama", StringComparison.OrdinalIgnoreCase)
            || ad.Contains("Ara", StringComparison.OrdinalIgnoreCase)
            || ad.Contains("Filtre", StringComparison.OrdinalIgnoreCase)
            || ad.Contains("Search", StringComparison.OrdinalIgnoreCase))
            return true;

        return tb.Style?.ToString()?.Contains("Filter", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static bool FiltreComboMu(ComboBox cb)
    {
        var ad = cb.Name ?? "";
        if (ad.StartsWith("Cmb", StringComparison.OrdinalIgnoreCase)
            || ad.Contains("Filtre", StringComparison.OrdinalIgnoreCase)
            || ad.Contains("Rapor", StringComparison.OrdinalIgnoreCase)
            || ad.Contains("Sec", StringComparison.OrdinalIgnoreCase))
            return true;

        return cb.Style?.ToString()?.Contains("Filter", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static string ButonMetni(Button btn)
    {
        if (btn.Content is string s)
            return s;

        if (btn.Content is TextBlock tb)
            return tb.Text;

        if (btn.Content is DependencyObject d)
            return CocukMetinleri(d);

        return btn.Content?.ToString() ?? "";
    }

    private static string CocukMetinleri(DependencyObject dugum)
    {
        var parcalar = new List<string>();
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(dugum); i++)
        {
            var cocuk = VisualTreeHelper.GetChild(dugum, i);
            if (cocuk is TextBlock tb && !string.IsNullOrWhiteSpace(tb.Text))
                parcalar.Add(tb.Text);
            else
                parcalar.Add(CocukMetinleri(cocuk));
        }

        return string.Join(' ', parcalar.Where(p => !string.IsNullOrWhiteSpace(p)));
    }
}

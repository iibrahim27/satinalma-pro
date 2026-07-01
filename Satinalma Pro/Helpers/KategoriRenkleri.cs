using System.Windows;
using System.Windows.Media;

namespace SatinalmaPro.Helpers;

public static class KategoriRenkleri
{
    private static readonly Dictionary<string, Color> Renkler = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Agrega"] = Color.FromRgb(224, 242, 254),
        ["Bağlayıcı"] = Color.FromRgb(254, 243, 199),
        ["Çimento"] = Color.FromRgb(254, 243, 199),
        ["Demir"] = Color.FromRgb(237, 233, 254),
        ["Hizmet"] = Color.FromRgb(209, 250, 229),
        ["İşçilik"] = Color.FromRgb(219, 234, 254),
        ["Nakliye"] = Color.FromRgb(224, 242, 254),
        ["Kira"] = Color.FromRgb(254, 249, 195),
        ["Mühendislik"] = Color.FromRgb(237, 233, 254),
        ["Danışmanlık"] = Color.FromRgb(243, 232, 255),
        ["Bakım-Onarım"] = Color.FromRgb(255, 237, 213),
        ["Güvenlik"] = Color.FromRgb(254, 226, 226),
        ["Temizlik"] = Color.FromRgb(204, 251, 241),
        ["Yakıt"] = Color.FromRgb(255, 228, 230),
        ["Akaryakıt"] = Color.FromRgb(255, 228, 230),
        ["Malzeme"] = Color.FromRgb(219, 234, 254),
        ["Ekipman"] = Color.FromRgb(243, 232, 255),
        ["Diğer"] = Color.FromRgb(241, 245, 249)
    };

    private static readonly Color[] YedekRenkler =
    [
        Color.FromRgb(224, 231, 255),
        Color.FromRgb(204, 251, 241),
        Color.FromRgb(254, 226, 226),
        Color.FromRgb(254, 249, 195),
        Color.FromRgb(233, 213, 255),
        Color.FromRgb(207, 250, 254)
    ];

    public static Brush GetFirca(string kategori) =>
        FircaOnbellegi.Al($"kat:{kategori}", () => GetRenk(kategori));

    public static Color GetRenk(string kategori)
    {
        if (string.IsNullOrWhiteSpace(kategori))
            return Renkler["Diğer"];

        if (Renkler.TryGetValue(kategori.Trim(), out var renk))
            return renk;

        var index = Math.Abs(kategori.GetHashCode()) % YedekRenkler.Length;
        return YedekRenkler[index];
    }
}

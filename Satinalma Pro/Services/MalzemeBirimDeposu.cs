using System.Windows.Controls;
using SatinalmaPro.Models;

namespace SatinalmaPro.Services;

public static class MalzemeBirimDeposu
{
    public static readonly string[] Varsayilanlar =
    [
        "Adet", "Ton", "Kg", "Lt", "m", "m²", "m³"
    ];

    public static IReadOnlyList<string> Liste => UygulamaAyarDeposu.Ayarlar.MalzemeBirimleri;

    public static void VarsayilanlariHazirla()
    {
        if (UygulamaAyarDeposu.Ayarlar.MalzemeBirimleri.Count > 0)
            return;

        UygulamaAyarDeposu.Ayarlar.MalzemeBirimleri.AddRange(Varsayilanlar);
        UygulamaAyarDeposu.Kaydet();
    }

    public static bool Ekle(string ad)
    {
        ad = ad.Trim();
        if (string.IsNullOrWhiteSpace(ad))
            return false;

        if (Liste.Any(b => b.Equals(ad, StringComparison.OrdinalIgnoreCase)))
            return false;

        UygulamaAyarDeposu.Ayarlar.MalzemeBirimleri.Add(ad);
        UygulamaAyarDeposu.Kaydet();
        return true;
    }

    public static bool Sil(string ad)
    {
        var liste = UygulamaAyarDeposu.Ayarlar.MalzemeBirimleri;
        var bulunan = liste.FirstOrDefault(b => b.Equals(ad, StringComparison.OrdinalIgnoreCase));
        if (bulunan is null)
            return false;

        if (liste.Count <= 1)
            return false;

        liste.Remove(bulunan);
        UygulamaAyarDeposu.Kaydet();
        return true;
    }

    public static void ComboDoldur(ComboBox combo, string? secili = null)
    {
        combo.Items.Clear();
        foreach (var birim in Liste)
            combo.Items.Add(birim);

        if (string.IsNullOrWhiteSpace(secili))
        {
            if (combo.Items.Count > 0)
                combo.SelectedIndex = 0;
            return;
        }

        for (var i = 0; i < combo.Items.Count; i++)
        {
            if (combo.Items[i]?.ToString()?.Equals(secili, StringComparison.OrdinalIgnoreCase) == true)
            {
                combo.SelectedIndex = i;
                return;
            }
        }

        if (combo.Items.Count > 0)
            combo.SelectedIndex = 0;
    }
}

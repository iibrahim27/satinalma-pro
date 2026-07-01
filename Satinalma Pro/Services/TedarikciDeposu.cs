namespace SatinalmaPro.Services;

public static class TedarikciDeposu
{
    public static IEnumerable<string> Liste() =>
        ModulVeriDeposu.AlinanMalzemeler
            .Select(k => k.Tedarikci)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(s => s, StringComparer.CurrentCultureIgnoreCase);

    public static void ComboDoldur(System.Windows.Controls.ComboBox combo, string? secili = null)
    {
        var liste = Liste().ToList();
        combo.ItemsSource = liste;

        if (!string.IsNullOrWhiteSpace(secili))
            combo.Text = secili;
    }
}

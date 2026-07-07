using System.Windows.Controls;
using System.Windows.Media;
using SatinalmaPro.Services;
using SatinalmaPro.Theme;

namespace SatinalmaPro.Controls.Dashboard;

public partial class ErpRightWidgetsPanel : UserControl
{
    public ErpRightWidgetsPanel() => InitializeComponent();

    public void Bagla(
        IReadOnlyList<AnaSayfaHatirlatma> hatirlatmalar,
        AnaSayfaFinansOzet finans,
        IReadOnlyList<AnaSayfaTopUrun> topUrunler)
    {
        HatirlatmaListe.ItemsSource = hatirlatmalar.Select(h => new
        {
            h.Metin,
            Renk = AppTheme.Brush(h.RenkHex)
        }).ToList();

        TxtGelir.Text = $"Gelir: {finans.Gelir}";
        TxtGider.Text = $"Gider: {finans.Gider}";
        TxtKar.Text = $"Kâr: {finans.Kar}";
        TxtMarj.Text = $"%{finans.KarMarjiYuzde:0.#}";

        TopUrunListe.ItemsSource = topUrunler;
    }
}

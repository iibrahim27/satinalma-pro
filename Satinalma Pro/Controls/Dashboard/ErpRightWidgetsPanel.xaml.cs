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

        TxtGelir.Text = $"Stok değeri: {finans.Gelir}";
        TxtGider.Text = $"Bu ay harcama: {finans.Gider}";
        TxtKar.Text = $"Fark: {finans.Kar}";
        TxtMarj.Text = $"%{finans.KarMarjiYuzde:0.#}";

        TopUrunListe.ItemsSource = topUrunler;
    }
}

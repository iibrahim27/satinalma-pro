using System.Windows.Controls;

namespace SatinalmaPro.Views.Modules.Satinalma.Part1;

public partial class SatinalmaBosSekmeView : UserControl
{
    public SatinalmaBosSekmeView() => InitializeComponent();

    public void Goster(string baslik, string aciklama)
    {
        TxtBaslik.Text = baslik;
        TxtAciklama.Text = aciklama;
    }
}

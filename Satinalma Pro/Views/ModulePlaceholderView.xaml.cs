using System.Windows.Controls;

namespace SatinalmaPro.Views;

public partial class ModulePlaceholderView : UserControl
{
    public ModulePlaceholderView(string moduleTitle)
    {
        InitializeComponent();
        TxtBaslik.Text = moduleTitle;
    }
}

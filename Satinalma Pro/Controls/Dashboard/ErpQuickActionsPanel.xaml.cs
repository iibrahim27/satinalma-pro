using System.Windows;
using System.Windows.Controls;

namespace SatinalmaPro.Controls.Dashboard;

public partial class ErpQuickActionsPanel : UserControl
{
    public event Action<string>? ModulSecildi;

    public ErpQuickActionsPanel() => InitializeComponent();

    private void HizliIslem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string modul)
            ModulSecildi?.Invoke(modul);
    }
}

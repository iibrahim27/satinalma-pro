using SatinalmaPro.Shared.Helpers;

namespace SatinalmaPro.Mobile.Views.Controls;

public partial class SurumAltBilgiBar : ContentView
{
    public SurumAltBilgiBar()
    {
        InitializeComponent();
        LblAltBilgi.Text = UygulamaBilgisi.AltBilgiMetni(AppInfo.VersionString);
    }
}

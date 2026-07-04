using SatinalmaPro.Mobile.ViewModels.Yonetim;

namespace SatinalmaPro.Mobile.Views.Yonetim;

public partial class YonetimProfilPage : ContentPage
{
    public YonetimProfilPage(YonetimProfilViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}

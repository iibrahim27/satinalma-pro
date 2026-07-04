using SatinalmaPro.Mobile.ViewModels.Yonetim;

namespace SatinalmaPro.Mobile.Views.Yonetim;

[QueryProperty(nameof(TeklifId), "id")]
public partial class YonetimTeklifDetayPage : ContentPage
{
    private readonly YonetimTeklifDetayViewModel _vm;
    private string? _teklifId;

    public string? TeklifId
    {
        get => _teklifId;
        set
        {
            _teklifId = value;
            if (!string.IsNullOrEmpty(value))
                _ = _vm.YukleCommand.ExecuteAsync(value);
        }
    }

    public YonetimTeklifDetayPage(YonetimTeklifDetayViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
        DurumGorunumu.TekrarDeneTiklandi += (_, _) => _ = _vm.YukleCommand.ExecuteAsync(_teklifId);
    }
}

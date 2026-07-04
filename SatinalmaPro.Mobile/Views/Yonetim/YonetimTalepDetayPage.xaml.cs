using SatinalmaPro.Mobile.ViewModels.Yonetim;

namespace SatinalmaPro.Mobile.Views.Yonetim;

[QueryProperty(nameof(TalepId), "id")]
public partial class YonetimTalepDetayPage : ContentPage
{
    private readonly YonetimTalepDetayViewModel _vm;
    private string? _talepId;

    public string? TalepId
    {
        get => _talepId;
        set
        {
            _talepId = value;
            if (!string.IsNullOrEmpty(value))
                _ = _vm.YukleCommand.ExecuteAsync(value);
        }
    }

    public YonetimTalepDetayPage(YonetimTalepDetayViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
        DurumGorunumu.TekrarDeneTiklandi += (_, _) => _ = _vm.YukleCommand.ExecuteAsync(_talepId);
    }
}

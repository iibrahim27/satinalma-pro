using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SatinalmaPro.Models;
using SatinalmaPro.Views;
using SatinalmaPro.Services;
using SatinalmaPro.Theme;

namespace SatinalmaPro.Controls.Dashboard;

public partial class ModuleCardControl : UserControl
{
    private string _modulBaslik = "";

    public event EventHandler<string>? ModulSecildi;

    public ModuleCardControl()
    {
        InitializeComponent();
        MouseEnter += (_, _) => Card.Effect = (System.Windows.Media.Effects.Effect?)FindResource("DashCardShadowHover");
        MouseLeave += (_, _) => Card.Effect = (System.Windows.Media.Effects.Effect?)FindResource("DashCardShadow");
    }

    public void Bagla(ModulKarti kart)
    {
        _modulBaslik = kart.Title;
        TxtBaslik.Text = IconProvider.ModulKisaAd(kart.Title);
        TxtAciklama.Text = kart.Subtitle;

        var renk = kart.GradientStart;
        IconKutu.Background = AppTheme.TintBrush(renk, 36);
        Icon.Kind = IconProvider.ModulIkonu(kart.Title);
        Icon.StrokeBrush = new System.Windows.Media.SolidColorBrush(renk);

        if (kart.BildirimRozetiGorunur)
        {
            Badge.Visibility = Visibility.Visible;
            TxtBadge.Text = kart.BildirimRozetiMetni;
        }
        else
        {
            Badge.Visibility = Visibility.Collapsed;
        }
    }

    private void Card_Click(object sender, MouseButtonEventArgs e) =>
        ModulSecildi?.Invoke(this, _modulBaslik);
}

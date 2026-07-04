using SatinalmaPro.Shared.Helpers;

using Microsoft.Maui.Controls.Shapes;

namespace SatinalmaPro.Mobile.Views.Controls;

/// <summary>Malzeme adı yazarken bilinen adları filtreleyip önerir; eşleşme yoksa serbest giriş.</summary>
public sealed class MalzemeOneriGirisView : VerticalStackLayout
{
    private readonly Entry _entry;
    private readonly CollectionView _oneriler;
    private readonly Border _oneriCerceve;
    private Func<string, IEnumerable<string>> _ara = _ => [];
    private bool _icGuncelleme;

    public MalzemeOneriGirisView()
    {
        Spacing = 0;

        _entry = new Entry { Placeholder = "Malzeme" };
        _entry.TextChanged += Entry_TextChanged;
        _entry.Unfocused += (_, _) => OneriCercevesiniGizle();

        _oneriler = new CollectionView
        {
            SelectionMode = SelectionMode.Single,
            MaximumHeightRequest = 160
        };
        _oneriler.ItemTemplate = new DataTemplate(() =>
        {
            var label = new Label
            {
                FontSize = 14,
                Padding = new Thickness(12, 10),
                TextColor = TemaKaynaklari.BirincilMetin
            };
            label.SetBinding(Label.TextProperty, ".");
            return label;
        });
        _oneriler.SelectionChanged += Oneriler_SelectionChanged;

        _oneriCerceve = new Border
        {
            IsVisible = false,
            Stroke = TemaKaynaklari.KartCerceve,
            StrokeThickness = 1,
            BackgroundColor = TemaKaynaklari.KartArkaPlan,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Padding = 0,
            Content = _oneriler
        };

        Children.Add(_entry);
        Children.Add(_oneriCerceve);
    }

    public string Text
    {
        get => _entry.Text ?? "";
        set
        {
            if (_entry.Text == value)
                return;
            _icGuncelleme = true;
            _entry.Text = value;
            _icGuncelleme = false;
        }
    }

    public void OneriKaynaginiAyarla(Func<string, IEnumerable<string>> ara) => _ara = ara;

    private void Entry_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_icGuncelleme)
            return;

        var metin = _entry.Text ?? "";
        if (string.IsNullOrWhiteSpace(metin))
        {
            OneriCercevesiniGizle();
            return;
        }

        var liste = _ara(metin).ToList();
        if (liste.Count == 0)
        {
            OneriCercevesiniGizle();
            return;
        }

        _oneriler.ItemsSource = liste;
        _oneriCerceve.IsVisible = true;
    }

    private void Oneriler_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_oneriler.SelectedItem is not string secim)
            return;

        _icGuncelleme = true;
        _entry.Text = secim;
        _icGuncelleme = false;
        _oneriler.SelectedItem = null;
        OneriCercevesiniGizle();
    }

    private void OneriCercevesiniGizle()
    {
        _oneriCerceve.IsVisible = false;
        _oneriler.ItemsSource = null;
    }
}

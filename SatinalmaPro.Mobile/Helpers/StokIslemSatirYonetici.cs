using SatinalmaPro.Mobile.Views.Controls;

namespace SatinalmaPro.Mobile.Helpers;

public sealed class StokIslemSatirYonetici
{
    private readonly VerticalStackLayout _panel;
    private readonly Func<string, IEnumerable<string>> _malzemeOneri;
    private readonly List<Satir> _satirlar = [];

    public StokIslemSatirYonetici(VerticalStackLayout panel, Func<string, IEnumerable<string>> malzemeOneri)
    {
        _panel = panel;
        _malzemeOneri = malzemeOneri;
    }

    public int SatirSayisi => _satirlar.Count;

    public void Temizle()
    {
        _panel.Clear();
        _satirlar.Clear();
    }

    public void SatirEkle(string malzeme = "", string miktar = "")
    {
        var satir = new Satir();
        _satirlar.Add(satir);

        satir.Malzeme = new MalzemeOneriGirisView { Text = malzeme };
        satir.Malzeme.OneriKaynaginiAyarla(_malzemeOneri);

        satir.Miktar = new Entry
        {
            Placeholder = "Miktar",
            Keyboard = Keyboard.Numeric,
            Text = miktar,
            VerticalOptions = LayoutOptions.Center
        };

        var sil = new Button
        {
            Text = "Sil",
            Style = Application.Current?.Resources["BtnSecondary"] as Style,
            Padding = new Thickness(10, 6),
            FontSize = 12,
            VerticalOptions = LayoutOptions.Center
        };
        sil.Clicked += (_, _) => SatirSil(satir);

        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(88),
                new ColumnDefinition(64)
            },
            ColumnSpacing = 8,
            Padding = new Thickness(0, 4)
        };

        grid.Add(satir.Malzeme, 0);
        grid.Add(satir.Miktar, 1);
        grid.Add(sil, 2);

        satir.Kapsayici = new Border
        {
            Stroke = Colors.LightGray,
            StrokeThickness = 1,
            Padding = new Thickness(10, 8),
            Content = grid
        };

        _panel.Add(satir.Kapsayici);
    }

    public void SatirSil(Satir satir)
    {
        if (_satirlar.Count <= 1)
        {
            satir.Malzeme.Text = "";
            satir.Miktar.Text = "";
            return;
        }

        _satirlar.Remove(satir);
        _panel.Remove(satir.Kapsayici);
    }

    public IEnumerable<(string Malzeme, double Miktar)> GecerliSatirlar()
    {
        foreach (var satir in _satirlar)
        {
            var malzeme = satir.Malzeme.Text?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(malzeme))
                continue;

            var miktar = double.TryParse(satir.Miktar.Text?.Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var m)
                ? m
                : 0;

            if (miktar <= 0)
                continue;

            yield return (malzeme, miktar);
        }
    }

    public sealed class Satir
    {
        public Border Kapsayici { get; set; } = null!;
        public MalzemeOneriGirisView Malzeme { get; set; } = null!;
        public Entry Miktar { get; set; } = null!;
    }
}

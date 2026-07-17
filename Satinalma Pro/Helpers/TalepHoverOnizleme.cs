using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SatinalmaPro.Models;
using SatinalmaPro.Services;

namespace SatinalmaPro.Helpers;

/// <summary>
/// Satınalma liste satırlarında, detaya geçmeden talep ve teklif içeriğini gösterir.
/// Liste modellerinin Talep veya Id özelliğini kullanır; bu nedenle tüm talep sekmelerine
/// aynı davranış eklenebilir.
/// </summary>
public static class TalepHoverOnizleme
{
    private static readonly DependencyProperty EtkinProperty = DependencyProperty.RegisterAttached(
        "Etkin",
        typeof(bool),
        typeof(TalepHoverOnizleme),
        new PropertyMetadata(false));

    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    public static void Etkinlestir(DataGrid tablo)
    {
        if ((bool)tablo.GetValue(EtkinProperty))
            return;

        tablo.SetValue(EtkinProperty, true);
        ToolTipService.SetInitialShowDelay(tablo, 420);
        ToolTipService.SetBetweenShowDelay(tablo, 120);
        ToolTipService.SetShowDuration(tablo, 18_000);
        tablo.LoadingRow += Tablo_LoadingRow;
    }

    private static void Tablo_LoadingRow(object? sender, DataGridRowEventArgs e)
    {
        var talep = TalebiBul(e.Row.Item);
        e.Row.ToolTip = talep is null ? null : OnizlemeKarti(talep);
    }

    private static SatinalmaTalep? TalebiBul(object? satir)
    {
        if (satir is SatinalmaTalep talep)
            return talep;

        if (satir is null)
            return null;

        var tip = satir.GetType();
        if (tip.GetProperty("Talep")?.GetValue(satir) is SatinalmaTalep model)
            return model;

        if (tip.GetProperty("Id")?.GetValue(satir) is Guid id)
            return SatinalmaDepo.Talepler.FirstOrDefault(t => t.Id == id);

        return null;
    }

    private static Border OnizlemeKarti(SatinalmaTalep talep)
    {
        var panel = new StackPanel { Width = 440 };
        panel.Children.Add(BaslikSatiri(talep));
        panel.Children.Add(AnaBilgi(talep));

        if (!string.IsNullOrWhiteSpace(talep.TalepAciklamasi))
        {
            panel.Children.Add(BolumBasligi("Talep açıklaması", new Thickness(0, 13, 0, 4)));
            panel.Children.Add(Metin(talep.TalepAciklamasi, 12, Brushes.WhiteSmoke, textWrapping: TextWrapping.Wrap));
        }

        panel.Children.Add(BolumBasligi("Talep edilenler", new Thickness(0, 13, 0, 5)));
        var kalemler = (talep.Kalemler ?? [])
            .Where(k => !string.IsNullOrWhiteSpace(k.Malzeme))
            .OrderBy(k => k.SiraNo)
            .ToList();

        if (kalemler.Count == 0)
        {
            panel.Children.Add(Metin("Malzeme satırı girilmemiş.", 12, Renk("#B7CBD7")));
        }
        else
        {
            foreach (var kalem in kalemler.Take(4))
                panel.Children.Add(KalemSatiri(kalem));

            if (kalemler.Count > 4)
                panel.Children.Add(Metin($"+{kalemler.Count - 4} kalem daha", 11, Renk("#8DDBD1"), new Thickness(0, 5, 0, 0)));
        }

        panel.Children.Add(TeklifBolumu(talep));

        var tumKalemler = talep.Kalemler ?? [];
        if (tumKalemler.Any(k => k.KabulEdilenMiktar > 0))
        {
            var kabulEdilen = tumKalemler.Sum(k => k.KabulEdilenMiktar);
            var istenen = tumKalemler.Sum(k => k.Miktar);
            panel.Children.Add(BolumBasligi("Mal kabul", new Thickness(0, 13, 0, 4)));
            panel.Children.Add(Metin(
                $"{kabulEdilen.ToString("N2", Tr)} / {istenen.ToString("N2", Tr)} birim kabul edildi",
                12,
                Renk("#C9E8E2")));
        }

        panel.Children.Add(new Border
        {
            Height = 1,
            Background = Renk("#31546A"),
            Margin = new Thickness(0, 14, 0, 9)
        });
        panel.Children.Add(Metin("Önizleme · Ayrıntıyı açmak için satıra çift tıklayın", 11, Renk("#9DB7C7")));

        return new Border
        {
            Background = Renk("#102D43"),
            BorderBrush = Renk("#3C6982"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(16, 14, 16, 13),
            MaxWidth = 470,
            Child = panel,
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 18,
                ShadowDepth = 3,
                Opacity = 0.32,
                Color = Colors.Black
            }
        };
    }

    private static Grid BaslikSatiri(SatinalmaTalep talep)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var sol = new StackPanel();
        sol.Children.Add(Metin(
            string.IsNullOrWhiteSpace(talep.TalepNo) ? "Talep önizleme" : talep.TalepNo,
            17,
            Brushes.White,
            fontWeight: FontWeights.SemiBold));
        sol.Children.Add(Metin(
            $"{BosYerineCizgi(talep.TalepEden)} · {BosYerineCizgi(talep.Tarih)}",
            11,
            Renk("#B7CBD7"),
            new Thickness(0, 3, 0, 0)));
        grid.Children.Add(sol);

        var rozet = new Border
        {
            Background = Renk(OncelikRengi(talep.TalepTuru)),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(9, 4, 9, 4),
            VerticalAlignment = VerticalAlignment.Top,
            Child = Metin(TalepTurleri.GorunenAd(talep.TalepTuru), 10, Brushes.White, fontWeight: FontWeights.SemiBold)
        };
        Grid.SetColumn(rozet, 1);
        grid.Children.Add(rozet);
        return grid;
    }

    private static Grid AnaBilgi(SatinalmaTalep talep)
    {
        var grid = new Grid { Margin = new Thickness(0, 12, 0, 0) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var durum = new StackPanel();
        durum.Children.Add(Metin("SÜREÇ DURUMU", 9, Renk("#8DDBD1"), fontWeight: FontWeights.SemiBold));
        durum.Children.Add(Metin(BosYerineCizgi(talep.Durum), 12, Brushes.White, new Thickness(0, 3, 0, 0)));
        grid.Children.Add(durum);

        var saha = new StackPanel { Margin = new Thickness(16, 0, 0, 0) };
        saha.Children.Add(Metin("ŞANTİYE / HEDEF", 9, Renk("#8DDBD1"), fontWeight: FontWeights.SemiBold));
        saha.Children.Add(Metin(BosYerineCizgi(talep.SantiyeAdi), 12, Brushes.White, new Thickness(0, 3, 0, 0)));
        Grid.SetColumn(saha, 1);
        grid.Children.Add(saha);
        return grid;
    }

    private static UIElement TeklifBolumu(SatinalmaTalep talep)
    {
        var panel = new StackPanel { Margin = new Thickness(0, 13, 0, 0) };
        panel.Children.Add(BolumBasligi("Teklif / firma özeti", new Thickness(0, 0, 0, 5)));

        var teklifler = (talep.Teklifler ?? [])
            .Where(t => !string.IsNullOrWhiteSpace(t.FirmaAdi))
            .OrderBy(t => t.GenelToplam)
            .ToList();

        if (teklifler.Count == 0)
        {
            panel.Children.Add(Metin("Henüz teklif veya firma kaydı yok.", 12, Renk("#B7CBD7")));
            return panel;
        }

        foreach (var teklif in teklifler.Take(3))
        {
            var onayli = teklif.Onaylandi || teklif.Id == talep.OnaylananTeklifId
                || (talep.Kalemler ?? []).Any(k => k.OnaylananTeklifId == teklif.Id);
            var metin = teklif.GenelToplam > 0
                ? teklif.GenelToplam.ToString("N2", Tr) + " ₺"
                : "Fiyat girilmemiş";
            panel.Children.Add(TeklifSatiri(teklif.FirmaAdi, metin, onayli));
        }

        if (teklifler.Count > 3)
            panel.Children.Add(Metin($"+{teklifler.Count - 3} teklif daha", 11, Renk("#8DDBD1"), new Thickness(0, 5, 0, 0)));

        return panel;
    }

    private static Grid KalemSatiri(SatinalmaTalepKalemi kalem)
    {
        var grid = new Grid { Margin = new Thickness(0, 2, 0, 2) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.Children.Add(Metin("• " + kalem.Malzeme.Trim(), 12, Brushes.White));
        var miktar = Metin($"{kalem.Miktar.ToString("N2", Tr)} {kalem.Birim}".Trim(), 12, Renk("#C9E8E2"));
        Grid.SetColumn(miktar, 1);
        grid.Children.Add(miktar);
        return grid;
    }

    private static Grid TeklifSatiri(string firma, string tutar, bool onayli)
    {
        var grid = new Grid { Margin = new Thickness(0, 2, 0, 2) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var firmaMetni = (onayli ? "✓ " : "• ") + firma.Trim();
        grid.Children.Add(Metin(firmaMetni, 12, onayli ? Renk("#82E6B8") : Brushes.White));
        var tutarMetni = Metin(tutar, 11, Renk("#C9E8E2"));
        Grid.SetColumn(tutarMetni, 1);
        grid.Children.Add(tutarMetni);
        return grid;
    }

    private static TextBlock BolumBasligi(string metin, Thickness margin) =>
        Metin(metin.ToUpperInvariant(), 9, Renk("#8DDBD1"), margin, FontWeights.SemiBold);

    private static TextBlock Metin(
        string metin,
        double boyut,
        Brush renk,
        Thickness? margin = null,
        FontWeight? fontWeight = null,
        TextWrapping textWrapping = TextWrapping.NoWrap) =>
        new()
        {
            Text = metin,
            FontSize = boyut,
            Foreground = renk,
            Margin = margin ?? new Thickness(),
            FontWeight = fontWeight ?? FontWeights.Normal,
            TextTrimming = TextTrimming.CharacterEllipsis,
            TextWrapping = textWrapping
        };

    private static Brush Renk(string hex) =>
        new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)!);

    private static string BosYerineCizgi(string? metin) =>
        string.IsNullOrWhiteSpace(metin) ? "—" : metin.Trim();

    private static string OncelikRengi(string? tur) => tur switch
    {
        TalepTurleri.Acil => "#D9485F",
        TalepTurleri.Oncelikli => "#D88932",
        _ => "#287C91"
    };
}

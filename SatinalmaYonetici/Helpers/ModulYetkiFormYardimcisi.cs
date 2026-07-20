using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SatinalmaPro.Shared.Models;
using SatinalmaPro.Shared.Services;

namespace SatinalmaYonetici.Helpers;

/// <summary>Kullanıcı formundaki modül yetki paneli (okuma / yazma / sekmeler).</summary>
public sealed class ModulYetkiFormYardimcisi
{
    private sealed class Satir
    {
        public required string ModulAdi { get; init; }
        public required CheckBox Okuma { get; init; }
        public required CheckBox Yazma { get; init; }
        public required List<CheckBox> SekmeKutulari { get; init; }
    }

    private readonly StackPanel _panel;
    private readonly List<Satir> _satirlar = [];
    private bool _dolduruluyor;

    public ModulYetkiFormYardimcisi(StackPanel panel)
    {
        _panel = panel;
        Olustur();
    }

    public void RolVarsayilaniniYukle(string? rol)
    {
        var n = KullaniciRolleri.Normalize(rol);
        YetkileriRoldenYukle(n, MasaustuRolHaritasi.MasaustuModulleri(n));
    }

    public void YetkileriYukle(IEnumerable<ModulYetkiKaydi>? yetkiler, IEnumerable<string>? moduller, string? rol)
    {
        var liste = yetkiler?.ToList() ?? [];
        if (liste.Count > 0)
        {
            YetkileriYukle(liste, rol);
            return;
        }

        var n = KullaniciRolleri.Normalize(rol);
        var mod = moduller?.ToList() ?? [];
        if (mod.Count == 0)
            mod = MasaustuRolHaritasi.MasaustuModulleri(n).ToList();
        YetkileriRoldenYukle(n, mod);
    }

    public void TumunuTemizle()
    {
        _dolduruluyor = true;
        try
        {
            foreach (var satir in _satirlar)
            {
                satir.Okuma.IsChecked = false;
                satir.Yazma.IsChecked = false;
                foreach (var sekme in satir.SekmeKutulari)
                    sekme.IsChecked = false;
            }
        }
        finally
        {
            _dolduruluyor = false;
        }
    }

    public (List<string> Moduller, List<ModulYetkiKaydi> Yetkiler) Topla(string? rol)
    {
        var n = KullaniciRolleri.Normalize(rol);
        var liste = new List<ModulYetkiKaydi>();
        foreach (var satir in _satirlar)
        {
            if (satir.Okuma.IsChecked != true && satir.Yazma.IsChecked != true)
                continue;

            var sekmeler = satir.SekmeKutulari
                .Where(k => k.IsChecked == true)
                .Select(k => k.Content?.ToString() ?? "")
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            var yazma = satir.Yazma.IsChecked == true && YazmaAtanabilirMi(n, satir.ModulAdi);
            liste.Add(new ModulYetkiKaydi
            {
                Modul = satir.ModulAdi,
                Okuma = satir.Okuma.IsChecked == true,
                Yazma = yazma,
                Sekmeler = sekmeler.Count == satir.SekmeKutulari.Count ? [] : sekmeler
            });
        }

        return (liste.Where(y => y.Okuma).Select(y => y.Modul).ToList(), liste);
    }

    private void Olustur()
    {
        _panel.Children.Clear();
        _satirlar.Clear();

        foreach (var modulAdi in ModulKatalogu.Tum)
        {
            var ust = new Grid { Margin = new Thickness(0, 0, 0, 2) };
            ust.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            ust.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            ust.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var baslik = new TextBlock
            {
                Text = modulAdi,
                FontWeight = FontWeights.SemiBold,
                FontSize = 12.5,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(baslik, 0);

            var okuma = new CheckBox { Content = "Okuma", Margin = new Thickness(12, 0, 10, 0), VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(okuma, 1);
            var yazma = new CheckBox { Content = "Yazma", VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(yazma, 2);

            okuma.Checked += (_, _) =>
            {
                if (_dolduruluyor) return;
                if (yazma.IsChecked == true && okuma.IsChecked != true)
                    okuma.IsChecked = true;
            };
            yazma.Checked += (_, _) =>
            {
                if (_dolduruluyor) return;
                if (yazma.IsChecked == true)
                    okuma.IsChecked = true;
            };
            okuma.Unchecked += (_, _) =>
            {
                if (_dolduruluyor) return;
                if (okuma.IsChecked != true)
                    yazma.IsChecked = false;
            };

            ust.Children.Add(baslik);
            ust.Children.Add(okuma);
            ust.Children.Add(yazma);

            var kolon = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };
            kolon.Children.Add(ust);

            var sekmeKutulari = new List<CheckBox>();
            var sekmeler = ModulKatalogu.SekmeleriAl(modulAdi);
            if (sekmeler.Count > 0)
            {
                var sekmePanel = new WrapPanel { Margin = new Thickness(0, 4, 0, 0) };
                foreach (var sekme in sekmeler)
                {
                    var kutu = new CheckBox
                    {
                        Content = sekme,
                        Margin = new Thickness(0, 0, 12, 4),
                        FontSize = 11,
                        Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139))
                    };
                    sekmeKutulari.Add(kutu);
                    sekmePanel.Children.Add(kutu);
                }
                kolon.Children.Add(sekmePanel);
            }

            _satirlar.Add(new Satir
            {
                ModulAdi = modulAdi,
                Okuma = okuma,
                Yazma = yazma,
                SekmeKutulari = sekmeKutulari
            });

            _panel.Children.Add(new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(0, 0, 0, 6),
                Child = kolon
            });
        }
    }

    private void YetkileriRoldenYukle(string rol, IEnumerable<string> moduller)
    {
        _dolduruluyor = true;
        try
        {
            var set = moduller.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var yazmaAtanabilir = RolYazmaAtanabilir(rol);

            foreach (var satir in _satirlar)
            {
                var okuma = set.Contains(satir.ModulAdi);
                satir.Okuma.IsChecked = okuma;

                if (satir.ModulAdi.Equals("Satınalma", StringComparison.OrdinalIgnoreCase))
                {
                    var r = KullaniciRolleri.Normalize(rol);
                    satir.Yazma.IsChecked = okuma && (yazmaAtanabilir ||
                        r is KullaniciRolleri.Yonetim or KullaniciRolleri.Sef or KullaniciRolleri.Saha);
                }
                else
                    satir.Yazma.IsChecked = okuma && yazmaAtanabilir;

                YazmaKutusunuSinirla(satir.Yazma, satir.ModulAdi, rol);

                foreach (var sekme in satir.SekmeKutulari)
                {
                    var ad = sekme.Content?.ToString() ?? "";
                    if (satir.ModulAdi.Equals("Satınalma", StringComparison.OrdinalIgnoreCase))
                    {
                        sekme.IsChecked = okuma && KullaniciRolleri
                            .VarsayilanSatinalmaSekmeler(rol)
                            .Contains(ad, StringComparer.OrdinalIgnoreCase);
                    }
                    else
                        sekme.IsChecked = okuma;
                }
            }
        }
        finally
        {
            _dolduruluyor = false;
        }
    }

    private void YetkileriYukle(IEnumerable<ModulYetkiKaydi> yetkiler, string? rol)
    {
        _dolduruluyor = true;
        try
        {
            var sozluk = yetkiler.ToDictionary(y => y.Modul, StringComparer.OrdinalIgnoreCase);
            foreach (var satir in _satirlar)
            {
                if (!sozluk.TryGetValue(satir.ModulAdi, out var yetki))
                {
                    satir.Okuma.IsChecked = false;
                    satir.Yazma.IsChecked = false;
                    foreach (var sekme in satir.SekmeKutulari)
                        sekme.IsChecked = false;
                    continue;
                }

                satir.Okuma.IsChecked = yetki.Okuma;
                satir.Yazma.IsChecked = yetki.Yazma && YazmaAtanabilirMi(rol, satir.ModulAdi);
                YazmaKutusunuSinirla(satir.Yazma, satir.ModulAdi, rol);
                foreach (var sekmeKutu in satir.SekmeKutulari)
                {
                    var ad = sekmeKutu.Content?.ToString() ?? "";
                    sekmeKutu.IsChecked = yetki.Sekmeler.Count == 0 ||
                                          yetki.Sekmeler.Contains(ad, StringComparer.OrdinalIgnoreCase);
                }
            }
        }
        finally
        {
            _dolduruluyor = false;
        }
    }

    private static bool RolYazmaAtanabilir(string? rol)
    {
        var r = KullaniciRolleri.Normalize(rol);
        return KullaniciRolleri.AdminMi(rol) || r == KullaniciRolleri.Satinalma;
    }

    private static bool YazmaAtanabilirMi(string? rol, string modulAdi)
    {
        if (modulAdi.Equals("Satınalma", StringComparison.OrdinalIgnoreCase))
        {
            var r = KullaniciRolleri.Normalize(rol);
            return RolYazmaAtanabilir(rol)
                   || r is KullaniciRolleri.Yonetim or KullaniciRolleri.Sef or KullaniciRolleri.Saha;
        }

        return RolYazmaAtanabilir(rol);
    }

    private static void YazmaKutusunuSinirla(CheckBox yazma, string modulAdi, string? rol)
    {
        yazma.IsEnabled = YazmaAtanabilirMi(rol, modulAdi);
        if (!yazma.IsEnabled)
            yazma.IsChecked = false;
    }
}

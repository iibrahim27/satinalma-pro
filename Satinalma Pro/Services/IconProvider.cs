using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SatinalmaPro.Services;

public enum DashboardIconKind
{
    Home,
    Package,
    Warehouse,
    Layers,
    Building2,
    Fuel,
    Truck,
    Wallet,
    ShoppingCart,
    FileBarChart,
    Settings,
    Bell,
    Moon,
    Sun,
    ChevronDown,
    ArrowRight,
    TrendingUp,
    ClipboardList,
    AlertTriangle
}

/// <summary>Lucide tarzı vektör ikonlar — tek kaynak.</summary>
public static class IconProvider
{
    private static readonly Dictionary<DashboardIconKind, string> Paths = new()
    {
        [DashboardIconKind.Home] = "M3 9.5 12 3l9 6.5V20a1 1 0 0 1-1 1h-5v-6H9v6H4a1 1 0 0 1-1-1Z",
        [DashboardIconKind.Package] = "M16.5 9.4 7.55 4.24M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16Z M3.3 7.5 12 12.7l8.7-5.2M12 22V12.7",
        [DashboardIconKind.Warehouse] = "M22 8.35V20a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V8.35A2 2 0 0 1 3.26 6.5l8-3.2a2 2 0 0 1 1.48 0l8 3.2A2 2 0 0 1 22 8.35Z M6 18h.01M10 18h.01M14 18h.01M18 18h.01M6 14h.01M10 14h.01M14 14h.01M18 14h.01",
        [DashboardIconKind.Layers] = "M12 2 2 7l10 5 10-5-10-5Z M2 17l10 5 10-5M2 12l10 5 10-5",
        [DashboardIconKind.Building2] = "M6 22V4a2 2 0 0 1 2-2h8a2 2 0 0 1 2 2v18Z M6 12H4a2 2 0 0 0-2 2v6a2 2 0 0 0 2 2h2M18 9h2a2 2 0 0 1 2 2v9a2 2 0 0 1-2 2h-2M10 6h4M10 10h4M10 14h4M10 18h4",
        [DashboardIconKind.Fuel] = "M3 22h12M5 22V7a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v15M19 22V11l-3-3M19 8h3",
        [DashboardIconKind.Truck] = "M10 17h4M3 17h2M17 17h2M5 17a2 2 0 1 0 4 0 2 2 0 0 0-4 0M15 17a2 2 0 1 0 4 0 2 2 0 0 0-4 0M3 11V6a2 2 0 0 1 2-2h9v7H3Z M14 11h4l3 3v3h-7",
        [DashboardIconKind.Wallet] = "M19 7V6a2 2 0 0 0-2-2H7a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2v-1M17 11h.01M3 10h14v4H3Z",
        [DashboardIconKind.ShoppingCart] = "M6 2 3 6v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V6l-3-4Z M3 6h18M16 10a4 4 0 0 1-8 0",
        [DashboardIconKind.FileBarChart] = "M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8Z M14 2v6h6M8 13h.01M12 13h.01M16 13h.01M8 17h.01M12 17h4",
        [DashboardIconKind.Settings] = "M12.22 2h-.44a2 2 0 0 0-2 1.18l-.2 1.18a2 2 0 0 1-1.05.86l-1.12.48a2 2 0 0 1-1.08-.12l-1.05-.72a2 2 0 0 0-2.27.05l-.31.31a2 2 0 0 0-.05 2.27l.72 1.05a2 2 0 0 1 .12 1.08l-.48 1.12a2 2 0 0 1-.86 1.05l-1.18.2A2 2 0 0 0 2 12.22v.44a2 2 0 0 0 1.18 2l1.18.2a2 2 0 0 1 1.05.86l.48 1.12a2 2 0 0 1-.12 1.08l-.72 1.05a2 2 0 0 0 .05 2.27l.31.31a2 2 0 0 0 2.27.05l1.05-.72a2 2 0 0 1 1.08-.12l1.12.48a2 2 0 0 1 1.05.86l.2 1.18A2 2 0 0 0 11.78 22h.44a2 2 0 0 0 2-1.18l.2-1.18a2 2 0 0 1 1.05-.86l1.12-.48a2 2 0 0 1 1.08.12l1.05.72a2 2 0 0 0 2.27-.05l.31-.31a2 2 0 0 0 .05-2.27l-.72-1.05a2 2 0 0 1-.12-1.08l.48-1.12a2 2 0 0 1 .86-1.05l1.18-.2A2 2 0 0 0 22 12.78v-.44a2 2 0 0 0-1.18-2l-1.18-.2a2 2 0 0 1-1.05-.86l-.48-1.12a2 2 0 0 1 .12-1.08l.72-1.05a2 2 0 0 0-.05-2.27l-.31-.31a2 2 0 0 0-2.27-.05l-1.05.72a2 2 0 0 1-1.08.12l-1.12-.48a2 2 0 0 1-.86-1.05l-.2-1.18A2 2 0 0 0 12.22 2Z M12 15a3 3 0 1 0 0-6 3 3 0 0 0 0 6Z",
        [DashboardIconKind.Bell] = "M6 8a6 6 0 0 1 12 0c0 7 3 9 3 9H3s3-2 3-9M10.3 21a1.94 1.94 0 0 0 3.4 0",
        [DashboardIconKind.Moon] = "M12 3a6 6 0 0 0 9 9 9 9 0 1 1-9-9Z",
        [DashboardIconKind.Sun] = "M12 2v2M12 20v2M4.93 4.93l1.41 1.41M17.66 17.66l1.41 1.41M2 12h2M20 12h2M4.93 19.07l1.41-1.41M17.66 6.34l1.41-1.41M17 12a5 5 0 1 1-10 0 5 5 0 0 1 10 0Z",
        [DashboardIconKind.ChevronDown] = "m6 9 6 6 6-6",
        [DashboardIconKind.ArrowRight] = "M5 12h14M12 5l7 7-7 7",
        [DashboardIconKind.TrendingUp] = "m22 7-8.5 8.5-5-5L2 17M16 7h6v6",
        [DashboardIconKind.ClipboardList] = "M16 4h2a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h2M9 2h6v4H9zM9 12h6M9 16h6",
        [DashboardIconKind.AlertTriangle] = "M10.29 3.86 1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0Z M12 9v4M12 17h.01"
    };

    public static Geometry GetGeometry(DashboardIconKind kind)
    {
        if (!Paths.TryGetValue(kind, out var data))
            return Geometry.Empty;

        var g = Geometry.Parse(data);
        g.Freeze();
        return g;
    }

    public static Path Olustur(DashboardIconKind kind, Brush? stroke = null, double size = 18, double thickness = 1.75)
    {
        var path = new Path
        {
            Data = GetGeometry(kind),
            Stretch = Stretch.Uniform,
            Width = size,
            Height = size,
            Stroke = stroke ?? Brushes.Black,
            StrokeThickness = thickness,
            StrokeLineJoin = PenLineJoin.Round,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            Fill = Brushes.Transparent,
            SnapsToDevicePixels = true
        };
        return path;
    }

    public static DashboardIconKind ModulIkonu(string modulBaslik) => modulBaslik switch
    {
        "Alınan Malzemeler" => DashboardIconKind.Package,
        "Stok Yönetimi" => DashboardIconKind.Warehouse,
        "Agrega" => DashboardIconKind.Layers,
        "Çimento" => DashboardIconKind.Building2,
        "Akaryakıt Takip" => DashboardIconKind.Fuel,
        "Araç Filo Takip" => DashboardIconKind.Truck,
        "Finansman Raporlama" => DashboardIconKind.Wallet,
        "Satınalma" => DashboardIconKind.ShoppingCart,
        "Raporlamalar" => DashboardIconKind.FileBarChart,
        "Ayarlar" => DashboardIconKind.Settings,
        _ => DashboardIconKind.Package
    };

    public static string ModulKisaAd(string modulBaslik) => modulBaslik switch
    {
        "Finansman Raporlama" => "Finansman",
        "Raporlamalar" => "Raporlar",
        _ => modulBaslik
    };
}

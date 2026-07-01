using System.Windows.Media;

namespace SatinalmaPro.Models;

public sealed class ModuleInfo
{
    public required string Number { get; init; }
    public required string Title { get; init; }
    public required string Subtitle { get; init; }
    public required string IconGlyph { get; init; }
    public required Color GradientStart { get; init; }
    public required Color GradientEnd { get; init; }
}

public static class ModuleCatalog
{
    public static IReadOnlyList<ModuleInfo> All { get; } =
    [
        new ModuleInfo
        {
            Number = "01",
            Title = "Alınan Malzemeler",
            Subtitle = "Tedarik edilen malzeme kayıtları",
            IconGlyph = "\uE7BF",
            GradientStart = Color.FromRgb(99, 102, 241),
            GradientEnd = Color.FromRgb(129, 140, 248)
        },
        new ModuleInfo
        {
            Number = "02",
            Title = "Stok Yönetimi",
            Subtitle = "Depo stok takibi ve kritik seviye uyarıları",
            IconGlyph = "\uE7BF",
            GradientStart = Color.FromRgb(20, 184, 166),
            GradientEnd = Color.FromRgb(45, 212, 191)
        },
        new ModuleInfo
        {
            Number = "03",
            Title = "Agrega",
            Subtitle = "Agrega stok ve hareketleri",
            IconGlyph = "\uE7C1",
            GradientStart = Color.FromRgb(16, 185, 129),
            GradientEnd = Color.FromRgb(52, 211, 153)
        },
        new ModuleInfo
        {
            Number = "04",
            Title = "Çimento",
            Subtitle = "Çimento giriş ve kullanım takibi",
            IconGlyph = "\uE7F4",
            GradientStart = Color.FromRgb(100, 116, 139),
            GradientEnd = Color.FromRgb(148, 163, 184)
        },
        new ModuleInfo
        {
            Number = "05",
            Title = "Akaryakıt Takip",
            Subtitle = "Yakıt alımı ve tüketim izleme",
            IconGlyph = "\uE804",
            GradientStart = Color.FromRgb(245, 158, 11),
            GradientEnd = Color.FromRgb(251, 191, 36)
        },
        new ModuleInfo
        {
            Number = "06",
            Title = "Araç Filo Takip",
            Subtitle = "Filo, plaka ve araç operasyonları",
            IconGlyph = "\uE804",
            GradientStart = Color.FromRgb(59, 130, 246),
            GradientEnd = Color.FromRgb(96, 165, 250)
        },
        new ModuleInfo
        {
            Number = "07",
            Title = "Finansman Raporlama",
            Subtitle = "Gelir, nakit akışı ve finansal özet",
            IconGlyph = "\uE9F9",
            GradientStart = Color.FromRgb(139, 92, 246),
            GradientEnd = Color.FromRgb(192, 132, 252)
        },
        new ModuleInfo
        {
            Number = "08",
            Title = "Satınalma",
            Subtitle = "Tedarik yönetim merkezi — talep, teklif, onay, sipariş",
            IconGlyph = "\uE719",
            GradientStart = Color.FromRgb(249, 115, 22),
            GradientEnd = Color.FromRgb(251, 146, 60)
        },
        new ModuleInfo
        {
            Number = "09",
            Title = "Raporlamalar",
            Subtitle = "Tüm modül raporları ve özetler",
            IconGlyph = "\uE9D9",
            GradientStart = Color.FromRgb(6, 182, 212),
            GradientEnd = Color.FromRgb(34, 211, 238)
        },
        new ModuleInfo
        {
            Number = "10",
            Title = "Ayarlar",
            Subtitle = "Modül ayarları, yedekleme ve veri yönetimi",
            IconGlyph = "\uE713",
            GradientStart = Color.FromRgb(71, 85, 105),
            GradientEnd = Color.FromRgb(148, 163, 184)
        }
    ];

    public static ModuleInfo? Bul(string baslik) =>
        All.FirstOrDefault(m => m.Title.Equals(baslik, StringComparison.OrdinalIgnoreCase));
}

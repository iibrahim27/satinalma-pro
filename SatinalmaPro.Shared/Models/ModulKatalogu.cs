namespace SatinalmaPro.Shared.Models;

/// <summary>Pro masaüstü modül adları ve sekme kataloğu — Yönetici yetki formu için.</summary>
public static class ModulKatalogu
{
    public static IReadOnlyList<string> Tum { get; } =
    [
        "Alınan Malzemeler",
        "Stok Yönetimi",
        "Agrega",
        "Çimento",
        "Akaryakıt Takip",
        "Araç Filo Takip",
        "Finansman Raporlama",
        "Satınalma",
        "Raporlamalar",
        "Ayarlar"
    ];

    private static readonly Dictionary<string, string[]> Sekmeler = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Stok Yönetimi"] = ["Stok Durumu", "Stok Hareketleri", "Stok Girişi", "Stok Çıkışı", "Stok Sayım"],
        ["Ayarlar"] = ["Genel", "Satınalma", "Malzeme Kategorileri", "Birim Terimleri", "Araç Filo", "Veri Dosyaları", "Yedekleme"],
        ["Raporlamalar"] = ["Modül Özeti", "Grup Özeti", "Detay"],
        ["Finansman Raporlama"] = ["Modül", "Nakit Akışı", "Vadeler", "Grup", "Hareketler"],
        ["Satınalma"] =
        [
            "Taleplerim",
            "Gelen Talepler",
            "Onay Bekleyen",
            "Teklif Bekleyen",
            "Teklif Girişi",
            "Karşılaştırma",
            "Teklif Onay",
            "Onaylanan Talepler",
            "Geçmiş Talepler",
            "Geçmiş Teklifli Onaylar",
            "Alınan Malzemeler",
            "Red Talepler",
        ]
    };

    public static IReadOnlyList<string> SekmeleriAl(string modulAdi) =>
        Sekmeler.TryGetValue(modulAdi, out var liste) ? liste : [];
}

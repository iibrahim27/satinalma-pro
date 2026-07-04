namespace SatinalmaPro.Models;

public static class ModulSekmeKatalogu
{
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

    public static bool SekmeliModulMu(string modulAdi) => Sekmeler.ContainsKey(modulAdi);
}

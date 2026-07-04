using SatinalmaPro.Services;

namespace SatinalmaPro.Views.Modules;

public sealed class SatinalmaNavOge
{
    public required string Id { get; init; }
    public required string Baslik { get; init; }
    public required string Aciklama { get; init; }
    public required DashboardIconKind Icon { get; init; }
    public string? SekmeAdi { get; init; }
}

public sealed class SatinalmaNavGrubu
{
    public required string Baslik { get; init; }
    public required IReadOnlyList<SatinalmaNavOge> Ogeler { get; init; }
}

/// <summary>Satınalma shell sol menü yapısı — rol filtresi dışarıda uygulanır.</summary>
public static class SatinalmaNavYapisi
{
    public const string GenelBakisId = "__genel__";

    public static IReadOnlyList<SatinalmaNavGrubu> TumGruplar { get; } =
    [
        new SatinalmaNavGrubu
        {
            Baslik = "Genel",
            Ogeler =
            [
                new SatinalmaNavOge
                {
                    Id = GenelBakisId,
                    Baslik = "Genel Bakış",
                    Aciklama = "Özet ve hızlı erişim",
                    Icon = DashboardIconKind.Home
                }
            ]
        },
        new SatinalmaNavGrubu
        {
            Baslik = "Talepler",
            Ogeler =
            [
                Nav("Taleplerim", "Taleplerim", "Oluşturduğunuz talepler", DashboardIconKind.ClipboardList),
                Nav("Gelen Talepler", "Gelen Talepler", "Yönetim onayı bekleyen", DashboardIconKind.Bell),
                Nav("Onay Bekleyen", "Onay Bekleyen", "İşlemdeki talepler", DashboardIconKind.AlertTriangle),
                Nav("Red Talepler", "Red Talepler", "Reddedilen talepler", DashboardIconKind.Package)
            ]
        },
        new SatinalmaNavGrubu
        {
            Baslik = "Teklif",
            Ogeler =
            [
                Nav("Teklif Bekleyen", "Teklif Bekleyen", "Teklif istenen talepler", DashboardIconKind.FileBarChart),
                Nav("Teklif Girişi", "Teklif Girişi", "Tedarikçi teklifleri", DashboardIconKind.ShoppingCart),
                Nav("Firma/Fiyat Girişi", "Firma/Fiyat Girişi", "Teklifsiz onay sonrası fiyat", DashboardIconKind.ShoppingCart),
                Nav("Karşılaştırma", "Karşılaştırma", "Teklif karşılaştırma", DashboardIconKind.Layers)
            ]
        },
        new SatinalmaNavGrubu
        {
            Baslik = "Onay",
            Ogeler =
            [
                Nav("Teklif Onay", "Teklif Onay", "Yönetim teklif onayı", DashboardIconKind.Wallet),
                Nav("Onaylanan Teklifler", "Onaylanan Teklifler", "Onaylanmış teklifler", DashboardIconKind.FileBarChart),
                Nav("Onay Geçmişi", "Onay Geçmişi", "Tüm onay geçmişi", DashboardIconKind.FileBarChart),
                Nav("Onaylanan Talepler", "Onaylanan Talepler", "Onaylanmış talepler", DashboardIconKind.Package),
                Nav("Geçmiş Talepler", "Geçmiş Talepler", "Tamamlanan teklifsiz", DashboardIconKind.FileBarChart),
                Nav("Geçmiş Teklifli Onaylar", "Geçmiş Teklifli", "Teklifli geçmiş", DashboardIconKind.FileBarChart)
            ]
        },
        new SatinalmaNavGrubu
        {
            Baslik = "Sipariş & Depo",
            Ogeler =
            [
                Nav("Alınan Malzemeler", "Alınan Malzemeler", "Sipariş ve mal kabul", DashboardIconKind.Truck),
                Nav("Gelen Siparişler", "Gelen Siparişler", "Depoya giren malzeme", DashboardIconKind.Warehouse)
            ]
        }
    ];

    private static SatinalmaNavOge Nav(string sekme, string baslik, string aciklama, DashboardIconKind icon) =>
        new()
        {
            Id = sekme,
            SekmeAdi = sekme,
            Baslik = baslik,
            Aciklama = aciklama,
            Icon = icon
        };

    public static SatinalmaNavOge? Bul(string id)
    {
        foreach (var grup in TumGruplar)
        {
            var oge = grup.Ogeler.FirstOrDefault(o => o.Id == id);
            if (oge is not null)
                return oge;
        }

        return null;
    }
}

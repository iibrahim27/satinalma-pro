namespace SatinalmaPro.Shared.Helpers;

/// <summary>Talep durumuna göre liste kartı renk grupları.</summary>
public static class TalepDurumRenkleri
{
    public const string GrupBekliyor = "bekliyor";
    public const string GrupOnaylandi = "onaylandi";
    public const string GrupSiparis = "siparis";
    public const string GrupRed = "red";

    public static string Grup(string? gorunenDurum) => gorunenDurum switch
    {
        SatinalmaTalepDurumEtiketi.RedEdildi => GrupRed,
        SatinalmaTalepDurumEtiketi.Onaylandi or SatinalmaTalepDurumEtiketi.TeklifOnaylandi => GrupOnaylandi,
        SatinalmaTalepDurumEtiketi.Sipariste => GrupSiparis,
        SatinalmaTalepDurumEtiketi.DepoTeslimOldu => GrupOnaylandi,
        SatinalmaTalepDurumEtiketi.TeklifBekleniyor => GrupBekliyor,
        _ => GrupBekliyor
    };

    public static (string arka, string kenar, string rozetArka, string rozetYazi) Renkler(string? gorunenDurum) =>
        Renkler(gorunenDurum, acikTema: true);

    public static (string arka, string kenar, string rozetArka, string rozetYazi) Renkler(string? gorunenDurum, bool acikTema) =>
        acikTema ? AcikRenkler(gorunenDurum) : KoyuRenkler(gorunenDurum);

    private static (string arka, string kenar, string rozetArka, string rozetYazi) AcikRenkler(string? gorunenDurum) =>
        Grup(gorunenDurum) switch
        {
            GrupRed => ("#FEF2F2", "#FCA5A5", "#FEE2E2", "#B91C1C"),
            GrupOnaylandi => ("#F0FDF4", "#86EFAC", "#DCFCE7", "#15803D"),
            GrupSiparis => ("#EFF6FF", "#93C5FD", "#DBEAFE", "#1D4ED8"),
            _ => ("#FFFBEB", "#FCD34D", "#FEF3C7", "#B45309")
        };

    private static (string arka, string kenar, string rozetArka, string rozetYazi) KoyuRenkler(string? gorunenDurum) =>
        Grup(gorunenDurum) switch
        {
            GrupRed => ("#7F1D1D", "#EF4444", "#991B1B", "#FECACA"),
            GrupOnaylandi => ("#14532D", "#22C55E", "#166534", "#BBF7D0"),
            GrupSiparis => ("#1E3A8A", "#3B82F6", "#1D4ED8", "#BFDBFE"),
            _ => ("#78350F", "#F59E0B", "#92400E", "#FDE68A")
        };
}

using System.Globalization;

namespace SatinalmaPro.Views.Modules.Satinalma.Part1;

public class OnaylananKalemDetaySatiri
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    public string Malzeme { get; init; } = "";
    public string MiktarMetni { get; init; } = "";
    public string Firma { get; init; } = "";
    public decimal BirimFiyat { get; init; }
    public decimal Toplam { get; init; }

    public string BirimFiyatMetni => BirimFiyat > 0
        ? BirimFiyat.ToString("N2", Tr) + " ₺"
        : "—";

    public string ToplamMetni => Toplam > 0
        ? Toplam.ToString("N2", Tr) + " ₺"
        : "—";
}

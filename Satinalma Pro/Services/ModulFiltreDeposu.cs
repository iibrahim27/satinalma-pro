using System.Text.Json;

namespace SatinalmaPro.Services;

/// <summary>Oturum boyunca modül filtre durumlarını saklar — modüller arası geçişte sıfırlanmaz.</summary>
public static class ModulFiltreDeposu
{
    private static readonly Dictionary<string, string> Depo = new(StringComparer.Ordinal);

    private static readonly JsonSerializerOptions JsonSecenekleri = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static void Kaydet<T>(string modulAdi, T durum) where T : class =>
        Depo[modulAdi] = JsonSerializer.Serialize(durum, JsonSecenekleri);

    public static T? Oku<T>(string modulAdi) where T : class
    {
        if (!Depo.TryGetValue(modulAdi, out var json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonSecenekleri);
        }
        catch
        {
            Depo.Remove(modulAdi);
            return null;
        }
    }

    public static void Sil(string modulAdi) => Depo.Remove(modulAdi);
}

public sealed class ErpModulFiltreDurumu
{
    public DateTime? Baslangic { get; set; }
    public DateTime? Bitis { get; set; }
    public string Tedarikci { get; set; } = "Tümü";
    public List<string> CinsSecimleri { get; set; } = [];
    public string Tur { get; set; } = "Tümü";
    public string Santiye { get; set; } = "Tümü";
    public string TeslimAlan { get; set; } = "";
    public string IrsaliyeNo { get; set; } = "";
    public string GridArama { get; set; } = "";
    public bool FiltrePanelAcik { get; set; }
    public double FiltrePanelYukseklik { get; set; }
}

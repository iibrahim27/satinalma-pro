using System.Text.Json;
using System.Text.Json.Serialization;
using SatinalmaPro.Models.SatinalmaMerkezi;
using SatinalmaPro.Shared;

namespace SatinalmaPro.Services;

public static class IadeDeposu
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static List<IadeKayit> Kayitlar { get; } = [];

    public static async Task YukleAsync(CancellationToken iptal = default)
    {
        Kayitlar.Clear();
        if (!OturumYoneticisi.GirisYapildi || OturumYoneticisi.Firestore is null)
            return;

        var json = await OturumYoneticisi.Firestore.BelgeJsonOkuAsync(FirestoreYollari.IadeKayitlari, iptal);
        if (string.IsNullOrWhiteSpace(json))
            return;

        try
        {
            var liste = JsonSerializer.Deserialize<List<IadeKayit>>(json, Json) ?? [];
            Kayitlar.AddRange(liste);
        }
        catch
        {
            // bozuk json — sessizce atla
        }
    }

    public static IReadOnlyList<IadeSatirModel> MerkeziSatirlar()
    {
        if (Kayitlar.Count == 0)
            return [];

        return Kayitlar.Select(k => new IadeSatirModel
        {
            IadeNo = k.IadeNo,
            SiparisNo = k.SiparisNo,
            Firma = k.Firma,
            Malzeme = k.Malzeme,
            Miktar = k.Miktar,
            Neden = k.Neden,
            Durum = k.Durum,
            Tarih = k.Tarih,
            DurumRenk = k.Durum.Contains("Onay", StringComparison.OrdinalIgnoreCase)
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(5, 150, 105))
                : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 158, 11))
        }).ToList();
    }
}

public sealed class IadeKayit
{
    public string IadeNo { get; set; } = "";
    public string SiparisNo { get; set; } = "";
    public string Firma { get; set; } = "";
    public string Malzeme { get; set; } = "";
    public string Miktar { get; set; } = "";
    public string Neden { get; set; } = "";
    public string Durum { get; set; } = "";
    public string Tarih { get; set; } = "";
}

using System.Text.Json;
using SatinalmaPro.Models;

namespace SatinalmaPro.Services;

public static class EpostaSablonDeposu
{
    private const string BelgeYolu = "veri/eposta_sablonlari";

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static EpostaSablonAyarlari Ayarlar { get; private set; } = new();

    public static async Task YukleAsync(CancellationToken iptal = default)
    {
        if (OturumYoneticisi.Firestore is null)
            return;

        try
        {
            var json = await OturumYoneticisi.Firestore.BelgeJsonOkuAsync(BelgeYolu, iptal);
            if (string.IsNullOrWhiteSpace(json))
                return;

            Ayarlar = JsonSerializer.Deserialize<EpostaSablonAyarlari>(json, Json) ?? new EpostaSablonAyarlari();
        }
        catch
        {
            // varsayılan şablon
        }
    }

    public static async Task KaydetAsync(CancellationToken iptal = default)
    {
        if (OturumYoneticisi.Firestore is null)
            throw new InvalidOperationException("Bulut bağlantısı yok.");

        var json = JsonSerializer.Serialize(Ayarlar, Json);
        await OturumYoneticisi.Firestore.BelgeJsonYazAsync(
            BelgeYolu, json, OturumYoneticisi.Auth?.Uid, iptal);
    }

    public static string SablonuDoldur(string sablon, KullaniciProfili kullanici)
    {
        return sablon
            .Replace("{adSoyad}", string.IsNullOrWhiteSpace(kullanici.AdSoyad) ? kullanici.Eposta : kullanici.AdSoyad)
            .Replace("{eposta}", kullanici.Eposta)
            .Replace("{firmaAdi}", string.IsNullOrWhiteSpace(UygulamaAyarDeposu.Ayarlar.FirmaAdi)
                ? "Satınalma Pro"
                : UygulamaAyarDeposu.Ayarlar.FirmaAdi);
    }
}

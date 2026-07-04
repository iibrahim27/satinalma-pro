using System.Text.Json;
using System.Text.Json.Nodes;
using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Shared.Helpers;

/// <summary>Bulut senkronunda satınalma ayarları ve imza listelerini birleştirir.</summary>
public static class SatinalmaAyarlarSenkronYardimcisi
{
    public static void ImzaListeleriniBirlestir(IList<ImzaAyari> hedef, IList<ImzaAyari>? kaynak)
    {
        if (kaynak is null || kaynak.Count == 0)
            return;

        foreach (var kaynakImza in kaynak)
        {
            if (string.IsNullOrWhiteSpace(kaynakImza.Unvan))
                continue;

            var mevcut = hedef.FirstOrDefault(h =>
                string.Equals(h.Unvan?.Trim(), kaynakImza.Unvan.Trim(), StringComparison.OrdinalIgnoreCase));

            if (mevcut is not null)
            {
                if (string.IsNullOrWhiteSpace(mevcut.AdSoyad) && !string.IsNullOrWhiteSpace(kaynakImza.AdSoyad))
                    mevcut.AdSoyad = kaynakImza.AdSoyad.Trim();

                mevcut.Unvan = kaynakImza.Unvan.Trim();
                mevcut.Aktif = mevcut.Aktif || kaynakImza.Aktif;
            }
            else
            {
                hedef.Add(new ImzaAyari
                {
                    Unvan = kaynakImza.Unvan.Trim(),
                    AdSoyad = kaynakImza.AdSoyad?.Trim() ?? "",
                    Aktif = kaynakImza.Aktif
                });
            }
        }
    }

    public static void MetinAlanlariniBirlestir(SatinalmaAyarlar hedef, SatinalmaAyarlar kaynak)
    {
        if (string.IsNullOrWhiteSpace(hedef.SartnameMetni) && !string.IsNullOrWhiteSpace(kaynak.SartnameMetni))
            hedef.SartnameMetni = kaynak.SartnameMetni;

        if (string.IsNullOrWhiteSpace(hedef.TeklifIstemeSartnameleri) &&
            !string.IsNullOrWhiteSpace(kaynak.TeklifIstemeSartnameleri))
            hedef.TeklifIstemeSartnameleri = kaynak.TeklifIstemeSartnameleri;
    }

    private static readonly string[] MobilGuncellenebilirAlanlar =
    [
        "sonTalepSira",
        "sonSiparisSira",
        "silinenTalepIdleri",
        "firmaAdi",
        "varsayilanUsdKuru",
        "varsayilanEurKuru"
    ];

    /// <summary>Mobil yalnızca sayaç alanlarını günceller; imza ve şartname korunur.</summary>
    public static string MobilGuncellemesiniUygula(
        string? mevcutBulutJson,
        SatinalmaAyarlar mobil,
        JsonSerializerOptions secenekler)
    {
        var guncelleme = JsonSerializer.SerializeToNode(mobil, secenekler) as JsonObject;
        if (guncelleme is null)
            return "{}";

        JsonObject kok;
        if (string.IsNullOrWhiteSpace(mevcutBulutJson))
            kok = new JsonObject();
        else
            kok = JsonNode.Parse(mevcutBulutJson) as JsonObject ?? new JsonObject();

        foreach (var alan in MobilGuncellenebilirAlanlar)
        {
            if (!guncelleme.TryGetPropertyValue(alan, out var deger) || deger is null)
                continue;

            if (alan is "firmaAdi" && string.IsNullOrWhiteSpace(deger.GetValue<string>()))
                continue;

            kok[alan] = deger.DeepClone();
        }

        return kok.ToJsonString(secenekler);
    }
}

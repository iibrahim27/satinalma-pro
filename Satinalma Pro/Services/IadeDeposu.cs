using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models.SatinalmaMerkezi;
using SatinalmaPro.Shared;

namespace SatinalmaPro.Services;

public static class IadeDeposu
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    public static List<IadeKayit> Kayitlar { get; } = [];

    public static async Task YukleAsync(CancellationToken iptal = default)
    {
        Kayitlar.Clear();

        if (OturumYoneticisi.GirisYapildi && OturumYoneticisi.Firestore is not null)
        {
            var json = await OturumYoneticisi.Firestore
                .BelgeJsonOkuAsync(FirestoreYollari.IadeKayitlari, iptal);
            if (!string.IsNullOrWhiteSpace(json))
            {
                KayitlariUygula(json);
                await YerelKaydetAsync(iptal);
                return;
            }
        }

        YerelYukle();
    }

    public static void YerelYukle()
    {
        var yol = YerelDosyaYolu();
        if (!File.Exists(yol))
            return;

        try
        {
            var json = File.ReadAllText(yol);
            KayitlariUygula(json);
        }
        catch
        {
            // bozuk json — sessizce atla
        }
    }

    public static async Task KaydetAsync(CancellationToken iptal = default)
    {
        await YerelKaydetAsync(iptal);

        if (OturumYoneticisi.Firestore is null)
            return;

        var json = JsonSerializer.Serialize(Kayitlar, Json);
        await OturumYoneticisi.Firestore.BelgeJsonYazAsync(
            FirestoreYollari.IadeKayitlari,
            json,
            OturumYoneticisi.Auth?.Uid,
            iptal);
    }

    public static void Ekle(IadeKayit kayit)
    {
        if (Kayitlar.Any(k => k.IadeNo.Equals(kayit.IadeNo, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Bu iade numarası zaten kayıtlı: {kayit.IadeNo}");

        Kayitlar.Insert(0, kayit);
    }

    public static string YeniIadeNoOlustur()
    {
        var yil = DateTime.Now.Year;
        var prefix = $"IAD-{yil}-";
        var max = Kayitlar
            .Where(k => k.IadeNo.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Select(k =>
            {
                var parca = k.IadeNo.Length > prefix.Length ? k.IadeNo[prefix.Length..] : "";
                return int.TryParse(parca, out var n) ? n : 0;
            })
            .DefaultIfEmpty(0)
            .Max();

        SatinalmaDepo.Ayarlar.SonIadeSira = Math.Max(SatinalmaDepo.Ayarlar.SonIadeSira, max);
        SatinalmaDepo.Ayarlar.SonIadeSira++;
        SatinalmaDepo.Kaydet();
        return $"{prefix}{SatinalmaDepo.Ayarlar.SonIadeSira:D3}";
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
                ? FircaOnbellegi.Al("#059669", System.Windows.Media.Color.FromRgb(5, 150, 105))
                : FircaOnbellegi.Al("#F59E0B", System.Windows.Media.Color.FromRgb(245, 158, 11))
        }).ToList();
    }

    private static void KayitlariUygula(string json)
    {
        var liste = JsonSerializer.Deserialize<List<IadeKayit>>(json, Json) ?? [];
        Kayitlar.Clear();
        Kayitlar.AddRange(liste);
    }

    private static async Task YerelKaydetAsync(CancellationToken iptal)
    {
        SatinalmaProKlasor.Olustur();
        var json = JsonSerializer.Serialize(Kayitlar, Json);
        await File.WriteAllTextAsync(YerelDosyaYolu(), json, iptal);
    }

    private static string YerelDosyaYolu() => SatinalmaProKlasor.DosyaYolu("iade_kayitlari.json");
}

public sealed class IadeKayit
{
    public string IadeNo { get; set; } = "";
    public string SiparisNo { get; set; } = "";
    public string Firma { get; set; } = "";
    public string Malzeme { get; set; } = "";
    public string Miktar { get; set; } = "";
    public double MiktarSayi { get; set; }
    public string Birim { get; set; } = "";
    public string Neden { get; set; } = "";
    public string Durum { get; set; } = "";
    public string Tarih { get; set; } = "";
    public Guid? TalepId { get; set; }
    public Guid? KalemId { get; set; }
    public string DepoSaha { get; set; } = "";
    public bool StokCikisiYapildi { get; set; }
}

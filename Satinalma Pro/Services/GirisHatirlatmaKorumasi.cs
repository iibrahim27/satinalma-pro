using System.IO;
using System.Text.Json;
using SatinalmaPro.Helpers;

namespace SatinalmaPro.Services;

/// <summary>Kurulum izlerini tutar. Güncellemede giriş bilgileri silinmez.</summary>
public static class GirisHatirlatmaKorumasi
{
    private static readonly string IzDosyasi = SatinalmaProKlasor.DosyaYolu("kurulum_izleri.json");
    private static readonly string InstallIdDosyasi = Path.Combine(AppContext.BaseDirectory, ".install_id");

    private static readonly JsonSerializerOptions JsonSecenekleri = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static void KurulumVeSurumKontrolu(IEnumerable<string>? args = null)
    {
        var mevcutSurum = UygulamaBilgisi.Versiyon;
        var mevcutInstallId = InstallIdOku();
        var mevcutExeImzasi = ExeImzasiOlustur();
        var onceki = IzOku();

        IzKaydet(new KurulumIzleri
        {
            Surum = mevcutSurum,
            InstallId = mevcutInstallId ?? onceki?.InstallId ?? "",
            ExeImzasi = mevcutExeImzasi
        });
    }

    private static string? InstallIdOku()
    {
        try
        {
            return File.Exists(InstallIdDosyasi)
                ? File.ReadAllText(InstallIdDosyasi).Trim()
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static string ExeImzasiOlustur()
    {
        try
        {
            var yol = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(yol) || !File.Exists(yol))
                return "";

            var bilgi = new FileInfo(yol);
            return $"{bilgi.Length}:{bilgi.LastWriteTimeUtc.Ticks}";
        }
        catch
        {
            return "";
        }
    }

    private static KurulumIzleri? IzOku()
    {
        if (!File.Exists(IzDosyasi))
            return null;

        try
        {
            return JsonSerializer.Deserialize<KurulumIzleri>(File.ReadAllText(IzDosyasi), JsonSecenekleri);
        }
        catch
        {
            return null;
        }
    }

    private static void IzKaydet(KurulumIzleri iz)
    {
        try
        {
            SatinalmaProKlasor.Olustur();
            File.WriteAllText(IzDosyasi, JsonSerializer.Serialize(iz, JsonSecenekleri));
        }
        catch
        {
            // isteğe bağlı
        }
    }

    private sealed class KurulumIzleri
    {
        public string Surum { get; set; } = "";
        public string InstallId { get; set; } = "";
        public string ExeImzasi { get; set; } = "";
    }
}

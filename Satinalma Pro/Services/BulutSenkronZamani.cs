using System.IO;
using System.Text.Json;
using SatinalmaPro.Shared.SaaS;

namespace SatinalmaPro.Services;

/// <summary>
/// Firestore belge güncelleme zamanlarını dosya mtime yerine takip eder.
/// Kiracıya özel dosya + bellek; firma değişiminde sıfırlanır.
/// </summary>
internal static class BulutSenkronZamani
{
    private static readonly object Kilit = new();
    private static Dictionary<string, DateTime>? _onbellek;
    private static string? _onbellekTenantId;

    private static string DosyaYolu
    {
        get
        {
            var tid = KiracıOturumu.TenantId;
            var ad = string.IsNullOrWhiteSpace(tid)
                ? "bulut_senkron_zaman.json"
                : $"bulut_senkron_zaman_{tid}.json";
            return SatinalmaProKlasor.DosyaYolu(ad);
        }
    }

    /// <summary>Firma değişiminde / çıkışta zaman damgalarını bellekten siler.</summary>
    public static void KiraciDegisti()
    {
        lock (Kilit)
        {
            _onbellek = null;
            _onbellekTenantId = null;
        }
    }

    public static bool YeniVeriVar(string anahtar, DateTime? bulutUtc)
    {
        if (bulutUtc is null)
            return false;

        var son = Yukle().GetValueOrDefault(anahtar, DateTime.MinValue);
        return bulutUtc.Value > son;
    }

    public static void Kaydet(string anahtar, DateTime bulutUtc)
    {
        lock (Kilit)
        {
            var sozluk = Yukle();
            sozluk[anahtar] = bulutUtc;
            _onbellek = sozluk;
            _onbellekTenantId = KiracıOturumu.TenantId;
            SatinalmaProKlasor.Olustur();
            File.WriteAllText(DosyaYolu, JsonSerializer.Serialize(sozluk));
        }
    }

    private static Dictionary<string, DateTime> Yukle()
    {
        lock (Kilit)
        {
            var tid = KiracıOturumu.TenantId;
            if (_onbellek is not null &&
                string.Equals(_onbellekTenantId, tid, StringComparison.Ordinal))
                return _onbellek;

            _onbellekTenantId = tid;

            if (!File.Exists(DosyaYolu))
            {
                _onbellek = new Dictionary<string, DateTime>(StringComparer.Ordinal);
                return _onbellek;
            }

            try
            {
                _onbellek = JsonSerializer.Deserialize<Dictionary<string, DateTime>>(
                                 File.ReadAllText(DosyaYolu))
                             ?? new Dictionary<string, DateTime>(StringComparer.Ordinal);
            }
            catch
            {
                _onbellek = new Dictionary<string, DateTime>(StringComparer.Ordinal);
            }

            return _onbellek;
        }
    }
}

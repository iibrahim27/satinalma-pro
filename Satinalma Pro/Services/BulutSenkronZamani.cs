using System.IO;
using System.Text.Json;

namespace SatinalmaPro.Services;

/// <summary>
/// Firestore belge güncelleme zamanlarını dosya mtime yerine takip eder.
/// </summary>
internal static class BulutSenkronZamani
{
    private static readonly object Kilit = new();
    private static Dictionary<string, DateTime>? _onbellek;

    private static string DosyaYolu =>
        SatinalmaProKlasor.DosyaYolu("bulut_senkron_zaman.json");

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
            SatinalmaProKlasor.Olustur();
            File.WriteAllText(DosyaYolu, JsonSerializer.Serialize(sozluk));
        }
    }

    private static Dictionary<string, DateTime> Yukle()
    {
        lock (Kilit)
        {
            if (_onbellek is not null)
                return _onbellek;

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

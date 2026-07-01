using System.Security.Cryptography;
using System.Text;

namespace SatinalmaPro.Mobile.Services;

public static class GuvenliGirisDeposu
{
    private const string BiyometrikKey = "guvenli_giris_biyometrik";
    private const string PinHashKey = "guvenli_giris_pin_hash";
    private const string EpostaIpucuKey = "guvenli_giris_eposta";

    public static async Task<bool> BiyometrikAktifMiAsync()
    {
        var deger = await GuvenliOkuAsync(BiyometrikKey);
        return deger == "1";
    }

    public static async Task BiyometrikAyarlaAsync(bool aktif)
    {
        if (aktif)
            await GuvenliYazAsync(BiyometrikKey, "1");
        else
            GuvenliSil(BiyometrikKey);
    }

    public static async Task<bool> PinAyarliMiAsync()
    {
        var deger = await GuvenliOkuAsync(PinHashKey);
        return !string.IsNullOrEmpty(deger);
    }

    public static async Task PinAyarlaAsync(string pin) =>
        await GuvenliYazAsync(PinHashKey, PinHash(pin));

    public static Task PinKaldirAsync()
    {
        GuvenliSil(PinHashKey);
        return Task.CompletedTask;
    }

    public static async Task<bool> PinDogrulaAsync(string pin)
    {
        var kayitli = await GuvenliOkuAsync(PinHashKey);
        if (string.IsNullOrEmpty(kayitli))
            return false;
        return kayitli == PinHash(pin);
    }

    public static async Task<bool> GuvenliGirisAktifMiAsync() =>
        await BiyometrikAktifMiAsync() || await PinAyarliMiAsync();

    public static async Task EpostaIpucuAyarlaAsync(string? eposta)
    {
        if (string.IsNullOrWhiteSpace(eposta))
            GuvenliSil(EpostaIpucuKey);
        else
            await GuvenliYazAsync(EpostaIpucuKey, eposta.Trim());
    }

    public static Task<string?> EpostaIpucuAlAsync() => GuvenliOkuAsync(EpostaIpucuKey);

    public static Task GuvenliGirisTemizleAsync()
    {
        GuvenliSil(BiyometrikKey);
        GuvenliSil(PinHashKey);
        GuvenliSil(EpostaIpucuKey);
        return Task.CompletedTask;
    }

    private static async Task<string?> GuvenliOkuAsync(string key)
    {
        try
        {
            return await SecureStorage.GetAsync(key);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SecureStorage okuma ({key}): {ex.Message}");
            return Preferences.Default.Get(key, (string?)null);
        }
    }

    private static async Task GuvenliYazAsync(string key, string value)
    {
        try
        {
            await SecureStorage.SetAsync(key, value);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SecureStorage yazma ({key}): {ex.Message}");
            Preferences.Default.Set(key, value);
        }
    }

    private static void GuvenliSil(string key)
    {
        try
        {
            SecureStorage.Remove(key);
        }
        catch
        {
            // yoksay
        }

        Preferences.Default.Remove(key);
    }

    private static string PinHash(string pin) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(pin.Trim())));
}

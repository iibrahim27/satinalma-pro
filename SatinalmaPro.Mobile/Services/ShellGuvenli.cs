namespace SatinalmaPro.Mobile.Services;

/// <summary>Shell.Current null oldugunda cokmeyi onler.</summary>
public static class ShellGuvenli
{
    public static Page? AktifSayfa =>
        Shell.Current?.CurrentPage
        ?? Application.Current?.Windows.FirstOrDefault()?.Page;

    public static Task DisplayAlertAsync(string baslik, string mesaj, string tamam) =>
        AktifSayfa?.DisplayAlert(baslik, mesaj, tamam) ?? Task.CompletedTask;

    public static Task<bool> DisplayAlertAsync(string baslik, string mesaj, string evet, string hayir) =>
        AktifSayfa?.DisplayAlert(baslik, mesaj, evet, hayir) ?? Task.FromResult(false);

    public static Task<string?> DisplayPromptAsync(string baslik, string mesaj, string ok, string iptal) =>
        AktifSayfa?.DisplayPromptAsync(baslik, mesaj, ok, iptal) ?? Task.FromResult<string?>(null);

    public static Task<string?> DisplayActionSheetAsync(string baslik, string iptal, string? destruction, params string[] buttons) =>
        AktifSayfa?.DisplayActionSheet(baslik, iptal, destruction, buttons) ?? Task.FromResult<string?>(null);

    public static async Task GoToAsync(string route)
    {
        if (Shell.Current is null)
            return;
        await Shell.Current.GoToAsync(route);
    }
}

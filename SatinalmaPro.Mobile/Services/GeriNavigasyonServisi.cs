namespace SatinalmaPro.Mobile.Services;

/// <summary>
/// Android geri tuşu ve Shell geri navigasyonu.
/// Alt sayfadan bir önceki ekrana döner; kök sekmede ana panele gider.
/// </summary>
public static class GeriNavigasyonServisi
{
    public static bool GeriDene()
    {
        var shell = Shell.Current;
        if (shell is null)
            return false;

        if (shell.FlyoutIsPresented)
        {
            shell.FlyoutIsPresented = false;
            return true;
        }

        var segments = MevcutSegmentler(shell);
        if (segments.Length > 1)
        {
            _ = shell.GoToAsync("..");
            return true;
        }

        var kok = segments.FirstOrDefault() ?? "";
        if (!string.Equals(kok, "main", StringComparison.OrdinalIgnoreCase))
        {
            _ = shell.GoToAsync("//main");
            return true;
        }

        return false;
    }

    public static async Task<bool> GeriAsync()
    {
        var shell = Shell.Current;
        if (shell is null)
            return false;

        if (shell.FlyoutIsPresented)
        {
            shell.FlyoutIsPresented = false;
            return true;
        }

        var segments = MevcutSegmentler(shell);
        if (segments.Length > 1)
        {
            await shell.GoToAsync("..");
            return true;
        }

        var kok = segments.FirstOrDefault() ?? "";
        if (!string.Equals(kok, "main", StringComparison.OrdinalIgnoreCase))
        {
            await shell.GoToAsync("//main");
            return true;
        }

        return false;
    }

    private static string[] MevcutSegmentler(Shell shell)
    {
        var location = shell.CurrentState?.Location?.OriginalString ?? "";
        if (string.IsNullOrWhiteSpace(location))
            return [];

        var soru = location.IndexOf('?', StringComparison.Ordinal);
        if (soru >= 0)
            location = location[..soru];

        var kok = location.IndexOf("//", StringComparison.Ordinal);
        var yol = kok >= 0 ? location[(kok + 2)..] : location;
        return yol.Split('/', StringSplitOptions.RemoveEmptyEntries);
    }
}

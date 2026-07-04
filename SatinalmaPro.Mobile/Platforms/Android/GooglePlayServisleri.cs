using Android.Content;
using Android.Gms.Common;

namespace SatinalmaPro.Mobile;

public static class GooglePlayServisleri
{
    public static bool KullanilabilirMi(Context? context = null)
    {
        context ??= global::Android.App.Application.Context;
        if (context is null)
            return false;

        try
        {
            var sonuc = GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(context);
            return sonuc == ConnectionResult.Success;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Google Play Services: {ex.Message}");
            return false;
        }
    }
}

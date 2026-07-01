using Android.Gms.Extensions;
using Firebase.Messaging;
using Microsoft.Maui.Networking;
using SatinalmaPro.Mobile.Services;

namespace SatinalmaPro.Mobile;

public sealed class FcmPlatformServisi : IFcmPlatformServisi
{
    public Task BaslatAsync()
    {
        try
        {
            FirebaseMessaging.Instance.AutoInitEnabled = GooglePlayServisleri.KullanilabilirMi();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FCM başlatma: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    public async Task<string?> TokenAlAsync()
    {
        if (!GooglePlayServisleri.KullanilabilirMi())
            return null;

        if (Connectivity.NetworkAccess is not (NetworkAccess.Internet or NetworkAccess.ConstrainedInternet))
            return null;

        try
        {
            var token = await FirebaseMessaging.Instance.GetToken();
            return token?.ToString();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FCM token: {ex.Message}");
            return null;
        }
    }
}

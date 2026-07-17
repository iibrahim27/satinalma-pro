using Android.App;
using Android;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using SatinalmaPro.Mobile.Services;

namespace SatinalmaPro.Mobile;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    private const int BildirimIzinKodu = 1001;
    private static DateTime _sonBildirimYenileme = DateTime.MinValue;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        AndroidBildirimKanali.Olustur();
        BildirimIzniIste();
        try { SatinalmaPro.Mobile.Services.YerelBildirimGosterici.KanaliHazirla(); } catch { }
        IntentRouteIsle(Intent);
    }

    protected override void OnResume()
    {
        base.OnResume();
        AndroidBildirimKanali.Olustur();
        BildirimIzniIste();
        var oturum = MauiProgram.Services?.GetService<Services.OturumServisi>();
        if (oturum?.GirisYapildi == true)
            oturum.Dinleyici.Baslat();
        _ = BildirimleriYenileAsync();
    }

    protected override void OnPause()
    {
        MauiProgram.Services?.GetService<Services.OturumServisi>()?.Dinleyici.Durdur();
        base.OnPause();
    }

    private static async Task BildirimleriYenileAsync()
    {
        if ((DateTime.UtcNow - _sonBildirimYenileme).TotalSeconds < 10)
            return;

        _sonBildirimYenileme = DateTime.UtcNow;

        try
        {
            var oturum = MauiProgram.Services?.GetService<Services.OturumServisi>();
            if (oturum?.GirisYapildi == true)
                await oturum.Dinleyici.SenkronizeVeGosterAsync();
        }
        catch
        {
            // sessiz
        }
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        if (intent is not null)
        {
            Intent = intent;
            IntentRouteIsle(intent);
        }
    }

    private static void IntentRouteIsle(Intent? intent)
    {
        if (intent is null)
            return;

        var route = intent.GetStringExtra("bildirim_route");
        if (string.IsNullOrWhiteSpace(route))
            route = intent.GetStringExtra("route");

        var bildirimId = intent.GetStringExtra("bildirimId");

        if (string.IsNullOrWhiteSpace(route))
        {
            var talepId = intent.GetStringExtra("talepId");
            if (!string.IsNullOrWhiteSpace(talepId))
                route = $"talep-detay?id={talepId}";
        }

        if (!string.IsNullOrWhiteSpace(route))
        {
            if (!string.IsNullOrWhiteSpace(bildirimId))
            {
                if (Guid.TryParse(bildirimId, out var bid))
                    BildirimGosterimKaydi.Isaretle(bid);
                route = route.Contains('?') ? $"{route}&bid={bildirimId}" : $"{route}?bid={bildirimId}";
            }

            BildirimNavigasyonServisi.BekleyenRouteAyarla(route);
        }
        else if (!string.IsNullOrWhiteSpace(bildirimId) && Guid.TryParse(bildirimId, out var yalnizBid))
        {
            BildirimGosterimKaydi.Isaretle(yalnizBid);
        }
    }

    private void BildirimIzniIste()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(33))
            return;

        if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.PostNotifications) == Permission.Granted)
            return;

        ActivityCompat.RequestPermissions(this, [Manifest.Permission.PostNotifications], BildirimIzinKodu);
    }

    public override void OnBackPressed()
    {
        if (GeriNavigasyonServisi.GeriDene())
            return;

        OnBackPressedDispatcher.OnBackPressed();
    }
}

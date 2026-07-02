using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;

namespace SatinalmaPro.Mobile;

public static class AndroidBildirimKanali
{
    public const string KanalId = "satinalma_pro";

    public static void Olustur(Context? context = null)
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            return;

        context ??= global::Android.App.Application.Context;
        var nm = (NotificationManager?)context.GetSystemService(Context.NotificationService);
        if (nm is null)
            return;

        if (nm.GetNotificationChannel(KanalId) is not null)
            return;

        var kanal = new NotificationChannel(KanalId, "Satınalma Pro", NotificationImportance.High)
        {
            Description = "Talep, onay ve teklif bildirimleri",
            LockscreenVisibility = NotificationVisibility.Public
        };
        kanal.EnableVibration(true);
        kanal.EnableLights(true);
        nm.CreateNotificationChannel(kanal);
    }
}

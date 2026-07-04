using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using Firebase.Messaging;
using SatinalmaPro.Mobile.Services;
using SatinalmaPro.Shared.Models;
using SatinalmaPro.Shared.Services;

namespace SatinalmaPro.Mobile;

[Service(Exported = false, ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeDataSync)]
public class BildirimForegroundService : Service
{
    private const int BildirimId = 9001;
    private const int KontrolAraligiMs = 10_000;
    private Timer? _zamanlayici;
    private bool _calisiyor;
    private static Services.OturumServisi? _yalnizOturum;

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        if (_calisiyor)
            return StartCommandResult.Sticky;

        _calisiyor = true;
        AndroidBildirimKanali.Olustur();
        var kanal = new NotificationChannel("satinalma_pro_bg", "Satınalma Pro", NotificationImportance.Low)
        {
            Description = "Arka plan bildirim senkronu"
        };
        var nm = (NotificationManager?)GetSystemService(NotificationService);
        nm?.CreateNotificationChannel(kanal);

        var bildirim = new NotificationCompat.Builder(this, "satinalma_pro_bg")
            .SetContentTitle("Satınalma Pro")
            .SetContentText("Bildirimler dinleniyor")
            .SetSmallIcon(Resource.Mipmap.appicon)
            .SetOngoing(true)
            .Build();

        StartForeground(BildirimId, bildirim);
        _zamanlayici = new Timer(_ => _ = SenkronizeEtAsync(), null, 0, KontrolAraligiMs);
        return StartCommandResult.Sticky;
    }

    private async Task SenkronizeEtAsync()
    {
        try
        {
            var oturum = OturumAl();
            if (!await oturum.OturumuGerekirseYukleAsync())
                return;

            await oturum.Dinleyici.BildirimPollAsync();
        }
        catch
        {
            // sessiz
        }
    }

    private static Services.OturumServisi OturumAl()
    {
        var oturum = IPlatformApplication.Current?.Services.GetService<Services.OturumServisi>();
        if (oturum is not null)
            return oturum;

        _yalnizOturum ??= new Services.OturumServisi();
        return _yalnizOturum;
    }

    public override void OnDestroy()
    {
        _zamanlayici?.Dispose();
        _calisiyor = false;
        base.OnDestroy();
    }

    public override IBinder? OnBind(Intent? intent) => null;

    public static void Baslat(Context context)
    {
        var intent = new Intent(context, typeof(BildirimForegroundService));
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            context.StartForegroundService(intent);
        else
            context.StartService(intent);
    }

    public static void Durdur(Context context) =>
        context.StopService(new Intent(context, typeof(BildirimForegroundService)));
}

[Service(Exported = false)]
[IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
public class SatinalmaFirebaseMessagingService : FirebaseMessagingService
{
    public override void OnMessageReceived(RemoteMessage message)
    {
        var data = message.Data;
        var baslik = message.GetNotification()?.Title
                     ?? VeriAl(data, "title")
                     ?? "Satınalma Pro";
        var govde = message.GetNotification()?.Body
                    ?? VeriAl(data, "body")
                    ?? "";

        var route = VeriAl(data, "route") ?? "";
        if (string.IsNullOrWhiteSpace(route) && data.TryGetValue("talepId", out var tid) && !string.IsNullOrWhiteSpace(tid))
            route = $"talep-detay?id={tid}";

        var bildirimId = VeriAl(data, "bildirimId") ?? "";
        var bildirim = new BildirimKaydi
        {
            Baslik = baslik,
            Mesaj = govde,
            Tip = VeriAl(data, "tip") ?? "",
            TalepId = Guid.TryParse(VeriAl(data, "talepId"), out var g) ? g : null
        };
        if (!Guid.TryParse(bildirimId, out var bid))
            bid = bildirim.Id;

        bildirim.Id = bid;
        if (!BildirimGosterimKaydi.IlkGosterimMi(bid))
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                var oturum = IPlatformApplication.Current?.Services.GetService<Services.OturumServisi>();
                if (oturum?.GirisYapildi == true)
                {
                    await oturum.Depo.BildirimleriSenkronizeEtAsync();
                    MainThread.BeginInvokeOnMainThread(oturum.VeriGuncellendiBildir);
                }
            }
            catch
            {
                // yoksay
            }
        });

        GosterVeYonlendir(baslik, govde, route, bildirim.Id.GetHashCode(), bildirimId);
    }

    private void GosterVeYonlendir(string baslik, string govde, string route, int id, string bildirimId)
    {
        if (Guid.TryParse(bildirimId, out var bid))
        {
            if (!BildirimGosterimKaydi.IlkGosterimMi(bid))
                return;
            BildirimGosterimKaydi.Isaretle(bid);
        }

        var launchIntent = PackageManager?.GetLaunchIntentForPackage(PackageName!)?.SetFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);
        if (launchIntent is null)
            return;

        if (!string.IsNullOrWhiteSpace(route))
            launchIntent.PutExtra("bildirim_route", route);
        if (!string.IsNullOrWhiteSpace(bildirimId))
            launchIntent.PutExtra("bildirimId", bildirimId);

        var pending = PendingIntent.GetActivity(
            this, Math.Abs(id),
            launchIntent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        var kanal = new NotificationChannel(AndroidBildirimKanali.KanalId, "Satınalma Pro", NotificationImportance.High)
        {
            Description = "Talep ve onay bildirimleri",
            LockscreenVisibility = NotificationVisibility.Public
        };
        kanal.EnableVibration(true);
        var nm = (NotificationManager?)GetSystemService(NotificationService);
        nm?.CreateNotificationChannel(kanal);

        var bildirim = new NotificationCompat.Builder(this, AndroidBildirimKanali.KanalId)
            .SetContentTitle(baslik)
            .SetContentText(govde)
            .SetStyle(new NotificationCompat.BigTextStyle().BigText(govde))
            .SetSmallIcon(Resource.Mipmap.appicon)
            .SetAutoCancel(true)
            .SetContentIntent(pending)
            .SetPriority(NotificationCompat.PriorityMax)
            .SetCategory(NotificationCompat.CategoryMessage)
            .SetDefaults((int)(NotificationDefaults.Sound | NotificationDefaults.Vibrate))
            .Build();

        nm?.Notify(Math.Abs(id), bildirim);
    }

    private static string? VeriAl(IDictionary<string, string> data, string anahtar) =>
        data.TryGetValue(anahtar, out var deger) ? deger : null;

    public override void OnNewToken(string token)
    {
        Services.OturumServisi.FcmTokenAyarla(token);
        _ = Task.Run(async () =>
        {
            try
            {
                var oturum = IPlatformApplication.Current?.Services.GetService<Services.OturumServisi>();
                if (oturum?.GirisYapildi == true)
                    await oturum.FcmTokenKaydetAsync();
            }
            catch
            {
                // yoksay
            }
        });
    }
}

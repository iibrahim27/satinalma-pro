using Android.App;
using Android.Content;
using AndroidX.Core.App;
using Firebase.Messaging;
using SatinalmaPro.Mobile.Services;
using SatinalmaPro.Shared.Helpers;
using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Mobile;

[Service(Exported = false)]
[IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
public sealed class SatinalmaFirebaseMessagingService : FirebaseMessagingService
{
    public override void OnMessageReceived(RemoteMessage message)
    {
        var veri = message.Data;
        var baslik = message.GetNotification()?.Title
                     ?? VeriAl(veri, "title")
                     ?? "Satınalma Pro";
        var govde = message.GetNotification()?.Body
                    ?? VeriAl(veri, "body")
                    ?? "";

        var route = VeriAl(veri, "route") ?? "";
        if (string.IsNullOrWhiteSpace(route)
            && veri.TryGetValue("talepId", out var talepId)
            && !string.IsNullOrWhiteSpace(talepId))
        {
            route = $"talep-detay?id={talepId}";
        }

        var inboxDocId = VeriAl(veri, "inboxDocId");
        var bildirimId = VeriAl(veri, "bildirimId");
        var id = !string.IsNullOrWhiteSpace(inboxDocId)
            ? BildirimMantikAnahtari.InboxDocIddenGuid(inboxDocId)
            : Guid.TryParse(bildirimId, out var bildirilenId)
                ? bildirilenId
                : Guid.NewGuid();

        if (!BildirimGosterimKaydi.IlkGosterimMi(id))
            return;

        _ = BildirimleriSenkronizeEtAsync();
        GosterVeYonlendir(baslik, govde, route, id.GetHashCode(), id);
    }

    private static async Task BildirimleriSenkronizeEtAsync()
    {
        try
        {
            var oturum = IPlatformApplication.Current?.Services.GetService<OturumServisi>();
            if (oturum?.GirisYapildi != true)
                return;

            await oturum.Depo.BildirimleriSenkronizeEtAsync();
            MainThread.BeginInvokeOnMainThread(oturum.VeriGuncellendiBildir);
        }
        catch
        {
            // Kalıcı gelen kutusu sonraki uygulama senkronunda yeniden okunur.
        }
    }

    private void GosterVeYonlendir(string baslik, string govde, string route, int id, Guid bildirimId)
    {
        if (!BildirimGosterimKaydi.IlkGosterimMi(bildirimId))
            return;

        var launchIntent = PackageManager?.GetLaunchIntentForPackage(PackageName!)
            ?.SetFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);
        if (launchIntent is null)
            return;

        BildirimGosterimKaydi.Isaretle(bildirimId);

        if (!string.IsNullOrWhiteSpace(route))
            launchIntent.PutExtra("bildirim_route", route);
        launchIntent.PutExtra("bildirimId", bildirimId.ToString());

        var pending = PendingIntent.GetActivity(
            this,
            Math.Abs(id),
            launchIntent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        AndroidBildirimKanali.Olustur();
        var olusturucu = new NotificationCompat.Builder(this, AndroidBildirimKanali.KanalId);
        olusturucu.SetContentTitle(baslik);
        olusturucu.SetContentText(govde);
        olusturucu.SetStyle(new NotificationCompat.BigTextStyle().BigText(govde));
        olusturucu.SetSmallIcon(Resource.Mipmap.appicon);
        olusturucu.SetAutoCancel(true);
        olusturucu.SetContentIntent(pending);
        olusturucu.SetPriority(NotificationCompat.PriorityHigh);
        olusturucu.SetCategory(NotificationCompat.CategoryMessage);
        olusturucu.SetDefaults((int)(NotificationDefaults.Sound | NotificationDefaults.Vibrate));

        var yonetici = (NotificationManager?)GetSystemService(NotificationService);
        yonetici?.Notify(Math.Abs(id), olusturucu.Build());
    }

    private static string? VeriAl(IDictionary<string, string> veri, string anahtar) =>
        veri.TryGetValue(anahtar, out var deger) ? deger : null;

    public override void OnNewToken(string token)
    {
        OturumServisi.FcmTokenAyarla(token);
        _ = TokeniKaydetAsync();
    }

    private static async Task TokeniKaydetAsync()
    {
        try
        {
            var oturum = IPlatformApplication.Current?.Services.GetService<OturumServisi>();
            if (oturum?.GirisYapildi == true)
                await oturum.FcmTokenKaydetAsync();
        }
        catch
        {
            // Token, sonraki oturum açılışında tekrar kaydedilir.
        }
    }
}

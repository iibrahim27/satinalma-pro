using Android.App;
using Android.Content;
using SatinalmaPro.Mobile.Services;

namespace SatinalmaPro.Mobile;

[BroadcastReceiver(Exported = true, Enabled = true, DirectBootAware = true)]
[IntentFilter(new[]
{
    Intent.ActionBootCompleted,
    Intent.ActionLockedBootCompleted,
    Intent.ActionMyPackageReplaced
})]
public class BootCompletedReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context is null || intent?.Action is null)
            return;

        if (!OturumServisi.KayitliOturumVar())
            return;

        BildirimForegroundService.Baslat(context);
    }
}

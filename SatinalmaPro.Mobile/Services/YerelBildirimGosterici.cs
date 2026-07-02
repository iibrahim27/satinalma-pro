using Plugin.LocalNotification;
using Plugin.LocalNotification.AndroidOption;
using SatinalmaPro.Shared.Models;
using SatinalmaPro.Shared.Services;

namespace SatinalmaPro.Mobile.Services;

public static class YerelBildirimGosterici
{
    public static void KanaliHazirla()
    {
#if ANDROID
        AndroidBildirimKanali.Olustur();
#endif
        try
        {
            LocalNotificationCenter.Current.RequestNotificationPermission();
        }
        catch
        {
            // platform henüz hazır olmayabilir
        }
    }

    public static void Goster(BildirimKaydi bildirim, string? kullaniciRol = null)
    {
        KanaliHazirla();

        var route = BildirimRotaServisi.HedefRoute(bildirim, kullaniciRol);
        var returning = route.Contains('?')
            ? $"{route}&bid={bildirim.Id}"
            : $"{route}?bid={bildirim.Id}";

        try
        {
            LocalNotificationCenter.Current.Show(new NotificationRequest
            {
                NotificationId = Math.Abs(bildirim.Id.GetHashCode()),
                Title = bildirim.Baslik,
                Description = bildirim.Mesaj,
                CategoryType = NotificationCategoryType.Status,
                ReturningData = returning,
                Android =
                {
                    AutoCancel = true,
                    ChannelId = AndroidBildirimKanali.KanalId,
                    Priority = AndroidPriority.High,
                    VisibilityType = AndroidVisibilityType.Public
                }
            });
        }
        catch
        {
            // bildirim gösterilemezse sessiz
        }
    }
}

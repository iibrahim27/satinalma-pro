using System.IO;
using System.Windows;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.Win32;
using SatinalmaPro.Models;
using SatinalmaPro.Services;

namespace SatinalmaPro.Helpers;

public static class MasaustuActionCenterBildirim
{
    private const string EskiAumid = "METRIK.SatinalmaPro";
    private static bool _hazir;
    private static bool _calisiyor;

    public static bool Calisiyor => _calisiyor;

    public static void Baslat()
    {
        if (_hazir)
            return;

        _hazir = true;
        try
        {
            EskiKimligiTemizle();

            ToastNotificationManagerCompat.OnActivated += OnToastActivated;

            _ = ToastNotificationManagerCompat.CreateToastNotifier();
            _calisiyor = true;
        }
        catch (Exception ex)
        {
            _calisiyor = false;
            HataGunlugu.Kaydet(ex, "ActionCenterBaslat");
        }
    }

    public static bool Goster(BildirimKaydi bildirim)
    {
        if (!_calisiyor)
            Baslat();

        if (!_calisiyor)
            return false;

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
            return dispatcher.Invoke(() => Goster(bildirim));

        try
        {
            var argumanlar = new ToastArguments().Add("bildirimId", bildirim.Id.ToString());

            new ToastContentBuilder()
                .AddToastActivationInfo(argumanlar.ToString(), ToastActivationType.Foreground)
                .AddText(bildirim.Baslik)
                .AddText(bildirim.Mesaj)
                .AddAudio(new ToastAudio())
                .Show(toast =>
                {
                    toast.Tag = bildirim.Id.ToString();
                    toast.Group = "SatinalmaPro";
                });

            return true;
        }
        catch (Exception ex)
        {
            _calisiyor = false;
            HataGunlugu.Kaydet(ex, "ActionCenterGoster");
            return false;
        }
    }

    private static void OnToastActivated(ToastNotificationActivatedEventArgsCompat toastArgs)
    {
        var argumanlar = ToastArguments.Parse(toastArgs.Argument);
        if (!argumanlar.TryGetValue("bildirimId", out var idStr) || !Guid.TryParse(idStr, out var id))
            return;

        Application.Current?.Dispatcher.Invoke(async () =>
        {
            try
            {
                await BildirimDeposu.YukleAsync();
                var bildirim = BildirimDeposu.Bildirimler.FirstOrDefault(b => b.Id == id);
                if (bildirim is null)
                    return;

                Application.Current.MainWindow?.Activate();
                if (Application.Current.MainWindow is MainWindow mw)
                {
                    mw.WindowState = WindowState.Normal;
                    mw.Show();
                    mw.Activate();
                }

                await BildirimYoneticisi.OkunduIsaretleAsync(bildirim);
                MasaustuBildirimNavigasyon.BildirimdenGit(bildirim);
            }
            catch (Exception ex)
            {
                HataGunlugu.Kaydet(ex, "ActionCenterTik");
            }
        });
    }

    private static void EskiKimligiTemizle()
    {
        try
        {
            Registry.CurrentUser.DeleteSubKeyTree(
                $@"Software\Classes\AppUserModelId\{EskiAumid}",
                throwOnMissingSubKey: false);
        }
        catch
        {
            // Eski kayıt yoksa sorun değil.
        }

        var programs = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
            "Programs",
            "Satınalma Pro.lnk");
        try
        {
            if (File.Exists(programs))
                File.Delete(programs);
        }
        catch
        {
            // Eski kısayol silinemezse devam et.
        }
    }
}

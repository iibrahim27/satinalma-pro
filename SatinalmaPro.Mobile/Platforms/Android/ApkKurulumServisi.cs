using Android.Content;
using Android.OS;
using AndroidX.Core.Content;
using Microsoft.Maui.ApplicationModel;
using SatinalmaPro.Mobile.Services;
using Application = Android.App.Application;

namespace SatinalmaPro.Mobile;

public sealed class ApkKurulumServisi : IApkKurulumServisi
{
    public bool KurulumIznineHazir()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            return true;

        var context = Platform.CurrentActivity ?? Application.Context;
        if (context.PackageManager is null)
            return false;

        if (context.PackageManager.CanRequestPackageInstalls())
            return true;

        var ayar = new Intent(Android.Provider.Settings.ActionManageUnknownAppSources);
        ayar.SetData(Android.Net.Uri.Parse($"package:{context.PackageName}"));
        ayar.AddFlags(ActivityFlags.NewTask);
        context.StartActivity(ayar);
        return false;
    }

    public void Kur(string apkYol)
    {
        var activity = Platform.CurrentActivity
            ?? throw new InvalidOperationException("Kurulum ekranı açılamadı. Uygulamayı yeniden açıp tekrar deneyin.");

        var dosya = new Java.IO.File(apkYol);
        if (!dosya.Exists())
            throw new FileNotFoundException("APK bulunamadı.", apkYol);

        var yetki = $"{activity.PackageName}.fileProvider";
        var uri = AndroidX.Core.Content.FileProvider.GetUriForFile(activity, yetki, dosya)
            ?? throw new InvalidOperationException("APK dosyası paylaşılamadı.");

        var intent = new Intent(Intent.ActionView);
        intent.SetDataAndType(uri, "application/vnd.android.package-archive");
        intent.AddFlags(ActivityFlags.GrantReadUriPermission);
        intent.ClipData = ClipData.NewUri(activity.ContentResolver, "SatinalmaPro", uri);
        activity.StartActivity(intent);
    }
}

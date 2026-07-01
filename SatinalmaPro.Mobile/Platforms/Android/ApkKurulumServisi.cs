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
        var context = Platform.CurrentActivity ?? Application.Context;
        var dosya = new Java.IO.File(apkYol);
        if (!dosya.Exists())
            throw new FileNotFoundException("APK bulunamadı.", apkYol);

        Android.Net.Uri uri;
        if (Build.VERSION.SdkInt >= BuildVersionCodes.N)
        {
            var yetki = $"{context.PackageName}.fileProvider";
            uri = AndroidX.Core.Content.FileProvider.GetUriForFile(context, yetki, dosya)!;
        }
        else
        {
#pragma warning disable CS0618
            uri = Android.Net.Uri.FromFile(dosya)!;
#pragma warning restore CS0618
        }

        var intent = new Intent(Intent.ActionView);
        intent.SetDataAndType(uri, "application/vnd.android.package-archive");
        intent.AddFlags(ActivityFlags.NewTask | ActivityFlags.GrantReadUriPermission);

        context.StartActivity(intent);
    }
}

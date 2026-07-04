#if ANDROID
using AndroidX.Biometric;
using AndroidX.Core.Content;
using AndroidX.Fragment.App;
using Java.Lang;
using Microsoft.Maui.ApplicationModel;

namespace SatinalmaPro.Mobile.Services;

public sealed class BiyometrikKimlikServisi : IBiyometrikKimlikServisi
{
    private static int DesteklenenAuthenticators =>
        (int)BiometricManager.Authenticators.BiometricStrong
        | (int)BiometricManager.Authenticators.BiometricWeak;

    public Task<bool> KullanilabilirMiAsync()
    {
        try
        {
            var activity = Platform.CurrentActivity as FragmentActivity;
            if (activity is null)
                return Task.FromResult(false);

            var manager = BiometricManager.From(activity);
            var sonuc = manager.CanAuthenticate(DesteklenenAuthenticators);
            return Task.FromResult(sonuc == BiometricManager.BiometricSuccess);
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Biyometrik kullanılabilirlik: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    public Task<bool> DogrulaAsync(string mesaj = "Kimliğinizi doğrulayın")
    {
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        try
        {
            var activity = Platform.CurrentActivity as FragmentActivity;
            if (activity is null)
            {
                tcs.TrySetResult(false);
                return tcs.Task;
            }

            activity.RunOnUiThread(() =>
            {
                try
                {
                    var executor = ContextCompat.GetMainExecutor(activity)
                        ?? new MainThreadExecutor();
                    var callback = new BiyometrikCallback(tcs);
                    var prompt = new BiometricPrompt(activity, executor, callback);
                    var info = new BiometricPrompt.PromptInfo.Builder()
                        .SetTitle("Satınalma Pro")
                        .SetSubtitle(mesaj)
                        .SetNegativeButtonText("İptal")
                        .SetAllowedAuthenticators(DesteklenenAuthenticators)
                        .Build();
                    prompt.Authenticate(info);
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Biyometrik prompt: {ex.Message}");
                    tcs.TrySetResult(false);
                }
            });
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Biyometrik doğrulama: {ex.Message}");
            tcs.TrySetResult(false);
        }

        return tcs.Task;
    }

    private sealed class MainThreadExecutor : Java.Lang.Object, Java.Util.Concurrent.IExecutor
    {
        public void Execute(Java.Lang.IRunnable? command)
        {
            if (command is null)
                return;
            MainThread.BeginInvokeOnMainThread(() => command.Run());
        }
    }

    private sealed class BiyometrikCallback(TaskCompletionSource<bool> tcs) : BiometricPrompt.AuthenticationCallback
    {
        public override void OnAuthenticationSucceeded(BiometricPrompt.AuthenticationResult result) =>
            tcs.TrySetResult(true);

        public override void OnAuthenticationError(int errorCode, ICharSequence? errString) =>
            tcs.TrySetResult(false);

        public override void OnAuthenticationFailed()
        {
            // Kullanıcı tekrar deneyebilir
        }
    }
}
#else
namespace SatinalmaPro.Mobile.Services;

public sealed class BiyometrikKimlikServisi : IBiyometrikKimlikServisi
{
    public Task<bool> KullanilabilirMiAsync() => Task.FromResult(false);
    public Task<bool> DogrulaAsync(string mesaj = "Kimliğinizi doğrulayın") => Task.FromResult(false);
}
#endif

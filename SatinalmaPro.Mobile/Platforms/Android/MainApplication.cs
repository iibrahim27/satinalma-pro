using Android.App;
using Android.Runtime;
using Firebase.Messaging;

namespace SatinalmaPro.Mobile;

[Application]
public class MainApplication : MauiApplication
{
	public MainApplication(IntPtr handle, JniHandleOwnership ownership)
		: base(handle, ownership)
	{
		try
		{
			FirebaseMessaging.Instance.AutoInitEnabled = false;
		}
		catch
		{
			// google-services.json yoksa sessiz
		}
	}

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}

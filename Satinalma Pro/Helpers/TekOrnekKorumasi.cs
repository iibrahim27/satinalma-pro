using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

namespace SatinalmaPro.Helpers;

public static class TekOrnekKorumasi
{
    private const string MutexAdi = "SatinalmaPro_TekOrnek_Mutex";
    public const string GuncellemeSonrasiArg = "--guncelleme-sonrasi";
    private const string ToastAktivasyonArg = "-ToastActivated";
    private const int SwRestore = 9;

    private static Mutex? _mutex;

    public static bool GuncellemeSonrasiMi(IEnumerable<string> args) =>
        args.Any(a => a.Equals(GuncellemeSonrasiArg, StringComparison.OrdinalIgnoreCase));

    public static bool ToastAktivasyonuMu(IEnumerable<string>? args) =>
        (args ?? []).Any(a => a.Equals(ToastAktivasyonArg, StringComparison.OrdinalIgnoreCase));

    public static bool IlkOrnekMi(IEnumerable<string>? args = null)
    {
        var argumanlar = args ?? [];

        if (GuncellemeSonrasiMi(argumanlar))
        {
            for (var deneme = 0; deneme < 60; deneme++)
            {
                _mutex?.Dispose();
                _mutex = new Mutex(true, MutexAdi, out var ilkOrnek);
                if (ilkOrnek)
                    return true;

                try { _mutex.ReleaseMutex(); } catch (ApplicationException) { /* yoksay */ }
                _mutex.Dispose();
                _mutex = null;
                Thread.Sleep(500);
            }

            return true;
        }

        _mutex = new Mutex(true, MutexAdi, out var ilk);
        if (!ilk && ToastAktivasyonuMu(argumanlar))
            return false;

        return ilk;
    }

    public static void SerbestBirak()
    {
        if (_mutex is null)
            return;

        try
        {
            _mutex.ReleaseMutex();
        }
        catch (ApplicationException)
        {
            // Mutex bu işlem tarafından sahiplenilmemiş olabilir.
        }

        _mutex.Dispose();
        _mutex = null;
    }

    public static void MevcutPencereyiOneGetir()
    {
        var simdiki = Process.GetCurrentProcess();
        foreach (var surec in Process.GetProcessesByName(simdiki.ProcessName))
        {
            if (surec.Id == simdiki.Id)
                continue;

            var tutamac = surec.MainWindowHandle;
            if (tutamac == IntPtr.Zero)
                continue;

            ShowWindow(tutamac, SwRestore);
            SetForegroundWindow(tutamac);
            break;
        }
    }

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
}

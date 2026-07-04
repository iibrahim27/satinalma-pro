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
    private const int SwShow = 5;

    private static Mutex? _mutex;
    private static bool _sonPencereBulundu;

    static TekOrnekKorumasi()
    {
        OneGetirMesaji = RegisterWindowMessage("SatinalmaPro_WPF_OneGetir_v1");
    }

    public static int OneGetirMesaji { get; }

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

    /// <summary>Çalışan sürece WPF Show/Activate sinyali gönderir (gizli pencere için).</summary>
    public static bool IkinciOrnekSinyaliGonder()
    {
        var simdiki = Process.GetCurrentProcess();
        var gonderildi = false;

        foreach (var surec in Process.GetProcessesByName(simdiki.ProcessName))
        {
            if (surec.Id == simdiki.Id)
                continue;

            try
            {
                EnumWindows((hwnd, _) =>
                {
                    GetWindowThreadProcessId(hwnd, out var pencerePid);
                    if (pencerePid != surec.Id)
                        return true;

                    SendMessage(hwnd, OneGetirMesaji, IntPtr.Zero, IntPtr.Zero);
                    gonderildi = true;
                    return true;
                }, IntPtr.Zero);
            }
            catch
            {
                // yoksay
            }
        }

        return gonderildi;
    }

    /// <summary>Mevcut örneği öne getirir. Pencere bulunamazsa takılı süreç olabilir.</summary>
    public static bool MevcutPencereyiOneGetir()
    {
        _sonPencereBulundu = false;
        var simdiki = Process.GetCurrentProcess();

        foreach (var surec in Process.GetProcessesByName(simdiki.ProcessName))
        {
            if (surec.Id == simdiki.Id)
                continue;

            try
            {
                var tutamac = surec.MainWindowHandle;
                if (tutamac == IntPtr.Zero)
                    tutamac = IlkPencereTutamaciniBul(surec.Id, sadeceGorunur: false);
                else if (!IsWindowVisible(tutamac) && IlkPencereTutamaciniBul(surec.Id, sadeceGorunur: true) == IntPtr.Zero)
                {
                    // Gizli ana pencere — yine de göster
                }

                if (tutamac == IntPtr.Zero)
                    continue;

                _sonPencereBulundu = true;
                if (IsIconic(tutamac))
                    ShowWindow(tutamac, SwRestore);
                else
                    ShowWindow(tutamac, SwShow);

                SetForegroundWindow(tutamac);
                return true;
            }
            catch
            {
                // Süreç kapanmış olabilir.
            }
        }

        return false;
    }

    public static bool SonPencereBulundu => _sonPencereBulundu;

    /// <summary>Görünür pencere yok ama süreç yaşıyorsa — arka planda takılı kalmış olabilir.</summary>
    public static bool TakiliSurecVarMi()
    {
        var simdiki = Process.GetCurrentProcess();
        foreach (var surec in Process.GetProcessesByName(simdiki.ProcessName))
        {
            if (surec.Id == simdiki.Id)
                continue;

            try
            {
                if (surec.HasExited)
                    continue;

                if (surec.MainWindowHandle != IntPtr.Zero || HerhangiPencereVarMi(surec.Id))
                    continue;

                return true;
            }
            catch
            {
                // yoksay
            }
        }

        return false;
    }

    public static bool TakiliSureciSonlandir()
    {
        var simdiki = Process.GetCurrentProcess();
        var sonlandirildi = false;

        foreach (var surec in Process.GetProcessesByName(simdiki.ProcessName))
        {
            if (surec.Id == simdiki.Id)
                continue;

            try
            {
                if (surec.HasExited)
                    continue;

                if (surec.MainWindowHandle != IntPtr.Zero || HerhangiPencereVarMi(surec.Id))
                    continue;

                surec.Kill(entireProcessTree: true);
                surec.WaitForExit(3000);
                sonlandirildi = true;
            }
            catch
            {
                // yoksay
            }
        }

        return sonlandirildi;
    }

    private static IntPtr IlkPencereTutamaciniBul(int pid, bool sadeceGorunur = true)
    {
        IntPtr bulunan = IntPtr.Zero;

        EnumWindows((hwnd, _) =>
        {
            GetWindowThreadProcessId(hwnd, out var pencerePid);
            if (pencerePid != pid)
                return true;

            if (sadeceGorunur && !IsWindowVisible(hwnd))
                return true;

            bulunan = hwnd;
            return false;
        }, IntPtr.Zero);

        return bulunan;
    }

    private static bool HerhangiPencereVarMi(int pid)
    {
        var var = false;

        EnumWindows((hwnd, _) =>
        {
            GetWindowThreadProcessId(hwnd, out var pencerePid);
            if (pencerePid != pid)
                return true;

            var = true;
            return false;
        }, IntPtr.Zero);

        return var;
    }

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int RegisterWindowMessage(string lpString);

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
}

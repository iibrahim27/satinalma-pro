using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace SatinalmaPro.Helpers;

/// <summary>İkinci örnek açıldığında çalışan sürece WPF penceresini güvenle gösterir.</summary>
public static class PencereOneGetirDinleyicisi
{
    private static readonly HashSet<IntPtr> BagliKaynaklar = [];

    public static void Bagla(Window pencere)
    {
        if (pencere.IsLoaded)
            KaynakBagla(pencere);
        else
            pencere.SourceInitialized += (_, _) => KaynakBagla(pencere);
    }

    private static void KaynakBagla(Window pencere)
    {
        if (PresentationSource.FromVisual(pencere) is not HwndSource kaynak)
            return;

        if (!BagliKaynaklar.Add(kaynak.Handle))
            return;

        kaynak.AddHook(WndProc);
    }

    private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != TekOrnekKorumasi.OneGetirMesaji)
            return IntPtr.Zero;

        Application.Current?.Dispatcher.BeginInvoke(PencereyiOneGetir);
        handled = true;
        return IntPtr.Zero;
    }

    private static void PencereyiOneGetir()
    {
        Window? hedef = Application.Current?.MainWindow;
        if (hedef is null || !hedef.IsLoaded)
        {
            hedef = Application.Current?.Windows.OfType<Window>()
                .FirstOrDefault(w => w.IsLoaded);
        }

        if (hedef is null)
            return;

        if (hedef.WindowState == WindowState.Minimized)
            hedef.WindowState = WindowState.Normal;

        hedef.ShowInTaskbar = true;
        hedef.Show();
        hedef.Activate();
        hedef.Topmost = true;
        hedef.Topmost = false;
        hedef.Focus();
    }
}

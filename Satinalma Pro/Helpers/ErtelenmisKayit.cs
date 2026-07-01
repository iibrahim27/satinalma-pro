using System.Windows.Threading;

namespace SatinalmaPro.Helpers;

public static class ErtelenmisKayit
{
    private static readonly Dictionary<string, Action> _bekleyen = new(StringComparer.Ordinal);
    private static DispatcherTimer? _timer;
    private static int _topluIslem;

    public static void BeginBatch() => Interlocked.Increment(ref _topluIslem);

    public static void EndBatch()
    {
        if (Interlocked.Decrement(ref _topluIslem) <= 0 && _bekleyen.Count > 0)
            HemenCalistir();
    }

    public static void Planla(string anahtar, Action kaydet)
    {
        _bekleyen[anahtar] = kaydet;

        if (_topluIslem > 0)
            return;

        ZamanlayiciBaslat();
    }

    public static void HemenCalistir()
    {
        _timer?.Stop();

        if (_bekleyen.Count == 0)
            return;

        foreach (var kaydet in _bekleyen.Values.ToList())
            kaydet();

        _bekleyen.Clear();
    }

    private static void ZamanlayiciBaslat()
    {
        _timer ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(750) };
        _timer.Tick -= ZamanlayiciTik;
        _timer.Tick += ZamanlayiciTik;
        _timer.Stop();
        _timer.Start();
    }

    private static void ZamanlayiciTik(object? sender, EventArgs e)
    {
        _timer?.Stop();
        HemenCalistir();
    }
}

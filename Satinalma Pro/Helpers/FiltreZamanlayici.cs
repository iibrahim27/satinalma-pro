using System.Windows.Threading;

namespace SatinalmaPro.Helpers;

public sealed class FiltreZamanlayici
{
    private readonly DispatcherTimer _timer;
    private readonly Action _calistir;

    public FiltreZamanlayici(Action calistir, int gecikmeMs = 350)
    {
        _calistir = calistir;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(gecikmeMs) };
        _timer.Tick += (_, _) =>
        {
            _timer.Stop();
            _calistir();
        };
    }

    public void Tetikle()
    {
        _timer.Stop();
        _timer.Start();
    }

    public void Hemen()
    {
        _timer.Stop();
        _calistir();
    }
}

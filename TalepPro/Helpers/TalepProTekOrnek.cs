using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace TalepPro.Helpers;

public static class TalepProTekOrnek
{
    public const string ArkaPlanBaslatArg = "--arka-plan";

    private const string MutexAdi = "TalepPro_TekOrnek_Mutex";
    private const string PipeAdi = "TalepPro_DeepLink_Pipe_v1";

    private static Mutex? _mutex;
    private static CancellationTokenSource? _pipeCts;

    public static bool ArkaPlanBaslatMi(IEnumerable<string>? args) =>
        (args ?? []).Any(a => a.Equals(ArkaPlanBaslatArg, StringComparison.OrdinalIgnoreCase));

    public static bool IlkOrnekMi(IEnumerable<string>? args = null)
    {
        _mutex = new Mutex(true, MutexAdi, out var ilk);
        return ilk;
    }

    public static void SerbestBirak()
    {
        try { _pipeCts?.Cancel(); } catch { /* ignore */ }
        _pipeCts = null;
        if (_mutex is null) return;
        try { _mutex.ReleaseMutex(); } catch (ApplicationException) { /* ignore */ }
        _mutex.Dispose();
        _mutex = null;
    }

    public static void IkinciOrnekSinyaliGonder(IEnumerable<string>? args)
    {
        try
        {
            using var client = new NamedPipeClientStream(".", PipeAdi, PipeDirection.Out);
            client.Connect(800);
            var payload = string.Join('\n', args ?? []);
            var bytes = Encoding.UTF8.GetBytes(payload);
            client.Write(bytes);
            client.Flush();
        }
        catch
        {
            // İlk örnek giriş/açılış aşamasındaysa pipe henüz dinlemiyor olabilir
        }
    }

    public static void OneGetirDinleyicisiniKur(Window pencere, Action<string[]> deepLink)
    {
        _pipeCts = new CancellationTokenSource();
        var ct = _pipeCts.Token;
        _ = Task.Run(async () =>
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await using var server = new NamedPipeServerStream(
                        PipeAdi, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                    await server.WaitForConnectionAsync(ct);
                    using var ms = new MemoryStream();
                    await server.CopyToAsync(ms, ct);
                    var text = Encoding.UTF8.GetString(ms.ToArray());
                    var arglar = text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                        .Where(a => !a.Equals(ArkaPlanBaslatArg, StringComparison.OrdinalIgnoreCase))
                        .ToArray();
                    await pencere.Dispatcher.InvokeAsync(() =>
                    {
                        TalepProTepsiYoneticisi.Goster();
                        deepLink(arglar);
                    }, DispatcherPriority.Normal);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    await Task.Delay(300, ct);
                }
            }
        }, ct);
    }
}

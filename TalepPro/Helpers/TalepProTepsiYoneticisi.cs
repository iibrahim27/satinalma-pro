using System.Diagnostics;
using System.Windows;
using WinForms = System.Windows.Forms;
using Drawing = System.Drawing;

namespace TalepPro.Helpers;

/// <summary>Pencere kapatıldığında Talep Pro'yu sistem tepsisinde tutar.</summary>
public static class TalepProTepsiYoneticisi
{
    private static WinForms.NotifyIcon? _ikon;
    private static Window? _pencere;

    public static bool TepsideGizli { get; private set; }

    public static void Bagla(Window pencere)
    {
        _pencere = pencere;
        if (_ikon is not null)
            return;

        _ikon = new WinForms.NotifyIcon
        {
            Icon = UygulamaIkonu(),
            Text = "Talep Pro",
            Visible = true
        };

        var menu = new WinForms.ContextMenuStrip();
        menu.Items.Add("Talep Pro'yu Göster", null, (_, _) => Goster());
        menu.Items.Add("Satınalma Pro'yu Aç", null, (_, _) =>
            SatinalmaPro.Helpers.UygulamaKoordinasyonu.SatinalmaProModulAc(null));
        menu.Items.Add("Uygulamadan Çık", null, (_, _) => TamamenKapatTiklandi());
        _ikon.ContextMenuStrip = menu;
        _ikon.DoubleClick += (_, _) => Goster();
    }

    public static void TepsiyeGizle(bool bildirimGoster = true)
    {
        if (_pencere is null)
            return;

        TepsideGizli = true;
        _pencere.ShowInTaskbar = false;
        _pencere.Hide();

        if (!bildirimGoster)
            return;

        _ikon?.ShowBalloonTip(
            2500,
            "Talep Pro",
            "Arka planda çalışmaya devam ediyor.",
            WinForms.ToolTipIcon.Info);
    }

    public static void Goster()
    {
        if (_pencere is null)
            return;

        TepsideGizli = false;
        _pencere.ShowInTaskbar = true;

        if (_pencere.WindowState == WindowState.Minimized)
            _pencere.WindowState = WindowState.Normal;

        _pencere.Show();
        _pencere.Activate();
        _pencere.Topmost = true;
        _pencere.Topmost = false;
        _pencere.Focus();
    }

    public static void Temizle()
    {
        if (_ikon is null)
            return;

        _ikon.Visible = false;
        _ikon.Dispose();
        _ikon = null;
        TepsideGizli = false;
        _pencere = null;
    }

    private static void TamamenKapatTiklandi()
    {
        if (_pencere is MainWindow ana)
            ana.TamamenKapatIstendi();
        else
            Application.Current.Shutdown();
    }

    private static Drawing.Icon UygulamaIkonu()
    {
        try
        {
            var exe = Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrWhiteSpace(exe))
            {
                var ikon = Drawing.Icon.ExtractAssociatedIcon(exe);
                if (ikon is not null)
                    return ikon;
            }
        }
        catch
        {
            // yoksay
        }

        return Drawing.SystemIcons.Application;
    }
}

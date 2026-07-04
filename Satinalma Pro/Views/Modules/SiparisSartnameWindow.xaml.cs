using System.Windows;
using SatinalmaPro.Services;

namespace SatinalmaPro.Views.Modules;

public partial class SiparisSartnameWindow : Window
{
    public string OnaylananMetin { get; private set; } = "";

    public SiparisSartnameWindow(string varsayilanMetin, string? talepNo = null)
    {
        InitializeComponent();

        TxtSartname.Text = varsayilanMetin ?? "";
        TxtAlt.Text = string.IsNullOrWhiteSpace(talepNo)
            ? "Ayarlardaki şartname metni aşağıda önizlenir. Düzenleyip sipariş PDF'ine ekleyebilirsiniz."
            : $"Talep {talepNo} — Ayarlardaki şartname metnini düzenleyin; onayladığınız metin sipariş PDF'ine yazılır.";

        Loaded += (_, _) => TxtSartname.Focus();
    }

    public static string? DuzenleVeOnayla(string varsayilanMetin, string? talepNo = null)
    {
        var pencere = new SiparisSartnameWindow(varsayilanMetin, talepNo)
        {
            Owner = Application.Current?.MainWindow
        };

        return pencere.ShowDialog() == true ? pencere.OnaylananMetin : null;
    }

    private void Iptal_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void KaydetVeDevam_Click(object sender, RoutedEventArgs e)
    {
        OnaylananMetin = TxtSartname.Text.Trim();

        if (ChkAyarlaraKaydet.IsChecked == true)
        {
            SatinalmaDepo.Ayarlar.SartnameMetni = OnaylananMetin;
            SatinalmaDepo.Kaydet();
        }

        DialogResult = true;
        Close();
    }
}

using System.Windows;
using System.Windows.Controls;
using SatinalmaPro.Helpers;

namespace SatinalmaPro.Views.Controls;

public partial class SayfalamaCubugu : UserControl
{
    public event Action? IlkTiklandi;
    public event Action? OncekiTiklandi;
    public event Action? SonrakiTiklandi;
    public event Action? SonTiklandi;

    public SayfalamaCubugu()
    {
        InitializeComponent();
    }

    public void Guncelle(int guncelSayfa, int toplamSayfa, int toplamKayit)
    {
        TxtSayfa.Text = toplamKayit == 0
            ? "Kayıt yok"
            : $"Sayfa {guncelSayfa} / {toplamSayfa}  ·  {toplamKayit} kayıt  (sayfa başına {ModulSayfalama.SayfaBoyutu})";

        var tekSayfa = toplamSayfa <= 1;
        BtnIlk.IsEnabled = !tekSayfa && guncelSayfa > 1;
        BtnOnceki.IsEnabled = !tekSayfa && guncelSayfa > 1;
        BtnSonraki.IsEnabled = !tekSayfa && guncelSayfa < toplamSayfa;
        BtnSon.IsEnabled = !tekSayfa && guncelSayfa < toplamSayfa;

        Visibility = toplamKayit <= ModulSayfalama.SayfaBoyutu ? Visibility.Collapsed : Visibility.Visible;
    }

    private void Ilk_Click(object sender, RoutedEventArgs e) => IlkTiklandi?.Invoke();
    private void Onceki_Click(object sender, RoutedEventArgs e) => OncekiTiklandi?.Invoke();
    private void Sonraki_Click(object sender, RoutedEventArgs e) => SonrakiTiklandi?.Invoke();
    private void Son_Click(object sender, RoutedEventArgs e) => SonTiklandi?.Invoke();
}

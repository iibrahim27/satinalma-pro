using System.Windows;
using SatinalmaPro.Helpers;
using SatinalmaPro.Services;

namespace SatinalmaPro.Views;

public partial class GirisPenceresi : Window
{
    public bool GirisTamamlandi { get; private set; }

    public GirisPenceresi()
    {
        InitializeComponent();
        GirisKontrol.GirisBasarili += () =>
        {
            GirisTamamlandi = true;
            DialogResult = true;
            Close();
        };
    }

    public static bool OturumAc(Window? sahip)
    {
        if (!OturumYoneticisi.BulutAktif)
        {
            MessageBox.Show(
                "Firebase yapılandırılmamış — yerel mod aktif.\n\n" +
                "• Veriler yalnızca bu bilgisayarda saklanır\n" +
                "• Mobil uygulama ile senkron olmaz\n" +
                "• Tüm modül yetkileri açıktır\n\n" +
                "Bulut kurulumu için: Ayarlar → Genel → Kurulum Kılavuzunu Aç",
                UygulamaBilgisi.Ad,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return true;
        }

        var pencere = new GirisPenceresi();
        if (sahip is not null)
            pencere.Owner = sahip;

        return pencere.ShowDialog() == true && pencere.GirisTamamlandi;
    }
}

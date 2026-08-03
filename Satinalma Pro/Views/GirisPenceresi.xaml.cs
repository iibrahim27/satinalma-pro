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
        Loaded += (_, _) => GirisKontrol.TercihleriYukle();
    }

    public void MarkayiAyarla(string pencereBasligi, string marka, string altBaslik, string aciklama)
    {
        Title = pencereBasligi;
        TxtMarka.Text = marka;
        TxtAltBaslik.Text = altBaslik;
        TxtAciklama.Text = aciklama;
    }

    public static bool OturumAc(Window? sahip) =>
        OturumAc(sahip, null, null, null, null);

    public static bool OturumAc(
        Window? sahip,
        string? pencereBasligi,
        string? marka,
        string? altBaslik,
        string? aciklama)
    {
        if (!OturumYoneticisi.BulutAktif)
        {
            MessageBox.Show(
                "Firebase yapılandırılmamış — yerel mod aktif.\n\n" +
                "• Veriler yalnızca bu bilgisayarda saklanır\n" +
                "• Mobil uygulama ile senkron olmaz\n" +
                "• Tüm modül yetkileri açıktır\n\n" +
                "Bulut kurulumu için: Ayarlar → Genel → Kurulum Kılavuzunu Aç",
                marka ?? UygulamaBilgisi.Ad,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return true;
        }

        var pencere = new GirisPenceresi();
        if (!string.IsNullOrWhiteSpace(pencereBasligi) ||
            !string.IsNullOrWhiteSpace(marka))
        {
            pencere.MarkayiAyarla(
                pencereBasligi ?? "Talep Pro — Giriş",
                marka ?? "Talep Pro",
                altBaslik ?? "Talep, teklif ve onay süreçleri",
                aciklama ?? "Satınalma taleplerinizi buradan yönetin.");
        }

        if (sahip is not null)
            pencere.Owner = sahip;

        return pencere.ShowDialog() == true && pencere.GirisTamamlandi;
    }
}

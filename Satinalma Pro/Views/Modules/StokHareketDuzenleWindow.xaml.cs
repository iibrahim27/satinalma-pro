using System.Globalization;
using System.Windows;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;

namespace SatinalmaPro.Views.Modules;

public partial class StokHareketDuzenleWindow : Window
{
    private readonly StokHareketKaydi _hareket;

    public StokHareketDuzenleWindow(StokHareketKaydi hareket)
    {
        InitializeComponent();
        _hareket = hareket;

        TxtBaslik.Text = $"{hareket.HareketTipi} Düzenle";
        TxtAltBaslik.Text = $"{hareket.MalzemeAdi} — {hareket.DepoSaha}";
        TxtTarih.Text = hareket.Tarih;
        TxtMalzeme.Text = hareket.MalzemeAdi;
        TxtDepo.Text = hareket.DepoSaha;
        TxtBirim.Text = hareket.Birim;
        TxtBelge.Text = hareket.BelgeNo;
        TxtIslemYapan.Text = hareket.IslemYapan;
        TxtAciklama.Text = hareket.Aciklama;

        if (hareket.HareketTipi == StokHareketTipleri.Sayim)
        {
            LblMiktar.Text = "Sayım Miktarı";
            TxtMiktar.Text = hareket.SayimMiktar?.ToString(CultureInfo.CurrentCulture) ?? hareket.Miktar.ToString(CultureInfo.CurrentCulture);
        }
        else
        {
            TxtMiktar.Text = hareket.Miktar.ToString(CultureInfo.CurrentCulture);
        }
    }

    private void Kaydet_Click(object sender, RoutedEventArgs e)
    {
        if (!double.TryParse(TxtMiktar.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var miktar) || miktar < 0)
        {
            MessageBox.Show("Geçerli bir miktar girin.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            StokIslemServisi.HareketGuncelle(
                _hareket,
                TxtTarih.Text.Trim(),
                miktar,
                TxtBelge.Text.Trim(),
                TxtIslemYapan.Text.Trim(),
                TxtAciklama.Text.Trim());

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Iptal_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

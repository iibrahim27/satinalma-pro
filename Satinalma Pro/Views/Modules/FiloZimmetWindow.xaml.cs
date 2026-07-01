using System.Globalization;
using System.Windows;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;

namespace SatinalmaPro.Views.Modules;

public partial class FiloZimmetWindow : Window
{
    private readonly FiloAracKaydi _arac;

    public FiloZimmetWindow(FiloAracKaydi arac)
    {
        InitializeComponent();
        _arac = arac;
        TxtBaslik.Text = $"{arac.Plaka} — Zimmet";
    }

    private void Zimmetle_Click(object sender, RoutedEventArgs e)
    {
        var sofor = TxtSofor.Text.Trim();
        if (string.IsNullOrWhiteSpace(sofor))
        {
            MessageBox.Show("Şoför / operatör adı zorunludur.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var pdfYolu = FiloFormPdfOlusturucu.ZimmetFormuKaydet(_arac, sofor);
        if (string.IsNullOrWhiteSpace(pdfYolu))
        {
            MessageBox.Show("Zimmet PDF oluşturulamadı.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        ModulVeriDeposu.FiloZimmetleri.Add(new FiloZimmetKaydi
        {
            Plaka = _arac.Plaka,
            SoforAdi = sofor,
            Tarih = DateTime.Now.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
            Aktif = true,
            PdfDosyaYolu = pdfYolu
        });

        ModulVeriDeposu.KaydetFilo();
        DialogResult = true;
        Close();
    }

    private void Iptal_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

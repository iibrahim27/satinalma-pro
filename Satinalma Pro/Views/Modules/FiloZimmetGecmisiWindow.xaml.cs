using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SatinalmaPro.Models;
using SatinalmaPro.Services;

namespace SatinalmaPro.Views.Modules;

public partial class FiloZimmetGecmisiWindow : Window
{
    private readonly FiloAracKaydi _arac;

    public FiloZimmetGecmisiWindow(FiloAracKaydi arac)
    {
        InitializeComponent();
        _arac = arac;
        TxtBaslik.Text = $"{arac.Plaka} — Zimmet PDF Geçmişi";
        TxtKlasor.Text = $"PDF klasörü: {FiloZimmetPdfDeposu.ZimmetKlasoru}";
        ListeyiYenile();
    }

    private void ListeyiYenile()
    {
        var kayitlar = ModulVeriDeposu.FiloZimmetleri
            .Where(z => z.Plaka.Equals(_arac.Plaka, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(z => z.Tarih)
            .ThenByDescending(z => z.SoforAdi)
            .ToList();

        ZimmetGrid.ItemsSource = kayitlar;
        ButonlariGuncelle();
    }

    private FiloZimmetKaydi? SeciliZimmet() => ZimmetGrid.SelectedItem as FiloZimmetKaydi;

    private void ZimmetGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        ButonlariGuncelle();

    private void ZimmetGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e) =>
        PdfAc_Click(sender, e);

    private void ButonlariGuncelle()
    {
        var secili = SeciliZimmet();
        var pdfVar = secili is not null && FiloZimmetPdfDeposu.MevcutMu(secili.PdfDosyaYolu);
        BtnPdfAc.IsEnabled = pdfVar;
        BtnPdfYazdir.IsEnabled = pdfVar;
    }

    private void PdfAc_Click(object sender, RoutedEventArgs e)
    {
        if (SeciliZimmet() is not { } zimmet)
            return;

        FiloFormPdfOlusturucu.ZimmetPdfAc(zimmet.PdfDosyaYolu);
    }

    private void PdfYazdir_Click(object sender, RoutedEventArgs e)
    {
        if (SeciliZimmet() is not { } zimmet)
            return;

        FiloFormPdfOlusturucu.ZimmetPdfYazdir(zimmet.PdfDosyaYolu);
    }

    private void KlasoruAc_Click(object sender, RoutedEventArgs e) =>
        FiloZimmetPdfDeposu.KlasoruAc();

    private void Kapat_Click(object sender, RoutedEventArgs e) => Close();
}

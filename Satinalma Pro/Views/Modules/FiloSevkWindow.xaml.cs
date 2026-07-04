using System.Windows;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;

namespace SatinalmaPro.Views.Modules;

public partial class FiloSevkWindow : Window
{
    private readonly FiloAracKaydi _arac;

    public FiloSevkWindow(FiloAracKaydi arac)
    {
        InitializeComponent();
        _arac = arac;
        TxtBaslik.Text = $"{arac.Plaka} — Sevk";
        TxtKaynakSaha.Text = arac.Saha;
    }

    private void Sevk_Click(object sender, RoutedEventArgs e)
    {
        var hedef = TxtHedefSaha.Text.Trim();
        if (string.IsNullOrWhiteSpace(hedef))
        {
            MessageBox.Show("Hedef şantiye / saha zorunludur.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var kaynak = TxtKaynakSaha.Text.Trim();
        var aciklama = TxtAciklama.Text.Trim();

        FiloFormPdfOlusturucu.SevkFormuOlustur(_arac, kaynak, hedef, aciklama);

        _arac.Saha = hedef;
        _arac.Durum = "Pasif";
        if (!string.IsNullOrWhiteSpace(aciklama))
            _arac.Aciklama = string.IsNullOrWhiteSpace(_arac.Aciklama)
                ? $"Sevk: {aciklama}"
                : $"{_arac.Aciklama} | Sevk: {aciklama}";

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

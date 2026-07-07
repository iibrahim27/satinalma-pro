using System.Windows;
using System.Windows.Controls;
using SatinalmaPro.Services;

namespace SatinalmaPro.Helpers;

/// <summary>Agrega / Çimento vb. ERP modül filtrelerini oturum boyunca korur.</summary>
public static class ErpModulFiltreYardimcisi
{
    public static ErpModulFiltreDurumu Olustur(
        DatePicker baslangic,
        DatePicker bitis,
        ComboBox tedarikci,
        List<string> cinsSecimleri,
        ComboBox tur,
        ComboBox santiye,
        TextBox teslimAlan,
        TextBox irsaliyeNo,
        TextBox gridArama,
        bool filtrePanelAcik,
        double filtrePanelYukseklik) =>
        new()
        {
            Baslangic = baslangic.SelectedDate,
            Bitis = bitis.SelectedDate,
            Tedarikci = ComboMetin(tedarikci),
            CinsSecimleri = [.. cinsSecimleri],
            Tur = ComboMetin(tur),
            Santiye = ComboMetin(santiye),
            TeslimAlan = teslimAlan.Text,
            IrsaliyeNo = irsaliyeNo.Text,
            GridArama = gridArama.Text,
            FiltrePanelAcik = filtrePanelAcik,
            FiltrePanelYukseklik = filtrePanelYukseklik
        };

    public static void Uygula(
        ErpModulFiltreDurumu durum,
        DatePicker baslangic,
        DatePicker bitis,
        ComboBox tedarikci,
        List<string> cinsSecimleri,
        ComboBox tur,
        ComboBox santiye,
        TextBox teslimAlan,
        TextBox irsaliyeNo,
        TextBox gridArama,
        ref bool filtrePanelAcik,
        ref double filtrePanelYukseklik)
    {
        baslangic.SelectedDate = durum.Baslangic;
        bitis.SelectedDate = durum.Bitis;
        ComboSec(tedarikci, durum.Tedarikci);
        cinsSecimleri.Clear();
        cinsSecimleri.AddRange(durum.CinsSecimleri);
        ComboSec(tur, durum.Tur);
        ComboSec(santiye, durum.Santiye);
        teslimAlan.Text = durum.TeslimAlan;
        irsaliyeNo.Text = durum.IrsaliyeNo;
        gridArama.Text = durum.GridArama;
        filtrePanelAcik = durum.FiltrePanelAcik;
        filtrePanelYukseklik = durum.FiltrePanelYukseklik > 0 ? durum.FiltrePanelYukseklik : filtrePanelYukseklik;
    }

    public static void FiltrePanelGorunumunuUygula(Border kap, bool acik, double yukseklik, TextBlock toggleMetin, TextBlock toggleIkon)
    {
        kap.BeginAnimation(FrameworkElement.MaxHeightProperty, null);

        if (acik && yukseklik > 0)
            kap.MaxHeight = yukseklik;
        else
            kap.MaxHeight = 0;

        toggleMetin.Text = acik ? "Filtreleri Gizle" : "Filtreleri Göster";
        toggleIkon.Text = acik ? "\uE70E" : "\uE70D";
    }

    private static string ComboMetin(ComboBox combo) =>
        (combo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? combo.Text;

    private static void ComboSec(ComboBox combo, string? deger)
    {
        if (string.IsNullOrWhiteSpace(deger))
        {
            combo.SelectedIndex = 0;
            return;
        }

        for (var i = 0; i < combo.Items.Count; i++)
        {
            if ((combo.Items[i] as ComboBoxItem)?.Content?.ToString() == deger)
            {
                combo.SelectedIndex = i;
                return;
            }
        }
    }
}

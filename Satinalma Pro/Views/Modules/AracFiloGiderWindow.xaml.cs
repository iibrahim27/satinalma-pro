using System.Globalization;

using System.Windows;

using System.Windows.Controls;

using System.Windows.Input;

using SatinalmaPro.Helpers;

using SatinalmaPro.Models;

using SatinalmaPro.Services;



namespace SatinalmaPro.Views.Modules;



public partial class AracFiloGiderWindow : Window

{

    private readonly FiloAracKaydi _arac;

    private readonly List<FiloGiderKaydi> _giderler;



    public AracFiloGiderWindow(FiloAracKaydi arac)

    {

        InitializeComponent();

        _arac = arac;

        _giderler = ModulVeriDeposu.FiloGiderleri

            .Where(g => g.Plaka.Equals(arac.Plaka, StringComparison.OrdinalIgnoreCase))

            .ToList();

        TxtBaslik.Text = $"{arac.Plaka} — Gider Kayıtları";

        GiderleriYenile();

    }



    private void GiderleriYenile()

    {

        GiderGrid.ItemsSource = null;

        GiderGrid.ItemsSource = _giderler.OrderByDescending(g => g.Tarih).ToList();

        var toplam = _giderler.Sum(g => g.Tutar);

        TxtToplam.Text = $"Toplam gider: ₺{toplam:N2}";

        GiderButonlariniGuncelle();

    }



    private FiloGiderKaydi? SeciliGider() => GiderGrid.SelectedItem as FiloGiderKaydi;



    private void GiderGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) =>

        GiderButonlariniGuncelle();



    private void GiderButonlariniGuncelle()

    {

        var secili = SeciliGider() is not null;

        BtnGiderDuzenle.IsEnabled = secili;

        BtnGiderSil.IsEnabled = secili;

    }



    private void GiderEkle_Click(object sender, RoutedEventArgs e)

    {

        var gider = new FiloGiderKaydi

        {

            Plaka = _arac.Plaka,

            Tarih = DateTime.Now.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),

            GiderTipi = "Bakım"

        };



        var pencere = new FiloGiderDuzenleWindow(gider) { Owner = this };

        if (pencere.ShowDialog() != true)

            return;



        _giderler.Add(gider);

        ModulVeriDeposu.FiloGiderleri.Add(gider);

        GiderleriYenile();

    }



    private void GiderDuzenle_Click(object sender, RoutedEventArgs e) =>

        GiderDuzenle(SeciliGider());



    private void GiderGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e) =>

        GiderDuzenle(SeciliGider());



    private void GiderDuzenle(FiloGiderKaydi? gider)

    {

        if (gider is null)

            return;



        var pencere = new FiloGiderDuzenleWindow(gider) { Owner = this };

        if (pencere.ShowDialog() != true)

            return;



        GiderleriYenile();

    }



    private void GiderSil_Click(object sender, RoutedEventArgs e)

    {

        if (SeciliGider() is not { } gider)

            return;



        if (MessageBox.Show("Seçili gider kaydı silinsin mi?", "Gider Sil",

                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)

            return;



        _giderler.Remove(gider);

        ModulVeriDeposu.FiloGiderleri.Remove(gider);

        ModulVeriDeposu.KaydetFilo();

        GiderleriYenile();

    }



    private void Kapat_Click(object sender, RoutedEventArgs e) => Close();

}



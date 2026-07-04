using System.Windows;

using System.Windows.Controls;



namespace SatinalmaPro.Views.Modules;



public partial class RaporCokluSecWindow : Window

{

    private readonly List<SecimOgesi> _ogeler = [];



    public IReadOnlyList<string> Secilenler { get; private set; } = [];



    public RaporCokluSecWindow(string baslik, string aciklama, IEnumerable<string> tumOgeler,

        IEnumerable<string>? mevcutSecim = null)

    {

        InitializeComponent();

        Title = baslik;

        TxtBaslik.Text = baslik;

        TxtAciklama.Text = aciklama;



        var secimSet = new HashSet<string>(mevcutSecim ?? [], StringComparer.CurrentCultureIgnoreCase);

        foreach (var oge in tumOgeler.OrderBy(o => o, StringComparer.CurrentCultureIgnoreCase))

        {

            _ogeler.Add(new SecimOgesi

            {

                Ad = oge,

                Secili = secimSet.Contains(oge)

            });

        }



        ListeyiGuncelle();

    }



    private void ListeyiGuncelle(string? arama = null)

    {

        SecimListesi.Items.Clear();

        var filtre = arama?.Trim() ?? "";



        foreach (var oge in _ogeler)

        {

            if (!string.IsNullOrEmpty(filtre) &&

                !oge.Ad.Contains(filtre, StringComparison.CurrentCultureIgnoreCase))

                continue;



            var cb = new CheckBox

            {

                Content = oge.Ad,

                IsChecked = oge.Secili,

                Margin = new Thickness(6, 4, 6, 4),

                Tag = oge

            };

            cb.Checked += SecimDegisti;

            cb.Unchecked += SecimDegisti;

            SecimListesi.Items.Add(cb);

        }



        SecimSayisiniGuncelle();

    }



    private void SecimDegisti(object sender, RoutedEventArgs e)

    {

        if (sender is CheckBox cb && cb.Tag is SecimOgesi oge)

            oge.Secili = cb.IsChecked == true;



        SecimSayisiniGuncelle();

    }



    private void SecimSayisiniGuncelle()

    {

        var adet = _ogeler.Count(o => o.Secili);

        TxtSecimSayisi.Text = adet == 0 ? "Tümü (filtre yok)" : $"{adet} seçili";

    }



    private void TxtAra_TextChanged(object sender, TextChangedEventArgs e) =>

        ListeyiGuncelle(TxtAra.Text);



    private void TumunuSec_Click(object sender, RoutedEventArgs e)

    {

        var filtre = TxtAra.Text.Trim();

        foreach (var oge in _ogeler)

        {

            if (string.IsNullOrEmpty(filtre) ||

                oge.Ad.Contains(filtre, StringComparison.CurrentCultureIgnoreCase))

                oge.Secili = true;

        }



        ListeyiGuncelle(filtre);

    }



    private void Temizle_Click(object sender, RoutedEventArgs e)

    {

        foreach (var oge in _ogeler)

            oge.Secili = false;



        ListeyiGuncelle(TxtAra.Text);

    }



    private void Iptal_Click(object sender, RoutedEventArgs e)

    {

        DialogResult = false;

        Close();

    }



    private void Tamam_Click(object sender, RoutedEventArgs e)

    {

        Secilenler = _ogeler.Where(o => o.Secili).Select(o => o.Ad).ToList();

        DialogResult = true;

        Close();

    }



    private sealed class SecimOgesi

    {

        public string Ad { get; set; } = "";

        public bool Secili { get; set; }

    }

}



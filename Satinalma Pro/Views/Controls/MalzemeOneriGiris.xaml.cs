using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SatinalmaPro.Helpers;
using SatinalmaPro.Services;

namespace SatinalmaPro.Views.Controls;

/// <summary>Malzeme adı yazarken bilinen adları filtreleyip altta listeler; eşleşme yoksa serbest giriş.</summary>
public partial class MalzemeOneriGiris : UserControl
{
    public static readonly DependencyProperty MetinProperty = DependencyProperty.Register(
        nameof(Metin),
        typeof(string),
        typeof(MalzemeOneriGiris),
        new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, MetinDegisti));

    private bool _icGuncelleme;
    private Func<string, IEnumerable<string>> _ara = MalzemeAdiOneriServisi.Ara;

    public event EventHandler<string>? MetinOnaylandi;
    public event EventHandler<string>? MetinYazildi;

    public MalzemeOneriGiris()
    {
        InitializeComponent();
        OneriListesi.PreviewKeyDown += OneriListesi_PreviewKeyDown;
    }

    public string Metin
    {
        get => (string)GetValue(MetinProperty);
        set => SetValue(MetinProperty, value);
    }

    public void MetneOdaklan()
    {
        MetinKutusu.Focus();
        MetinKutusu.SelectAll();
    }

    public void OneriKaynaginiAyarla(Func<string, IEnumerable<string>> ara) =>
        _ara = ara ?? MalzemeAdiOneriServisi.Ara;

    public void MetniTemizle()
    {
        _icGuncelleme = true;
        MetinKutusu.Text = "";
        Metin = "";
        _icGuncelleme = false;
        OnerileriGizle();
    }

    private static void MetinDegisti(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not MalzemeOneriGiris ctrl || ctrl._icGuncelleme)
            return;

        var yeni = e.NewValue as string ?? "";
        if (ctrl.MetinKutusu.Text == yeni)
            return;

        ctrl._icGuncelleme = true;
        ctrl.MetinKutusu.Text = yeni;
        ctrl._icGuncelleme = false;
        ctrl.OnerileriGuncelle(yeni);
    }

    private void MetinKutusu_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_icGuncelleme)
            return;

        var metin = MetinKutusu.Text ?? "";
        _icGuncelleme = true;
        Metin = metin;
        _icGuncelleme = false;
        OnerileriGuncelle(metin);
        MetinYazildi?.Invoke(this, metin);
    }

    private void OnerileriGuncelle(string metin)
    {
        if (string.IsNullOrWhiteSpace(metin))
        {
            OnerileriGizle();
            return;
        }

        try
        {
            var liste = _ara(metin).ToList();
            if (liste.Count == 0)
            {
                OnerileriGizle();
                return;
            }

            OneriListesi.ItemsSource = liste;
            OneriListesi.SelectedIndex = -1;
            OneriPopup.IsOpen = true;
        }
        catch
        {
            OnerileriGizle();
        }
    }

    private void OneriListesi_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Klavye gezinmede yalnızca vurgu — seçim Enter veya tıklama ile
    }

    private void OneriListesi_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject kaynak)
            return;

        var oge = ItemsControl.ContainerFromElement(OneriListesi, kaynak) as ListBoxItem;
        if (oge?.Content is not string secim)
            return;

        SecimUygula(secim);
        e.Handled = true;
    }

    private void OneriListesi_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!OneriPopup.IsOpen || OneriListesi.Items.Count == 0)
            return;

        if (e.Key == Key.Enter && OneriListesi.SelectedItem is string secim)
        {
            SecimUygula(secim);
            e.Handled = true;
            return;
        }

        if (e.Key is Key.Up or Key.Down)
        {
            VurguyuKaydir(e.Key == Key.Down ? 1 : -1);
            e.Handled = true;
        }
    }

    private void SecimUygula(string secim)
    {
        _icGuncelleme = true;
        MetinKutusu.Text = secim;
        Metin = secim;
        _icGuncelleme = false;
        OneriListesi.SelectedItem = null;
        OnerileriGizle();
        MetinKutusu.Focus();
        MetinKutusu.CaretIndex = secim.Length;
        MetinOnaylandi?.Invoke(this, secim);
    }

    private void MetinKutusu_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (e.NewFocus is DependencyObject hedef)
        {
            if (AltElemanMi(OneriListesi, hedef))
                return;
            if (OneriPopup.Child is DependencyObject cerceve && AltElemanMi(cerceve, hedef))
                return;
        }

        OnerileriGizle();
        var metin = (MetinKutusu.Text ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(metin))
            MetinOnaylandi?.Invoke(this, metin);
    }

    private void MetinKutusu_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F2)
        {
            OnerileriGizle();
            var listeSecimi = MalzemeSecimWindow.Goster(Window.GetWindow(this), MetinKutusu.Text);
            if (!string.IsNullOrWhiteSpace(listeSecimi))
                SecimUygula(listeSecimi);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape)
        {
            if (OneriPopup.IsOpen)
            {
                OnerileriGizle();
                e.Handled = true;
            }
            return;
        }

        if (e.Key == Key.Down)
        {
            if (!OneriPopup.IsOpen)
                OnerileriGuncelle(MetinKutusu.Text ?? "");

            if (OneriListesi.Items.Count == 0)
                return;

            if (OneriListesi.SelectedIndex < 0)
                OneriListesi.SelectedIndex = 0;
            else
                VurguyuKaydir(1);

            e.Handled = true;
            return;
        }

        if (e.Key == Key.Up && OneriPopup.IsOpen && OneriListesi.Items.Count > 0)
        {
            VurguyuKaydir(-1);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter && OneriPopup.IsOpen && OneriListesi.SelectedItem is string secim)
        {
            SecimUygula(secim);
            e.Handled = true;
        }
    }

    private void VurguyuKaydir(int adim)
    {
        if (OneriListesi.Items.Count == 0)
            return;

        var index = OneriListesi.SelectedIndex;
        if (index < 0)
            index = adim > 0 ? 0 : OneriListesi.Items.Count - 1;
        else
            index = Math.Clamp(index + adim, 0, OneriListesi.Items.Count - 1);

        OneriListesi.SelectedIndex = index;
        OneriListesi.ScrollIntoView(OneriListesi.SelectedItem);
    }

    private void OnerileriGizle()
    {
        OneriPopup.IsOpen = false;
        OneriListesi.ItemsSource = null;
        OneriListesi.SelectedIndex = -1;
    }

    private static bool AltElemanMi(DependencyObject ust, DependencyObject? alt)
    {
        while (alt != null)
        {
            if (alt == ust)
                return true;
            alt = VisualTreeYardimcisi.GetParent(alt);
        }

        return false;
    }
}

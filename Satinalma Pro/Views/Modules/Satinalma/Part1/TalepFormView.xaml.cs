using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;
using SatinalmaPro.Views.Controls;

namespace SatinalmaPro.Views.Modules.Satinalma.Part1;

public partial class TalepFormView : UserControl
{
    public event Action? Degisti;
    public event Action? KapatIstendi;

    private SatinalmaTalep? _talep;
    private bool _duzenlenebilir = true;

    public SatinalmaTalep? AktifTalep => _talep;

    public ObservableCollection<string> Birimler { get; } = [];

    public TalepFormView()
    {
        InitializeComponent();
        DataContext = this;

        ModulVeriDeposu.Yukle();
        MalzemeBirimDeposu.VarsayilanlariHazirla();
        foreach (var birim in MalzemeBirimDeposu.Liste)
            Birimler.Add(birim);
    }

    public void Yukle(SatinalmaTalep talep, bool duzenlenebilir = true)
    {
        _talep = talep;
        _duzenlenebilir = duzenlenebilir;

        TxtTarih.Text = talep.Tarih;
        TxtTalepNo.Text = string.IsNullOrWhiteSpace(talep.TalepNo) ? "(Kayıtta atanır)" : talep.TalepNo;
        TxtTalepEden.Text = talep.TalepEden;
        TxtAciklama.Text = talep.TalepAciklamasi ?? "";

        RbAcil.IsChecked = talep.TalepTuru == TalepTurleri.Acil;
        RbOncelikli.IsChecked = talep.TalepTuru == TalepTurleri.Oncelikli;
        RbNormal.IsChecked = talep.TalepTuru is TalepTurleri.Normal or "" or null
                            || (talep.TalepTuru != TalepTurleri.Acil && talep.TalepTuru != TalepTurleri.Oncelikli);

        KalemTablosu.ItemsSource = talep.Kalemler;
        KalemTablosu.IsReadOnly = !duzenlenebilir;

        RbAcil.IsEnabled = RbOncelikli.IsEnabled = RbNormal.IsEnabled = duzenlenebilir;
        TxtAciklama.IsReadOnly = !duzenlenebilir;
        BtnSil.IsEnabled = duzenlenebilir && KullaniciYetkileri.SatinalmaTalepSilebilir(talep);
        BtnKaydet.IsEnabled = duzenlenebilir;
        BtnYazdir.IsEnabled = true;
        BtnOnayaGonder.IsEnabled = duzenlenebilir;
        TxtHata.Visibility = Visibility.Collapsed;
    }

    public void YeniTalep()
    {
        var talep = SatinalmaPart1Servisi.YeniTalepOlustur();
        Yukle(talep, duzenlenebilir: true);
    }

    private void FormdanTalebeAktar()
    {
        if (_talep is null)
            return;

        _talep.TalepAciklamasi = TxtAciklama.Text.Trim();
        _talep.TalepTuru = RbAcil.IsChecked == true
            ? TalepTurleri.Acil
            : RbOncelikli.IsChecked == true
                ? TalepTurleri.Oncelikli
                : TalepTurleri.Normal;
    }

    private void KalemTablosu_LoadingRow(object sender, DataGridRowEventArgs e)
    {
        e.Row.Loaded -= KalemSatiri_Yuklendi;
        e.Row.Loaded += KalemSatiri_Yuklendi;
    }

    private void KalemSatiri_Yuklendi(object sender, RoutedEventArgs e)
    {
        if (sender is not DataGridRow satir)
            return;

        satir.Loaded -= KalemSatiri_Yuklendi;
        var oneri = VisualTreeYardimcisi.FindDescendant<MalzemeOneriGiris>(satir);
        if (oneri is null)
            return;

        oneri.OneriKaynaginiAyarla(MalzemeAdiOneriServisi.Ara);
        oneri.IsEnabled = _duzenlenebilir;
    }

    private void SatirEkle_Click(object sender, RoutedEventArgs e)
    {
        if (_talep is null || !_duzenlenebilir)
            return;

        SatinalmaPart1Servisi.KalemEkle(_talep);
        KalemTablosu.Items.Refresh();
    }

    private void SatirSil_Click(object sender, RoutedEventArgs e)
    {
        if (_talep is null || !_duzenlenebilir)
            return;

        SatinalmaPart1Servisi.KalemSil(_talep, KalemTablosu.SelectedItem as SatinalmaTalepKalemi);
        KalemTablosu.Items.Refresh();
    }

    private async void Kaydet_Click(object sender, RoutedEventArgs e)
    {
        if (_talep is null)
            return;

        FormdanTalebeAktar();
        if (!SatinalmaPart1Servisi.GecerliMi(_talep, out var hata))
        {
            HataGoster(hata);
            return;
        }

        try
        {
            await SatinalmaPart1Servisi.KaydetAsync(_talep);
            TxtTalepNo.Text = _talep.TalepNo;
            TxtHata.Visibility = Visibility.Collapsed;
            MessageBox.Show("Talep kaydedildi.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
            Degisti?.Invoke();
        }
        catch (Exception ex)
        {
            HataGoster(ex.Message);
        }
    }

    private void Yazdir_Click(object sender, RoutedEventArgs e)
    {
        if (_talep is null)
            return;

        FormdanTalebeAktar();
        if (!SatinalmaPart1Servisi.GecerliMi(_talep, out var hata))
        {
            HataGoster(hata);
            return;
        }

        var eskiNo = _talep.TalepNo;
        if (string.IsNullOrWhiteSpace(eskiNo))
            _talep.TalepNo = "TASLAK";

        try
        {
            SatinalmaPdfOlusturucu.TalepFormuYazdir(_talep, SatinalmaDepo.Ayarlar);
        }
        finally
        {
            if (string.IsNullOrWhiteSpace(eskiNo))
                _talep.TalepNo = eskiNo;
        }
    }

    private async void OnayaGonder_Click(object sender, RoutedEventArgs e)
    {
        if (_talep is null)
            return;

        FormdanTalebeAktar();
        if (!SatinalmaPart1Servisi.GecerliMi(_talep, out var hata))
        {
            HataGoster(hata);
            return;
        }

        var onay = MessageBox.Show(
            "Talep yönetim ve satınalmaya gönderilsin mi?\n(Bildirim yalnızca bu rollere gider.)",
            UygulamaBilgisi.Ad, MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (onay != MessageBoxResult.Yes)
            return;

        try
        {
            await SatinalmaPart1Servisi.OnayaGonderAsync(_talep);
            TxtTalepNo.Text = _talep.TalepNo;
            MessageBox.Show(
                $"{_talep.TalepNo} onaya gönderildi.\nYönetim «Gelen Talepler» sekmesinde görecek.",
                UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
            Degisti?.Invoke();
            KapatIstendi?.Invoke();
        }
        catch (Exception ex)
        {
            HataGoster(ex.Message);
        }
    }

    private async void Sil_Click(object sender, RoutedEventArgs e)
    {
        if (_talep is null)
            return;

        var onay = MessageBox.Show("Talep silinsin mi?", UygulamaBilgisi.Ad,
            MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (onay != MessageBoxResult.Yes)
            return;

        try
        {
            await SatinalmaPart1Servisi.SilAsync(_talep);
            Degisti?.Invoke();
            KapatIstendi?.Invoke();
        }
        catch (Exception ex)
        {
            HataGoster(ex.Message);
        }
    }

    private void HataGoster(string mesaj)
    {
        TxtHata.Text = mesaj;
        TxtHata.Visibility = Visibility.Visible;
    }
}

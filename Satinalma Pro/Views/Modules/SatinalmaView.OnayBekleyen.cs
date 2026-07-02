using System.Windows;
using System.Windows.Controls;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;

namespace SatinalmaPro.Views.Modules;

public partial class SatinalmaView
{
    private SatinalmaTalep? _onayBekleyenSeciliTalep;

    private void OnayBekleyenListesiniYenile()
    {
        var sahipModu = KullaniciYetkileri.SatinalmaSadeceTalepModu();
        var liste = SatinalmaDepo.Talepler
            .Where(t => SatinalmaTabFiltreleri.OnayBekleyen(t, sahipModu))
            .OrderByDescending(t => t.TalepTuru == Models.TalepTurleri.Acil)
            .ThenByDescending(t => t.Tarih)
            .ThenByDescending(t => t.TalepNo)
            .Select(t => new OnayBekleyenSatiri(t))
            .ToList();

        OnayBekleyenTablosu.ItemsSource = liste;

        if (_onayBekleyenSeciliTalep is not null)
        {
            var satir = liste.FirstOrDefault(s => s.Talep.Id == _onayBekleyenSeciliTalep.Id);
            BtnOnayBekleyenYenidenGonder.IsEnabled = satir is not null
                && SatinalmaYonetimGonderimi.YenidenGonderebilir(satir.Talep);
        }
        else
        {
            BtnOnayBekleyenYenidenGonder.IsEnabled = false;
        }
    }

    private void OnayBekleyenTablosu_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (OnayBekleyenTablosu.SelectedItem is OnayBekleyenSatiri satir)
        {
            _onayBekleyenSeciliTalep = satir.Talep;
            _seciliTalep = satir.Talep;
            _talepFormModu = false;
            BtnOnayBekleyenYenidenGonder.IsEnabled =
                SatinalmaYonetimGonderimi.YenidenGonderebilir(satir.Talep);
            TalepFormuGizle();
            TalepOnizlemePenceresiniAc(satir.Talep);
            return;
        }

        _onayBekleyenSeciliTalep = null;
        BtnOnayBekleyenYenidenGonder.IsEnabled = false;
    }

    private void OnayBekleyenTablosu_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (OnayBekleyenTablosu.SelectedItem is OnayBekleyenSatiri satir)
            TalepOnizlemePenceresiniAc(satir.Talep);
    }

    private async void OnayBekleyenYenidenGonder_Click(object sender, RoutedEventArgs e)
    {
        if (OnayBekleyenTablosu.SelectedItem is not OnayBekleyenSatiri satir)
        {
            MessageBox.Show("Yeniden göndermek için bir talep seçin.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var talep = satir.Talep;
        if (!SatinalmaYonetimGonderimi.YenidenGonderebilir(talep))
        {
            MessageBox.Show("Bu talep yönetime yeniden gönderilemez.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var onay = MessageBox.Show(
            $"{talep.TalepNo} için yönetime yeniden bildirim gönderilsin mi?",
            UygulamaBilgisi.Ad, MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (onay != MessageBoxResult.Yes)
            return;

        try
        {
            await SatinalmaYonetimGonderimi.YenidenGonderAsync(talep);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Gönderilemedi:\n{ex.Message}", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        OnayBekleyenListesiniYenile();
        AkisSekmeleriniYenile();
        MessageBox.Show($"{talep.TalepNo} için yönetime yeniden bildirim gönderildi.", UygulamaBilgisi.Ad,
            MessageBoxButton.OK, MessageBoxImage.Information);
    }
}

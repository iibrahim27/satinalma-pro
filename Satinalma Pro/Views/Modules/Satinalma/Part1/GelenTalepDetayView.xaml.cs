using System.Windows;
using System.Windows.Controls;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;
using SatinalmaPro.Shared.Procurement.Detail;
namespace SatinalmaPro.Views.Modules.Satinalma.Part1;
public partial class GelenTalepDetayView : UserControl
{
    public event Action? Geri;
    public event Action? Degisti;
    public event Action<string>? Yonlendir;
    private SatinalmaTalep? _talep;
    private PurchaseRequestDetailUiState? _ui;
    public GelenTalepDetayView() => InitializeComponent();
    public void Yukle(SatinalmaTalep talep)
    {
        _talep = SatinalmaDepo.Talepler.FirstOrDefault(t => t.Id == talep.Id) ?? talep;
        ProcurementTalepAdapter.StatusSenkronizeEt(_talep);
        TxtBaslik.Text = $"Talep Â· {_talep.TalepNo}";
        TxtOzet.Text = $"{_talep.Tarih} Â· Talep eden: {_talep.TalepEden}";
        TxtTalepDurumu.Text = SatinalmaPart1DurumEtiketi.TalepDurumu(_talep);
        TxtTeklifDurumu.Text = SatinalmaPart1DurumEtiketi.TeklifDurumu(_talep);
        TxtOncelik.Text = TalepTurleri.GorunenAd(_talep.TalepTuru);
        KalemTablosu.ItemsSource = _talep.Kalemler.OrderBy(k => k.SiraNo).ToList();
        var rol = OturumYoneticisi.AktifKullanici?.Rol;
        _ui = PurchaseRequestDetailServisi.UiDurumu(
            _talep,
            rol,
            PurchaseRequestDetailScreen.ManagementSubmittedReview);
        AksiyonlariUygula(_ui);
    }
    private void AksiyonlariUygula(PurchaseRequestDetailUiState ui)
    {
        BtnOnayla.Visibility = ui.IsActionVisible(PurchaseRequestDetailAction.DirectApprove)
            ? Visibility.Visible : Visibility.Collapsed;
        BtnOnayla.Content = ui.LabelFor(PurchaseRequestDetailAction.DirectApprove);
        BtnTeklifAl.Visibility = ui.IsActionVisible(PurchaseRequestDetailAction.StartQuoteProcess)
            ? Visibility.Visible : Visibility.Collapsed;
        BtnTeklifAl.Content = ui.LabelFor(PurchaseRequestDetailAction.StartQuoteProcess);
        BtnReddet.Visibility = ui.IsActionVisible(PurchaseRequestDetailAction.RejectRequest)
            ? Visibility.Visible : Visibility.Collapsed;
        BtnReddet.Content = ui.LabelFor(PurchaseRequestDetailAction.RejectRequest);
        BtnMiktarDuzenle.Visibility = _talep is not null
            && KullaniciYetkileri.YonetimTalepMiktarDuzenleyebilir(_talep)
            ? Visibility.Visible
            : Visibility.Collapsed;
        var aktif = ui.VisibleActions.Count > 0;
        BtnOnayla.IsEnabled = aktif;
        BtnTeklifAl.IsEnabled = aktif;
        BtnReddet.IsEnabled = aktif;
    }
    private void Geri_Click(object sender, RoutedEventArgs e) => Geri?.Invoke();

    private async void MiktarDuzenle_Click(object sender, RoutedEventArgs e)
    {
        if (_talep is null || !KullaniciYetkileri.YonetimTalepMiktarDuzenleyebilir(_talep))
        {
            MessageBox.Show(
                "Talep miktarı yalnızca onay öncesinde Yönetim rolü tarafından düzenlenebilir.",
                UygulamaBilgisi.Ad,
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var pencere = new TalepMiktarDuzenlemeWindow(_talep)
        {
            Owner = Window.GetWindow(this)
        };
        if (pencere.ShowDialog() != true)
            return;

        try
        {
            SatinalmaTalepYardimcisi.TalepKalemleriniTekliflerleSenkronla(_talep);
            SatinalmaTalepYardimcisi.Dokun(_talep);
            await SatinalmaKayitYardimcisi.KaydetVeBulutaGonderAsync(_talep);
            Yukle(_talep);
            Degisti?.Invoke();
            MessageBox.Show(
                "Talep miktarları güncellendi. Teklif tutarları yeni miktarlara göre hesaplandı.",
                UygulamaBilgisi.Ad,
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void TalepPdf_Click(object sender, RoutedEventArgs e)
    {
        if (_talep is null)
            return;
        try
        {
            SatinalmaPdfOlusturucu.TalepFormuYazdir(_talep, SatinalmaDepo.Ayarlar);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
    private async void Onayla_Click(object sender, RoutedEventArgs e) =>
        await AksiyonCalistirAsync(PurchaseRequestDetailAction.DirectApprove, null);
    private async void TeklifAl_Click(object sender, RoutedEventArgs e) =>
        await AksiyonCalistirAsync(PurchaseRequestDetailAction.StartQuoteProcess, null);
    private async void Reddet_Click(object sender, RoutedEventArgs e)
    {
        if (_talep is null)
            return;
        var gerekce = MetinGirisDialog.Goster(
            Window.GetWindow(this),
            "Talep Red",
            "Red gerekÃ§esi:");
        if (gerekce is null)
            return;
        await AksiyonCalistirAsync(PurchaseRequestDetailAction.RejectRequest, gerekce);
    }
    private async Task AksiyonCalistirAsync(PurchaseRequestDetailAction action, string? not)
    {
        if (_talep is null)
            return;
        var onayMesaji = action switch
        {
            PurchaseRequestDetailAction.DirectApprove =>
                "Talep doÄŸrudan onaylansÄ±n mÄ±?\nSatÄ±nalma bilgilendirilir.",
            PurchaseRequestDetailAction.StartQuoteProcess =>
                "Teklif sÃ¼reci baÅŸlatÄ±lsÄ±n mÄ±?\nTalep satÄ±nalma ekibine iletilecek.",
            PurchaseRequestDetailAction.RejectRequest =>
                "Talep reddedilsin mi?",
            _ => null
        };
        if (onayMesaji is not null)
        {
            var onay = MessageBox.Show(onayMesaji, UygulamaBilgisi.Ad,
                MessageBoxButton.YesNo,
                action == PurchaseRequestDetailAction.RejectRequest
                    ? MessageBoxImage.Warning
                    : MessageBoxImage.Question);
            if (onay != MessageBoxResult.Yes)
                return;
        }
        try
        {
            var rol = OturumYoneticisi.AktifKullanici?.Rol;
            await PurchaseRequestDetailServisi.UygulaAsync(_talep, action, rol, not: not);
            var basari = action switch
            {
                PurchaseRequestDetailAction.DirectApprove => "Talep onaylandÄ±.",
                PurchaseRequestDetailAction.StartQuoteProcess =>
                    "Teklif sÃ¼reci baÅŸlatÄ±ldÄ±.\nÂ«Teklif Ä°stenenlerÂ» sekmesinde gÃ¶rÃ¼necek.",
                PurchaseRequestDetailAction.RejectRequest => "Talep reddedildi.",
                _ => "Ä°ÅŸlem tamamlandÄ±."
            };
            MessageBox.Show(basari, UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
            Degisti?.Invoke();
            var hedefRoute = action switch
            {
                PurchaseRequestDetailAction.RejectRequest => SatinalmaPart1Menusu.YonetimRedVerilen,
                PurchaseRequestDetailAction.DirectApprove => SatinalmaPart1Menusu.YonetimDirekOnaylanan,
                PurchaseRequestDetailAction.StartQuoteProcess =>
                    KullaniciRolleri.Normalize(OturumYoneticisi.AktifKullanici?.Rol) == KullaniciRolleri.Satinalma
                        ? SatinalmaPart1Menusu.SatinalmaTeklifIstenen
                        : SatinalmaPart1Menusu.YonetimTeklifBekleyen,
                _ => null
            };
            if (!string.IsNullOrWhiteSpace(hedefRoute))
                Yonlendir?.Invoke(hedefRoute);
            else
                Geri?.Invoke();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}

using SatinalmaPro.Mobile;
using SatinalmaPro.Mobile.Services;
using SatinalmaPro.Shared.Helpers;
using SatinalmaPro.Shared.Models;
using SatinalmaPro.Shared.Services;

namespace SatinalmaPro.Mobile.Views;

[QueryProperty(nameof(TalepId), "id")]
public partial class TalepDetayPage : ContentPage
{
    private readonly OturumServisi _oturum;
    private string _talepId = "";

    public string TalepId
    {
        get => _talepId;
        set
        {
            _talepId = value;
            _ = YukleAsync();
        }
    }

    public TalepDetayPage(OturumServisi oturum)
    {
        InitializeComponent();
        _oturum = oturum;
    }

    private async Task YukleAsync()
    {
        if (!Guid.TryParse(_talepId, out var id))
            return;

        await _oturum.VerileriYenileAsync();
        var talep = _oturum.Depo.Talepler.FirstOrDefault(t => t.Id == id);
        if (talep is null)
        {
            await DisplayAlert("Hata", "Talep bulunamadı.", "Tamam");
            return;
        }

        LblTalepNo.Text = talep.TalepNo;
        LblDurum.Text = $"Durum: {talep.GorunenDurum}";
        LblTur.Text = $"Tür: {TalepTurleri.GorunenAd(talep.TalepTuru)}";
        LblAciklama.Text = talep.TalepAciklamasi;

        if (!string.IsNullOrWhiteSpace(talep.RedGerekcesi))
        {
            LblRed.IsVisible = true;
            LblRed.Text = $"Red gerekçesi: {talep.RedGerekcesi}";
        }
        else
        {
            LblRed.IsVisible = false;
        }

        var uid = _oturum.Depo.AktifKullanici?.Uid;
        var duzenlenebilir = MobilYetkiServisi.TalepDuzenleyebilir(_oturum.Rol, talep, uid, _oturum.KullaniciAdi);
        BtnDuzenle.IsVisible = duzenlenebilir;
        BtnSil.IsVisible = SatinalmaOnayYetkisi.TalepSilebilir(_oturum.Depo.AktifKullanici);

        talep.Kalemler ??= [];
        talep.Teklifler ??= [];

        KalemlerPanel.Clear();
        KalemOnayPanel.Clear();
        KalemOnayPanel.IsVisible = false;
        LblKalemOnayBaslik.IsVisible = false;
        SiparisPanel.IsVisible = false;
        BtnOnayBelgesi.IsVisible = false;

        foreach (var k in talep.Kalemler.OrderBy(x => x.SiraNo))
        {
            KalemlerPanel.Add(new Label
            {
                Text = $"{k.SiraNo}. {k.Malzeme} — {k.Miktar:N2} {k.Birim}",
                TextColor = TemaKaynaklari.VurguMetin
            });
        }

        if (SatinalmaTalepYardimcisi.FormDuzenlenebilir(talep))
        {
            BtnGonder.IsVisible = true;
            OnayBilgiPanel.IsVisible = false;
            ImzaBilgiPanel.IsVisible = false;
        }
        else if (talep.Durum == SatinalmaTalepDurumlari.ImzaSurecinde)
        {
            BtnGonder.IsVisible = false;
            ImzaBilgiPanel.IsVisible = true;
            OnayBilgiPanel.IsVisible = false;
        }
        else
        {
            BtnGonder.IsVisible = false;
            ImzaBilgiPanel.IsVisible = false;
            OnayBilgiPanel.IsVisible = talep.YonetimOnayKilitli || talep.Durum == SatinalmaTalepDurumlari.Onaylandi;
            if (OnayBilgiPanel.IsVisible)
            {
                LblOnayTipi.Text = $"Onay: {SatinalmaMobilServisi.OnayTipiMetni(talep)}";
                LblOnaylayan.Text = string.IsNullOrWhiteSpace(talep.YonetimOnaylayanAd)
                    ? ""
                    : $"Onaylayan: {talep.YonetimOnaylayanAd}";
                LblOnayTarihi.Text = string.IsNullOrWhiteSpace(talep.YonetimOnayTarihi)
                    ? ""
                    : $"Tarih: {talep.YonetimOnayTarihi}";

                var teklif = talep.OnaylananTeklif;
                if (teklif is not null)
                {
                    teklif.FiyatlariHesapla(talep.Kalemler);
                    LblOnayFirma.Text = $"Firma: {teklif.FirmaAdi} · {teklif.GenelToplam:N2} ₺";
                    LblOnayFirma.IsVisible = true;
                }
                else
                {
                    LblOnayFirma.IsVisible = false;
                }

                BtnOnayBelgesi.IsVisible = MobilYetkiServisi.YonetimOnayPdfGorebilir(_oturum.Rol);

                if (talep.HerhangiKalemOnayli)
                {
                    LblKalemOnayBaslik.IsVisible = true;
                    KalemOnayPanel.IsVisible = true;
                    foreach (var kalem in talep.Kalemler.OrderBy(k => k.SiraNo))
                    {
                        if (kalem.OnaylananTeklifId is not { } teklifId)
                            continue;

                        var kalemTeklif = talep.Teklifler.FirstOrDefault(t => t.Id == teklifId);
                        kalemTeklif?.FiyatlariHesapla(talep.Kalemler);
                        var fiyat = kalemTeklif?.Fiyatlar.FirstOrDefault(f => f.KalemId == kalem.Id);
                        var birimFiyat = fiyat?.TlBirimFiyat(kalemTeklif?.UsdKuru ?? 0, kalemTeklif?.EurKuru ?? 0) ?? 0;
                        var toplam = fiyat?.ToplamTutar ?? 0;
                        KalemOnayPanel.Add(new Label
                        {
                            Text = $"{kalem.Malzeme} — {kalemTeklif?.FirmaAdi ?? "—"} · {birimFiyat:N2} ₺/birim · {toplam:N2} ₺",
                            FontSize = 12,
                            TextColor = TemaKaynaklari.VurguMetin,
                            LineBreakMode = LineBreakMode.WordWrap
                        });
                    }
                }

                if (talep.Durum == SatinalmaTalepDurumlari.SiparisOlusturuldu
                    || talep.Kalemler.Any(k => k.KabulEdilenMiktar > 0))
                {
                    SiparisPanel.IsVisible = true;
                    LblSiparisNo.Text = string.IsNullOrWhiteSpace(talep.SiparisNo)
                        ? "Sipariş no: henüz atanmadı"
                        : $"Sipariş no: {talep.SiparisNo}";

                    var tamamlanan = talep.Kalemler.Count(k => k.SiparisTamamlandi);
                    var toplamKalem = talep.Kalemler.Count;
                    LblSiparisDurum.Text = tamamlanan >= toplamKalem && toplamKalem > 0
                        ? "Tüm kalemler depoya alındı."
                        : $"Teslim: {tamamlanan}/{toplamKalem} kalem tamamlandı";
                }
            }
        }
    }

    private async void OnayBelgesi_Clicked(object sender, EventArgs e)
    {
        if (!Guid.TryParse(_talepId, out var id))
            return;

        var talep = _oturum.Depo.Talepler.FirstOrDefault(t => t.Id == id);
        if (talep is null)
            return;

        try
        {
            IsBusy = true;
            await MobilBelgePaylasServisi.PdfOlusturVePaylasAsync(
                () => MobilPdfOlusturucu.YonetimOnayBelgesiPdf(talep),
                $"Yonetim_Onay_{talep.TalepNo}.pdf",
                "Yönetim Onay Belgesi");
        }
        catch (Exception ex)
        {
            await DisplayAlert("PDF Hatası", ex.Message, "Tamam");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async void Gonder_Clicked(object sender, EventArgs e)
    {
        if (!Guid.TryParse(_talepId, out var id))
            return;

        var talep = _oturum.Depo.Talepler.FirstOrDefault(t => t.Id == id);
        if (talep is null)
            return;

        try
        {
            await _oturum.Satinalma.YonetimeGonderAsync(talep);
            await DisplayAlert("Gönderildi", "Talep imza sürecine gönderildi.", "Tamam");
            await YukleAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", ex.Message, "Tamam");
        }
    }

    private async void Duzenle_Clicked(object sender, EventArgs e)
    {
        if (!Guid.TryParse(_talepId, out var id))
            return;
        await BildirimNavigasyonServisi.RouteGitAsync($"talep-duzenle?id={id}", _oturum);
    }

    private async void Sil_Clicked(object sender, EventArgs e)
    {
        if (!Guid.TryParse(_talepId, out var id))
            return;

        var talep = _oturum.Depo.Talepler.FirstOrDefault(t => t.Id == id);
        if (talep is null)
            return;

        var onayli = talep.HerhangiKalemOnayli
                     || talep.Durum is SatinalmaTalepDurumlari.Onaylandi
                         or SatinalmaTalepDurumlari.SiparisOlusturuldu;

        var mesaj = onayli
            ? $"{talep.TalepNo} kalıcı olarak silinecek.\n\nTüm onaylar, teklifler ve bildirimler de silinir.\n\nDevam edilsin mi?"
            : $"{talep.TalepNo} kalıcı olarak silinecek. Devam edilsin mi?";

        var onay = await DisplayAlert("Talep Sil", mesaj, "Sil", "İptal");
        if (!onay)
            return;

        try
        {
            BtnSil.IsEnabled = false;
            await _oturum.Satinalma.TalepSilAsync(talep);
            await DisplayAlert("Silindi", "Talep ve bağlı kayıtlar silindi.", "Tamam");
            await ShellGuvenli.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", ex.Message, "Tamam");
        }
        finally
        {
            BtnSil.IsEnabled = true;
        }
    }

    private async void BelgePaylas_Clicked(object sender, EventArgs e)
    {
        if (!Guid.TryParse(_talepId, out var id))
            return;

        var talep = _oturum.Depo.Talepler.FirstOrDefault(t => t.Id == id);
        if (talep is null)
            return;

        try
        {
            IsBusy = true;
            await MobilBelgePaylasServisi.PdfOlusturVePaylasAsync(
                () => MobilPdfOlusturucu.TalepPdf(talep),
                $"{talep.TalepNo}_talep.pdf",
                "Talep PDF");
        }
        catch (Exception ex)
        {
            await DisplayAlert("PDF Hatası", ex.Message, "Tamam");
        }
        finally
        {
            IsBusy = false;
        }
    }
}

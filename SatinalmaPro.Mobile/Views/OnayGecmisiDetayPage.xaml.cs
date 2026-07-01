using Microsoft.Maui.Controls.Shapes;
using SatinalmaPro.Mobile.Services;
using SatinalmaPro.Shared.Models;
using SatinalmaPro.Shared.Services;

namespace SatinalmaPro.Mobile.Views;

[QueryProperty(nameof(TalepId), "id")]
public partial class OnayGecmisiDetayPage : ContentPage
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

    public OnayGecmisiDetayPage(OturumServisi oturum)
    {
        InitializeComponent();
        _oturum = oturum;
        RefreshView.Command = new Command(async () =>
        {
            await YukleAsync();
            RefreshView.IsRefreshing = false;
        });
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!string.IsNullOrWhiteSpace(_talepId))
            await YukleAsync();
    }

    private async Task YukleAsync()
    {
        if (!Guid.TryParse(_talepId, out var id))
            return;

        await _oturum.VerileriYenileAsync();
        Icerik.Clear();

        var talep = _oturum.Depo.Talepler.FirstOrDefault(t => t.Id == id);
        if (talep is null)
        {
            Icerik.Add(new Label { Text = "Talep bulunamadı.", TextColor = Colors.Gray });
            return;
        }

        talep.Kalemler ??= [];
        talep.Teklifler ??= [];

        var baslik = string.IsNullOrWhiteSpace(talep.TalepAciklamasi)
            ? talep.TalepEden
            : talep.TalepAciklamasi;

        Icerik.Add(new Label
        {
            Text = $"{talep.TalepNo} — {baslik}",
            FontAttributes = FontAttributes.Bold,
            FontSize = 18,
            TextColor = TemaKaynaklari.BirincilMetin
        });

        Icerik.Add(BilgiSatiri("Onay tipi", SatinalmaMobilServisi.OnayTipiMetni(talep)));
        Icerik.Add(BilgiSatiri("Durum", talep.GorunenDurum));
        Icerik.Add(BilgiSatiri("Onaylayan", string.IsNullOrWhiteSpace(talep.YonetimOnaylayanAd) ? "—" : talep.YonetimOnaylayanAd));
        if (!string.IsNullOrWhiteSpace(talep.YonetimOnaylayanEposta))
            Icerik.Add(BilgiSatiri("E-posta", talep.YonetimOnaylayanEposta));
        Icerik.Add(BilgiSatiri("Onay tarihi", string.IsNullOrWhiteSpace(talep.YonetimOnayTarihi) ? "—" : talep.YonetimOnayTarihi));
        Icerik.Add(BilgiSatiri("Talep eden", talep.TalepEden));

        if (!string.IsNullOrWhiteSpace(talep.SiparisNo))
            Icerik.Add(BilgiSatiri("Sipariş no", talep.SiparisNo));

        Icerik.Add(KalemKutusu(talep));

        var onayTeklif = talep.OnaylananTeklif;
        if (onayTeklif is not null)
        {
            onayTeklif.FiyatlariHesapla(talep.Kalemler);
            var kdvHaric = onayTeklif.Fiyatlar.Sum(f => f.ToplamTutar);

            Icerik.Add(new Label
            {
                Text = "Onaylanan Teklif",
                FontAttributes = FontAttributes.Bold,
                FontSize = 15,
                Margin = new Thickness(0, 8, 0, 0),
                TextColor = TemaKaynaklari.BirincilMetin
            });

            var kart = new Border
            {
                Padding = 12,
                BackgroundColor = TemaKaynaklari.OnayPanelArkaPlan,
                Stroke = TemaKaynaklari.OnayPanelMetin,
                StrokeShape = new RoundRectangle { CornerRadius = 8 }
            };

            var kartIcerik = new VerticalStackLayout { Spacing = 6 };
            kartIcerik.Add(new Label
            {
                Text = onayTeklif.FirmaAdi,
                FontAttributes = FontAttributes.Bold,
                TextColor = TemaKaynaklari.BirincilMetin
            });
            kartIcerik.Add(new Label
            {
                Text = $"Toplam (KDV hariç): {kdvHaric:N2} ₺ · KDV dahil: {onayTeklif.GenelToplam:N2} ₺",
                FontSize = 12,
                TextColor = Colors.Gray
            });

            foreach (var kalem in talep.Kalemler.OrderBy(k => k.SiraNo))
            {
                var fiyat = onayTeklif.Fiyatlar.FirstOrDefault(f => f.KalemId == kalem.Id);
                var birimFiyat = fiyat?.BirimFiyat ?? 0;
                var satirKdvHaric = fiyat?.ToplamTutar ?? 0;
                kartIcerik.Add(new Label
                {
                    Text = $"• {kalem.Malzeme} — {kalem.Miktar:N2} {kalem.Birim} · {birimFiyat:N2} ₺/birim · {satirKdvHaric:N2} ₺ (KDV hariç)",
                    FontSize = 12,
                    TextColor = TemaKaynaklari.VurguMetin,
                    LineBreakMode = LineBreakMode.WordWrap
                });
            }

            kart.Content = kartIcerik;
            Icerik.Add(kart);
        }
        else if (talep.TeklifsizYonetimOnayi)
        {
            var metin = talep.TalepTuru == TalepTurleri.Acil
                ? "Acil yönetim onayı — satınalma fatura sonrası tedarikçi ve fiyat girecek."
                : "Teklifsiz yönetim onayı — firma ve fiyat satınalma tarafından girilecek.";
            Icerik.Add(new Label
            {
                Text = metin,
                FontSize = 12,
                TextColor = Colors.Gray,
                Margin = new Thickness(0, 8, 0, 0)
            });
        }
    }

    private async void OnayPdf_Clicked(object sender, EventArgs e)
    {
        if (!Guid.TryParse(_talepId, out var id))
            return;

        var talep = _oturum.Depo.Talepler.FirstOrDefault(t => t.Id == id);
        if (talep is null)
            return;

        if (!MobilYetkiServisi.YonetimOnayPdfGorebilir(_oturum.Rol))
        {
            await DisplayAlert("Yetki", "Onay belgesi görüntüleme yetkiniz yok.", "Tamam");
            return;
        }

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

    private static View BilgiSatiri(string etiket, string deger)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star }
            },
            ColumnSpacing = 8,
            Margin = new Thickness(0, 2)
        };
        grid.Add(new Label { Text = $"{etiket}:", FontSize = 12, TextColor = TemaKaynaklari.SolukMetin });
        grid.Add(new Label
        {
            Text = deger,
            FontSize = 12,
            TextColor = TemaKaynaklari.VurguMetin,
            LineBreakMode = LineBreakMode.WordWrap
        }, 1, 0);
        return grid;
    }

    private static View KalemKutusu(SatinalmaTalep talep)
    {
        var panel = new VerticalStackLayout { Spacing = 4 };
        panel.Add(new Label
        {
            Text = "Talep Kalemleri",
            FontAttributes = FontAttributes.Bold,
            FontSize = 13,
            TextColor = TemaKaynaklari.IkincilMetin
        });

        foreach (var satir in talep.KalemSatirlari())
        {
            panel.Add(new Label
            {
                Text = $"• {satir}",
                FontSize = 12,
                TextColor = TemaKaynaklari.VurguMetin
            });
        }

        if (talep.Kalemler.Count == 0)
            panel.Add(new Label { Text = "Kalem bilgisi yok", FontSize = 12, TextColor = Colors.Gray });

        return new Border
        {
            Padding = 10,
            Margin = new Thickness(0, 8, 0, 8),
            BackgroundColor = TemaKaynaklari.KartArkaPlan,
            Stroke = TemaKaynaklari.KartCerceve,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Content = panel
        };
    }
}

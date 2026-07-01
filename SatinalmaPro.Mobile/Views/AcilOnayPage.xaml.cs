using SatinalmaPro.Mobile.Services;

using SatinalmaPro.Shared.Models;



namespace SatinalmaPro.Mobile.Views;



[QueryProperty(nameof(TalepId), "talepId")]

public partial class AcilOnayPage : ContentPage

{

    private readonly OturumServisi _oturum;

    private string _talepId = "";

    private SatinalmaTalep? _talep;



    public string TalepId

    {

        get => _talepId;

        set

        {

            _talepId = value;

            _ = YukleAsync();

        }

    }



    public AcilOnayPage(OturumServisi oturum)

    {

        InitializeComponent();

        _oturum = oturum;

    }



    private async Task YukleAsync()

    {

        if (!Guid.TryParse(_talepId, out var id))

            return;



        await _oturum.VerileriYenileAsync();

        _talep = _oturum.Depo.Talepler.FirstOrDefault(t => t.Id == id);

        if (_talep is null)

        {

            await DisplayAlert("Hata", "Talep bulunamadı.", "Tamam");

            await ShellGuvenli.GoToAsync("..");

            return;

        }



        if (_talep.TalepTuru != TalepTurleri.Acil)

        {

            await DisplayAlert("Uyarı", "Bu talep acil değil.", "Tamam");

            await ShellGuvenli.GoToAsync("..");

            return;

        }



        LblTalepNo.Text = _talep.TalepNo;

        LblTalepEden.Text = $"Talep eden: {_talep.TalepEden} · {_talep.Tarih}";



        if (!string.IsNullOrWhiteSpace(_talep.TalepAciklamasi))

        {

            LblAciklama.Text = _talep.TalepAciklamasi;

            LblAciklama.IsVisible = true;

        }



        KalemPanel.Clear();

        foreach (var satir in _talep.KalemSatirlari())

        {

            KalemPanel.Add(new Label

            {

                Text = $"• {satir}",

                TextColor = TemaKaynaklari.VurguMetin,

                FontSize = 13

            });

        }



        if (KalemPanel.Count == 0)

            KalemPanel.Add(new Label { Text = "Kalem bilgisi yok", TextColor = Colors.Gray, FontSize = 13 });

    }



    private async void Onayla_Clicked(object sender, EventArgs e)

    {

        if (_talep is null)

            return;



        var onay = await DisplayAlert(

            "Acil Onay",

            $"{_talep.TalepNo} acil talep olarak onaylanacak.\n\nSatınalma birimi tedarik ve fatura girişini tamamlayacaktır.\n\nDevam?",

            "Onayla", "İptal");

        if (!onay)

            return;



        try

        {

            await _oturum.Satinalma.YonetimAcilOnaylaAsync(_talep);

            await DisplayAlert("Onaylandı",

                $"{_talep.TalepNo} onaylandı.\nSatınalma birimine bildirim gönderildi.",

                "Tamam");

            await ShellGuvenli.GoToAsync("//gecmis-talepler");

        }

        catch (Exception ex)

        {

            await DisplayAlert("Hata", ex.Message, "Tamam");

        }

    }



    private async void Reddet_Clicked(object sender, EventArgs e)

    {

        if (_talep is null)

            return;



        var gerekce = await DisplayPromptAsync("Red Gerekçesi", "Red sebebini yazın:", "Reddet", "İptal");

        if (gerekce is null)

            return;



        try

        {

            await _oturum.Satinalma.YonetimReddetAsync(_talep, gerekce);

            await DisplayAlert("Reddedildi", $"{_talep.TalepNo} reddedildi.", "Tamam");

            await ShellGuvenli.GoToAsync("//red-talepler");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", ex.Message, "Tamam");
        }
    }
}

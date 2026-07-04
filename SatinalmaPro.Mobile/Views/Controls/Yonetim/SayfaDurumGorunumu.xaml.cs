using SatinalmaPro.Mobile.Models.Yonetim;

namespace SatinalmaPro.Mobile.Views.Controls.Yonetim;

public partial class SayfaDurumGorunumu : ContentView
{
    public static readonly BindableProperty DurumProperty =
        BindableProperty.Create(nameof(Durum), typeof(YonetimSayfaDurumu), typeof(SayfaDurumGorunumu),
            YonetimSayfaDurumu.Icerik, propertyChanged: DurumDegisti);

    public static readonly BindableProperty BosBaslikProperty =
        BindableProperty.Create(nameof(BosBaslik), typeof(string), typeof(SayfaDurumGorunumu), "Kayıt bulunamadı");

    public static readonly BindableProperty BosMesajProperty =
        BindableProperty.Create(nameof(BosMesaj), typeof(string), typeof(SayfaDurumGorunumu), "Henüz görüntülenecek veri yok.");

    public static readonly BindableProperty HataMesajProperty =
        BindableProperty.Create(nameof(HataMesaj), typeof(string), typeof(SayfaDurumGorunumu), "Veriler yüklenemedi.");

    public static readonly BindableProperty IcerikProperty =
        BindableProperty.Create(nameof(Icerik), typeof(View), typeof(SayfaDurumGorunumu), null,
            propertyChanged: IcerikDegisti);

    public event EventHandler? TekrarDeneTiklandi;

    public SayfaDurumGorunumu()
    {
        InitializeComponent();
        BtnTekrarDene.Clicked += (_, _) => TekrarDeneTiklandi?.Invoke(this, EventArgs.Empty);
    }

    public YonetimSayfaDurumu Durum
    {
        get => (YonetimSayfaDurumu)GetValue(DurumProperty);
        set => SetValue(DurumProperty, value);
    }

    public string BosBaslik
    {
        get => (string)GetValue(BosBaslikProperty);
        set => SetValue(BosBaslikProperty, value);
    }

    public string BosMesaj
    {
        get => (string)GetValue(BosMesajProperty);
        set => SetValue(BosMesajProperty, value);
    }

    public string HataMesaj
    {
        get => (string)GetValue(HataMesajProperty);
        set => SetValue(HataMesajProperty, value);
    }

    public View? Icerik
    {
        get => (View?)GetValue(IcerikProperty);
        set => SetValue(IcerikProperty, value);
    }

    private static void DurumDegisti(BindableObject bindable, object _, object __)
    {
        if (bindable is SayfaDurumGorunumu view)
            view.GorunumuGuncelle();
    }

    private static void IcerikDegisti(BindableObject bindable, object _, object __)
    {
        if (bindable is SayfaDurumGorunumu view && view.Icerik is View icerik)
            view.IcerikPanel.Content = icerik;
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName is nameof(BosBaslik))
            LblBosBaslik.Text = BosBaslik;
        else if (propertyName is nameof(BosMesaj))
            LblBosMesaj.Text = BosMesaj;
        else if (propertyName is nameof(HataMesaj))
            LblHataMesaj.Text = HataMesaj;
    }

    private void GorunumuGuncelle()
    {
        YukleniyorPanel.IsVisible = Durum == YonetimSayfaDurumu.Yukleniyor;
        BosPanel.IsVisible = Durum == YonetimSayfaDurumu.Bos;
        HataPanel.IsVisible = Durum == YonetimSayfaDurumu.Hata;
        IcerikPanel.IsVisible = Durum == YonetimSayfaDurumu.Icerik;
    }
}

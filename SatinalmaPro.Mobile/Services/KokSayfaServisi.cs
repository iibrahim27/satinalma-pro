namespace SatinalmaPro.Mobile.Services;

public static class KokSayfaServisi
{
    public static void Ayarla(Page sayfa)
    {
        var app = Application.Current;
        if (app is null)
            return;

        var pencere = app.Windows.FirstOrDefault();
        if (pencere is not null)
            pencere.Page = sayfa;
    }
}

namespace SatinalmaPro.Mobile.Services;

public interface IBiyometrikKimlikServisi
{
    Task<bool> KullanilabilirMiAsync();
    Task<bool> DogrulaAsync(string mesaj = "Kimliğinizi doğrulayın");
}

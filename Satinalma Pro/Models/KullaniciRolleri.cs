namespace SatinalmaPro.Models;

public static class KullaniciRolleri
{
    public const string Admin = "Admin";
    public const string Yonetim = "Yönetim";
    public const string Satinalma = "Satınalma";
    public const string Sef = "Şef";
    public const string Saha = "Saha";
    public const string Atolye = "Atölye";
    public const string Depo = "Depo";
    public const string Okuma = "Okuma";

    public static IReadOnlyList<string> Tum { get; } =
        SatinalmaPro.Shared.Models.KullaniciRolleri.Tum;

    public static string GorunenAd(string rol) => Normalize(rol);

    public static bool AdminMi(string? rol) =>
        SatinalmaPro.Shared.Models.KullaniciRolleri.AdminMi(rol);

    public static string Normalize(string? rol) =>
        SatinalmaPro.Shared.Models.KullaniciRolleri.Normalize(rol);

    public static bool YazabilirMi(string? rol) =>
        SatinalmaPro.Shared.Models.KullaniciRolleri.YazabilirMi(rol);

    public static bool SatinalmaTeklifGirebilir(string? rol) =>
        SatinalmaPro.Shared.Models.KullaniciRolleri.SatinalmaTeklifGirebilir(rol);

    public static bool KendiTalepleriniTakipEder(string? rol) =>
        SatinalmaPro.Shared.Models.KullaniciRolleri.KendiTalepleriniTakipEder(rol);

    public static IReadOnlyList<string> VarsayilanModuller(string? rol)
    {
        rol = Normalize(rol);
        return SatinalmaPro.Shared.Services.MasaustuRolHaritasi.MasaustuModulleri(rol).ToList();
    }
}


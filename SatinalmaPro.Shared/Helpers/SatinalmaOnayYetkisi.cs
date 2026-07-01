using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Shared.Helpers;

public static class SatinalmaOnayYetkisi
{
    public static bool FirmaOnayiDuzenlenebilir(KullaniciProfili? kullanici)
    {
        if (kullanici is null || !kullanici.Aktif)
            return false;

        if (KullaniciRolleri.AdminMi(kullanici.Rol))
            return true;

        return KullaniciRolleri.Normalize(kullanici.Rol) is KullaniciRolleri.Yonetim or KullaniciRolleri.Satinalma;
    }

    public static bool FirmaOnayiSaltOkunur(SatinalmaTalep talep, KullaniciProfili? kullanici) =>
        talep.YonetimOnayKilitli && !FirmaOnayiDuzenlenebilir(kullanici);

    public static bool TalepSilebilir(KullaniciProfili? kullanici)
    {
        if (kullanici is null || !kullanici.Aktif)
            return false;

        if (KullaniciRolleri.AdminMi(kullanici.Rol))
            return true;

        return KullaniciRolleri.Normalize(kullanici.Rol) == KullaniciRolleri.Satinalma;
    }

    /// <summary>Yönetim kararı: direkt onay, acil onay, teklif iste, red.</summary>
    public static bool YonetimKararVerebilir(KullaniciProfili? kullanici)
    {
        if (kullanici is null || !kullanici.Aktif)
            return false;

        if (KullaniciRolleri.AdminMi(kullanici.Rol))
            return true;

        return KullaniciRolleri.Normalize(kullanici.Rol) is KullaniciRolleri.Yonetim or KullaniciRolleri.Satinalma;
    }

    /// <summary>Teklif / talep onayı — yalnızca Satınalma ve Yönetim.</summary>
    public static bool TeklifOnayVerebilir(KullaniciProfili? kullanici) =>
        FirmaOnayiDuzenlenebilir(kullanici);
}

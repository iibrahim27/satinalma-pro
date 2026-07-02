using SatinalmaPro.Shared.Helpers;
using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Shared.Services;

public static class MobilYetkiServisi
{
    public static bool RotaGorebilir(string? rol, string route) =>
        RolRouteServisi.ErisilebilirRotalar(rol).Contains(route);

    public static bool SatinalmaSadeceTalepModu(string? rol) =>
        KullaniciRolleri.SadeceTalepModu(rol);

    public static bool SekmeGorebilir(string? rol, string sekme) =>
        KullaniciRolleri.SatinalmaSekmesiGorebilir(rol, sekme);

    public static bool TalepDuzenleyebilir(string? rol, SatinalmaTalep talep, string? kullaniciUid, string? kullaniciAd)
    {
        if (!KullaniciRolleri.TalepOlusturabilir(rol))
            return false;

        if (KullaniciRolleri.AdminMi(rol))
            return SatinalmaTalepYardimcisi.TalepKalemleriDuzenlenebilir(talep);

        if (!SatinalmaTalepYardimcisi.TalepKalemleriDuzenlenebilir(talep))
            return false;

        return talep.OlusturanUid == kullaniciUid
               || (!string.IsNullOrWhiteSpace(kullaniciAd) && talep.TalepEden == kullaniciAd);
    }

    public static bool StokDurumGorebilir(string? rol) => RotaGorebilir(rol, "stok-durum");

    public static bool StokHareketGorebilir(string? rol) => RotaGorebilir(rol, "stok-hareket");

    public static bool StokGirisYapabilir(string? rol) => RotaGorebilir(rol, "stok-giris");

    public static bool StokCikisYapabilir(string? rol) => RotaGorebilir(rol, "stok-cikis");

    public static bool StokSayimYapabilir(string? rol) => RotaGorebilir(rol, "stok-sayim");

    public static bool StokYazabilir(string? rol) =>
        StokGirisYapabilir(rol) || StokCikisYapabilir(rol) || StokSayimYapabilir(rol);

    public static bool AlinanMalzemeGorebilir(string? rol) => RotaGorebilir(rol, "onaylanan-malzemeler");

    public static bool SatinalmaIslemiYapabilir(string? rol) =>
        KullaniciRolleri.Normalize(rol) is KullaniciRolleri.Admin or KullaniciRolleri.Satinalma;

    public static bool MalKabulVeStokAktarYapabilir(string? rol) =>
        KullaniciRolleri.Normalize(rol) is KullaniciRolleri.Admin or KullaniciRolleri.Satinalma;

    public static bool YonetimIslemiYapabilir(string? rol) =>
        KullaniciRolleri.Normalize(rol) is KullaniciRolleri.Admin
            or KullaniciRolleri.Yonetim
            or KullaniciRolleri.Satinalma;

    public static bool YonetimKararVerebilir(string? rol) =>
        RotaGorebilir(rol, "gelen-talepler");

    public static bool TeklifOnayGorebilir(string? rol) => RotaGorebilir(rol, "teklif-onay");

    public static bool YonetimOnayPdfGorebilir(string? rol) =>
        KullaniciRolleri.Normalize(rol) is KullaniciRolleri.Admin
            or KullaniciRolleri.Yonetim
            or KullaniciRolleri.Satinalma
            or KullaniciRolleri.Sef
            or KullaniciRolleri.Saha;
}

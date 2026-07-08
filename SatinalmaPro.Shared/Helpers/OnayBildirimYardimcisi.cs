using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Shared.Helpers;

public static class OnayBildirimYardimcisi
{
    public static bool SatinalmaOnayladi(string? onaylayanRol) =>
        KullaniciRolleri.Normalize(onaylayanRol) == KullaniciRolleri.Satinalma;

    public static IEnumerable<(string? HedefRol, string? HedefUid)> OnaylandiHedefleri(
        string? olusturanUid,
        string? onaylayanRol)
    {
        if (SatinalmaOnayladi(onaylayanRol))
        {
            if (!string.IsNullOrWhiteSpace(olusturanUid))
                yield return (null, olusturanUid);

            yield break;
        }

        if (!string.IsNullOrWhiteSpace(olusturanUid))
            yield return (null, olusturanUid);

        yield return (KullaniciRolleri.Satinalma, null);
    }

    public static string TeklifIstemeBildirimEk(string? onaylayanRol) =>
        SatinalmaOnayladi(onaylayanRol)
            ? "Satınalma birimi teklifiniz için teklif girişi talep etti."
            : "Yönetim teklifiniz için satınalmadan teklif talep etti.";
}

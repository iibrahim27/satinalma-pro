using System.Globalization;
using System.Text;
using SatinalmaPro.Models;

namespace SatinalmaPro.Helpers;

/// <summary>Şef/saha talep sahipliği — uid, isim ve eski kayıt uyumluluğu.</summary>
public static class SatinalmaTalepSahiplikYardimcisi
{
    public static bool KullanicininTalebi(SatinalmaTalep t, string? uid, string? adSoyad, string? eposta = null)
    {
        if (!string.IsNullOrWhiteSpace(uid) && !string.IsNullOrWhiteSpace(t.OlusturanUid)
            && string.Equals(t.OlusturanUid.Trim(), uid.Trim(), StringComparison.OrdinalIgnoreCase))
            return true;

        if (AdlariEslesir(t.TalepEden, adSoyad))
            return true;

        if (!string.IsNullOrWhiteSpace(eposta)
            && !string.IsNullOrWhiteSpace(t.TalepEden)
            && string.Equals(t.TalepEden.Trim(), eposta.Trim(), StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    public static int OlusturanUidGocEt(IEnumerable<SatinalmaTalep> talepler, KullaniciProfili? kullanici)
    {
        if (kullanici is null || string.IsNullOrWhiteSpace(kullanici.Uid))
            return 0;

        var sayac = 0;
        foreach (var talep in talepler)
        {
            if (!string.IsNullOrWhiteSpace(talep.OlusturanUid))
                continue;

            if (!KullanicininTalebi(talep, kullanici.Uid, kullanici.AdSoyad, kullanici.Eposta))
                continue;

            talep.OlusturanUid = kullanici.Uid;
            if (string.IsNullOrWhiteSpace(talep.OlusturanRol))
                talep.OlusturanRol = kullanici.Rol;
            sayac++;
        }

        return sayac;
    }

    private static bool AdlariEslesir(string? talepEden, string? adSoyad)
    {
        if (string.IsNullOrWhiteSpace(talepEden) || string.IsNullOrWhiteSpace(adSoyad))
            return false;

        var a = AdNorm(talepEden);
        var b = AdNorm(adSoyad);
        if (a.Length == 0 || b.Length == 0)
            return false;

        if (a == b)
            return true;

        if (a.Length >= 4 && b.Length >= 4 && (a.Contains(b, StringComparison.Ordinal) || b.Contains(a, StringComparison.Ordinal)))
            return true;

        return false;
    }

    private static string AdNorm(string deger)
    {
        var sb = new StringBuilder(deger.Length);
        foreach (var c in deger.Trim().Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue;

            sb.Append(char.ToLowerInvariant(c));
        }

        return sb.ToString().Replace(" ", "", StringComparison.Ordinal);
    }
}

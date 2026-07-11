using System.Windows;
using System.Windows.Controls;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Shared.Services;

namespace SatinalmaPro.Services;

public static class KullaniciYetkileri
{
    public static bool ModulGorebilir(string modulAdi) => ModulOkuyabilir(modulAdi);

    public static bool ModulOkuyabilir(string modulAdi)
    {
        if (!OturumYoneticisi.BulutAktif)
            return true;

        var kullanici = OturumYoneticisi.AktifKullanici;
        if (kullanici is null || !kullanici.Aktif)
            return false;

        // Ayarlar: Satınalma + Admin (kiracı operasyonu).
        if (modulAdi.Equals("Ayarlar", StringComparison.OrdinalIgnoreCase))
            return KullaniciRolleri.Normalize(kullanici.Rol) == KullaniciRolleri.Satinalma
                || KullaniciRolleri.AdminMi(kullanici.Rol);

        if (KullaniciRolleri.AdminMi(kullanici.Rol))
            return true;

        var yetki = ModulYetkisiniBul(kullanici, modulAdi);
        if (yetki is not null)
            return yetki.Okuma;

        if (kullanici.Moduller.Count > 0)
            return kullanici.Moduller.Contains(modulAdi, StringComparer.OrdinalIgnoreCase);

        return RolVarsayilanGorebilir(kullanici.Rol, modulAdi);
    }

    public static bool ModulYazabilir(string modulAdi)
    {
        if (!OturumYoneticisi.BulutAktif)
            return true;

        var kullanici = OturumYoneticisi.AktifKullanici;
        if (kullanici is null || !kullanici.Aktif)
            return false;

        if (modulAdi.Equals("Ayarlar", StringComparison.OrdinalIgnoreCase))
            return KullaniciRolleri.Normalize(kullanici.Rol) == KullaniciRolleri.Satinalma
                || KullaniciRolleri.AdminMi(kullanici.Rol);

        if (KullaniciRolleri.AdminMi(kullanici.Rol))
            return true;

        if (!ModulOkuyabilir(modulAdi))
            return false;

        var rol = KullaniciRolleri.Normalize(kullanici.Rol);
        var satinalmaModul = modulAdi.Equals("Satınalma", StringComparison.OrdinalIgnoreCase);

        if (satinalmaModul)
            return SatinalmaModuluYazabilir(kullanici, rol);

        if (modulAdi.Equals("Stok Yönetimi", StringComparison.OrdinalIgnoreCase))
            return StokYazabilir();

        // Satınalma dışı roller yalnızca Satınalma/Stok özel kurallarıyla yazar (yukarıda).
        if (rol != KullaniciRolleri.Satinalma)
            return false;

        // Açık yetki kaydı varsa ona uy; yoksa Satınalma rolü görebildiği modüllerde yazar.
        // (Boş ModulYetkileri ile mal kabul Alınan Malzemeler'e eklenip kayboluyordu.)
        var yetki = ModulYetkisiniBul(kullanici, modulAdi);
        return yetki is null || yetki.Yazma;
    }

    /// <summary>Okuma var, yazma yok — filtre/arama/rapor kullanılabilir.</summary>
    public static bool ModulSaltOkunur(string modulAdi) =>
        ModulOkuyabilir(modulAdi) && !ModulYazabilir(modulAdi);

    public static bool YazmaIslemiEngellendi(string modulAdi)
    {
        if (ModulYazabilir(modulAdi))
            return false;

        MessageBox.Show(
            "Bu modülde yalnızca görüntüleme yetkiniz var.\nDüzenleme, silme ve kaydetme işlemleri yapılamaz.",
            UygulamaBilgisi.Ad,
            MessageBoxButton.OK,
            MessageBoxImage.Information);
        return true;
    }

    private static bool SatinalmaModuluYazabilir(KullaniciProfili kullanici, string rol)
    {
        if (rol == KullaniciRolleri.Satinalma)
            return true;

        if (rol == KullaniciRolleri.Yonetim)
            return true;

        var yetki = ModulYetkisiniBul(kullanici, "Satınalma");
        if (yetki?.Yazma == true)
            return true;

        return rol is KullaniciRolleri.Sef or KullaniciRolleri.Saha;
    }

    public static bool RolYazmaAtanabilir(string? rol)
    {
        if (!OturumYoneticisi.BulutAktif)
            return true;

        var r = KullaniciRolleri.Normalize(rol);
        return KullaniciRolleri.AdminMi(rol) || r == KullaniciRolleri.Satinalma;
    }

    public static bool SekmeGorebilir(string modulAdi, string sekmeAdi)
    {
        if (!ModulOkuyabilir(modulAdi))
            return false;

        if (!OturumYoneticisi.BulutAktif)
            return true;

        var kullanici = OturumYoneticisi.AktifKullanici;
        if (KullaniciRolleri.AdminMi(kullanici?.Rol))
            return true;

        var yetki = kullanici is null ? null : ModulYetkisiniBul(kullanici, modulAdi);
        if (yetki?.Sekmeler.Count > 0)
        {
            var hedef = modulAdi.Equals("Satınalma", StringComparison.OrdinalIgnoreCase)
                ? SatinalmaPro.Shared.Services.MasaustuRolHaritasi.SatinalmaSekmeNormalize(sekmeAdi)
                : sekmeAdi;

            if (modulAdi.Equals("Satınalma", StringComparison.OrdinalIgnoreCase)
                && hedef.Equals(MasaustuRolHaritasi.TeklifGirisi, StringComparison.OrdinalIgnoreCase)
                && !KullaniciRolleri.SatinalmaTeklifGirebilir(kullanici?.Rol))
                return false;

            return yetki.Sekmeler.Any(s =>
            {
                var ad = modulAdi.Equals("Satınalma", StringComparison.OrdinalIgnoreCase)
                    ? SatinalmaPro.Shared.Services.MasaustuRolHaritasi.SatinalmaSekmeNormalize(s)
                    : s;
                return ad.Equals(hedef, StringComparison.OrdinalIgnoreCase);
            });
        }

        if (modulAdi.Equals("Satınalma", StringComparison.OrdinalIgnoreCase))
            return SatinalmaPro.Shared.Services.MasaustuRolHaritasi.SatinalmaSekmesiGorebilir(kullanici?.Rol, sekmeAdi);

        if (modulAdi.Equals("Stok Yönetimi", StringComparison.OrdinalIgnoreCase))
            return SatinalmaPro.Shared.Services.MasaustuRolHaritasi.StokSekmesiGorebilir(kullanici?.Rol, sekmeAdi);

        return true;
    }

    public static bool SatinalmaSurecTakipModu()
    {
        if (!OturumYoneticisi.BulutAktif || AdminMi)
            return false;

        return SatinalmaSadeceTalepModu();
    }

    /// <summary>Yönetim rolü — yalnızca onay kararları ve geçmiş; operasyonel satınalma yok.</summary>
    public static bool YonetimOnayModu()
    {
        if (!OturumYoneticisi.BulutAktif || AdminMi)
            return false;

        return KullaniciRolleri.Normalize(OturumYoneticisi.AktifKullanici?.Rol) == KullaniciRolleri.Yonetim;
    }

    public static bool StokYazabilir()
    {
        if (!OturumYoneticisi.BulutAktif)
            return true;

        return SatinalmaPro.Shared.Services.MobilYetkiServisi.StokYazabilir(OturumYoneticisi.AktifKullanici?.Rol);
    }

    public static bool StokYazmaIslemiEngellendi()
    {
        if (StokYazabilir())
            return false;

        MessageBox.Show(
            "Stok giriş/çıkış düzenleme yetkiniz yok.\nYalnızca görüntüleme yapabilirsiniz.",
            UygulamaBilgisi.Ad,
            MessageBoxButton.OK,
            MessageBoxImage.Information);
        return true;
    }

    public static bool SatinalmaSadeceTalepModu()
    {
        if (!OturumYoneticisi.BulutAktif || AdminMi)
            return false;

        return ModulOkuyabilir("Satınalma")
               && SekmeGorebilir("Satınalma", "Taleplerim")
               && !SekmeGorebilir("Satınalma", "Teklif Bekleyen")
               && !SekmeGorebilir("Satınalma", "Teklif Girişi")
               && !SekmeGorebilir("Satınalma", "Karşılaştırma");
    }

    public static bool SatinalmaTalepDuzenleyebilir(SatinalmaTalep talep)
    {
        if (!ModulYazabilir("Satınalma"))
            return false;

        return SatinalmaTalepYetkileri.TalepDuzenleyebilir(
            OturumYoneticisi.AktifKullanici?.Rol,
            talep,
            OturumYoneticisi.AktifKullanici?.Uid,
            AktifKullaniciAdi());
    }

    public static bool SatinalmaTalepKalemDuzenleyebilir(SatinalmaTalep talep)
    {
        if (!ModulYazabilir("Satınalma"))
            return false;

        return SatinalmaTalepYetkileri.TalepKalemDuzenleyebilir(
            OturumYoneticisi.AktifKullanici?.Rol,
            talep,
            OturumYoneticisi.AktifKullanici?.Uid,
            AktifKullaniciAdi());
    }

    /// <summary>Satınalma — talep eden adı ve tarih düzenleyebilir.</summary>
    public static bool SatinalmaTalepMetaDuzenleyebilir() =>
        ModulYazabilir("Satınalma")
        && SatinalmaTalepYetkileri.TalepMetaDuzenleyebilir(OturumYoneticisi.AktifKullanici?.Rol);

    public static bool YonetimeYenidenGonderebilir(SatinalmaTalep talep) =>
        ModulYazabilir("Satınalma")
        && SatinalmaTalepYetkileri.YonetimeYenidenGonderebilir(
            OturumYoneticisi.AktifKullanici?.Rol, talep);

    /// <summary>Yönetim / satınalma: teklif ve talep onayı verebilir.</summary>
    public static bool TeklifOnayVerebilir() => SatinalmaFirmaOnayiDuzenlenebilir();

    /// <summary>Yönetim / satınalma: direkt onay, acil onay, teklif iste, red.</summary>
    public static bool YonetimKararVerebilir()
    {
        if (!OturumYoneticisi.BulutAktif)
            return true;

        var kullanici = OturumYoneticisi.AktifKullanici;
        if (kullanici is null || !kullanici.Aktif)
            return false;

        if (AdminMi)
            return true;

        var rol = KullaniciRolleri.Normalize(kullanici.Rol);
        return rol is KullaniciRolleri.Yonetim or KullaniciRolleri.Satinalma
               && ModulYazabilir("Satınalma");
    }

    /// <summary>Yönetim / satınalma: onaylanmış talepte firma atamasını düzenleyebilir veya geri alabilir.</summary>
    public static bool SatinalmaFirmaOnayiDuzenlenebilir()
    {
        if (!OturumYoneticisi.BulutAktif)
            return true;

        var kullanici = OturumYoneticisi.AktifKullanici;
        if (kullanici is null || !kullanici.Aktif)
            return false;

        if (AdminMi)
            return true;

        var rol = KullaniciRolleri.Normalize(kullanici.Rol);
        if (rol is KullaniciRolleri.Yonetim or KullaniciRolleri.Satinalma)
            return ModulYazabilir("Satınalma");

        return false;
    }

    /// <summary>Admin/satınalma: tüm talepler; şef/saha: yalnızca kendi talepleri.</summary>
    public static bool SatinalmaTalepSilebilir(SatinalmaTalep? talep = null)
    {
        if (!ModulYazabilir("Satınalma"))
            return false;

        if (!OturumYoneticisi.BulutAktif)
            return true;

        var kullanici = OturumYoneticisi.AktifKullanici;
        if (kullanici is null || !kullanici.Aktif)
            return false;

        if (talep is null)
            return SatinalmaTalepYetkileri.SatinalmaTamYetki(kullanici.Rol);

        return SatinalmaTalepYetkileri.TalepSilebilir(
            kullanici.Rol, talep, kullanici.Uid, AktifKullaniciAdi());
    }

    public static string? AktifKullaniciAdi()
    {
        var k = OturumYoneticisi.AktifKullanici;
        if (k is null)
            return null;
        return string.IsNullOrWhiteSpace(k.AdSoyad) ? null : k.AdSoyad.Trim();
    }

    public static bool Duzenleyebilir
    {
        get
        {
            if (!OturumYoneticisi.BulutAktif)
                return true;

            var kullanici = OturumYoneticisi.AktifKullanici;
            if (kullanici is null || !kullanici.Aktif)
                return false;

            if (KullaniciRolleri.AdminMi(kullanici.Rol))
                return true;

            var rol = KullaniciRolleri.Normalize(kullanici.Rol);
            if (rol is KullaniciRolleri.Satinalma or KullaniciRolleri.Yonetim
                or KullaniciRolleri.Sef or KullaniciRolleri.Saha)
                return true;

            return kullanici.ModulYetkileri.Any(y =>
                y.Yazma && ModulYazabilir(y.Modul));
        }
    }

    public static bool AdminMi =>
        !OturumYoneticisi.BulutAktif ||
        KullaniciRolleri.AdminMi(OturumYoneticisi.AktifKullanici?.Rol);

    public static void ModulErisiminiUygula(FrameworkElement kok, string modulAdi)
    {
        if (ModulYazabilir(modulAdi))
            return;

        SaltOkunurModuYardimcisi.Uygula(kok);
    }

    public static void SekmeleriUygula(TabControl sekmeler, string modulAdi)
    {
        foreach (TabItem sekme in sekmeler.Items)
        {
            var baslik = sekme.Header?.ToString() ?? "";
            sekme.Visibility = SekmeGorebilir(modulAdi, baslik)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        if (sekmeler.SelectedItem is TabItem secili &&
            secili.Visibility != Visibility.Visible &&
            sekmeler.Items.Count > 0)
        {
            var ilk = sekmeler.Items.Cast<TabItem>().FirstOrDefault(t => t.Visibility == Visibility.Visible);
            if (ilk is not null)
                sekmeler.SelectedItem = ilk;
        }
    }

    /// <summary>Mal kabul ve stoğa aktarım — Satınalma ve Depo.</summary>
    public static bool MalKabulVeStokAktarYapabilir()
    {
        if (!OturumYoneticisi.BulutAktif)
            return true;

        var rol = OturumYoneticisi.AktifKullanici?.Rol;
        return KullaniciRolleri.Normalize(rol) is KullaniciRolleri.Admin
            or KullaniciRolleri.Satinalma
            or KullaniciRolleri.Depo;
    }

    public static bool YonetimIslemiYapabilir()
    {
        if (!OturumYoneticisi.BulutAktif)
            return true;

        var rol = OturumYoneticisi.AktifKullanici?.Rol;
        return AdminMi || KullaniciRolleri.Normalize(rol) is KullaniciRolleri.Yonetim or KullaniciRolleri.Satinalma;
    }

    public static bool TalepOlusturabilir()
    {
        if (!OturumYoneticisi.BulutAktif)
            return true;

        return SatinalmaPro.Shared.Models.KullaniciRolleri.TalepOlusturabilir(OturumYoneticisi.AktifKullanici?.Rol);
    }

    private static ModulYetkiKaydi? ModulYetkisiniBul(KullaniciProfili kullanici, string modulAdi) =>
        kullanici.ModulYetkileri.FirstOrDefault(y =>
            y.Modul.Equals(modulAdi, StringComparison.OrdinalIgnoreCase));

    private static bool RolVarsayilanGorebilir(string rol, string modulAdi) =>
        KullaniciRolleri.VarsayilanModuller(rol)
            .Contains(modulAdi, StringComparer.OrdinalIgnoreCase);
}

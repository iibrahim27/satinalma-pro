using System.Windows;
using SatinalmaPro.Models;
using SatinalmaPro.Services;
using SatinalmaPro.Shared.Services;

namespace SatinalmaPro;

public static class MasaustuBildirimNavigasyon
{
    public static void BildirimdenGit(BildirimKaydi bildirim)
    {
        if (Application.Current?.MainWindow is not MainWindow mw)
            return;

        if (!string.IsNullOrWhiteSpace(bildirim.DeepLink) &&
            DeepLinkServisi.Coz(bildirim.DeepLink) is { } dl)
        {
            mw.BildirimdenModuleGit(DeepLinkServisi.MasaustuHedef(dl));
            return;
        }

        if (!string.IsNullOrWhiteSpace(bildirim.DesktopRoute))
        {
            var parcalar = bildirim.DesktopRoute.Split('|');
            if (parcalar.Length >= 2)
            {
                Guid? talepId = parcalar.Length > 2 && Guid.TryParse(parcalar[2], out var id) ? id : bildirim.TalepId;
                mw.BildirimdenModuleGit(new MasaustuBildirimHedef(parcalar[0], parcalar[1], 0, talepId));
                return;
            }
        }

        var rol = OturumYoneticisi.AktifKullanici?.Rol;
        var hedef = BildirimRotaServisi.MasaustuHedef(new SatinalmaPro.Shared.Models.BildirimKaydi
        {
            Tip = bildirim.Tip,
            TalepId = bildirim.TalepId
        }, rol);

        mw.BildirimdenModuleGit(hedef);
    }
}

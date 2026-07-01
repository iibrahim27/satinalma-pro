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

        var hedef = BildirimRotaServisi.MasaustuHedef(new SatinalmaPro.Shared.Models.BildirimKaydi
        {
            Tip = bildirim.Tip,
            TalepId = bildirim.TalepId
        });

        mw.BildirimdenModuleGit(hedef);
    }
}

using SatinalmaPro.Models;
using SatinalmaPro.Helpers;

namespace SatinalmaPro.Services;

public static class SatinalmaTalepSilmeYardimcisi
{
    public static async Task SilAsync(SatinalmaTalep talep, CancellationToken iptal = default)
    {
        var talepId = talep.Id;
        var belgeNolari = BelgeNumaralariniTopla(talep);

        BildirimDeposu.Bildirimler.RemoveAll(b => b.TalepId == talepId);

        var silinenMalzeme = ModulVeriDeposu.AlinanMalzemeler
            .Where(k => MalzemeKaydiSilinmeli(k, talepId, belgeNolari))
            .ToList();

        foreach (var kayit in silinenMalzeme)
            ModulVeriDeposu.AlinanMalzemeler.Remove(kayit);

        SatinalmaDepo.Talepler.Remove(talep);
        SatinalmaTalepSenkronYardimcisi.SilindiIsaretle(talepId, SatinalmaDepo.Ayarlar);
        SatinalmaDepo.Kaydet();

        if (silinenMalzeme.Count > 0)
            ModulVeriDeposu.KaydetAlinanMalzemeler();

        if (OturumYoneticisi.BulutAktif && OturumYoneticisi.GirisYapildi)
        {
            await BulutVeriSenkronu.SilmeSonrasiGonderAsync(iptal);
            await BildirimDeposu.KaydetAsync(iptal);
        }
    }

    private static HashSet<string> BelgeNumaralariniTopla(SatinalmaTalep talep)
    {
        var belgeNolari = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(talep.TalepNo))
            belgeNolari.Add(talep.TalepNo.Trim());

        if (!string.IsNullOrWhiteSpace(talep.SiparisNo))
            belgeNolari.Add(talep.SiparisNo.Trim());

        if (talep.FirmaSiparisNolari is not null)
        {
            foreach (var no in talep.FirmaSiparisNolari.Values)
            {
                if (!string.IsNullOrWhiteSpace(no))
                    belgeNolari.Add(no.Trim());
            }
        }

        return belgeNolari;
    }

    private static bool MalzemeKaydiSilinmeli(AlinanMalzemeKaydi kayit, Guid talepId, HashSet<string> belgeNolari)
    {
        if (kayit.SatinalmaTalepId == talepId)
            return true;

        if (kayit.SatinalmaTalepId is not null)
            return false;

        var faturaNo = kayit.FaturaNo.Trim();
        return !string.IsNullOrWhiteSpace(faturaNo) && belgeNolari.Contains(faturaNo);
    }
}

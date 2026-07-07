using SatinalmaPro.Models;

namespace SatinalmaPro.Services;

public static class AlinanMalzemeAktarimServisi
{
    public static string AktarimBelgeNo(OnaylananMalzemeSatiri satir) =>
        string.IsNullOrWhiteSpace(satir.SiparisNo) ? satir.TalepNo.Trim() : satir.SiparisNo.Trim();

    public static bool DahaOnceAktarildi(OnaylananMalzemeSatiri satir, IEnumerable<AlinanMalzemeKaydi> kayitlar)
    {
        var belgeNo = AktarimBelgeNo(satir);
        var talepNo = satir.TalepNo.Trim();
        var siparisNo = satir.SiparisNo.Trim();

        foreach (var kayit in kayitlar)
        {
            if (satir.TalepId != Guid.Empty && satir.KalemId != Guid.Empty &&
                kayit.SatinalmaTalepId == satir.TalepId &&
                kayit.SatinalmaKalemId == satir.KalemId)
                return true;

            if (Eslesir(kayit, belgeNo, satir.Malzeme))
                return true;

            if (!string.IsNullOrWhiteSpace(talepNo) &&
                !talepNo.Equals(belgeNo, StringComparison.OrdinalIgnoreCase) &&
                Eslesir(kayit, talepNo, satir.Malzeme))
                return true;

            if (!string.IsNullOrWhiteSpace(siparisNo) &&
                !siparisNo.Equals(belgeNo, StringComparison.OrdinalIgnoreCase) &&
                Eslesir(kayit, siparisNo, satir.Malzeme))
                return true;
        }

        return false;
    }

    public static string TekrarAktarimUyariMetni(OnaylananMalzemeSatiri satir)
    {
        var belgeNo = AktarimBelgeNo(satir);
        var noBilgi = string.IsNullOrWhiteSpace(satir.SiparisNo)
            ? $"Talep No: {satir.TalepNo}"
            : $"Sipariş No: {satir.SiparisNo} (Talep: {satir.TalepNo})";

        return $"{satir.Malzeme}\n{noBilgi}\n\nBu kalem daha önce Alınan Malzemelere aktarılmış.\nAynı talep/sipariş numarasıyla tekrar aktarılamaz.";
    }

    private static bool Eslesir(AlinanMalzemeKaydi kayit, string belgeNo, string malzeme) =>
        !string.IsNullOrWhiteSpace(belgeNo) &&
        kayit.FaturaNo.Equals(belgeNo, StringComparison.OrdinalIgnoreCase) &&
        kayit.MalzemeHizmet.Equals(malzeme, StringComparison.OrdinalIgnoreCase);

    public static void StogaGirisKaydet(
        OnaylananMalzemeSatiri satir,
        double miktar,
        string kategori,
        string tarih,
        string teslimAlan,
        string depoSaha,
        bool sahayaDirekt = false,
        string? sahaHedef = null)
    {
        var belgeNo = StokBelgeNoUretici.SonrakiGirisBelgeNo();
        var islemYapan = KullaniciYetkileri.AktifKullaniciAdi() ?? "";

        StokIslemServisi.GirisYap(
            tarih,
            satir.Malzeme,
            kategori,
            satir.Birim,
            miktar,
            depoSaha,
            satir.BirimFiyati,
            belgeNo,
            islemYapan,
            teslimAlan);

        if (sahayaDirekt && !string.IsNullOrWhiteSpace(sahaHedef))
        {
            StokIslemServisi.CikisYap(
                tarih,
                satir.Malzeme,
                depoSaha,
                miktar,
                $"{belgeNo}-Ç",
                islemYapan,
                sahaHedef.Trim());
        }
    }
}

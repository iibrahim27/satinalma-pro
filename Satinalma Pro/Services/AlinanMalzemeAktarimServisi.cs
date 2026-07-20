using SatinalmaPro.Models;

namespace SatinalmaPro.Services;

public sealed record SahayaCikisSatiri(
    string Malzeme,
    double Miktar,
    string Birim,
    string DepoSaha,
    string CikisBelgeNo);

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

    /// <summary>
    /// Excel / manuel Alınan Malzeme kaydından stok giriş hareketi oluşturur.
    /// Tarih = alınan malzeme tarihi; depo = indirildiği saha (boşsa Genel).
    /// </summary>
    public static StokHareketKaydi? ExcelKayittanStokGiris(AlinanMalzemeKaydi kayit)
    {
        if (string.IsNullOrWhiteSpace(kayit.MalzemeHizmet) || kayit.Miktar <= 0.0001)
            return null;

        var depo = string.IsNullOrWhiteSpace(kayit.IndirildigiSaha)
            ? "Genel"
            : kayit.IndirildigiSaha.Trim();
        var kategori = string.IsNullOrWhiteSpace(kayit.Kategori) ? "Malzeme" : kayit.Kategori.Trim();
        var birim = string.IsNullOrWhiteSpace(kayit.Birim) ? "Adet" : kayit.Birim.Trim();
        var tarih = string.IsNullOrWhiteSpace(kayit.Tarih)
            ? DateTime.Now.ToString("dd.MM.yyyy")
            : kayit.Tarih.Trim();
        var belgeNo = !string.IsNullOrWhiteSpace(kayit.FaturaNo)
            ? kayit.FaturaNo.Trim()
            : StokBelgeNoUretici.SonrakiGirisBelgeNo();
        var islemYapan = KullaniciYetkileri.AktifKullaniciAdi() ?? "";

        MalzemeKategoriDeposu.Ekle(kategori);

        return StokIslemServisi.GirisYap(
            tarih,
            kayit.MalzemeHizmet.Trim(),
            kategori,
            birim,
            kayit.Miktar,
            depo,
            kayit.BirimFiyati,
            belgeNo,
            islemYapan,
            kayit.TeslimAlan?.Trim() ?? "");
    }

    /// <summary>
    /// Mal kabul sonrası stok girişi; sahaya direkt ise aynı anda çıkış (teslim alana).
    /// </summary>
    public static SahayaCikisSatiri? StogaGirisKaydet(
        OnaylananMalzemeSatiri satir,
        double miktar,
        string kategori,
        string tarih,
        string teslimAlan,
        string depoSaha,
        bool sahayaDirekt = false,
        string? sahaHedef = null,
        string? ortakCikisBelgeNo = null)
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

        if (!sahayaDirekt || string.IsNullOrWhiteSpace(sahaHedef))
            return null;

        var cikisBelge = string.IsNullOrWhiteSpace(ortakCikisBelgeNo)
            ? $"{belgeNo}-Ç"
            : ortakCikisBelgeNo.Trim();

        // Teslim edilen = teslim alan kişi (fiş imza alanı); saha hedefi açıklamada.
        var hareket = StokIslemServisi.CikisYap(
            tarih,
            satir.Malzeme,
            depoSaha,
            miktar,
            cikisBelge,
            islemYapan,
            teslimAlan.Trim());
        hareket.Aciklama = $"Sahaya indirme: {sahaHedef.Trim()}";

        return new SahayaCikisSatiri(
            satir.Malzeme,
            miktar,
            satir.Birim,
            depoSaha,
            cikisBelge);
    }

    public static void SahayaCikisFisiYazdir(
        string tarih,
        string teslimAlan,
        string? sahaHedef,
        IReadOnlyList<SahayaCikisSatiri> satirlar)
    {
        if (satirlar.Count == 0)
            return;

        var belgeNo = satirlar[0].CikisBelgeNo;
        var fis = new StokCikisFisVerisi(
            belgeNo,
            tarih,
            StokCikisPdfOlusturucu.TeslimEdenMetni(),
            teslimAlan.Trim(),
            satirlar.Select(s => new StokCikisFisSatir(
                s.Malzeme,
                s.Miktar.ToString("N2"),
                s.Birim,
                s.DepoSaha)).ToList(),
            string.IsNullOrWhiteSpace(sahaHedef) ? null : sahaHedef.Trim());

        StokCikisPdfOlusturucu.OnizleVeYazdir(fis);
    }
}

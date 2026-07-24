using System.Globalization;

using SatinalmaPro.Helpers;

using SatinalmaPro.Models;



namespace SatinalmaPro.Services;



public static class RaporlamaServisi
{
    public static void VerileriYukle()

    {

        UygulamaVeriDeposu.OrnekVeriyiYukle();

        SatinalmaDepo.Yukle();

    }



    public static List<string> TumKategoriler()

    {

        VerileriYukle();

        return HamDetaySatirlari(new RaporFiltreleri { Modul = RaporModulleri.Tumu })

            .Select(s => s.Kategori)

            .Where(k => !string.IsNullOrWhiteSpace(k))

            .Distinct(StringComparer.CurrentCultureIgnoreCase)

            .OrderBy(k => k, StringComparer.CurrentCultureIgnoreCase)

            .ToList();

    }



    public static List<string> TumMalzemeler(IReadOnlyList<string>? kategoriler = null)
    {
        VerileriYukle();

        var filtre = new RaporFiltreleri
        {
            Modul = RaporModulleri.Tumu,
            Kategoriler = kategoriler ?? []
        };

        return HamDetaySatirlari(filtre)
            .Select(s => s.Aciklama)
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(m => m, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }



    public static List<RaporModulOzeti> ModulOzetleri(RaporFiltreleri filtre)
    {
        var tumModulFiltresi = new RaporFiltreleri
        {
            Baslangic = filtre.Baslangic,
            Bitis = filtre.Bitis,
            Modul = RaporModulleri.Tumu,
            RaporTuru = filtre.RaporTuru,
            Kategoriler = filtre.Kategoriler,
            Malzemeler = filtre.Malzemeler
        };
        var detaylar = FiltreliDetaySatirlari(tumModulFiltresi);

        return

        [

            OzetOlustur(RaporModulleri.AlinanMalzemeler, "#6366F1", detaylar),

            OzetOlustur(RaporModulleri.StokYonetimi, "#14B8A6", detaylar),

            OzetOlustur(RaporModulleri.Agrega, "#10B981", detaylar),

            OzetOlustur(RaporModulleri.Cimento, "#64748B", detaylar),

            OzetOlustur(RaporModulleri.Akaryakit, "#F59E0B", detaylar),

            OzetOlustur(RaporModulleri.Filo, "#3B82F6", detaylar),

            OzetOlustur(RaporModulleri.Satinalma, "#F43F5E", detaylar)

        ];

    }



    public static decimal GenelToplam(RaporFiltreleri filtre) =>

        ModulOzetleri(filtre).Sum(o => o.ToplamTutar);



    public static List<RaporDetaySatiri> DetaySatirlari(RaporFiltreleri filtre) =>

        FiltreliDetaySatirlari(filtre);



    public static List<RaporGrupOzeti> TedarikciOzeti(RaporFiltreleri filtre) =>

        GrupOzeti(FiltreliDetaySatirlari(filtre), s => string.IsNullOrWhiteSpace(s.Tedarikci) ? "Belirtilmemiş" : s.Tedarikci);



    public static List<RaporGrupOzeti> SahaOzeti(RaporFiltreleri filtre) =>

        GrupOzeti(FiltreliDetaySatirlari(filtre), s => string.IsNullOrWhiteSpace(s.Saha) ? "Belirtilmemiş" : s.Saha);



    public static List<RaporGrupOzeti> KategoriOzeti(RaporFiltreleri filtre) =>

        GrupOzeti(FiltreliDetaySatirlari(filtre), s => string.IsNullOrWhiteSpace(s.Kategori) ? "Belirtilmemiş" : s.Kategori);



    public static List<RaporMalzemeAnalizi> MalzemeAnalizleri(IReadOnlyList<RaporDetaySatiri> satirlar)

    {

        return satirlar

            .GroupBy(s => s.Aciklama, StringComparer.CurrentCultureIgnoreCase)

            .Select(g =>

            {

                var sirali = g

                    .Select(s => (s, tarih: TarihCoz(s.Tarih)))

                    .OrderBy(x => x.tarih)

                    .ToList();



                var fiyatli = sirali.Where(x => x.s.BirimFiyati > 0).ToList();

                var ilk = fiyatli.FirstOrDefault();

                var son = fiyatli.LastOrDefault();



                double? toplamArtis = null;
                decimal karZiyanTl = 0;
                decimal karZiyanToplamTl = 0;

                if (ilk.s != null && son.s != null && ilk.s.BirimFiyati > 0)
                {
                    karZiyanTl = son.s.BirimFiyati - ilk.s.BirimFiyati;
                    karZiyanToplamTl = Math.Round(karZiyanTl * (decimal)g.Sum(x => x.Miktar), 2);

                    if (!ReferenceEquals(ilk.s, son.s))
                        toplamArtis = (double)(karZiyanTl / ilk.s.BirimFiyati * 100m);
                }



                var birimler = g.Select(x => x.Birim).Where(b => !string.IsNullOrWhiteSpace(b)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();



                return new RaporMalzemeAnalizi

                {

                    MalzemeAdi = g.Key,

                    Kategori = g.Select(x => x.Kategori).FirstOrDefault(k => !string.IsNullOrWhiteSpace(k)) ?? "—",

                    KayitSayisi = g.Count(),

                    ToplamMiktar = g.Sum(x => x.Miktar),

                    Birim = birimler.Count == 1 ? birimler[0] : birimler.Count > 1 ? "Karışık" : "—",

                    MinBirimFiyat = fiyatli.Count > 0 ? fiyatli.Min(x => x.s.BirimFiyati) : 0,

                    MaxBirimFiyat = fiyatli.Count > 0 ? fiyatli.Max(x => x.s.BirimFiyati) : 0,

                    OrtBirimFiyat = fiyatli.Count > 0
                        ? Math.Round((decimal)fiyatli.Average(x => (double)x.s.BirimFiyati), 2, MidpointRounding.AwayFromZero)
                        : 0,

                    IlkBirimFiyat = ilk.s?.BirimFiyati ?? 0,

                    SonBirimFiyat = son.s?.BirimFiyati ?? 0,

                    ToplamArtisYuzdesi = toplamArtis,

                    ToplamTutar = g.Sum(x => x.Tutar),

                    KarZiyanTl = karZiyanTl,

                    KarZiyanToplamTl = karZiyanToplamTl

                };

            })

            .OrderByDescending(a => a.ToplamTutar)

            .ToList();

    }

    public static List<RaporAylikAlimOzeti> AylikAlimOzetleri(IReadOnlyList<RaporDetaySatiri> satirlar)
    {
        var tr = CultureInfo.GetCultureInfo("tr-TR");
        var gruplar = satirlar
            .Select(s => (s, tarih: TarihYardimcisi.TryParse(s.Tarih, out var dt) ? dt : DateTime.MinValue))
            .Where(x => x.tarih != DateTime.MinValue)
            .GroupBy(x => new { x.tarih.Year, x.tarih.Month })
            .OrderBy(g => g.Key.Year)
            .ThenBy(g => g.Key.Month)
            .ToList();

        var sonuclar = new List<RaporAylikAlimOzeti>();
        decimal? oncekiOrtFiyat = null;

        foreach (var g in gruplar)
        {
            var liste = g.Select(x => x.s).ToList();
            var miktar = liste.Sum(s => s.Miktar);
            var tutar = liste.Sum(s => s.Tutar);
            var birimler = liste
                .Select(s => s.Birim)
                .Where(b => !string.IsNullOrWhiteSpace(b))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var ortFiyat = miktar > 0
                ? Math.Round(tutar / (decimal)miktar, 2, MidpointRounding.AwayFromZero)
                : liste.Where(s => s.BirimFiyati > 0).Any()
                    ? Math.Round((decimal)liste.Where(s => s.BirimFiyati > 0).Average(s => (double)s.BirimFiyati), 2,
                        MidpointRounding.AwayFromZero)
                    : 0m;

            decimal? artisTl = null;
            double? artisYuzde = null;
            if (oncekiOrtFiyat is > 0 && ortFiyat > 0)
            {
                artisTl = ortFiyat - oncekiOrtFiyat.Value;
                artisYuzde = (double)(artisTl.Value / oncekiOrtFiyat.Value * 100m);
            }

            if (ortFiyat > 0)
                oncekiOrtFiyat = ortFiyat;

            sonuclar.Add(new RaporAylikAlimOzeti
            {
                AyEtiketi = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMMM yyyy", tr),
                ToplamMiktar = miktar,
                Birim = birimler.Count switch
                {
                    0 => "—",
                    1 => birimler[0],
                    _ => "Karışık"
                },
                ToplamTutar = tutar,
                OrtBirimFiyat = ortFiyat,
                ArtisTl = artisTl,
                ArtisYuzdesi = artisYuzde
            });
        }

        return sonuclar;
    }



    public static string FiltreOzetiMetni(RaporFiltreleri filtre)

    {

        var parcalar = new List<string> { filtre.RaporTuru };



        if (filtre.Baslangic is DateTime b)

            parcalar.Add($"Başlangıç: {b:dd.MM.yyyy}");

        if (filtre.Bitis is DateTime e)

            parcalar.Add($"Bitiş: {e:dd.MM.yyyy}");

        if (filtre.Modul != RaporModulleri.Tumu)

            parcalar.Add($"Modül: {filtre.Modul}");

        if (filtre.Kategoriler.Count > 0)

            parcalar.Add($"Kategori: {string.Join(", ", filtre.Kategoriler)}");

        if (filtre.Malzemeler.Count > 0)

            parcalar.Add($"Malzeme: {string.Join(", ", filtre.Malzemeler)}");



        return string.Join(" · ", parcalar);

    }



    private static List<RaporDetaySatiri> FiltreliDetaySatirlari(RaporFiltreleri filtre)

    {

        var liste = HamDetaySatirlari(filtre);

        RaporArtisHesaplayici.Hesapla(liste);

        return liste;

    }



    private static List<RaporDetaySatiri> HamDetaySatirlari(RaporFiltreleri filtre)

    {

        VerileriYukle();

        var liste = new List<RaporDetaySatiri>();

        var modul = filtre.Modul;



        if (ModulSecili(modul, RaporModulleri.AlinanMalzemeler))

        {

            foreach (var k in UygulamaVeriDeposu.AlinanMalzemeler)

            {

                if (!KayitUygunMu(k.Tarih, k.Kategori, k.MalzemeHizmet, filtre)) continue;

                k.ToplamTutariHesapla();

                liste.Add(SatirOlustur(RaporModulleri.AlinanMalzemeler, k.Tarih, k.FaturaNo, k.MalzemeHizmet,

                    k.Kategori, k.Tedarikci, k.IndirildigiSaha, k.Miktar, k.Birim, k.BirimFiyati, k.ToplamTutar));

            }

        }



        if (ModulSecili(modul, RaporModulleri.StokYonetimi))

        {

            foreach (var k in ModulVeriDeposu.Stok)

            {

                if (!KayitUygunMu(k.SonGuncelleme, k.Kategori, k.MalzemeAdi, filtre)) continue;

                k.ToplamDegerHesapla();

                liste.Add(SatirOlustur(RaporModulleri.StokYonetimi, k.SonGuncelleme, "", k.MalzemeAdi,

                    k.Kategori, k.DepoSaha, k.DepoSaha, k.MevcutMiktar, k.Birim, k.BirimMaliyet, k.ToplamDeger));

            }

        }



        if (ModulSecili(modul, RaporModulleri.Agrega))

        {

            foreach (var k in ModulVeriDeposu.Agrega)

            {

                var malzeme = $"{k.AgregaTuru} — {k.AgregaCinsi}";

                if (!KayitUygunMu(k.Tarih, k.AgregaTuru, malzeme, filtre)) continue;

                k.ToplamTutariHesapla();

                liste.Add(SatirOlustur(RaporModulleri.Agrega, k.Tarih, k.IrsaliyeNo, malzeme,

                    k.AgregaTuru, k.Tedarikci, k.IndirildigiSaha, k.Miktar, k.Birim, k.BirimFiyati, k.ToplamTutar));

            }

        }



        if (ModulSecili(modul, RaporModulleri.Cimento))

        {

            foreach (var k in ModulVeriDeposu.Cimento)

            {

                var malzeme = $"{k.CimentoSinifi} — {k.CimentoCinsi}";

                if (!KayitUygunMu(k.Tarih, k.CimentoSinifi, malzeme, filtre)) continue;

                k.ToplamTutariHesapla();

                liste.Add(SatirOlustur(RaporModulleri.Cimento, k.Tarih, k.IrsaliyeNo, malzeme,

                    k.CimentoSinifi, k.Tedarikci, k.IndirildigiSaha, k.Miktar, k.Birim, k.BirimFiyati, k.ToplamTutar));

            }

        }



        if (ModulSecili(modul, RaporModulleri.Akaryakit))

        {

            foreach (var k in ModulVeriDeposu.Akaryakit.Where(a => a.AlinanKayit))

            {

                var malzeme = $"{k.YakitTuru} — {k.AracMakineAdi}";

                if (!KayitUygunMu(k.Tarih, k.YakitTuru, malzeme, filtre)) continue;

                k.ToplamTutariHesapla();

                liste.Add(SatirOlustur(RaporModulleri.Akaryakit, k.Tarih, k.FaturaNo, malzeme,

                    k.YakitTuru, k.Istasyon, k.Saha, k.Miktar, k.Birim, k.BirimFiyati, k.ToplamTutar));

            }

        }



        if (ModulSecili(modul, RaporModulleri.Filo))

        {

            foreach (var g in ModulVeriDeposu.FiloGiderleri.Where(g => g.Tutar > 0))

            {

                var arac = ModulVeriDeposu.FiloAraclari.FirstOrDefault(a =>
                    a.Plaka.Equals(g.Plaka, StringComparison.OrdinalIgnoreCase));
                var malzeme = $"{g.GiderTipi} — {g.Plaka} {arac?.MarkaModel}".Trim();

                if (!KayitUygunMu(g.Tarih, g.GiderTipi, malzeme, filtre)) continue;

                liste.Add(SatirOlustur(RaporModulleri.Filo, g.Tarih, g.BelgeNo, malzeme,

                    g.GiderTipi, arac?.Sirket ?? "—", arac?.Saha ?? "—", 1, "Adet", g.Tutar, g.Tutar));

            }

        }



        if (ModulSecili(modul, RaporModulleri.Satinalma))

            liste.AddRange(SatinalmaDetaySatirlari(filtre));



        return liste.OrderByDescending(s => TarihCoz(s.Tarih)).ThenBy(s => s.Modul).ToList();

    }



    private static RaporDetaySatiri SatirOlustur(

        string modul, string tarih, string belgeNo, string aciklama, string kategori,

        string tedarikci, string saha, double miktar, string birim, decimal birimFiyati, decimal tutar) =>

        new()

        {

            Modul = modul,

            Tarih = tarih,

            BelgeNo = belgeNo,

            Aciklama = aciklama,

            Kategori = kategori,

            Tedarikci = tedarikci,

            Saha = saha,

            Miktar = miktar,

            Birim = birim,

            BirimFiyati = birimFiyati,

            Tutar = tutar

        };



    private static bool KayitUygunMu(string tarih, string kategori, string malzeme, RaporFiltreleri filtre)

    {

        if (!TarihAralikta(tarih, filtre.Baslangic, filtre.Bitis))

            return false;



        if (filtre.Kategoriler.Count > 0 &&

            !filtre.Kategoriler.Any(k => k.Equals(kategori, StringComparison.CurrentCultureIgnoreCase)))

            return false;



        if (filtre.Malzemeler.Count > 0 &&

            !filtre.Malzemeler.Any(m => m.Equals(malzeme, StringComparison.CurrentCultureIgnoreCase)))

            return false;



        return true;

    }



    private static List<RaporDetaySatiri> SatinalmaDetaySatirlari(RaporFiltreleri filtre)

    {

        var liste = new List<RaporDetaySatiri>();

        foreach (var talep in SatinalmaDepo.Talepler)

        {

            if (!TarihAralikta(talep.Tarih, filtre.Baslangic, filtre.Bitis)) continue;

            if (!talep.HerhangiKalemOnayli) continue;



            foreach (var kalem in talep.Kalemler.Where(k => k.OnaylananTeklifId != null))

            {

                if (filtre.Kategoriler.Count > 0 &&

                    !filtre.Kategoriler.Any(k => k.Equals(talep.Durum, StringComparison.CurrentCultureIgnoreCase)))

                    continue;



                if (filtre.Malzemeler.Count > 0 &&

                    !filtre.Malzemeler.Any(m => m.Equals(kalem.Malzeme, StringComparison.CurrentCultureIgnoreCase)))

                    continue;



                var teklif = talep.KalemOnayTeklifi(kalem);

                if (teklif == null) continue;

                var fiyat = teklif.Fiyatlar.FirstOrDefault(f => f.KalemId == kalem.Id);

                if (fiyat == null) continue;



                liste.Add(SatirOlustur(

                    RaporModulleri.Satinalma,

                    talep.Tarih,

                    string.IsNullOrWhiteSpace(talep.SiparisNo) ? talep.TalepNo : talep.SiparisNo,

                    kalem.Malzeme,

                    talep.Durum,

                    teklif.FirmaAdi,

                    "—",

                    kalem.Miktar,

                    kalem.Birim,

                    fiyat.BirimFiyat,

                    fiyat.ToplamTutar));

            }

        }



        return liste;

    }



    private static RaporModulOzeti OzetOlustur(string modul, string renk, List<RaporDetaySatiri> detaylar)

    {

        var modulSatirlari = detaylar.Where(d => d.Modul == modul).ToList();

        return new RaporModulOzeti

        {

            ModulAdi = modul,

            KayitSayisi = modulSatirlari.Count,

            ToplamTutar = modulSatirlari.Sum(s => s.Tutar),

            Renk = renk

        };

    }



    private static List<RaporGrupOzeti> GrupOzeti(List<RaporDetaySatiri> satirlar, Func<RaporDetaySatiri, string> grupSec) =>

        satirlar

            .GroupBy(grupSec, StringComparer.CurrentCultureIgnoreCase)

            .Select(g => new RaporGrupOzeti

            {

                GrupAdi = g.Key,

                KayitSayisi = g.Count(),

                ToplamTutar = g.Sum(x => x.Tutar),

                ModulDagilimi = string.Join(", ", g.GroupBy(x => x.Modul).Select(m => $"{m.Key} ({m.Count()})"))

            })

            .OrderByDescending(g => g.ToplamTutar)

            .ToList();



    private static bool ModulSecili(string secim, string modul) =>

        secim == RaporModulleri.Tumu || secim.Equals(modul, StringComparison.OrdinalIgnoreCase);



    private static bool TarihAralikta(string tarih, DateTime? baslangic, DateTime? bitis) =>
        TarihYardimcisi.Aralikta(tarih, baslangic, bitis);

    private static DateTime TarihCoz(string tarih) =>
        TarihYardimcisi.SiralamaDegeri(tarih);

}



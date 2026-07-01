using SatinalmaPro.Models;

namespace SatinalmaPro.Services;

public static class StokIslemServisi
{
    public static StokKaydi? StokBul(string malzeme, string depo) =>
        ModulVeriDeposu.Stok.FirstOrDefault(s =>
            s.MalzemeAdi.Equals(malzeme.Trim(), StringComparison.OrdinalIgnoreCase) &&
            s.DepoSaha.Equals(depo.Trim(), StringComparison.OrdinalIgnoreCase));

    public static StokKaydi StokBulVeyaOlustur(string malzeme, string kategori, string birim, string depo, decimal birimMaliyet = 0)
    {
        var mevcut = StokBul(malzeme, depo);
        if (mevcut is not null)
            return mevcut;

        mevcut = new StokKaydi
        {
            MalzemeAdi = malzeme.Trim(),
            Kategori = kategori.Trim(),
            Birim = birim.Trim(),
            DepoSaha = depo.Trim(),
            BirimMaliyet = birimMaliyet,
            SonGuncelleme = Bugun()
        };
        ModulVeriDeposu.Stok.Add(mevcut);
        return mevcut;
    }

    public static StokHareketKaydi GirisYap(
        string tarih, string malzeme, string kategori, string birim, double miktar,
        string depo, decimal birimMaliyet, string belgeNo, string teslimEden, string teslimEdilen)
    {
        var stok = StokBulVeyaOlustur(malzeme, kategori, birim, depo, birimMaliyet);
        stok.MevcutMiktar += miktar;
        if (birimMaliyet > 0)
            stok.BirimMaliyet = birimMaliyet;
        stok.SonGuncelleme = tarih;
        stok.ToplamDegerHesapla();

        var hareket = new StokHareketKaydi
        {
            Tarih = tarih,
            HareketTipi = StokHareketTipleri.Giris,
            MalzemeAdi = stok.MalzemeAdi,
            Kategori = stok.Kategori,
            Birim = stok.Birim,
            Miktar = miktar,
            DepoSaha = stok.DepoSaha,
            BirimMaliyet = stok.BirimMaliyet,
            BelgeNo = belgeNo,
            IslemYapan = teslimEden,
            TeslimEdilen = teslimEdilen
        };
        ModulVeriDeposu.StokHareketleri.Add(hareket);
        return hareket;
    }

    public static StokHareketKaydi CikisYap(
        string tarih, string malzeme, string depo, double miktar,
        string belgeNo, string teslimEden, string teslimEdilen)
    {
        var stok = StokBul(malzeme, depo)
            ?? throw new InvalidOperationException("Bu malzeme ve depo için stok kaydı bulunamadı.");

        if (miktar > stok.MevcutMiktar)
            throw new InvalidOperationException($"Yetersiz stok. Mevcut: {stok.MevcutMiktar:N2} {stok.Birim}");

        stok.MevcutMiktar -= miktar;
        stok.SonGuncelleme = tarih;
        stok.ToplamDegerHesapla();

        var hareket = new StokHareketKaydi
        {
            Tarih = tarih,
            HareketTipi = StokHareketTipleri.Cikis,
            MalzemeAdi = stok.MalzemeAdi,
            Kategori = stok.Kategori,
            Birim = stok.Birim,
            Miktar = miktar,
            DepoSaha = stok.DepoSaha,
            BirimMaliyet = stok.BirimMaliyet,
            BelgeNo = belgeNo,
            IslemYapan = teslimEden,
            TeslimEdilen = teslimEdilen
        };
        ModulVeriDeposu.StokHareketleri.Add(hareket);
        return hareket;
    }

    public static StokHareketKaydi SayimYap(
        string tarih, StokKaydi stok, double sayimMiktar, string islemYapan, string aciklama)
    {
        var onceki = stok.MevcutMiktar;
        var fark = sayimMiktar - onceki;

        stok.MevcutMiktar = sayimMiktar;
        stok.SonGuncelleme = tarih;
        stok.ToplamDegerHesapla();

        var hareket = new StokHareketKaydi
        {
            Tarih = tarih,
            HareketTipi = StokHareketTipleri.Sayim,
            MalzemeAdi = stok.MalzemeAdi,
            Kategori = stok.Kategori,
            Birim = stok.Birim,
            Miktar = Math.Abs(fark),
            OncekiMiktar = onceki,
            SayimMiktar = sayimMiktar,
            DepoSaha = stok.DepoSaha,
            BirimMaliyet = stok.BirimMaliyet,
            IslemYapan = islemYapan,
            Aciklama = aciklama
        };
        ModulVeriDeposu.StokHareketleri.Add(hareket);
        return hareket;
    }

    public static void HareketSil(StokHareketKaydi hareket)
    {
        var stok = StokBul(hareket.MalzemeAdi, hareket.DepoSaha);
        if (stok is null)
        {
            ModulVeriDeposu.StokHareketleri.Remove(hareket);
            return;
        }

        switch (hareket.HareketTipi)
        {
            case StokHareketTipleri.Giris:
                stok.MevcutMiktar -= hareket.Miktar;
                break;
            case StokHareketTipleri.Cikis:
                stok.MevcutMiktar += hareket.Miktar;
                break;
            case StokHareketTipleri.Sayim when hareket.OncekiMiktar.HasValue:
                stok.MevcutMiktar = hareket.OncekiMiktar.Value;
                break;
        }

        stok.SonGuncelleme = Bugun();
        stok.ToplamDegerHesapla();
        ModulVeriDeposu.StokHareketleri.Remove(hareket);
    }

    public static void HareketGuncelle(
        StokHareketKaydi eski,
        string tarih,
        double miktar,
        string belgeNo,
        string islemYapan,
        string aciklama)
    {
        var id = eski.Id;
        var tip = eski.HareketTipi;
        var malzeme = eski.MalzemeAdi;
        var kategori = eski.Kategori;
        var birim = eski.Birim;
        var depo = eski.DepoSaha;
        var maliyet = eski.BirimMaliyet;

        HareketSil(eski);

        StokHareketKaydi yeni = tip switch
        {
            StokHareketTipleri.Giris => GirisYap(tarih, malzeme, kategori, birim, miktar, depo, maliyet, belgeNo, islemYapan, eski.TeslimEdilen),
            StokHareketTipleri.Cikis => CikisYap(tarih, malzeme, depo, miktar, belgeNo, islemYapan, eski.TeslimEdilen),
            StokHareketTipleri.Sayim => SayimYap(
                tarih,
                StokBul(malzeme, depo) ?? throw new InvalidOperationException("Stok kaydı bulunamadı."),
                miktar,
                islemYapan,
                aciklama),
            _ => throw new InvalidOperationException("Bilinmeyen hareket tipi.")
        };

        yeni.Id = id;
    }

    public static IEnumerable<string> MalzemeListesi(string? kategori = null, string? arama = null, bool sadeceMevcutStok = false)
    {
        var liste = ModulVeriDeposu.Stok.AsEnumerable();

        if (sadeceMevcutStok)
            liste = liste.Where(s => s.MevcutMiktar > 0);

        if (!string.IsNullOrWhiteSpace(kategori))
            liste = liste.Where(s => s.Kategori.Equals(kategori.Trim(), StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(arama))
        {
            var metin = arama.Trim();
            liste = liste.Where(s => s.MalzemeAdi.Contains(metin, StringComparison.OrdinalIgnoreCase));
        }

        return liste.Select(s => s.MalzemeAdi)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(s => s, StringComparer.CurrentCultureIgnoreCase);
    }

    public static StokKaydi? StokBulMalzemeAdi(string malzeme, string? kategori = null, bool sadeceMevcutStok = false)
    {
        var liste = ModulVeriDeposu.Stok.Where(s =>
            s.MalzemeAdi.Equals(malzeme.Trim(), StringComparison.OrdinalIgnoreCase));

        if (sadeceMevcutStok)
            liste = liste.Where(s => s.MevcutMiktar > 0);

        if (!string.IsNullOrWhiteSpace(kategori))
            liste = liste.Where(s => s.Kategori.Equals(kategori.Trim(), StringComparison.OrdinalIgnoreCase));

        return liste.OrderByDescending(s => s.SonGuncelleme).FirstOrDefault();
    }

    public static IEnumerable<StokKaydi> DepoStokListesi(string? depo = null)
    {
        var liste = ModulVeriDeposu.Stok.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(depo))
            liste = liste.Where(s => s.DepoSaha.Equals(depo, StringComparison.OrdinalIgnoreCase));
        return liste.OrderBy(s => s.MalzemeAdi);
    }

    public static AlinanMalzemeKaydi AlinanMalzemeyeKaydet(
        StokIslemSatirKaydi satir,
        string tarih,
        string belgeNo,
        string tedarikci,
        string teslimAlan)
    {
        var kayit = new AlinanMalzemeKaydi
        {
            Tarih = tarih,
            FaturaNo = belgeNo,
            Kategori = satir.Kategori,
            MalzemeHizmet = satir.Malzeme,
            Miktar = satir.Miktar,
            Birim = satir.Birim,
            BirimFiyati = satir.BirimFiyat,
            Tedarikci = tedarikci,
            IndirildigiSaha = satir.DepoSaha,
            TeslimAlan = teslimAlan,
            Aciklama = $"Stok girişi — {belgeNo}"
        };
        kayit.ToplamTutariHesapla();
        ModulVeriDeposu.AlinanMalzemeler.Add(kayit);
        return kayit;
    }

    private static string Bugun() => DateTime.Now.ToString("dd.MM.yyyy");
}

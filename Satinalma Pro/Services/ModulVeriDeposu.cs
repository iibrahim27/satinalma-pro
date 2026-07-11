using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Shared.SaaS;

namespace SatinalmaPro.Services;

public static class ModulVeriDeposu
{
    private static readonly JsonSerializerOptions JsonSecenekleri = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static ObservableCollection<AlinanMalzemeKaydi> AlinanMalzemeler { get; } = [];
    public static ObservableCollection<StokKaydi> Stok { get; } = [];
    public static ObservableCollection<StokHareketKaydi> StokHareketleri { get; } = [];
    public static ObservableCollection<AgregaKaydi> Agrega { get; } = [];
    public static ObservableCollection<CimentoKaydi> Cimento { get; } = [];
    public static ObservableCollection<AkaryakitKaydi> Akaryakit { get; } = [];
    public static ObservableCollection<FiloAracKaydi> FiloAraclari { get; } = [];
    public static ObservableCollection<FiloGiderKaydi> FiloGiderleri { get; } = [];
    public static ObservableCollection<FiloZimmetKaydi> FiloZimmetleri { get; } = [];

    private static bool _yuklendi;
    private static bool _yukleniyor;
    private static bool _abonelikKuruldu;
    private static string? _yuklenenTenantId;

    private static string KiraciDosyaAdi(string dosyaAdi)
    {
        var tid = KiracıOturumu.TenantId;
        if (string.IsNullOrWhiteSpace(tid))
            return dosyaAdi;
        return $"{Path.GetFileNameWithoutExtension(dosyaAdi)}_{tid}{Path.GetExtension(dosyaAdi)}";
    }

    private static string YerelYol(string dosyaAdi) =>
        SatinalmaProKlasor.DosyaYolu(KiraciDosyaAdi(dosyaAdi));

    public static void BeginBatch() => ErtelenmisKayit.BeginBatch();

    public static void EndBatch() => ErtelenmisKayit.EndBatch();

    /// <summary>Firma değişiminde tüm modül bellek verisini temizler.</summary>
    public static void KiraciDegisti()
    {
        _yuklendi = false;
        _yuklenenTenantId = null;
        _yukleniyor = true;
        try
        {
            AlinanMalzemeler.Clear();
            Stok.Clear();
            StokHareketleri.Clear();
            Agrega.Clear();
            Cimento.Clear();
            Akaryakit.Clear();
            FiloAraclari.Clear();
            FiloGiderleri.Clear();
            FiloZimmetleri.Clear();
        }
        finally
        {
            _yukleniyor = false;
        }
        MalzemeAdiOneriServisi.OnbellekSifirla();
    }

    public static void Yukle()
    {
        var tid = KiracıOturumu.TenantId;
        if (_yuklendi && string.Equals(_yuklenenTenantId, tid, StringComparison.Ordinal))
            return;

        if (_yuklendi && !string.Equals(_yuklenenTenantId, tid, StringComparison.Ordinal))
            KiraciDegisti();

        _yuklendi = true;
        _yuklenenTenantId = tid;
        _yukleniyor = true;

        SatinalmaProKlasor.Olustur();
        JsonOku(AlinanMalzemeler, "alinan_malzemeler.json", AlinanMalzemeOrnekVeri);
        JsonOku(Stok, "stok.json", StokOrnekVeri);
        JsonOku(StokHareketleri, "stok_hareketleri.json", StokHareketOrnekVeri);
        JsonOku(Agrega, "agrega.json", AgregaOrnekVeri);
        JsonOku(Cimento, "cimento.json", CimentoOrnekVeri);
        JsonOku(Akaryakit, "akaryakit.json", AkaryakitOrnekVeri);
        FiloOku();
        ModulTarihleriniNormalizeEt();
        MalzemeKategoriDeposu.KayitlardanSenkronizeEt();

        _yukleniyor = false;
        if (!_abonelikKuruldu)
        {
            _abonelikKuruldu = true;
            AlinanMalzemeler.CollectionChanged += (_, _) => OtomatikKaydet("malzeme", KaydetAlinanMalzemeler);
            Stok.CollectionChanged += (_, _) => OtomatikKaydet("stok", KaydetStok);
            StokHareketleri.CollectionChanged += (_, _) => OtomatikKaydet("stok_hareket", KaydetStokHareketleri);
            Agrega.CollectionChanged += (_, _) => OtomatikKaydet("agrega", KaydetAgrega);
            Cimento.CollectionChanged += (_, _) => OtomatikKaydet("cimento", KaydetCimento);
            Akaryakit.CollectionChanged += (_, _) => OtomatikKaydet("akaryakit", KaydetAkaryakit);
            FiloAraclari.CollectionChanged += (_, _) => OtomatikKaydet("filo", KaydetFilo);
            FiloGiderleri.CollectionChanged += (_, _) => OtomatikKaydet("filo", KaydetFilo);
            FiloZimmetleri.CollectionChanged += (_, _) => OtomatikKaydet("filo", KaydetFilo);
        }
    }

    public static void KaydetAlinanMalzemeler()
    {
        JsonYaz("alinan_malzemeler.json", AlinanMalzemeler.ToList());
        MalzemeAdiOneriServisi.OnbellekSifirla();
    }

    public static void KaydetStok()
    {
        JsonYaz("stok.json", Stok.ToList());
        MalzemeAdiOneriServisi.OnbellekSifirla();
    }
    public static void KaydetStokHareketleri() => JsonYaz("stok_hareketleri.json", StokHareketleri.ToList());
    public static void KaydetAgrega() => JsonYaz("agrega.json", Agrega.ToList());
    public static void KaydetCimento() => JsonYaz("cimento.json", Cimento.ToList());
    public static void KaydetAkaryakit() => JsonYaz("akaryakit.json", Akaryakit.ToList());
    public static void KaydetFilo() => JsonYaz("filo.json", new FiloVeriPaketi
    {
        Araclar = FiloAraclari.ToList(),
        Giderler = FiloGiderleri.ToList(),
        Zimmetler = FiloZimmetleri.ToList()
    });

    public static void KaydetTumu()
    {
        KaydetAlinanMalzemeler();
        KaydetStok();
        KaydetStokHareketleri();
        KaydetAgrega();
        KaydetCimento();
        KaydetAkaryakit();
        KaydetFilo();
    }

    public static void YenidenYukle()
    {
        KiraciDegisti();
        Yukle();
    }

    public static void Sifirla(string dosyaAdi)
    {
        _yukleniyor = true;
        try
        {
            switch (dosyaAdi)
            {
                case "alinan_malzemeler.json":
                    AlinanMalzemeler.Clear();
                    KaydetAlinanMalzemeler();
                    break;
                case "stok.json":
                    Stok.Clear();
                    KaydetStok();
                    break;
                case "stok_hareketleri.json":
                    StokHareketleri.Clear();
                    KaydetStokHareketleri();
                    break;
                case "agrega.json":
                    Agrega.Clear();
                    KaydetAgrega();
                    break;
                case "cimento.json":
                    Cimento.Clear();
                    KaydetCimento();
                    break;
                case "akaryakit.json":
                    Akaryakit.Clear();
                    KaydetAkaryakit();
                    break;
                case "filo.json":
                    FiloAraclari.Clear();
                    FiloGiderleri.Clear();
                    FiloZimmetleri.Clear();
                    KaydetFilo();
                    break;
            }
        }
        finally
        {
            _yukleniyor = false;
        }
    }

    private static void OtomatikKaydet(string anahtar, Action kaydet)
    {
        if (_yukleniyor || !ModulAnahtariYazabilir(anahtar))
            return;

        ErtelenmisKayit.Planla(anahtar, kaydet);
        BulutVeriSenkronu.Planla(anahtar);
    }

    private static bool ModulAnahtariYazabilir(string anahtar)
    {
        if (!OturumYoneticisi.BulutAktif)
            return true;

        var modul = anahtar switch
        {
            "malzeme" => "Alınan Malzemeler",
            "stok" or "stok_hareket" => "Stok Yönetimi",
            "agrega" => "Agrega",
            "cimento" => "Çimento",
            "akaryakit" => "Akaryakıt Takip",
            "filo" => "Araç Filo Takip",
            _ => null
        };

        return modul is null || KullaniciYetkileri.ModulYazabilir(modul);
    }

    private static void JsonOku<T>(ObservableCollection<T> koleksiyon, string dosyaAdi, Action ornekVeri)
    {
        koleksiyon.Clear();
        var yol = YerelYol(dosyaAdi);
        if (!File.Exists(yol))
        {
            if (!OturumYoneticisi.BulutAktif)
                ornekVeri();
            return;
        }

        try
        {
            var liste = JsonSerializer.Deserialize<List<T>>(File.ReadAllText(yol), JsonSecenekleri) ?? [];
            BeginBatch();
            try
            {
                foreach (var kayit in liste)
                    koleksiyon.Add(kayit);
            }
            finally
            {
                EndBatch();
            }
        }
        catch
        {
            if (!OturumYoneticisi.BulutAktif)
                ornekVeri();
        }
    }

    private static void JsonYaz<T>(string dosyaAdi, T veri)
    {
        SatinalmaProKlasor.Olustur();
        var json = JsonSerializer.Serialize(veri, JsonSecenekleri);
        File.WriteAllText(YerelYol(dosyaAdi), json);
    }

    private static void FiloOku()
    {
        FiloAraclari.Clear();
        FiloGiderleri.Clear();
        FiloZimmetleri.Clear();
        var yol = YerelYol("filo.json");
        if (!File.Exists(yol))
        {
            if (!OturumYoneticisi.BulutAktif)
                FiloOrnekVeri();
            return;
        }

        try
        {
            var json = File.ReadAllText(yol);
            if (json.Contains("\"araclar\"", StringComparison.OrdinalIgnoreCase))
            {
                var paket = JsonSerializer.Deserialize<FiloVeriPaketi>(json, JsonSecenekleri) ?? new FiloVeriPaketi();
                foreach (var arac in paket.Araclar)
                    FiloAraclari.Add(arac);
                foreach (var gider in paket.Giderler)
                    FiloGiderleri.Add(gider);
                foreach (var zimmet in paket.Zimmetler)
                    FiloZimmetleri.Add(zimmet);
            }
            else
            {
                var eski = JsonSerializer.Deserialize<List<FiloKaydi>>(json, JsonSecenekleri) ?? [];
                var paket = FiloVeriMigrator.EskiKayittanOlustur(eski);
                foreach (var arac in paket.Araclar)
                    FiloAraclari.Add(arac);
                foreach (var gider in paket.Giderler)
                    FiloGiderleri.Add(gider);
                foreach (var zimmet in paket.Zimmetler)
                    FiloZimmetleri.Add(zimmet);
            }
        }
        catch (Exception ex)
        {
            HataGunlugu.Kaydet(ex, "ModulVeriDeposu.FiloOku");
            if (!OturumYoneticisi.BulutAktif)
                FiloOrnekVeri();
        }
    }

    private static void AlinanMalzemeOrnekVeri()
    {
        AlinanMalzemeler.Add(new AlinanMalzemeKaydi
        {
            Tarih = "16.06.2026", FaturaNo = "FT-2026-1842", Kategori = "Agrega",
            MalzemeHizmet = "Mıcır 0-11", Miktar = 42.5, Birim = "Ton", BirimFiyati = 3000,
            Tedarikci = "Delta Madencilik", IndirildigiSaha = "Merkez Şantiye",
            TeslimAlan = "Ahmet Yılmaz", Aciklama = "Acil sevkiyat"
        });
        AlinanMalzemeler.Add(new AlinanMalzemeKaydi
        {
            Tarih = "15.06.2026", FaturaNo = "FT-2026-1835", Kategori = "Bağlayıcı",
            MalzemeHizmet = "Çimento CEM I", Miktar = 18, Birim = "Ton", BirimFiyati = 4800,
            Tedarikci = "ABC Yapı", IndirildigiSaha = "Doğu Sahası",
            TeslimAlan = "Mehmet Kaya", Aciklama = ""
        });
        foreach (var kayit in AlinanMalzemeler)
            kayit.ToplamTutariHesapla();
    }

    private static void StokOrnekVeri()
    {
        Stok.Add(new StokKaydi
        {
            MalzemeAdi = "Mıcır 0-11",
            Kategori = "Agrega",
            Birim = "Ton",
            MevcutMiktar = 125,
            MinimumStok = 40,
            DepoSaha = "Merkez Şantiye",
            BirimMaliyet = 3000,
            SonGuncelleme = "20.06.2026",
            Aciklama = "Ana depo stoku"
        });
        Stok.Add(new StokKaydi
        {
            MalzemeAdi = "Demir Ø12",
            Kategori = "Demir",
            Birim = "Ton",
            MevcutMiktar = 8,
            MinimumStok = 15,
            DepoSaha = "Doğu Sahası",
            BirimMaliyet = 28500,
            SonGuncelleme = "19.06.2026",
            Aciklama = "Kritik seviyede"
        });
        Stok.Add(new StokKaydi
        {
            MalzemeAdi = "Naylon Branda",
            Kategori = "Malzeme",
            Birim = "Adet",
            MevcutMiktar = 0,
            MinimumStok = 20,
            DepoSaha = "Merkez Şantiye",
            BirimMaliyet = 450,
            SonGuncelleme = "15.06.2026",
            Aciklama = "Sipariş verilecek"
        });
        foreach (var kayit in Stok)
            kayit.ToplamDegerHesapla();
    }

    private static void StokHareketOrnekVeri()
    {
        StokHareketleri.Add(new StokHareketKaydi
        {
            Tarih = "18.06.2026",
            HareketTipi = StokHareketTipleri.Giris,
            MalzemeAdi = "Mıcır 0-11",
            Kategori = "Agrega",
            Birim = "Ton",
            Miktar = 50,
            DepoSaha = "Merkez Şantiye",
            BirimMaliyet = 3000,
            BelgeNo = "GR-2026-012",
            IslemYapan = "Ahmet Yılmaz",
            Aciklama = "İlk stok girişi"
        });
        StokHareketleri.Add(new StokHareketKaydi
        {
            Tarih = "19.06.2026",
            HareketTipi = StokHareketTipleri.Cikis,
            MalzemeAdi = "Mıcır 0-11",
            Kategori = "Agrega",
            Birim = "Ton",
            Miktar = 12,
            DepoSaha = "Merkez Şantiye",
            BirimMaliyet = 3000,
            BelgeNo = "CK-2026-004",
            IslemYapan = "Mehmet Kaya",
            Aciklama = "Şantiye kullanımı"
        });
    }

    private static void AgregaOrnekVeri()
    {
        Agrega.Add(new AgregaKaydi
        {
            Tarih = "16.06.2026", IrsaliyeNo = "AG-2026-042", AgregaTuru = "Mıcır", AgregaCinsi = "Mıcır 0-11",
            Miktar = 42.5, Birim = "Ton", BirimFiyati = 3000, Tedarikci = "Delta Madencilik",
            IndirildigiSaha = "Merkez Şantiye", TeslimAlan = "Ahmet Yılmaz", Aciklama = "Acil sevkiyat",
            FaturasiKesildi = true
        });
        Agrega.Add(new AgregaKaydi
        {
            Tarih = "15.06.2026", IrsaliyeNo = "AG-2026-041", AgregaTuru = "Kum", AgregaCinsi = "Kum 0-4",
            Miktar = 55, Birim = "Ton", BirimFiyati = 1800, Tedarikci = "Delta Madencilik",
            IndirildigiSaha = "Merkez Şantiye", TeslimAlan = "Ali Demir", Aciklama = ""
        });
        foreach (var kayit in Agrega)
            AgregaArtisHesaplayici.Hesapla([kayit]);
    }

    private static void CimentoOrnekVeri()
    {
        Cimento.Add(new CimentoKaydi
        {
            Tarih = "16.06.2026", IrsaliyeNo = "CM-2026-028", CimentoSinifi = "CEM I", CimentoCinsi = "CEM I 42.5R",
            Miktar = 18, Birim = "Ton", BirimFiyati = 4800, Tedarikci = "Akçansa",
            IndirildigiSaha = "Merkez Şantiye", TeslimAlan = "Ahmet Yılmaz", Aciklama = "",
            FaturasiKesildi = true
        });
    }

    private static void AkaryakitOrnekVeri()
    {
        Akaryakit.Add(new AkaryakitKaydi
        {
            KayitTipi = "Alınan", Tarih = "09.06.2026",
            Miktar = 5000, Birim = "Lt", BirimFiyati = 41.5m,
            Tedarikci = "OPET Toptan", TeslimAlan = "Ahmet Yılmaz"
        });
    }

    private static void FiloOrnekVeri()
    {
        FiloAraclari.Add(new FiloAracKaydi
        {
            Plaka = "34 ABC 123",
            AracTipi = "Binek",
            MarkaModel = "Ford Transit",
            ModelYili = "2022",
            SahiplikTipi = "Bizim",
            Sirket = "Metrik İnşaat",
            Saha = "Merkez Şantiye",
            MuayeneBitisTarihi = DateTime.Today.AddDays(10).ToString("dd.MM.yyyy"),
            SigortaBitisTarihi = DateTime.Today.AddMonths(4).ToString("dd.MM.yyyy"),
            Durum = "Aktif",
            KayitTarihi = "01.01.2026"
        });
        FiloAraclari.Add(new FiloAracKaydi
        {
            Plaka = "06 EKS 45",
            AracTipi = "İş Makinası",
            MarkaModel = "CAT 320D",
            ModelYili = "2020",
            SahiplikTipi = "Kiralık",
            Sirket = "Kiralama A.Ş.",
            Saha = "Doğu Şantiye",
            MuayeneBitisTarihi = DateTime.Today.AddMonths(2).ToString("dd.MM.yyyy"),
            SigortaBitisTarihi = DateTime.Today.AddMonths(1).ToString("dd.MM.yyyy"),
            Durum = "Aktif",
            KayitTarihi = "15.03.2025"
        });
        FiloGiderleri.Add(new FiloGiderKaydi
        {
            Plaka = "34 ABC 123",
            Tarih = "10.06.2026",
            GiderTipi = "Bakım",
            Tutar = 4500,
            BelgeNo = "BK-102",
            Aciklama = "Periyodik bakım"
        });
    }

    public static void BulutYuklemesiBaslat() => _yukleniyor = true;

    public static void BulutYuklemesiBitir() => _yukleniyor = false;

    public static void AlinanMalzemeleriYukle(string json) =>
        KoleksiyonuYenile(AlinanMalzemeler, JsonSerializer.Deserialize<List<AlinanMalzemeKaydi>>(json, JsonSecenekleri));

    public static void StokYukle(string json) =>
        KoleksiyonuYenile(Stok, JsonSerializer.Deserialize<List<StokKaydi>>(json, JsonSecenekleri));

    public static void StokHareketleriYukle(string json) =>
        KoleksiyonuYenile(StokHareketleri, JsonSerializer.Deserialize<List<StokHareketKaydi>>(json, JsonSecenekleri));

    public static void AgregaYukle(string json) =>
        KoleksiyonuYenile(Agrega, JsonSerializer.Deserialize<List<AgregaKaydi>>(FaturaNoAnahtariniNormalizeEt(json), JsonSecenekleri));

    public static void CimentoYukle(string json) =>
        KoleksiyonuYenile(Cimento, JsonSerializer.Deserialize<List<CimentoKaydi>>(FaturaNoAnahtariniNormalizeEt(json), JsonSecenekleri));

    /// <summary>Eski bulut kayıtları "FaturaNo" anahtarıyla gelir; camelCase ile hizala.</summary>
    private static string FaturaNoAnahtariniNormalizeEt(string json) =>
        json.Replace("\"FaturaNo\":", "\"faturaNo\":", StringComparison.Ordinal);

    public static void AkaryakitYukle(string json) =>
        KoleksiyonuYenile(Akaryakit, JsonSerializer.Deserialize<List<AkaryakitKaydi>>(json, JsonSecenekleri));

    public static void FiloYukle(string json)
    {
        var paket = JsonSerializer.Deserialize<FiloVeriPaketi>(json, JsonSecenekleri) ?? new FiloVeriPaketi();
        KoleksiyonuYenile(FiloAraclari, paket.Araclar);
        KoleksiyonuYenile(FiloGiderleri, paket.Giderler);
        KoleksiyonuYenile(FiloZimmetleri, paket.Zimmetler);
    }

    private static void KoleksiyonuYenile<T>(ObservableCollection<T> koleksiyon, List<T>? liste)
    {
        koleksiyon.Clear();
        if (liste is null)
            return;
        foreach (var kayit in liste)
            koleksiyon.Add(kayit);
    }

    private static void ModulTarihleriniNormalizeEt()
    {
        foreach (var k in AlinanMalzemeler)
            k.Tarih = TarihYardimcisi.Normalize(k.Tarih);
        foreach (var k in Agrega)
            k.Tarih = TarihYardimcisi.Normalize(k.Tarih);
        foreach (var k in Cimento)
            k.Tarih = TarihYardimcisi.Normalize(k.Tarih);
        foreach (var k in Akaryakit)
            k.Tarih = TarihYardimcisi.Normalize(k.Tarih);
        foreach (var h in StokHareketleri)
            h.Tarih = TarihYardimcisi.Normalize(h.Tarih);
        foreach (var s in Stok)
            s.SonGuncelleme = TarihYardimcisi.Normalize(s.SonGuncelleme);
    }
}

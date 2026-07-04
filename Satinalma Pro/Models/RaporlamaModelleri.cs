namespace SatinalmaPro.Models;



public class RaporFiltreleri

{

    public DateTime? Baslangic { get; set; }

    public DateTime? Bitis { get; set; }

    public string Modul { get; set; } = RaporModulleri.Tumu;

    public string RaporTuru { get; set; } = RaporTurleri.GenelOzet;

    public IReadOnlyList<string> Kategoriler { get; set; } = [];

    public IReadOnlyList<string> Malzemeler { get; set; } = [];



    public bool KategoriSecili => Kategoriler.Count > 0;

    public bool MalzemeSecili => Malzemeler.Count > 0;

    public bool DetayliPdfModu => KategoriSecili || MalzemeSecili;

}



public class RaporModulOzeti

{

    public string ModulAdi { get; set; } = "";

    public int KayitSayisi { get; set; }

    public decimal ToplamTutar { get; set; }

    public string Renk { get; set; } = "#6366F1";

}



public class RaporDetaySatiri

{

    public string Modul { get; set; } = "";

    public string Tarih { get; set; } = "";

    public string BelgeNo { get; set; } = "";

    public string Aciklama { get; set; } = "";

    public string Kategori { get; set; } = "";

    public string Tedarikci { get; set; } = "";

    public string Saha { get; set; } = "";

    public double Miktar { get; set; }

    public string Birim { get; set; } = "";

    public decimal BirimFiyati { get; set; }

    public decimal Tutar { get; set; }

    public double? ArtisYuzdesi { get; set; }



    public string ArtisYuzdesiMetin => ArtisYuzdesi switch

    {

        null => "—",

        > 0 => $"+{ArtisYuzdesi.Value:N1}%",

        _ => $"{ArtisYuzdesi.Value:N1}%"

    };

}



public class RaporGrupOzeti

{

    public string GrupAdi { get; set; } = "";

    public int KayitSayisi { get; set; }

    public decimal ToplamTutar { get; set; }

    public string ModulDagilimi { get; set; } = "";

}



public class RaporMalzemeAnalizi

{

    public string MalzemeAdi { get; set; } = "";

    public string Kategori { get; set; } = "";

    public int KayitSayisi { get; set; }

    public double ToplamMiktar { get; set; }

    public string Birim { get; set; } = "";

    public decimal MinBirimFiyat { get; set; }

    public decimal MaxBirimFiyat { get; set; }

    public decimal OrtBirimFiyat { get; set; }

    public decimal IlkBirimFiyat { get; set; }

    public decimal SonBirimFiyat { get; set; }

    public double? ToplamArtisYuzdesi { get; set; }

    public decimal ToplamTutar { get; set; }
    public decimal KarZiyanTl { get; set; }
    public decimal KarZiyanToplamTl { get; set; }

    public decimal AgirlikliOrtalamaFiyat =>
        ToplamMiktar > 0 ? Math.Round(ToplamTutar / (decimal)ToplamMiktar, 2, MidpointRounding.AwayFromZero) : OrtBirimFiyat;

    public string ToplamArtisMetin => ToplamArtisYuzdesi switch
    {
        null => "—",
        > 0 => $"+{ToplamArtisYuzdesi.Value:N1}%",
        _ => $"{ToplamArtisYuzdesi.Value:N1}%"
    };

    public string KarZiyanTlMetin => IlkBirimFiyat <= 0 || SonBirimFiyat <= 0 || IlkBirimFiyat == SonBirimFiyat
        ? "—"
        : KarZiyanTl switch
        {
            > 0 => $"+{KarZiyanTl:N2} ₺",
            _ => $"{KarZiyanTl:N2} ₺"
        };

    public string KarZiyanToplamTlMetin => IlkBirimFiyat <= 0 || SonBirimFiyat <= 0 || IlkBirimFiyat == SonBirimFiyat
        ? "—"
        : KarZiyanToplamTl switch
        {
            > 0 => $"+{KarZiyanToplamTl:N2} ₺",
            _ => $"{KarZiyanToplamTl:N2} ₺"
        };
}

public class RaporAylikAlimOzeti
{
    public string AyEtiketi { get; set; } = "";
    public double ToplamMiktar { get; set; }
    public string Birim { get; set; } = "";
    public decimal ToplamTutar { get; set; }
    public decimal OrtBirimFiyat { get; set; }
    public decimal? ArtisTl { get; set; }
    public double? ArtisYuzdesi { get; set; }

    public string ArtisTlMetin => ArtisTl switch
    {
        null => "—",
        > 0 => $"+{ArtisTl.Value:N2} ₺",
        _ => $"{ArtisTl.Value:N2} ₺"
    };

    public string ArtisYuzdesiMetin => ArtisYuzdesi switch
    {
        null => "—",
        > 0 => $"+{ArtisYuzdesi.Value:N1}%",
        _ => $"{ArtisYuzdesi.Value:N1}%"
    };
}



public static class RaporTurleri

{

    public const string GenelOzet = "Genel Özet";

    public const string DetayliHareketler = "Detaylı Hareketler";

    public const string TedarikciOzeti = "Tedarikçi Özeti";

    public const string SahaOzeti = "Saha Özeti";

    public const string KategoriOzeti = "Kategori Özeti";

    public const string SatinalmaTalepleri = "Satınalma Talepleri";



    public static IReadOnlyList<string> Tum { get; } =

    [

        GenelOzet, DetayliHareketler, TedarikciOzeti, SahaOzeti, KategoriOzeti, SatinalmaTalepleri

    ];

}



public static class RaporModulleri

{

    public const string Tumu = "Tüm Modüller";

    public const string AlinanMalzemeler = "Alınan Malzemeler";

    public const string StokYonetimi = "Stok Yönetimi";

    public const string Agrega = "Agrega";

    public const string Cimento = "Çimento";

    public const string Akaryakit = "Akaryakıt";

    public const string Filo = "Araç Filo";

    public const string Satinalma = "Satınalma";



    public static IReadOnlyList<string> Tum { get; } =

    [

        Tumu, AlinanMalzemeler, StokYonetimi, Agrega, Cimento, Akaryakit, Filo, Satinalma

    ];

}



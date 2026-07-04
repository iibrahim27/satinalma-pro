namespace SatinalmaPro.Models;

public class FinansmanFiltreleri
{
    public DateTime? Baslangic { get; set; }
    public DateTime? Bitis { get; set; }
    public string RaporTuru { get; set; } = FinansmanTurleri.FinansalOzet;
    public string Modul { get; set; } = FinansmanModulleri.Tumu;
    public string HareketTipi { get; set; } = FinansmanHareketTipleri.Tumu;
    public string Saha { get; set; } = "Tümü";
}

public class FinansmanGenelOzet
{
    public decimal ToplamGider { get; set; }
    public decimal ToplamGelir { get; set; }
    public decimal NetNakit => ToplamGelir - ToplamGider;
    public decimal BekleyenOdeme { get; set; }
    public decimal GecikenOdeme { get; set; }
    public decimal KdvToplam { get; set; }
    public int GiderKayitSayisi { get; set; }
    public int GelirKayitSayisi { get; set; }
    public int VadeKayitSayisi { get; set; }
}

public class FinansmanModulOzeti
{
    public string ModulAdi { get; set; } = "";
    public int KayitSayisi { get; set; }
    public decimal ToplamTutar { get; set; }
    public string Renk { get; set; } = "#8B5CF6";
    public string Tip { get; set; } = "Gider";
}

public class FinansmanHareketSatiri
{
    public string Tip { get; set; } = "";
    public string Tarih { get; set; } = "";
    public string Modul { get; set; } = "";
    public string BelgeNo { get; set; } = "";
    public string Kategori { get; set; } = "";
    public string Aciklama { get; set; } = "";
    public string Tedarikci { get; set; } = "";
    public string Saha { get; set; } = "";
    public decimal Tutar { get; set; }
    public string OdemeSekli { get; set; } = "";

    public string TutarMetin => Tip == FinansmanHareketTipleri.Gelir
        ? $"+₺{Tutar:N2}"
        : $"₺{Tutar:N2}";
}

public class FinansmanAylikOzet
{
    public string Ay { get; set; } = "";
    public int Yil { get; set; }
    public int AyNo { get; set; }
    public decimal Gider { get; set; }
    public decimal Gelir { get; set; }
    public decimal Net => Gelir - Gider;
    public int HareketSayisi { get; set; }
}

public class FinansmanVadeSatiri
{
    public string VadeTarihi { get; set; } = "";
    public string IslemTarihi { get; set; } = "";
    public string Firma { get; set; } = "";
    public string BelgeNo { get; set; } = "";
    public string Aciklama { get; set; } = "";
    public int VadeGunu { get; set; }
    public decimal Tutar { get; set; }
    public decimal KdvDahilTutar { get; set; }
    public string Durum { get; set; } = "";
    public int KalanGun { get; set; }
    public bool Odendi { get; set; }

    public string DurumMetin => Odendi
        ? "Ödendi"
        : KalanGun < 0
            ? $"Gecikmiş ({Math.Abs(KalanGun)} gün)"
            : KalanGun == 0
                ? "Bugün"
                : $"{KalanGun} gün";
}

public class FinansmanGrupOzeti
{
    public string GrupAdi { get; set; } = "";
    public int KayitSayisi { get; set; }
    public decimal ToplamTutar { get; set; }
    public decimal GelirTutar { get; set; }
    public decimal GiderTutar { get; set; }
    public string ModulDagilimi { get; set; } = "";
}

public static class FinansmanTurleri
{
    public const string FinansalOzet = "Finansal Özet";
    public const string GiderDetayi = "Gider Detayı";
    public const string GelirDetayi = "Gelir Detayı";
    public const string NakitAkisi = "Nakit Akışı (Aylık)";
    public const string VadeTakvimi = "Vade Takvimi";
    public const string ModulDagilimi = "Modül Dağılımı";
    public const string SahaOzeti = "Saha Özeti";
    public const string TedarikciOzeti = "Tedarikçi Özeti";
    public const string BekleyenOdemeler = "Bekleyen Ödemeler";

    public static IReadOnlyList<string> Tum { get; } =
    [
        FinansalOzet, GiderDetayi, GelirDetayi, NakitAkisi, VadeTakvimi,
        ModulDagilimi, SahaOzeti, TedarikciOzeti, BekleyenOdemeler
    ];
}

public static class FinansmanModulleri
{
    public const string Tumu = "Tüm Modüller";
    public const string AlinanMalzemeler = "Alınan Malzemeler";
    public const string Agrega = "Agrega";
    public const string Cimento = "Çimento";
    public const string Akaryakit = "Akaryakıt";
    public const string Filo = "Araç Filo";
    public const string Satinalma = "Satınalma";
    public const string Gelir = "Gelir Kayıtları";

    public static IReadOnlyList<string> Tum { get; } =
    [
        Tumu, AlinanMalzemeler, Agrega, Cimento,
        Akaryakit, Filo, Satinalma, Gelir
    ];
}

public static class FinansmanHareketTipleri
{
    public const string Tumu = "Tümü";
    public const string Gider = "Gider";
    public const string Gelir = "Gelir";

    public static IReadOnlyList<string> Tum { get; } = [Tumu, Gider, Gelir];
}

public static class FinansmanGelirKategorileri
{
    public static IReadOnlyList<string> Tum { get; } =
    [
        "Hakediş", "Avans", "Proje Geliri", "Kira Geliri", "Diğer Gelir"
    ];
}

using System.Globalization;
using System.Text.Json.Serialization;

namespace SatinalmaPro.Models;

public class AkaryakitKaydi
{
    public string KayitTipi { get; set; } = "Dağıtılan";
    public string Tarih { get; set; } = "";
    public string FaturaNo { get; set; } = "";
    public string AracTipi { get; set; } = "";
    public string PlakaVeyaKod { get; set; } = "";
    public string AracMakineAdi { get; set; } = "";
    public string YakitTuru { get; set; } = "";
    public double Miktar { get; set; }
    public string Birim { get; set; } = "Lt";
    public decimal BirimFiyati { get; set; }
    public decimal ToplamTutar { get; set; }
    public double? KmSayaci { get; set; }
    public double? SaatSayaci { get; set; }
    public double? Tuketim100Km { get; set; }
    public double? TuketimSaat { get; set; }
    public string Istasyon { get; set; } = "";
    public string Tedarikci { get; set; } = "";
    public string TeslimAlan { get; set; } = "";
    public string SoforOperator { get; set; } = "";
    public string Saha { get; set; } = "";
    public string Aciklama { get; set; } = "";

    [JsonIgnore]
    public bool AlinanKayit => AlinanKayitMi(KayitTipi);

    public static bool AlinanKayitMi(string? kayitTipi)
    {
        if (string.IsNullOrWhiteSpace(kayitTipi))
            return false;

        var tip = kayitTipi.Trim();
        var tr = CultureInfo.GetCultureInfo("tr-TR");
        return tr.CompareInfo.Compare(tip, "Alınan", CompareOptions.IgnoreCase) == 0
               || tr.CompareInfo.Compare(tip, "Alinan", CompareOptions.IgnoreCase) == 0;
    }

    [JsonIgnore]
    public string GosterilenTedarikci =>
        !string.IsNullOrWhiteSpace(Tedarikci) ? Tedarikci : Istasyon;

    [JsonIgnore]
    public string BirimFiyatiMetin => AlinanKayit && GosterilenBirimFiyati > 0
        ? $"₺{GosterilenBirimFiyati:N2}"
        : "—";

    [JsonIgnore]
    public decimal GosterilenBirimFiyati =>
        BirimFiyati > 0
            ? BirimFiyati
            : Miktar > 0 && ToplamTutar > 0
                ? Math.Round(ToplamTutar / (decimal)Miktar, 2)
                : 0;

    [JsonIgnore]
    public string ToplamTutarMetin => AlinanKayit && ToplamTutar > 0
        ? $"₺{ToplamTutar:N2}"
        : "—";

    [JsonIgnore]
    public string BirimMetin => AlinanKayit
        ? (string.IsNullOrWhiteSpace(Birim) ? "Lt" : Birim)
        : "—";

    [JsonIgnore]
    public string MiktarMetin => Miktar > 0
        ? AlinanKayit
            ? $"{Miktar:N1} {(string.IsNullOrWhiteSpace(Birim) ? "Lt" : Birim)}"
            : $"{Miktar:N1} Lt"
        : "—";

    [JsonIgnore]
    public string TedarikciMetin => AlinanKayit && !string.IsNullOrWhiteSpace(GosterilenTedarikci)
        ? GosterilenTedarikci
        : "—";

    [JsonIgnore]
    public string TeslimAlanMetin => AlinanKayit && !string.IsNullOrWhiteSpace(TeslimAlan)
        ? TeslimAlan
        : "—";

    [JsonIgnore]
    public string PlakaMetin => !AlinanKayit && !string.IsNullOrWhiteSpace(PlakaVeyaKod)
        ? PlakaVeyaKod
        : "—";

    [JsonIgnore]
    public string AracTipiMetin => !AlinanKayit && !string.IsNullOrWhiteSpace(AracTipi)
        ? AracTipi
        : "—";

    [JsonIgnore]
    public string AracMakineMetin => !AlinanKayit && !string.IsNullOrWhiteSpace(AracMakineAdi)
        ? AracMakineAdi
        : "—";

    [JsonIgnore]
    public string YakitTuruMetin => !AlinanKayit && !string.IsNullOrWhiteSpace(YakitTuru)
        ? YakitTuru
        : "—";

    [JsonIgnore]
    public string SoforMetin => !AlinanKayit && !string.IsNullOrWhiteSpace(SoforOperator)
        ? SoforOperator
        : "—";

    [JsonIgnore]
    public string SahaMetin => !AlinanKayit && !string.IsNullOrWhiteSpace(Saha)
        ? Saha
        : "—";

    [JsonIgnore]
    public string KmSayaciMetin => !AlinanKayit && KmSayaci is > 0
        ? $"{KmSayaci.Value:N0}"
        : "—";

    [JsonIgnore]
    public string SaatSayaciMetin => !AlinanKayit && SaatSayaci is > 0
        ? $"{SaatSayaci.Value:N1}"
        : "—";

    [JsonIgnore]
    public string Tuketim100KmMetin => !AlinanKayit && Tuketim100Km is not null
        ? $"{Tuketim100Km.Value:N1} L/100km"
        : "—";

    [JsonIgnore]
    public string TuketimSaatMetin => !AlinanKayit && TuketimSaat is not null
        ? $"{TuketimSaat.Value:N1} Lt/saat"
        : "—";

    [JsonIgnore]
    public string AciklamaMetin => !AlinanKayit && !string.IsNullOrWhiteSpace(Aciklama)
        ? Aciklama
        : "—";

    public void ToplamTutariHesapla()
    {
        if (!AlinanKayit)
        {
            BirimFiyati = 0;
            ToplamTutar = 0;
            return;
        }

        ToplamTutar = Math.Round((decimal)Miktar * BirimFiyati, 2);
    }
}

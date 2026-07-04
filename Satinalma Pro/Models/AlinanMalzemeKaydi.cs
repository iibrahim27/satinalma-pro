namespace SatinalmaPro.Models;

public class AlinanMalzemeKaydi
{
    public string Tarih { get; set; } = "";
    public string FaturaNo { get; set; } = "";
    public string Kategori { get; set; } = "";
    public string MalzemeHizmet { get; set; } = "";
    public double Miktar { get; set; }
    public string Birim { get; set; } = "";
    public decimal BirimFiyati { get; set; }
    public decimal ToplamTutar { get; set; }
    public double? ArtisYuzdesi { get; set; }
    public string Tedarikci { get; set; } = "";
    public string IndirildigiSaha { get; set; } = "";
    public string TeslimAlan { get; set; } = "";
    public string Aciklama { get; set; } = "";
    /// <summary>Satınalma aktarımı: kaynak talep kimliği (tekrar aktarımı önlemek için).</summary>
    public Guid? SatinalmaTalepId { get; set; }
    /// <summary>Satınalma aktarımı: kaynak kalem kimliği (tekrar aktarımı önlemek için).</summary>
    public Guid? SatinalmaKalemId { get; set; }

    public string ArtisYuzdesiMetin => ArtisYuzdesi switch
    {
        null => "—",
        > 0 => $"+{ArtisYuzdesi.Value:N1}%",
        _ => $"{ArtisYuzdesi.Value:N1}%"
    };

    public void ToplamTutariHesapla() =>
        ToplamTutar = Math.Round((decimal)Miktar * BirimFiyati, 2);

    public AlinanMalzemeKaydi Kopyala() => new()
    {
        Tarih = Tarih,
        FaturaNo = FaturaNo,
        Kategori = Kategori,
        MalzemeHizmet = MalzemeHizmet,
        Miktar = Miktar,
        Birim = Birim,
        BirimFiyati = BirimFiyati,
        ToplamTutar = ToplamTutar,
        ArtisYuzdesi = ArtisYuzdesi,
        Tedarikci = Tedarikci,
        IndirildigiSaha = IndirildigiSaha,
        TeslimAlan = TeslimAlan,
        Aciklama = Aciklama,
        SatinalmaTalepId = SatinalmaTalepId,
        SatinalmaKalemId = SatinalmaKalemId
    };
}

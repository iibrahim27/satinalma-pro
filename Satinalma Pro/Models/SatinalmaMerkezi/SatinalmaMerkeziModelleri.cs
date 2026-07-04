using System.Windows.Media;

namespace SatinalmaPro.Models.SatinalmaMerkezi;

public sealed class KpiKartModel
{
    public required string Baslik { get; init; }
    public required string Deger { get; init; }
    public required Brush Renk { get; init; }
    public required string FiltreAnahtar { get; init; }
}

public sealed class YapilacakIsModel
{
    public required string Baslik { get; init; }
    public required string Aciklama { get; init; }
    public required string Oncelik { get; init; }
    public required Brush OncelikRenk { get; init; }
    public required string IlgiliNo { get; init; }
}

public sealed class SonHareketModel
{
    public required string Mesaj { get; init; }
    public required string Kullanici { get; init; }
    public required string Zaman { get; init; }
}

public sealed class TalepSatirModel
{
    public required Guid Id { get; init; }
    public required string TalepNo { get; init; }
    public required string TalepTarihi { get; init; }
    public required string Santiye { get; init; }
    public required string TalepEden { get; init; }
    public required string Oncelik { get; init; }
    public required string Durum { get; init; }
    public required string YonetimKarari { get; init; }
    public required string SonIslemTarihi { get; init; }
    public Brush DurumRenk { get; init; } = Brushes.SlateGray;
    public Brush OncelikRenk { get; init; } = Brushes.Gray;
}

public sealed class TeklifSatirModel
{
    public required string Firma { get; init; }
    public required string Marka { get; init; }
    public required decimal BirimFiyat { get; init; }
    public required decimal Iskonto { get; init; }
    public required decimal Kdv { get; init; }
    public required decimal Toplam { get; init; }
    public required string TeslimSuresi { get; init; }
    public required string Vade { get; init; }
    public required string TeklifTarihi { get; init; }
    public required string Dosya { get; init; }
    public required string Durum { get; init; }
    public bool EnUygunFiyat { get; init; }
    public bool EnKisaTeslim { get; init; }
    public bool EnYuksekPerformans { get; init; }
}

public sealed class SiparisSatirModel
{
    public required Guid Id { get; init; }
    public required string SiparisNo { get; init; }
    public required string Firma { get; init; }
    public required string TalepNo { get; init; }
    public required string Santiye { get; init; }
    public required decimal ToplamTutar { get; init; }
    public required string SiparisTarihi { get; init; }
    public required string Durum { get; init; }
    public Brush DurumRenk { get; init; } = Brushes.SlateGray;
}

public sealed class DepoTakipSatirModel
{
    public required string Malzeme { get; init; }
    public required decimal SiparisMiktari { get; init; }
    public required decimal TeslimAlinan { get; init; }
    public required decimal Kalan { get; init; }
    public required decimal Eksik { get; init; }
    public required decimal Fazla { get; init; }
    public required string Durum { get; init; }
    public Brush DurumRenk { get; init; } = Brushes.SlateGray;
}

public sealed class DetayMalzemeModel
{
    public required string Ad { get; init; }
    public required string Miktar { get; init; }
    public required string Birim { get; init; }
}

public sealed class TimelineModel
{
    public required string Baslik { get; init; }
    public required string Tarih { get; init; }
    public required string Kullanici { get; init; }
    public bool Tamamlandi { get; init; }
}

public sealed class DosyaModel
{
    public required string Ad { get; init; }
    public required string Tip { get; init; }
    public required string Boyut { get; init; }
}

public sealed class IslemGecmisiModel
{
    public required string Kullanici { get; init; }
    public required string Islem { get; init; }
    public required string Tarih { get; init; }
    public required string EskiDeger { get; init; }
    public required string YeniDeger { get; init; }
}

public sealed class BildirimModel
{
    public required string Baslik { get; init; }
    public required string Mesaj { get; init; }
    public required string Zaman { get; init; }
    public bool Okundu { get; init; }
}

public sealed class TedarikciPerformansModel
{
    public required string Firma { get; init; }
    public required int ToplamSiparis { get; init; }
    public required decimal ToplamTutar { get; init; }
    public required int ZamanindaTeslim { get; init; }
    public required int EksikTeslim { get; init; }
    public required int Iade { get; init; }
    public required int Kalite { get; init; }
    public required string OrtTeslimSuresi { get; init; }
    public required int PerformansPuani { get; init; }
}

public sealed class IadeSatirModel
{
    public required string IadeNo { get; init; }
    public required string SiparisNo { get; init; }
    public required string Firma { get; init; }
    public required string Malzeme { get; init; }
    public required string Miktar { get; init; }
    public required string Neden { get; init; }
    public required string Durum { get; init; }
    public required string Tarih { get; init; }
    public Brush DurumRenk { get; init; } = Brushes.SlateGray;
}

public sealed class TamamlananSatirModel
{
    public required string KayitNo { get; init; }
    public required string Tip { get; init; }
    public required string Santiye { get; init; }
    public required string Firma { get; init; }
    public required string Tutar { get; init; }
    public required string TamamlanmaTarihi { get; init; }
    public required string Durum { get; init; }
}

public sealed class TalepDetayModel
{
    public required Guid Id { get; init; }
    public required string TalepNo { get; init; }
    public required string Santiye { get; init; }
    public required string TalepEden { get; init; }
    public required string Tarih { get; init; }
    public required string Oncelik { get; init; }
    public required string Durum { get; init; }
    public required string YonetimKarari { get; init; }
    public required string Aciklama { get; init; }
    public IReadOnlyList<DetayMalzemeModel> Malzemeler { get; init; } = [];
    public IReadOnlyList<DosyaModel> Dosyalar { get; init; } = [];
    public IReadOnlyList<DosyaModel> Fotograflar { get; init; } = [];
    public IReadOnlyList<TimelineModel> Timeline { get; init; } = [];
    public IReadOnlyList<IslemGecmisiModel> IslemGecmisi { get; init; } = [];
    public IReadOnlyList<TeklifSatirModel> Teklifler { get; init; } = [];
    public IReadOnlyList<string> Yorumlar { get; init; } = [];
}

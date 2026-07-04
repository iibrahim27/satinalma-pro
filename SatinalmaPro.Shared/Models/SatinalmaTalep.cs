namespace SatinalmaPro.Shared.Models;

using SatinalmaPro.Shared.Helpers;

public class SatinalmaTeklifFiyati
{
    public Guid KalemId { get; set; }
    public string Marka { get; set; } = "";
    public string ParaBirimi { get; set; } = "TRY";
    public decimal BirimFiyat { get; set; }
    public double KdvOrani { get; set; } = 20;
    public decimal ToplamTutar { get; set; }
    public decimal KdvTutari { get; set; }
    public decimal ToplamKdvDahil { get; set; }

    public decimal TlBirimFiyat(decimal usdKuru, decimal eurKuru) =>
        ParaBirimi.ToUpperInvariant() switch
        {
            "USD" => BirimFiyat * usdKuru,
            "EUR" => BirimFiyat * eurKuru,
            _ => BirimFiyat
        };

    public void Hesapla(double miktar, decimal usdKuru = 0, decimal eurKuru = 0)
    {
        var tlBirim = ParaBirimi.ToUpperInvariant() switch
        {
            "USD" => BirimFiyat * usdKuru,
            "EUR" => BirimFiyat * eurKuru,
            _ => BirimFiyat
        };
        ToplamTutar = Math.Round((decimal)miktar * tlBirim, 2);
        KdvTutari = Math.Round(ToplamTutar * (decimal)KdvOrani / 100m, 2);
        ToplamKdvDahil = ToplamTutar + KdvTutari;
    }
}

public class SatinalmaTeklif
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FirmaAdi { get; set; } = "";
    public string Marka { get; set; } = "";
    public int VadeGunu { get; set; }
    public string TeslimSuresi { get; set; } = "";
    public string OdemeSekli { get; set; } = "";
    public double KdvOrani { get; set; } = 20;
    public string Aciklama { get; set; } = "";
    public decimal UsdKuru { get; set; }
    public decimal EurKuru { get; set; }
    public bool Onaylandi { get; set; }
    public List<SatinalmaTeklifFiyati> Fiyatlar { get; set; } = [];

    public decimal GenelToplam => Fiyatlar.Sum(f => f.ToplamKdvDahil);

    public void FiyatlariHesapla(IEnumerable<SatinalmaTalepKalemi> kalemler)
    {
        foreach (var fiyat in Fiyatlar)
        {
            if (fiyat.KdvOrani <= 0 && KdvOrani > 0)
                fiyat.KdvOrani = KdvOrani;

            var kalem = kalemler.FirstOrDefault(k => k.Id == fiyat.KalemId);
            if (kalem != null)
                fiyat.Hesapla(kalem.Miktar, UsdKuru, EurKuru);
        }
    }
}

public class SatinalmaTalepKalemi
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int SiraNo { get; set; }
    public string Malzeme { get; set; } = "";
    public double Miktar { get; set; }
    public string Birim { get; set; } = "Adet";
    public string Aciklama { get; set; } = "";
    public Guid? OnaylananTeklifId { get; set; }
    public double KabulEdilenMiktar { get; set; }
    public bool SiparisTamamlandi { get; set; }
}

public class SatinalmaTalep
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TalepNo { get; set; } = "";
    public string Tarih { get; set; } = "";
    public string TalepEden { get; set; } = "";
    public string SantiyeAdi { get; set; } = "";
    public string TalepAciklamasi { get; set; } = "";
    public string TalepTuru { get; set; } = TalepTurleri.Normal;
    public string OlusturanUid { get; set; } = "";
    /// <summary>Talebi oluşturan kullanıcının rolü — satınalma iç teklif akışı için.</summary>
    public string OlusturanRol { get; set; } = "";
    public string RedGerekcesi { get; set; } = "";
    /// <summary>Yönetim teklifleri düzeltme için satınalmaya geri gönderdiğinde not.</summary>
    public string TeklifDuzeltmeNotu { get; set; } = "";
    /// <summary>UTC unix ms — bulut birleştirmede en güncel kayıt kazanır.</summary>
    public long GuncellemeUtc { get; set; }
    public Guid? YonetimOnerilenTeklifId { get; set; }
    /// <summary>Satınalmacı öneriyi elle seçtiyse true; aksi halde sistem en düşük fiyatlı teklifi önerir.</summary>
    public bool SatinalmaOnerisiElleSecildi { get; set; }
    public string Durum { get; set; } = SatinalmaTalepDurumlari.Taslak;
    public string SiparisNo { get; set; } = "";
    public Guid? OnaylananTeklifId { get; set; }
    public Dictionary<Guid, string> FirmaSiparisNolari { get; set; } = [];
    public string YonetimOnaylayanUid { get; set; } = "";
    public string YonetimOnaylayanAd { get; set; } = "";
    public string YonetimOnaylayanEposta { get; set; } = "";
    public string YonetimOnayTarihi { get; set; } = "";
    public bool YonetimOnayKilitli { get; set; }
    public bool TeklifsizYonetimOnayi { get; set; }
    public List<SatinalmaTalepKalemi> Kalemler { get; set; } = [];
    public List<SatinalmaTeklif> Teklifler { get; set; } = [];

    public SatinalmaTeklif? OnaylananTeklif =>
        OnaylananTeklifId is { } id ? Teklifler.FirstOrDefault(t => t.Id == id) : null;

    public bool HerhangiKalemOnayli =>
        Kalemler?.Any(k => k.OnaylananTeklifId != null) == true;

    /// <summary>Satınalma teklif girip yönetime gönderdi — karşılaştırma/onay bekleniyor.</summary>
    public bool TeklifOnayiBekliyor =>
        (Teklifler?.Count ?? 0) > 0 && !HerhangiKalemOnayli;

    /// <summary>İlk aşama: henüz teklif yok, satınalmadan teklif istenebilir.</summary>
    public bool TeklifIstenmeli =>
        TalepTuru != TalepTurleri.Acil
        && (Teklifler?.Count ?? 0) == 0
        && !TeklifOnayiBekliyor;

    public bool TeklifGirilmis => (Teklifler?.Count ?? 0) > 0;

    public bool TeklifsizFirmaFiyatBekliyor =>
        TeklifsizYonetimOnayi && !HerhangiKalemOnayli;

    public SatinalmaTeklif? KalemOnayTeklifi(SatinalmaTalepKalemi kalem) =>
        kalem.OnaylananTeklifId is { } id ? Teklifler.FirstOrDefault(t => t.Id == id) : null;

    /// <summary>Satınalma önerisi — elle seçim yoksa KDV dahil en düşük toplam.</summary>
    public SatinalmaTeklif? OnerilenTeklif()
    {
        TeklifFiyatlariniGuncelle();

        if (SatinalmaOnerisiElleSecildi && YonetimOnerilenTeklifId is { } id)
        {
            var secili = Teklifler.FirstOrDefault(t => t.Id == id);
            if (secili is not null)
                return secili;
        }

        return EnDusukFiyatliTeklif();
    }

    public SatinalmaTeklif? EnDusukFiyatliTeklif()
    {
        TeklifFiyatlariniGuncelle();
        return Teklifler.OrderBy(t => t.GenelToplam).ThenBy(t => t.FirmaAdi).FirstOrDefault();
    }

    /// <summary>Eski kayıtlarda elle seçim bayrağı yoksa bir kez ayırır.</summary>
    public void SatinalmaOnerisiMigrasyonu()
    {
        if (SatinalmaOnerisiElleSecildi || YonetimOnerilenTeklifId is not { } eskiId)
            return;

        var enDusuk = EnDusukFiyatliTeklif();
        if (enDusuk is null)
            return;

        if (eskiId != enDusuk.Id)
            SatinalmaOnerisiElleSecildi = true;
        else
            YonetimOnerilenTeklifId = null;
    }

    private void TeklifFiyatlariniGuncelle()
    {
        Kalemler ??= [];
        Teklifler ??= [];
        foreach (var teklif in Teklifler)
        {
            teklif.Fiyatlar ??= [];
            teklif.FiyatlariHesapla(Kalemler);
        }
    }

    public IEnumerable<string> KalemSatirlari() =>
        Kalemler.OrderBy(k => k.SiraNo).Select(k =>
        {
            var satir = $"{k.Malzeme} — {k.Miktar:N2} {k.Birim}";
            return string.IsNullOrWhiteSpace(k.Aciklama) ? satir : $"{satir} ({k.Aciklama})";
        });

    public string KalemOzeti =>
        Kalemler.Count == 0 ? "Kalem bilgisi yok" : string.Join("\n", KalemSatirlari());

    [System.Text.Json.Serialization.JsonIgnore]
    public string GorunenDurum => SatinalmaTalepDurumEtiketi.Olustur(this);
}

public static class SatinalmaTalepDurumlari
{
    public const string Taslak = "Taslak";
    public const string Hazirlaniyor = "Hazırlanıyor";
    public const string ImzaSurecinde = "İmza Sürecinde";
    public const string YonetimOnayinda = "Yönetim Onayında";
    public const string TeklifGirisi = "Teklif Girişi";
    public const string Karsilastirma = "Karşılaştırma";
    public const string Onaylandi = "Onaylandı";
    public const string Reddedildi = "Reddedildi";
    public const string SiparisOlusturuldu = "Sipariş Oluşturuldu";

    public static IReadOnlyList<string> TumDurumlar { get; } =
    [
        Taslak, Hazirlaniyor, ImzaSurecinde, YonetimOnayinda, TeklifGirisi,
        Karsilastirma, Onaylandi, Reddedildi, SiparisOlusturuldu
    ];

    /// <summary>İş akışı aşaması — bulut/yerel birleştirmede ileri aşama tercih edilir.</summary>
    public static int SurecAsamaSkoru(string? durum) => durum switch
    {
        SiparisOlusturuldu => 90,
        Onaylandi => 70,
        YonetimOnayinda => 60,
        Karsilastirma => 50,
        TeklifGirisi => 40,
        ImzaSurecinde => 30,
        Hazirlaniyor => 20,
        Reddedildi => 15,
        Taslak => 0,
        _ => 0
    };
}

public class SatinalmaAyarlar
{
    public string FirmaAdi { get; set; } = "";
    public string SartnameMetni { get; set; } = "";
    public string TeklifIstemeSartnameleri { get; set; } = "";
    public List<ImzaAyari>? SefImzalari { get; set; }
    public List<ImzaAyari>? YonetimImzalari { get; set; }
    public int SonTalepSira { get; set; }
    public int SonSiparisSira { get; set; }
    /// <summary>Tüm cihazlarda silinmiş sayılan talep kimlikleri.</summary>
    public List<Guid> SilinenTalepIdleri { get; set; } = [];
    public decimal VarsayilanUsdKuru { get; set; }
    public decimal VarsayilanEurKuru { get; set; }
}

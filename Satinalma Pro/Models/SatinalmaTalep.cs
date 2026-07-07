using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using SatinalmaPro.Helpers;

namespace SatinalmaPro.Models;

public class SatinalmaTeklifFiyati
{
    public Guid KalemId { get; set; }
    public string Marka { get; set; } = "";
    public string ParaBirimi { get; set; } = ParaBirimleri.Try;
    public decimal BirimFiyat { get; set; }
    public double KdvOrani { get; set; } = 20;
    public decimal ToplamTutar { get; set; }
    public decimal KdvTutari { get; set; }
    public decimal ToplamKdvDahil { get; set; }

    public decimal TlBirimFiyat(decimal usdKuru, decimal eurKuru) =>
        ParaBirimleri.TlCevir(BirimFiyat, ParaBirimi, usdKuru, eurKuru);

    public string BirimFiyatGosterim(decimal usdKuru, decimal eurKuru) =>
        ParaBirimleri.BirimFiyatGosterim(BirimFiyat, ParaBirimi, usdKuru, eurKuru);

    public void Hesapla(double miktar, decimal usdKuru = 0, decimal eurKuru = 0)
    {
        var tlBirim = TlBirimFiyat(usdKuru, eurKuru);
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
    public ObservableCollection<SatinalmaTeklifFiyati> Fiyatlar { get; set; } = [];

    public decimal AraToplam => (Fiyatlar ?? []).Sum(f => f.ToplamTutar);
    public decimal KdvTutari => (Fiyatlar ?? []).Sum(f => f.KdvTutari);
    public decimal GenelToplam => (Fiyatlar ?? []).Sum(f => f.ToplamKdvDahil);

    [JsonIgnore]
    public string AraToplamGosterim =>
        $"KDV Hariç: {AraToplam.ToString("N2", System.Globalization.CultureInfo.GetCultureInfo("tr-TR"))} ₺";

    [JsonIgnore]
    public string KdvGosterim =>
        $"KDV: {KdvTutari.ToString("N2", System.Globalization.CultureInfo.GetCultureInfo("tr-TR"))} ₺";

    [JsonIgnore]
    public string GenelToplamGosterim =>
        $"KDV Dahil: {GenelToplam.ToString("N2", System.Globalization.CultureInfo.GetCultureInfo("tr-TR"))} ₺";

    [JsonIgnore]
    public string MarkaOzeti
    {
        get
        {
            var markalar = (Fiyatlar ?? [])
                .Select(f => f.Marka)
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (markalar.Count == 0 && !string.IsNullOrWhiteSpace(Marka))
                return Marka;

            return markalar.Count switch
            {
                0 => "—",
                1 => markalar[0],
                _ => string.Join(", ", markalar)
            };
        }
    }

    public string MarkaMetni(Guid kalemId) =>
        (Fiyatlar ?? []).FirstOrDefault(f => f.KalemId == kalemId)?.Marka?.Trim() ?? "";

    public void FiyatlariHesapla(IEnumerable<SatinalmaTalepKalemi> kalemler)
    {
        Fiyatlar ??= [];
        var kalemList = kalemler.ToList();
        foreach (var fiyat in Fiyatlar)
        {
            if (fiyat.KdvOrani <= 0 && KdvOrani > 0)
                fiyat.KdvOrani = KdvOrani;

            var kalem = kalemList.FirstOrDefault(k => k.Id == fiyat.KalemId);
            if (kalem != null)
                fiyat.Hesapla(kalem.Miktar, UsdKuru, EurKuru);
        }
    }
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
    public string TeklifDuzeltmeNotu { get; set; } = "";
    /// <summary>UTC unix ms — bulut birleştirmede en güncel kayıt kazanır.</summary>
    public long GuncellemeUtc { get; set; }
    public Guid? YonetimOnerilenTeklifId { get; set; }
    /// <summary>Satınalmacı öneriyi elle seçtiyse true; aksi halde sistem en düşük fiyatlı teklifi önerir.</summary>
    public bool SatinalmaOnerisiElleSecildi { get; set; }
    /// <summary>Öneri kalem bazlı (farklı firmalardan birim fiyat) seçildiyse true.</summary>
    public bool SatinalmaKalemOnerisiElleSecildi { get; set; }
    public string Durum { get; set; } = SatinalmaTalepDurumlari.Taslak;
    /// <summary>Enterprise Firestore status (draft, submitted, quote_requested, …).</summary>
    public string Status { get; set; } = "";
    public string Priority { get; set; } = "normal";
    public bool HasReturnFlag { get; set; }
    public string SiparisNo { get; set; } = "";
    public Guid? OnaylananTeklifId { get; set; }
    public Dictionary<Guid, string> FirmaSiparisNolari { get; set; } = [];
    public string YonetimOnaylayanUid { get; set; } = "";
    public string YonetimOnaylayanAd { get; set; } = "";
    public string YonetimOnaylayanEposta { get; set; } = "";
    public string YonetimOnayTarihi { get; set; } = "";
    public bool YonetimOnayKilitli { get; set; }
    /// <summary>Yönetim teklif olmadan onayladı; firma/fiyat satınalma tarafından girilecek.</summary>
    public bool TeklifsizYonetimOnayi { get; set; }
    public ObservableCollection<SatinalmaTalepKalemi> Kalemler { get; set; } = [];
    public ObservableCollection<SatinalmaTeklif> Teklifler { get; set; } = [];

    [JsonIgnore]
    public bool TumKalemlerOnayli =>
        Kalemler is { Count: > 0 } && Kalemler.All(k => k.OnaylananTeklifId != null);

    [JsonIgnore]
    public bool HerhangiKalemOnayli =>
        Kalemler?.Any(k => k.OnaylananTeklifId != null) == true;

    public SatinalmaTeklif? OnaylananTeklif =>
        OnaylananTeklifId is { } id ? (Teklifler ?? []).FirstOrDefault(t => t.Id == id) : null;

    public SatinalmaTeklif? KalemOnayTeklifi(SatinalmaTalepKalemi kalem) =>
        kalem.OnaylananTeklifId is { } id ? (Teklifler ?? []).FirstOrDefault(t => t.Id == id) : null;

    [JsonIgnore]
    public bool TeklifsizFirmaFiyatBekliyor =>
        TeklifsizYonetimOnayi && !HerhangiKalemOnayli;

    /// <summary>Satınalma önerisi — elle seçim yoksa KDV dahil en düşük toplam.</summary>
    public SatinalmaTeklif? OnerilenTeklif() => OnerilenTeklifFirma();

    /// <summary>Tek firma önerisi (kalem bazlı öneride null).</summary>
    public SatinalmaTeklif? OnerilenTeklifFirma()
    {
        TeklifFiyatlariniGuncelle();

        if (SatinalmaKalemOnerisiElleSecildi)
            return null;

        if (SatinalmaOnerisiElleSecildi && YonetimOnerilenTeklifId is { } id)
        {
            var secili = (Teklifler ?? []).FirstOrDefault(t => t.Id == id);
            if (secili is not null)
                return secili;
        }

        return EnDusukFiyatliTeklif();
    }

    public void TeklifFiyatlariniGuncellePublic() => TeklifFiyatlariniGuncelle();

    public SatinalmaTeklif? EnDusukFiyatliTeklif()
    {
        TeklifFiyatlariniGuncelle();
        return (Teklifler ?? []).OrderBy(t => t.GenelToplam).ThenBy(t => t.FirmaAdi).FirstOrDefault();
    }

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

    [JsonIgnore]
    public string KalemSayisiMetni => $"{(Kalemler?.Count ?? 0)} kalem";

    [JsonIgnore]
    public string TeklifSayisiMetni => $"{Teklifler?.Count ?? 0} teklif";

    [JsonIgnore]
    public string TeklifGirisOzetMetni => $"{Tarih} · {TeklifSayisiMetni}";
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
        Taslak, Hazirlaniyor, ImzaSurecinde, YonetimOnayinda, TeklifGirisi, Karsilastirma,
        Onaylandi, Reddedildi, SiparisOlusturuldu
    ];
}

public static class TalepTurleri
{
    public const string Acil = "Acil";
    public const string Oncelikli = "Oncelikli";
    public const string Normal = "Normal";

    public static IReadOnlyList<string> Tum { get; } = [Acil, Oncelikli, Normal];

    public static string TurkceAd(string tur) => tur switch
    {
        Acil => "Acil",
        Oncelikli => "Öncelikli",
        Normal => "Normal",
        _ => string.IsNullOrWhiteSpace(tur) ? Normal : tur
    };

    public static string GorunenAd(string tur) => TurkceAd(tur);
}

public class ImzaAyari
{
    public string Unvan { get; set; } = "";
    public string AdSoyad { get; set; } = "";
    public bool Aktif { get; set; } = true;
}

public static class ImzaGruplari
{
    public const string Sef = "Sef";
    public const string Yonetim = "Yonetim";
}

public class SartnameDosyasi
{
    public string Ad { get; set; } = "";
    public string Metin { get; set; } = "";
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DosyaYolu { get; set; }
}

public class SatinalmaAyarlar
{
    public string FirmaAdi { get; set; } = "";
    public string LogoDosyaYolu { get; set; } = "";
    public string SartnameMetni { get; set; } = "";
    public string TeklifIstemeSartnameleri { get; set; } = "";
    public ObservableCollection<ImzaAyari> SefImzalari { get; set; } = [];
    public ObservableCollection<ImzaAyari> YonetimImzalari { get; set; } = [];
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ObservableCollection<ImzaAyari>? Imzalar { get; set; }
    public ObservableCollection<SartnameDosyasi> Sartnameler { get; set; } = [];
    public int SonTalepSira { get; set; }
    public int SonSiparisSira { get; set; }
    public int SonIadeSira { get; set; }
    public List<Guid> SilinenTalepIdleri { get; set; } = [];
    public decimal VarsayilanUsdKuru { get; set; }
    public decimal VarsayilanEurKuru { get; set; }
    /// <summary>Tüm verileri sıfırla sonrası — varsayılan imza ünvanları eklenmez.</summary>
    public bool ImzaAyarleriTemiz { get; set; }

    public static SatinalmaAyarlar VarsayilanOlustur() => new()
    {
        SefImzalari =
        [
            new ImzaAyari { Unvan = "Satınalma", Aktif = true },
            new ImzaAyari { Unvan = "Tünel Şefi", Aktif = true },
            new ImzaAyari { Unvan = "Şantiye Şefi", Aktif = true }
        ],
        YonetimImzalari =
        [
            new ImzaAyari { Unvan = "Proje Müdürü", Aktif = true }
        ]
    };

    /// <summary>Modül sıfırlama — imza ünvanları, şartname ve firma bilgisi boş.</summary>
    public static SatinalmaAyarlar SifirlanmisOlustur() => new()
    {
        SefImzalari = [],
        YonetimImzalari = [],
        Sartnameler = [],
        ImzaAyarleriTemiz = true
    };
}

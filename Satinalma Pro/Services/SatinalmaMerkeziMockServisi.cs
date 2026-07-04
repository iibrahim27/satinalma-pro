using System.Windows.Media;
using SatinalmaPro.Models.SatinalmaMerkezi;

namespace SatinalmaPro.Services;

/// <summary>Satınalma Merkezi mock veri — Firebase bağlantısı sonraki aşamada mevcut servislere yönlendirilecek.</summary>
public static class SatinalmaMerkeziMockServisi
{
    private static readonly Guid T1 = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1");
    private static readonly Guid T2 = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2");
    private static readonly Guid S1 = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1");

    public static IReadOnlyList<KpiKartModel> KpiKartlari() =>
    [
        new() { Baslik = "Bekleyen Talepler", Deger = "12", Renk = Brush("#F59E0B"), FiltreAnahtar = "bekleyen" },
        new() { Baslik = "Teklif Hazırlanacak", Deger = "5", Renk = Brush("#7C3AED"), FiltreAnahtar = "teklif" },
        new() { Baslik = "Yönetim Onayı Bekleyen", Deger = "3", Renk = Brush("#2563EB"), FiltreAnahtar = "onay" },
        new() { Baslik = "Sipariş Oluşturulacak", Deger = "4", Renk = Brush("#0891B2"), FiltreAnahtar = "siparis" },
        new() { Baslik = "Beklenen Teslimatlar", Deger = "7", Renk = Brush("#0D9488"), FiltreAnahtar = "teslimat" },
        new() { Baslik = "Kısmi Teslimler", Deger = "2", Renk = Brush("#EA580C"), FiltreAnahtar = "kismi" },
        new() { Baslik = "İade Bekleyen", Deger = "1", Renk = Brush("#DC2626"), FiltreAnahtar = "iade" },
        new() { Baslik = "Bugünkü Siparişler", Deger = "6", Renk = Brush("#059669"), FiltreAnahtar = "bugun" }
    ];

    public static IReadOnlyList<YapilacakIsModel> YapilacakIsler() =>
    [
        new() { Baslik = "ABC Yapı aranacak", Aciklama = "TLP-2024-089 teklif", Oncelik = "Yüksek", OncelikRenk = Brush("#DC2626"), IlgiliNo = "TLP-2024-089" },
        new() { Baslik = "Teklif hazırlanacak", Aciklama = "XYZ Elektrik fiyat bekleniyor", Oncelik = "Normal", OncelikRenk = Brush("#F59E0B"), IlgiliNo = "TLP-2024-091" },
        new() { Baslik = "Sipariş oluşturulacak", Aciklama = "Onaylanan teklif — Demir Çelik", Oncelik = "Acil", OncelikRenk = Brush("#DC2626"), IlgiliNo = "TLP-2024-085" },
        new() { Baslik = "Eksik teslim takibi", Aciklama = "SIP-2024-044 kısmi teslim", Oncelik = "Yüksek", OncelikRenk = Brush("#EA580C"), IlgiliNo = "SIP-2024-044" },
        new() { Baslik = "İade süreci", Aciklama = "Hasarlı malzeme bildirimi", Oncelik = "Normal", OncelikRenk = Brush("#64748B"), IlgiliNo = "SIP-2024-038" }
    ];

    public static IReadOnlyList<SonHareketModel> SonHareketler() =>
    [
        new() { Mesaj = "TLP-2024-092 oluşturuldu", Kullanici = "Ahmet Şef", Zaman = "14:32" },
        new() { Mesaj = "Teklif eklendi — ABC Yapı", Kullanici = "Mehmet Satınalma", Zaman = "13:55" },
        new() { Mesaj = "Yönetim onayı — Doğrudan Onay", Kullanici = "Yönetim", Zaman = "13:10" },
        new() { Mesaj = "SIP-2024-045 oluşturuldu", Kullanici = "Mehmet Satınalma", Zaman = "11:40" },
        new() { Mesaj = "Mal kabul — kısmi teslim", Kullanici = "Depo Sorumlusu", Zaman = "10:15" },
        new() { Mesaj = "İade süreci başlatıldı", Kullanici = "Mehmet Satınalma", Zaman = "09:50" }
    ];

    public static IReadOnlyList<TalepSatirModel> Talepler() =>
    [
        Satir(T1, "TLP-2024-092", "03.07.2026", "A Blok", "Ahmet Şef", "Acil", "Bekleyen", "Beklemede", "03.07.2026 14:32", "#DC2626", "#F59E0B"),
        Satir(T2, "TLP-2024-091", "02.07.2026", "B Blok", "Fatma Saha", "Normal", "Teklif Bekleyen", "Teklif İste", "02.07.2026 16:00", "#7C3AED", "#64748B"),
        Satir(Guid.NewGuid(), "TLP-2024-090", "01.07.2026", "C Blok", "Ali Atölye", "Normal", "Teklif Hazırlanıyor", "Teklif İste", "02.07.2026 09:00", "#7C3AED", "#64748B"),
        Satir(Guid.NewGuid(), "TLP-2024-089", "28.06.2026", "Merkez", "Mehmet Satınalma", "Normal", "Onaylandı", "Doğrudan Onay", "28.06.2026 15:00", "#059669", "#64748B"),
        Satir(Guid.NewGuid(), "TLP-2024-085", "25.06.2026", "D Blok", "Ahmet Şef", "Acil", "Siparişe Dönüşen", "Teklif İste", "26.06.2026 10:00", "#2563EB", "#DC2626"),
        Satir(Guid.NewGuid(), "TLP-2024-080", "20.06.2026", "A Blok", "Fatma Saha", "Normal", "Reddedildi", "Reddet", "21.06.2026 11:00", "#DC2626", "#64748B")
    ];

    public static IReadOnlyList<SiparisSatirModel> Siparisler() =>
    [
        new() { Id = S1, SiparisNo = "SIP-2024-045", Firma = "XYZ Elektrik", TalepNo = "TLP-2024-085", Santiye = "D Blok", ToplamTutar = 87500m, SiparisTarihi = "26.06.2026", Durum = "Sevkiyatta", DurumRenk = Brush("#2563EB") },
        new() { Id = Guid.NewGuid(), SiparisNo = "SIP-2024-044", Firma = "Demir Çelik A.Ş.", TalepNo = "TLP-2024-082", Santiye = "A Blok", ToplamTutar = 142000m, SiparisTarihi = "24.06.2026", Durum = "Kısmi Teslim", DurumRenk = Brush("#EA580C") },
        new() { Id = Guid.NewGuid(), SiparisNo = "SIP-2024-038", Firma = "Boya Sanayi", TalepNo = "TLP-2024-078", Santiye = "B Blok", ToplamTutar = 22400m, SiparisTarihi = "18.06.2026", Durum = "İade", DurumRenk = Brush("#DC2626") }
    ];

    public static IReadOnlyList<DepoTakipSatirModel> DepoTakip() =>
    [
        new() { Malzeme = "Demir 12mm", SiparisMiktari = 10, TeslimAlinan = 6, Kalan = 4, Eksik = 4, Fazla = 0, Durum = "Kısmi Teslim", DurumRenk = Brush("#EA580C") },
        new() { Malzeme = "Demir 16mm", SiparisMiktari = 8, TeslimAlinan = 8, Kalan = 0, Eksik = 0, Fazla = 0, Durum = "Tam Teslim", DurumRenk = Brush("#059669") },
        new() { Malzeme = "Elektrik Kablosu", SiparisMiktari = 500, TeslimAlinan = 0, Kalan = 500, Eksik = 0, Fazla = 0, Durum = "Bekleniyor", DurumRenk = Brush("#F59E0B") }
    ];

    public static TalepDetayModel TalepDetay(Guid id) =>
        id == T1 ? DetayT1() : DetayT2();

    public static IReadOnlyList<TedarikciPerformansModel> TedarikciPerformans() =>
    [
        new() { Firma = "ABC Yapı", ToplamSiparis = 24, ToplamTutar = 1250000m, ZamanindaTeslim = 95, EksikTeslim = 3, Iade = 1, Kalite = 92, OrtTeslimSuresi = "7 gün", PerformansPuani = 92 },
        new() { Firma = "XYZ Elektrik", ToplamSiparis = 18, ToplamTutar = 890000m, ZamanindaTeslim = 88, EksikTeslim = 5, Iade = 2, Kalite = 85, OrtTeslimSuresi = "5 gün", PerformansPuani = 86 },
        new() { Firma = "Demir Çelik A.Ş.", ToplamSiparis = 31, ToplamTutar = 2100000m, ZamanindaTeslim = 82, EksikTeslim = 10, Iade = 4, Kalite = 78, OrtTeslimSuresi = "10 gün", PerformansPuani = 80 }
    ];

    public static IReadOnlyList<SiparisSatirModel> BeklenenSiparisler() =>
        Siparisler().Where(s => s.Durum is "Sevkiyatta" or "Bekleniyor" or "Kısmi Teslim").ToList();

    public static IReadOnlyList<IadeSatirModel> Iadeler() =>
    [
        new() { IadeNo = "IAD-2024-012", SiparisNo = "SIP-2024-038", Firma = "Boya Sanayi", Malzeme = "İç Cephe Boyası", Miktar = "40 Litre", Neden = "Hasarlı ambalaj", Durum = "İncelemede", Tarih = "02.07.2026", DurumRenk = Brush("#F59E0B") },
        new() { IadeNo = "IAD-2024-011", SiparisNo = "SIP-2024-031", Firma = "Demir Çelik A.Ş.", Malzeme = "Demir 10mm", Miktar = "2 Ton", Neden = "Kalite uyumsuzluğu", Durum = "Onaylandı", Tarih = "28.06.2026", DurumRenk = Brush("#059669") }
    ];

    public static IReadOnlyList<TamamlananSatirModel> Tamamlananlar() =>
    [
        new() { KayitNo = "TLP-2024-075", Tip = "Talep", Santiye = "A Blok", Firma = "—", Tutar = "—", TamamlanmaTarihi = "15.06.2026", Durum = "Siparişe Dönüştü" },
        new() { KayitNo = "SIP-2024-032", Tip = "Sipariş", Santiye = "C Blok", Firma = "ABC Yapı", Tutar = "56.400 ₺", TamamlanmaTarihi = "20.06.2026", Durum = "Tam Teslim" },
        new() { KayitNo = "SIP-2024-028", Tip = "Sipariş", Santiye = "Merkez", Firma = "XYZ Elektrik", Tutar = "12.800 ₺", TamamlanmaTarihi = "18.06.2026", Durum = "Tam Teslim" }
    ];

    public static IReadOnlyList<BildirimModel> Bildirimler() =>
    [
        new() { Baslik = "Yeni Talep", Mesaj = "TLP-2024-092 oluşturuldu", Zaman = "14:32", Okundu = false },
        new() { Baslik = "Yönetim Teklif İstedi", Mesaj = "TLP-2024-091 için teklif toplanacak", Zaman = "13:00", Okundu = false },
        new() { Baslik = "Depo Mal Kabul", Mesaj = "SIP-2024-044 kısmi teslim", Zaman = "10:15", Okundu = true },
        new() { Baslik = "Eksik Teslim", Mesaj = "Demir 12mm eksik bildirildi", Zaman = "09:50", Okundu = false }
    ];

    private static TalepSatirModel Satir(Guid id, string no, string tarih, string santiye, string eden, string oncelik,
        string durum, string karar, string son, string oncelikRenk, string durumRenk) =>
        new()
        {
            Id = id, TalepNo = no, TalepTarihi = tarih, Santiye = santiye, TalepEden = eden,
            Oncelik = oncelik, Durum = durum, YonetimKarari = karar, SonIslemTarihi = son,
            OncelikRenk = Brush(oncelikRenk), DurumRenk = Brush(durumRenk)
        };

    private static SolidColorBrush Brush(string hex) => new((Color)ColorConverter.ConvertFromString(hex)!);

    private static TalepDetayModel DetayT1() => new()
    {
        Id = T1, TalepNo = "TLP-2024-092", Santiye = "A Blok Şantiye", TalepEden = "Ahmet Şef",
        Tarih = "03.07.2026", Oncelik = "Acil", Durum = "Bekleyen", YonetimKarari = "Beklemede",
        Aciklama = "Acil elektrik malzemesi ihtiyacı — şantiye durdurma riski.",
        Malzemeler =
        [
            new() { Ad = "Elektrik Kablosu NYM 3x2.5", Miktar = "500", Birim = "Metre" },
            new() { Ad = "Sigorta Kutusu", Miktar = "12", Birim = "Adet" }
        ],
        Dosyalar = [new() { Ad = "Teknik_Şartname.pdf", Tip = "PDF", Boyut = "245 KB" }],
        Fotograflar = [new() { Ad = "Saha_Foto_1.jpg", Tip = "Resim", Boyut = "1.2 MB" }],
        Timeline =
        [
            new() { Baslik = "Talep Oluşturuldu", Tarih = "03.07.2026 14:32", Kullanici = "Ahmet Şef", Tamamlandi = true },
            new() { Baslik = "Yönetim İncelemesi Bekleniyor", Tarih = "—", Kullanici = "—", Tamamlandi = false }
        ],
        IslemGecmisi =
        [
            new() { Kullanici = "Ahmet Şef", Islem = "Talep oluşturuldu", Tarih = "03.07.2026 14:32", EskiDeger = "—", YeniDeger = "TLP-2024-092" }
        ],
        Teklifler = TekliflerT2(),
        Yorumlar = ["Acil teslim gerekli.", "Şantiye elektrik kesintisi yaşanıyor."]
    };

    private static TalepDetayModel DetayT2() => new()
    {
        Id = T2, TalepNo = "TLP-2024-091", Santiye = "B Blok", TalepEden = "Fatma Saha",
        Tarih = "02.07.2026", Oncelik = "Normal", Durum = "Teklif Bekleyen", YonetimKarari = "Teklif İste",
        Aciklama = "Boya ve izolasyon malzemeleri.",
        Malzemeler = [new() { Ad = "İç Cephe Boyası", Miktar = "200", Birim = "Litre" }],
        Dosyalar = [], Fotograflar = [],
        Timeline =
        [
            new() { Baslik = "Talep Oluşturuldu", Tarih = "02.07.2026 09:00", Kullanici = "Fatma Saha", Tamamlandi = true },
            new() { Baslik = "Yönetim — Teklif İste", Tarih = "02.07.2026 16:00", Kullanici = "Yönetim", Tamamlandi = true },
            new() { Baslik = "Teklif Toplama", Tarih = "Devam ediyor", Kullanici = "Satınalma", Tamamlandi = false }
        ],
        IslemGecmisi =
        [
            new() { Kullanici = "Fatma Saha", Islem = "Talep oluşturuldu", Tarih = "02.07.2026 09:00", EskiDeger = "—", YeniDeger = "TLP-2024-091" },
            new() { Kullanici = "Yönetim", Islem = "Karar verildi", Tarih = "02.07.2026 16:00", EskiDeger = "Beklemede", YeniDeger = "Teklif İste" }
        ],
        Teklifler = TekliflerT2(),
        Yorumlar = []
    };

    private static IReadOnlyList<TeklifSatirModel> TekliflerT2() =>
    [
        new() { Firma = "ABC Yapı", Marka = "Knauf", BirimFiyat = 85m, Iskonto = 5m, Kdv = 20m, Toplam = 42000m, TeslimSuresi = "7 gün", Vade = "30 gün", TeklifTarihi = "03.07.2026", Dosya = "abc_teklif.pdf", Durum = "Aktif", EnUygunFiyat = false, EnKisaTeslim = false, EnYuksekPerformans = true },
        new() { Firma = "XYZ İnşaat", Marka = "Weber", BirimFiyat = 78m, Iskonto = 8m, Kdv = 20m, Toplam = 38500m, TeslimSuresi = "5 gün", Vade = "Peşin", TeklifTarihi = "03.07.2026", Dosya = "xyz_teklif.pdf", Durum = "Aktif", EnUygunFiyat = true, EnKisaTeslim = true, EnYuksekPerformans = false },
        new() { Firma = "Boya Sanayi", Marka = "Filli", BirimFiyat = 92m, Iskonto = 3m, Kdv = 20m, Toplam = 45200m, TeslimSuresi = "10 gün", Vade = "45 gün", TeklifTarihi = "02.07.2026", Dosya = "boya_teklif.pdf", Durum = "Aktif", EnUygunFiyat = false, EnKisaTeslim = false, EnYuksekPerformans = false }
    ];
}

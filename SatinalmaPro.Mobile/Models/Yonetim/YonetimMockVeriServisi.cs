namespace SatinalmaPro.Mobile.Models.Yonetim;

/// <summary>
/// Yönetim paneli UI önizlemesi için mock veri. Backend bağlantısı yoktur.
/// </summary>
public static class YonetimMockVeriServisi
{
    private static readonly List<YonetimTalepOgesi> Talepler =
    [
        OlusturTalep("t1", "TLP-2026-0142", "Merkez Şantiye", "Ahmet Yılmaz", "03.07.2026 09:15",
            5, "Acil beton ve demir ihtiyacı — temel dökümü yarın.", YonetimTalepOncelik.Acil, YonetimTalepDurum.Bekleyen),
        OlusturTalep("t2", "TLP-2026-0141", "Kuzey Projesi", "Mehmet Kaya", "03.07.2026 08:40",
            3, "Elektrik tesisat malzemeleri.", YonetimTalepOncelik.Normal, YonetimTalepDurum.Bekleyen),
        OlusturTalep("t3", "TLP-2026-0140", "Güney Blok", "Fatma Demir", "02.07.2026 16:20",
            8, "İç mekan boya ve alçıpan malzemeleri.", YonetimTalepOncelik.Normal, YonetimTalepDurum.Bekleyen),
        OlusturTalep("t4", "TLP-2026-0139", "Merkez Şantiye", "Ali Öztürk", "02.07.2026 11:00",
            2, "Su tesisatı boru ve fitting.", YonetimTalepOncelik.Acil, YonetimTalepDurum.Bekleyen),
        OlusturTalep("t5", "TLP-2026-0138", "Doğu Sitesi", "Zeynep Arslan", "01.07.2026 14:30",
            6, "Çatı izolasyon malzemeleri.", YonetimTalepOncelik.Normal, YonetimTalepDurum.Onaylanan),
        OlusturTalep("t6", "TLP-2026-0137", "Batı Konut", "Hasan Çelik", "01.07.2026 10:15",
            4, "Kapı ve pencere aksesuarları.", YonetimTalepOncelik.Normal, YonetimTalepDurum.Reddedilen),
        OlusturTalep("t7", "TLP-2026-0136", "Kuzey Projesi", "Emre Şahin", "30.06.2026 09:00",
            7, "Hafriyat ekipmanları kiralama talebi.", YonetimTalepOncelik.Normal, YonetimTalepDurum.TeklifBekliyor),
    ];

    private static readonly List<YonetimTeklifOgesi> Teklifler =
    [
        new()
        {
            Id = "tk1",
            TalepNo = "TLP-2026-0136",
            Santiye = "Kuzey Projesi",
            MalzemeOzeti = "Hafriyat ekipmanları (7 kalem)",
            FirmaSayisi = 3,
            ToplamTeklifSayisi = 3,
            Firmalar =
            [
                new() { Id = "f1", FirmaAdi = "Anadolu İnşaat Mak.", ToplamTutar = "₺142.500", TeslimSuresi = "2 gün", OdemeSekli = "Peşin", Aciklama = "Ekskavatör + operatör dahil.", EnUygun = true, TutarSayisal = 142500 },
                new() { Id = "f2", FirmaAdi = "MakinaPro Ltd.", ToplamTutar = "₺158.000", TeslimSuresi = "1 gün", OdemeSekli = "30 gün vadeli", Aciklama = "Hızlı teslimat garantisi.", EnUygun = false, TutarSayisal = 158000 },
                new() { Id = "f3", FirmaAdi = "Yıldız Ekipman", ToplamTutar = "₺149.750", TeslimSuresi = "3 gün", OdemeSekli = "Peşin", Aciklama = "İkinci el ekipman seçeneği mevcut.", EnUygun = false, TutarSayisal = 149750 },
            ]
        },
        new()
        {
            Id = "tk2",
            TalepNo = "TLP-2026-0133",
            Santiye = "Merkez Şantiye",
            MalzemeOzeti = "Beton ve demir (4 kalem)",
            FirmaSayisi = 4,
            ToplamTeklifSayisi = 4,
            Firmalar =
            [
                new() { Id = "f4", FirmaAdi = "Beton A.Ş.", ToplamTutar = "₺89.200", TeslimSuresi = "1 gün", OdemeSekli = "Peşin", EnUygun = true, TutarSayisal = 89200 },
                new() { Id = "f5", FirmaAdi = "Demir Ticaret", ToplamTutar = "₺94.500", TeslimSuresi = "2 gün", OdemeSekli = "15 gün vadeli", EnUygun = false, TutarSayisal = 94500 },
                new() { Id = "f6", FirmaAdi = "Yapı Malzeme Co.", ToplamTutar = "₺91.800", TeslimSuresi = "1 gün", OdemeSekli = "Peşin", EnUygun = false, TutarSayisal = 91800 },
                new() { Id = "f7", FirmaAdi = "İnşaat Deposu", ToplamTutar = "₺97.000", TeslimSuresi = "3 gün", OdemeSekli = "30 gün vadeli", EnUygun = false, TutarSayisal = 97000 },
            ]
        },
        new()
        {
            Id = "tk3",
            TalepNo = "TLP-2026-0128",
            Santiye = "Güney Blok",
            MalzemeOzeti = "Elektrik pano malzemeleri (5 kalem)",
            FirmaSayisi = 2,
            ToplamTeklifSayisi = 2,
            Firmalar =
            [
                new() { Id = "f8", FirmaAdi = "ElektrikPro", ToplamTutar = "₺67.400", TeslimSuresi = "5 gün", OdemeSekli = "Peşin", EnUygun = true, TutarSayisal = 67400 },
                new() { Id = "f9", FirmaAdi = "Volt Tedarik", ToplamTutar = "₺72.100", TeslimSuresi = "3 gün", OdemeSekli = "15 gün vadeli", EnUygun = false, TutarSayisal = 72100 },
            ]
        },
    ];

    private static readonly List<YonetimBildirimOgesi> Bildirimler =
    [
        new() { Id = "b1", Tip = YonetimBildirimTipi.YeniTalep, Baslik = "Yeni Talep", Mesaj = "TLP-2026-0142 — Merkez Şantiye (Acil)", Zaman = "5 dk önce", Okundu = false },
        new() { Id = "b2", Tip = YonetimBildirimTipi.YeniTeklif, Baslik = "Yeni Teklif", Mesaj = "TLP-2026-0136 için 3 firma teklif verdi", Zaman = "22 dk önce", Okundu = false },
        new() { Id = "b3", Tip = YonetimBildirimTipi.YeniTalep, Baslik = "Yeni Talep", Mesaj = "TLP-2026-0141 — Kuzey Projesi", Zaman = "1 saat önce", Okundu = false },
        new() { Id = "b4", Tip = YonetimBildirimTipi.OnaylananTalep, Baslik = "Onaylanan Talep", Mesaj = "TLP-2026-0138 doğrudan onaylandı", Zaman = "2 saat önce", Okundu = true },
        new() { Id = "b5", Tip = YonetimBildirimTipi.ReddedilenTalep, Baslik = "Reddedilen Talep", Mesaj = "TLP-2026-0137 reddedildi", Zaman = "Dün 16:45", Okundu = true },
        new() { Id = "b6", Tip = YonetimBildirimTipi.YeniTeklif, Baslik = "Yeni Teklif", Mesaj = "TLP-2026-0133 için teklifler hazır", Zaman = "Dün 11:20", Okundu = true },
    ];

    public static Task<YonetimDashboardOzet> DashboardGetirAsync(bool hataSimule = false)
    {
        if (hataSimule)
            throw new InvalidOperationException("Veriler yüklenemedi.");

        return Task.FromResult(new YonetimDashboardOzet
        {
            BekleyenTalepler = Talepler.Count(t => t.Durum == YonetimTalepDurum.Bekleyen),
            TeklifBekleyenler = Teklifler.Count,
            BugunOnaylananlar = Talepler.Count(t => t.Durum == YonetimTalepDurum.Onaylanan),
            Reddedilenler = Talepler.Count(t => t.Durum == YonetimTalepDurum.Reddedilen),
        });
    }

    public static Task<IReadOnlyList<YonetimTalepOgesi>> SonTaleplerGetirAsync(int adet = 5)
        => Task.FromResult<IReadOnlyList<YonetimTalepOgesi>>(Talepler.Take(adet).ToList());

    public static Task<IReadOnlyList<YonetimTalepOgesi>> TaleplerGetirAsync(string? filtre = null, string? arama = null)
    {
        var sorgu = Talepler.AsEnumerable();

        sorgu = filtre switch
        {
            "bekleyen" => sorgu.Where(t => t.Durum == YonetimTalepDurum.Bekleyen),
            "acil" => sorgu.Where(t => t.Oncelik == YonetimTalepOncelik.Acil),
            "normal" => sorgu.Where(t => t.Oncelik == YonetimTalepOncelik.Normal),
            "onaylanan" => sorgu.Where(t => t.Durum == YonetimTalepDurum.Onaylanan),
            "reddedilen" => sorgu.Where(t => t.Durum == YonetimTalepDurum.Reddedilen),
            _ => sorgu
        };

        if (!string.IsNullOrWhiteSpace(arama))
        {
            var a = arama.Trim().ToLowerInvariant();
            sorgu = sorgu.Where(t =>
                t.TalepNo.ToLowerInvariant().Contains(a) ||
                t.Santiye.ToLowerInvariant().Contains(a) ||
                t.TalepEden.ToLowerInvariant().Contains(a));
        }

        return Task.FromResult<IReadOnlyList<YonetimTalepOgesi>>(sorgu.ToList());
    }

    public static Task<YonetimTalepOgesi?> TalepGetirAsync(string id)
        => Task.FromResult(Talepler.FirstOrDefault(t => t.Id == id));

    public static Task<IReadOnlyList<YonetimTeklifOgesi>> TekliflerGetirAsync()
        => Task.FromResult<IReadOnlyList<YonetimTeklifOgesi>>(Teklifler);

    public static Task<YonetimTeklifOgesi?> TeklifGetirAsync(string id)
        => Task.FromResult(Teklifler.FirstOrDefault(t => t.Id == id));

    public static Task<IReadOnlyList<YonetimBildirimOgesi>> BildirimlerGetirAsync()
        => Task.FromResult<IReadOnlyList<YonetimBildirimOgesi>>(Bildirimler);

    public static int OkunmamisBildirimSayisi() => Bildirimler.Count(b => !b.Okundu);

    public static IReadOnlyList<YonetimFiltreOgesi> VarsayilanFiltreler() =>
    [
        new() { Anahtar = "tumu", Baslik = "Tümü", Secili = true },
        new() { Anahtar = "bekleyen", Baslik = "Bekleyen", Secili = false },
        new() { Anahtar = "acil", Baslik = "Acil", Secili = false },
        new() { Anahtar = "normal", Baslik = "Normal", Secili = false },
        new() { Anahtar = "onaylanan", Baslik = "Onaylanan", Secili = false },
        new() { Anahtar = "reddedilen", Baslik = "Reddedilen", Secili = false },
    ];

    private static YonetimTalepOgesi OlusturTalep(
        string id, string no, string santiye, string eden, string tarih,
        int kalem, string aciklama, YonetimTalepOncelik oncelik, YonetimTalepDurum durum)
    {
        var malzemeler = new List<YonetimMalzemeOgesi>();
        var ornekler = new[] { ("Çimento", "50", "Torba"), ("Demir Ø12", "2.5", "Ton"), ("Kum", "10", "m³"), ("Tuğla", "5000", "Adet"), ("Boya", "20", "Kova"), ("Kablo NYM 3x2.5", "200", "Metre"), ("Pano", "3", "Adet"), ("Boru PVC", "150", "Metre") };
        for (var i = 0; i < Math.Min(kalem, ornekler.Length); i++)
            malzemeler.Add(new YonetimMalzemeOgesi { Ad = ornekler[i].Item1, Miktar = ornekler[i].Item2, Birim = ornekler[i].Item3 });

        var fotograflar = oncelik == YonetimTalepOncelik.Acil ? new[] { "saha_foto_1.jpg", "saha_foto_2.jpg" } : Array.Empty<string>();
        var dosyalar = kalem > 4 ? new[] { "teknik_sartname.pdf" } : Array.Empty<string>();

        return new YonetimTalepOgesi
        {
            Id = id,
            TalepNo = no,
            Santiye = santiye,
            TalepEden = eden,
            TalepTarihi = tarih,
            MalzemeKalemSayisi = kalem,
            Aciklama = aciklama,
            Oncelik = oncelik,
            Durum = durum,
            Malzemeler = malzemeler,
            Fotograflar = fotograflar,
            Dosyalar = dosyalar,
        };
    }
}

namespace SatinalmaPro.Mobile.Models.Yonetim;

public enum YonetimSayfaDurumu
{
    Yukleniyor,
    Icerik,
    Bos,
    Hata
}

public enum YonetimTalepOncelik
{
    Acil,
    Normal
}

public enum YonetimTalepDurum
{
    Bekleyen,
    Onaylanan,
    Reddedilen,
    TeklifBekliyor
}

public sealed class YonetimDashboardOzet
{
    public int BekleyenTalepler { get; init; }
    public int TeklifBekleyenler { get; init; }
    public int BugunOnaylananlar { get; init; }
    public int Reddedilenler { get; init; }
}

public sealed class YonetimTalepOgesi
{
    public required string Id { get; init; }
    public required string TalepNo { get; init; }
    public required string Santiye { get; init; }
    public required string TalepEden { get; init; }
    public required string TalepTarihi { get; init; }
    public int MalzemeKalemSayisi { get; init; }
    public string? Aciklama { get; init; }
    public YonetimTalepOncelik Oncelik { get; init; }
    public YonetimTalepDurum Durum { get; init; }
    public IReadOnlyList<YonetimMalzemeOgesi> Malzemeler { get; init; } = [];
    public IReadOnlyList<string> Fotograflar { get; init; } = [];
    public IReadOnlyList<string> Dosyalar { get; init; } = [];

    public bool AcilMi => Oncelik == YonetimTalepOncelik.Acil;

    public string DurumMetni => Durum switch
    {
        YonetimTalepDurum.Bekleyen => "Bekleyen",
        YonetimTalepDurum.Onaylanan => "Onaylandı",
        YonetimTalepDurum.Reddedilen => "Reddedildi",
        YonetimTalepDurum.TeklifBekliyor => "Teklif Bekliyor",
        _ => "—"
    };

    public Color OncelikRengi => Oncelik == YonetimTalepOncelik.Acil
        ? Color.FromArgb("#DC2626")
        : Color.FromArgb("#EA580C");

    public Color OncelikArkaPlan => Oncelik == YonetimTalepOncelik.Acil
        ? Color.FromArgb("#FEE2E2")
        : Color.FromArgb("#FFEDD5");

    public string OncelikMetni => Oncelik == YonetimTalepOncelik.Acil ? "ACİL" : "NORMAL";

    public bool FotografVar => Fotograflar.Count > 0;
    public bool DosyaVar => Dosyalar.Count > 0;
}

public sealed class YonetimMalzemeOgesi
{
    public required string Ad { get; init; }
    public required string Miktar { get; init; }
    public required string Birim { get; init; }
}

public sealed class YonetimTeklifOgesi
{
    public required string Id { get; init; }
    public required string TalepNo { get; init; }
    public required string Santiye { get; init; }
    public required string MalzemeOzeti { get; init; }
    public int FirmaSayisi { get; init; }
    public int ToplamTeklifSayisi { get; init; }
    public IReadOnlyList<YonetimFirmaTeklifOgesi> Firmalar { get; init; } = [];
}

public sealed class YonetimFirmaTeklifOgesi
{
    public required string Id { get; init; }
    public required string FirmaAdi { get; init; }
    public required string ToplamTutar { get; init; }
    public required string TeslimSuresi { get; init; }
    public required string OdemeSekli { get; init; }
    public string? Aciklama { get; init; }
    public bool EnUygun { get; init; }
    public decimal TutarSayisal { get; init; }
}

public enum YonetimBildirimTipi
{
    YeniTalep,
    YeniTeklif,
    OnaylananTalep,
    ReddedilenTalep
}

public sealed class YonetimBildirimOgesi
{
    public required string Id { get; init; }
    public required YonetimBildirimTipi Tip { get; init; }
    public required string Baslik { get; init; }
    public required string Mesaj { get; init; }
    public required string Zaman { get; init; }
    public bool Okundu { get; init; }

    public string TipIkon => Tip switch
    {
        YonetimBildirimTipi.YeniTalep => "📥",
        YonetimBildirimTipi.YeniTeklif => "💼",
        YonetimBildirimTipi.OnaylananTalep => "✅",
        YonetimBildirimTipi.ReddedilenTalep => "🚫",
        _ => "🔔"
    };
}

public sealed class YonetimFiltreOgesi
{
    public required string Anahtar { get; init; }
    public required string Baslik { get; init; }
    public bool Secili { get; set; }
}

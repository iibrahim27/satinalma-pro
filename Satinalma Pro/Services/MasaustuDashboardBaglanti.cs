using System.Text.Json;
using System.Text.Json.Serialization;
using SatinalmaPro.Models;
using SatinalmaPro.Shared.Models;
using SatinalmaPro.Shared.Services;
using SharedStokKaydi = SatinalmaPro.Shared.Models.StokKaydi;
using SharedStokHareket = SatinalmaPro.Shared.Models.StokHareketKaydi;
using SharedTalep = SatinalmaPro.Shared.Models.SatinalmaTalep;

namespace SatinalmaPro.Services;

/// <summary>Masaüstü veri depolarını paylaşılan dashboard servisine bağlar.</summary>
public static class MasaustuDashboardBaglanti
{
    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly MasaustuDashboardSorgu Sorgu = new();

    public static DashboardOzet PanelOlustur()
    {
        var kullanici = OturumYoneticisi.AktifKullanici;
        var kaynak = VeriKaynagiOlustur();
        Sorgu.TalepleriGuncelle(kaynak.Talepler);

        return DashboardServisi.Olustur(
            kaynak,
            Sorgu,
            kullanici?.Rol,
            BildirimYoneticisi.OkunmamisSayisi);
    }

    public static DashboardVeriKaynagi VeriKaynagiOlustur()
    {
        var kullanici = OturumYoneticisi.AktifKullanici;
        return new DashboardVeriKaynagi
        {
            Talepler = TalepleriPaylasimaCevir(),
            Stok = StokPaylasimaCevir(),
            StokHareketleri = StokHareketleriPaylasimaCevir(),
            Uid = kullanici?.Uid ?? "",
            Ad = kullanici?.AdSoyad ?? ""
        };
    }

    private static IReadOnlyList<SharedTalep> TalepleriPaylasimaCevir() =>
        JsonSerializer.Deserialize<List<SharedTalep>>(
            JsonSerializer.Serialize(SatinalmaDepo.Talepler.ToList(), Json), Json) ?? [];

    private static IReadOnlyList<SharedStokKaydi> StokPaylasimaCevir() =>
        JsonSerializer.Deserialize<List<SharedStokKaydi>>(
            JsonSerializer.Serialize(ModulVeriDeposu.Stok.ToList(), Json), Json) ?? [];

    private static IReadOnlyList<SharedStokHareket> StokHareketleriPaylasimaCevir() =>
        JsonSerializer.Deserialize<List<SharedStokHareket>>(
            JsonSerializer.Serialize(ModulVeriDeposu.StokHareketleri.ToList(), Json), Json) ?? [];
}

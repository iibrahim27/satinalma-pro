using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Shared.SaaS;

namespace SatinalmaPro.Services;

public static class FinansmanVeriDeposu
{
    private static readonly JsonSerializerOptions JsonSecenekleri = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static ObservableCollection<FinansmanGelirKaydi> Gelirler { get; } = [];

    private static bool _yuklendi;
    private static bool _yukleniyor;
    private static bool _abonelikKuruldu;
    private static string? _yuklenenTenantId;

    private static string YerelYol()
    {
        var tid = KiracıOturumu.TenantId;
        var ad = string.IsNullOrWhiteSpace(tid)
            ? "finansman_gelir.json"
            : $"finansman_gelir_{tid}.json";
        return SatinalmaProKlasor.DosyaYolu(ad);
    }

    /// <summary>Firma değişiminde finansman bellek verisini temizler.</summary>
    public static void KiraciDegisti()
    {
        _yuklendi = false;
        _yuklenenTenantId = null;
        _yukleniyor = true;
        try
        {
            Gelirler.Clear();
        }
        finally
        {
            _yukleniyor = false;
        }
    }

    public static void Yukle()
    {
        var tid = KiracıOturumu.TenantId;
        if (_yuklendi && string.Equals(_yuklenenTenantId, tid, StringComparison.Ordinal))
            return;

        if (_yuklendi && !string.Equals(_yuklenenTenantId, tid, StringComparison.Ordinal))
            KiraciDegisti();

        _yuklendi = true;
        _yuklenenTenantId = tid;
        _yukleniyor = true;

        SatinalmaProKlasor.Olustur();
        var yol = YerelYol();

        if (File.Exists(yol))
        {
            try
            {
                var json = File.ReadAllText(yol);
                var liste = JsonSerializer.Deserialize<List<FinansmanGelirKaydi>>(json, JsonSecenekleri) ?? [];
                ErtelenmisKayit.BeginBatch();
                try
                {
                    foreach (var kayit in liste)
                        Gelirler.Add(kayit);
                }
                finally
                {
                    ErtelenmisKayit.EndBatch();
                }
            }
            catch
            {
                // boş başla
            }
        }
        else if (!OturumYoneticisi.BulutAktif)
            OrnekVeri();

        _yukleniyor = false;
        if (!_abonelikKuruldu)
        {
            _abonelikKuruldu = true;
            Gelirler.CollectionChanged += (_, _) =>
            {
                if (!_yukleniyor)
                {
                    ErtelenmisKayit.Planla("finansman", Kaydet);
                    BulutVeriSenkronu.Planla("finansman");
                }
            };
        }
    }

    public static void GelirleriYukle(string json)
    {
        _yukleniyor = true;
        try
        {
            Gelirler.Clear();
            var liste = JsonSerializer.Deserialize<List<FinansmanGelirKaydi>>(json, JsonSecenekleri) ?? [];
            foreach (var kayit in liste)
                Gelirler.Add(kayit);
        }
        finally
        {
            _yukleniyor = false;
        }
    }

    public static void Kaydet()
    {
        SatinalmaProKlasor.Olustur();
        var json = JsonSerializer.Serialize(Gelirler.ToList(), JsonSecenekleri);
        File.WriteAllText(YerelYol(), json);
    }

    public static void Sifirla()
    {
        _yukleniyor = true;
        try
        {
            Gelirler.Clear();
            Kaydet();
        }
        finally
        {
            _yukleniyor = false;
        }
    }

    public static void YenidenYukle()
    {
        KiraciDegisti();
        Yukle();
    }

    private static void OrnekVeri()
    {
        Gelirler.Add(new FinansmanGelirKaydi
        {
            Tarih = "15.06.2026",
            BelgeNo = "HAK-2026-06",
            Kategori = "Hakediş",
            Aciklama = "Haziran ayı hakediş ödemesi",
            Kaynak = "İşveren",
            Saha = "Merkez Şantiye",
            Tutar = 2_500_000m,
            OdemeSekli = "Havale"
        });
        Kaydet();
    }
}

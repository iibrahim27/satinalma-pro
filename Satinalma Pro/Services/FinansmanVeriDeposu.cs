using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;

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

    public static void Yukle()
    {
        if (_yuklendi) return;
        _yuklendi = true;
        _yukleniyor = true;

        SatinalmaProKlasor.Olustur();
        var yol = SatinalmaProKlasor.DosyaYolu("finansman_gelir.json");

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
        Gelirler.CollectionChanged += (_, _) =>
        {
            if (!_yukleniyor)
            {
                ErtelenmisKayit.Planla("finansman", Kaydet);
                BulutVeriSenkronu.Planla("finansman");
            }
        };
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

    public static void Kaydet() =>
        JsonYaz("finansman_gelir.json", Gelirler.ToList());

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
        _yuklendi = false;
        Gelirler.Clear();
        Yukle();
    }

    private static void JsonYaz(string dosyaAdi, List<FinansmanGelirKaydi> liste)
    {
        SatinalmaProKlasor.Olustur();
        var json = JsonSerializer.Serialize(liste, JsonSecenekleri);
        File.WriteAllText(SatinalmaProKlasor.DosyaYolu(dosyaAdi), json);
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

using System.IO;
using System.Text.Json;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Shared.Helpers;

namespace SatinalmaPro.Services;

/// <summary>Masaüstü toast hatırlatmalarını saatlik aralıkla sınırlar.</summary>
public static class BildirimHatirlatmaDeposu
{
    public static readonly TimeSpan HatirlatmaAraligi = TimeSpan.FromHours(1);

    private static readonly Dictionary<string, long> _sonGosterim = new(StringComparer.Ordinal);
    private static bool _yuklendi;

    public static bool GosterilebilirMi(BildirimKaydi bildirim, bool ilkGosterim = false)
    {
        if (bildirim.Okundu)
            return false;

        Yukle();
        var anahtar = BildirimMantikAnahtari.Olustur(ToShared(bildirim));
        if (ilkGosterim)
            return true;

        if (!_sonGosterim.TryGetValue(anahtar, out var sonUtc))
            return true;

        var gecen = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - sonUtc;
        return gecen >= (long)HatirlatmaAraligi.TotalMilliseconds;
    }

    public static void Gosterildi(BildirimKaydi bildirim)
    {
        Yukle();
        var anahtar = BildirimMantikAnahtari.Olustur(ToShared(bildirim));
        _sonGosterim[anahtar] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        Kaydet();
    }

    public static void Temizle(BildirimKaydi bildirim)
    {
        Yukle();
        var anahtar = BildirimMantikAnahtari.Olustur(ToShared(bildirim));
        if (_sonGosterim.Remove(anahtar))
            Kaydet();
    }

    private static string DosyaYolu
    {
        get
        {
            var tid = SatinalmaPro.Shared.SaaS.KiracıOturumu.TenantId;
            var ad = string.IsNullOrWhiteSpace(tid)
                ? "bildirim_hatirlatma.json"
                : $"bildirim_hatirlatma_{tid}.json";
            return SatinalmaProKlasor.DosyaYolu(ad);
        }
    }

    /// <summary>Firma değişiminde / çıkışta bellek sızıntısını önler.</summary>
    public static void KiraciDegisti()
    {
        _sonGosterim.Clear();
        _yuklendi = false;
    }

    private static void Yukle()
    {
        if (_yuklendi)
            return;

        _yuklendi = true;
        _sonGosterim.Clear();

        var yol = DosyaYolu;
        if (!File.Exists(yol))
            return;

        try
        {
            var json = File.ReadAllText(yol);
            var veri = JsonSerializer.Deserialize<Dictionary<string, long>>(json);
            if (veri is null)
                return;

            foreach (var (k, v) in veri)
                _sonGosterim[k] = v;
        }
        catch
        {
            _sonGosterim.Clear();
        }
    }

    private static void Kaydet()
    {
        try
        {
            SatinalmaProKlasor.Olustur();
            var json = JsonSerializer.Serialize(_sonGosterim);
            File.WriteAllText(DosyaYolu, json);
        }
        catch
        {
            // hatırlatma kaydı isteğe bağlı
        }
    }

    private static SatinalmaPro.Shared.Models.BildirimKaydi ToShared(BildirimKaydi b) => new()
    {
        Id = b.Id,
        Tip = b.Tip,
        TalepId = b.TalepId,
        HedefRol = b.HedefRol,
        HedefUid = b.HedefUid
    };
}

using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;

namespace SatinalmaPro.Views.Modules.Satinalma.Part1;

public sealed class TalepListeSatiriPart1 : INotifyPropertyChanged
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
    private bool _acik;

    public TalepListeSatiriPart1(SatinalmaTalep talep)
    {
        Talep = talep;
        talep.Kalemler ??= [];
        talep.Teklifler ??= [];
        foreach (var t in talep.Teklifler)
            t.FiyatlariHesapla(talep.Kalemler);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public SatinalmaTalep Talep { get; }

    public string TalepNo => string.IsNullOrWhiteSpace(Talep.TalepNo) ? "—" : Talep.TalepNo;
    public string Tarih => Talep.Tarih;
    public string TalepEden => Talep.TalepEden;
    public string Oncelik => TalepTurleri.GorunenAd(Talep.TalepTuru);
    public string TalepDurumu => SatinalmaPart1DurumEtiketi.TalepDurumu(Talep);
    public string TeklifDurumu => SatinalmaPart1DurumEtiketi.TeklifDurumu(Talep);

    public string SonIslem
    {
        get
        {
            if (Talep.GuncellemeUtc > 0)
            {
                var dt = DateTimeOffset.FromUnixTimeMilliseconds(Talep.GuncellemeUtc).ToLocalTime();
                return dt.ToString("dd.MM.yyyy HH:mm", Tr);
            }

            return string.IsNullOrWhiteSpace(Talep.Tarih) ? "—" : Talep.Tarih;
        }
    }

    public int KalemSayisi => Talep.Kalemler?.Count(k => !string.IsNullOrWhiteSpace(k.Malzeme)) ?? 0;
    public string KalemOzet => $"{KalemSayisi} kalem";

    public decimal TahminiTutar
    {
        get
        {
            var oneri = Talep.OnerilenTeklif() ?? Talep.EnDusukFiyatliTeklif();
            return oneri is not null ? oneri.GenelToplam : 0m;
        }
    }

    public string TahminiTutarMetin =>
        TahminiTutar > 0 ? TahminiTutar.ToString("C0", Tr) : "—";

    public bool AcilMi =>
        string.Equals(Talep.TalepTuru, TalepTurleri.Acil, StringComparison.OrdinalIgnoreCase)
        || string.Equals(Talep.Priority, "urgent", StringComparison.OrdinalIgnoreCase);

    public bool YuksekMi =>
        string.Equals(Talep.TalepTuru, TalepTurleri.Oncelikli, StringComparison.OrdinalIgnoreCase);

    public bool RevizeBekliyorMu =>
        !string.IsNullOrWhiteSpace(Talep.TeklifDuzeltmeNotu)
        || (Talep.Durum == SatinalmaTalepDurumlari.Karsilastirma
            && TeklifDurumu.Contains("Yeniden", StringComparison.OrdinalIgnoreCase));

    public bool BugunMu
    {
        get
        {
            var bugun = DateTime.Today;
            if (Talep.GuncellemeUtc > 0)
            {
                var dt = DateTimeOffset.FromUnixTimeMilliseconds(Talep.GuncellemeUtc).LocalDateTime.Date;
                if (dt == bugun) return true;
            }

            return DateTime.TryParse(Talep.Tarih, Tr, DateTimeStyles.None, out var t) && t.Date == bugun;
        }
    }

    public bool BuHaftaMi
    {
        get
        {
            DateTime? d = null;
            if (Talep.GuncellemeUtc > 0)
                d = DateTimeOffset.FromUnixTimeMilliseconds(Talep.GuncellemeUtc).LocalDateTime.Date;
            else if (DateTime.TryParse(Talep.Tarih, Tr, DateTimeStyles.None, out var t))
                d = t.Date;
            if (d is null) return false;
            var diff = (DateTime.Today - d.Value).TotalDays;
            return diff is >= 0 and < 7;
        }
    }

    public bool Acik
    {
        get => _acik;
        set
        {
            if (_acik == value) return;
            _acik = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DetayGorunur));
            OnPropertyChanged(nameof(OkIkon));
            OnPropertyChanged(nameof(SatirArka));
        }
    }

    public Visibility DetayGorunur => Acik ? Visibility.Visible : Visibility.Collapsed;
    public string OkIkon => Acik ? "\uE70E" : "\uE70D";
    public Brush SatirArka => Acik ? Hex("#E8F7F7") : Brushes.White;

    public Brush OncelikArka => AcilMi ? Hex("#FEE2E2") : YuksekMi ? Hex("#FFEDD5") : Hex("#F1F5F9");
    public Brush OncelikYazi => AcilMi ? Hex("#B91C1C") : YuksekMi ? Hex("#C2410C") : Hex("#475569");
    public Brush OncelikKenar => AcilMi ? Hex("#FECACA") : YuksekMi ? Hex("#FED7AA") : Hex("#E2E8F0");

    public Brush TalepDurumArka =>
        TalepDurumu.Contains("Revize", StringComparison.OrdinalIgnoreCase) ? Hex("#EDE9FE")
        : TalepDurumu.Contains("Onay", StringComparison.OrdinalIgnoreCase) ? Hex("#DBEAFE")
        : Hex("#F1F5F9");

    public Brush TalepDurumYazi =>
        TalepDurumu.Contains("Revize", StringComparison.OrdinalIgnoreCase) ? Hex("#6D28D9")
        : TalepDurumu.Contains("Onay", StringComparison.OrdinalIgnoreCase) ? Hex("#1D4ED8")
        : Hex("#475569");

    public Brush TalepDurumKenar =>
        TalepDurumu.Contains("Revize", StringComparison.OrdinalIgnoreCase) ? Hex("#DDD6FE")
        : TalepDurumu.Contains("Onay", StringComparison.OrdinalIgnoreCase) ? Hex("#BFDBFE")
        : Hex("#E2E8F0");

    public Brush TeklifDurumArka
    {
        get
        {
            var t = TeklifDurumu;
            if (ContainsAny(t, "karşılaştır", "onaylandı")) return Hex("#DCFCE7");
            if (ContainsAny(t, "güncell", "değerlendirme")) return Hex("#DBEAFE");
            if (ContainsAny(t, "beklen", "Yeniden")) return Hex("#FFEDD5");
            return Hex("#F1F5F9");
        }
    }

    public Brush TeklifDurumYazi
    {
        get
        {
            var t = TeklifDurumu;
            if (ContainsAny(t, "karşılaştır", "onaylandı")) return Hex("#166534");
            if (ContainsAny(t, "güncell", "değerlendirme")) return Hex("#1D4ED8");
            if (ContainsAny(t, "beklen", "Yeniden")) return Hex("#C2410C");
            return Hex("#64748B");
        }
    }

    public Brush TeklifDurumKenar
    {
        get
        {
            var t = TeklifDurumu;
            if (ContainsAny(t, "karşılaştır", "onaylandı")) return Hex("#BBF7D0");
            if (ContainsAny(t, "güncell", "değerlendirme")) return Hex("#BFDBFE");
            if (ContainsAny(t, "beklen", "Yeniden")) return Hex("#FED7AA");
            return Hex("#E2E8F0");
        }
    }

    private static bool ContainsAny(string text, params string[] parts) =>
        parts.Any(p => text.Contains(p, StringComparison.OrdinalIgnoreCase));

    private static Brush Hex(string hex) =>
        (Brush)new BrushConverter().ConvertFromString(hex)!;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

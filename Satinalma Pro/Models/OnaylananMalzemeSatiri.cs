using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SatinalmaPro.Models;

public class OnaylananMalzemeSatiri : INotifyPropertyChanged
{
    private double _kabulEdilenMiktar;
    private bool _siparisTamamlandi;

    public Guid TalepId { get; set; }
    public Guid KalemId { get; set; }
    public Guid TeklifId { get; set; }
    public string TalepNo { get; set; } = "";
    public string SiparisNo { get; set; } = "";
    public string Tarih { get; set; } = "";
    public string Durum { get; set; } = "";
    public string Firma { get; set; } = "";
    public string Marka { get; set; } = "";
    public string Malzeme { get; set; } = "";
    public double SiparisMiktari { get; set; }
    public string Birim { get; set; } = "";
    public decimal BirimFiyati { get; set; }
    public decimal ToplamTutar { get; set; }
    public int VadeGunu { get; set; }
    public string KalemAciklamasi { get; set; } = "";

    public double KabulEdilenMiktar
    {
        get => _kabulEdilenMiktar;
        set
        {
            if (Math.Abs(_kabulEdilenMiktar - value) < 0.0001) return;
            _kabulEdilenMiktar = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(KalanMiktar));
            OnPropertyChanged(nameof(KabulDurumu));
        }
    }

    public bool SiparisTamamlandi
    {
        get => _siparisTamamlandi;
        set
        {
            if (_siparisTamamlandi == value) return;
            _siparisTamamlandi = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(KabulDurumu));
        }
    }

    public double Miktar => SiparisMiktari;

    public double KalanMiktar => Math.Max(0, SiparisMiktari - KabulEdilenMiktar);

    public string KabulDurumu => SiparisTamamlandi || KabulEdilenMiktar >= SiparisMiktari
        ? "Tamamlandı"
        : KabulEdilenMiktar > 0
            ? "Kısmi"
            : "Bekliyor";

    public decimal KabulToplamTutar =>
        SiparisMiktari <= 0 ? 0 : Math.Round(ToplamTutar * (decimal)(KabulEdilenMiktar / SiparisMiktari), 2);

    public AlinanMalzemeKaydi AlinanMalzemeKaydinaDonustur(
        double? miktar = null,
        string? kategori = null,
        string? tarih = null,
        string? fisNo = null,
        string? teslimAlan = null,
        string? indirildigiSaha = null,
        string? aciklama = null) => new()
    {
        Tarih = string.IsNullOrWhiteSpace(tarih) ? Tarih : tarih.Trim(),
        FaturaNo = string.IsNullOrWhiteSpace(fisNo)
            ? (string.IsNullOrWhiteSpace(SiparisNo) ? TalepNo : SiparisNo)
            : fisNo.Trim(),
        Kategori = string.IsNullOrWhiteSpace(kategori) ? "Malzeme" : kategori.Trim(),
        MalzemeHizmet = Malzeme,
        Miktar = miktar ?? KabulEdilenMiktar,
        Birim = Birim,
        BirimFiyati = BirimFiyati,
        ToplamTutar = miktar.HasValue
            ? Math.Round(BirimFiyati * (decimal)miktar.Value, 2)
            : KabulToplamTutar,
        Tedarikci = Firma,
        IndirildigiSaha = indirildigiSaha?.Trim() ?? "",
        TeslimAlan = teslimAlan?.Trim() ?? "",
        Aciklama = string.IsNullOrWhiteSpace(aciklama)
            ? $"Satınalma: {TalepNo} — {Marka} — {KalemAciklamasi}".Trim(' ', '—', ' ')
            : aciklama.Trim(),
        SatinalmaTalepId = TalepId,
        SatinalmaKalemId = KalemId
    };

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

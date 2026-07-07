using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using SatinalmaPro.Helpers;

namespace SatinalmaPro.Models;

public sealed class KalemOneriSatiri : INotifyPropertyChanged
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
    private Guid? _onerilenTeklifId;

    public KalemOneriSatiri(SatinalmaTalepKalemi kalem, SatinalmaTalep talep)
    {
        Kalem = kalem;
        Talep = talep;
        Firmalar =
        [
            new FirmaSecenegi(null, "— Seçiniz —"),
            .. (talep.Teklifler ?? [])
                .Where(t => t is not null)
                .Select(t => new FirmaSecenegi(t.Id, FirmaEtiketi(t, kalem)))
        ];
        _onerilenTeklifId = kalem.OnerilenTeklifId;
    }

    public SatinalmaTalepKalemi Kalem { get; }
    public SatinalmaTalep Talep { get; }

    public Action? Degisti { get; init; }

    public string Malzeme => Kalem?.Malzeme ?? "";
    public double Miktar => Kalem?.Miktar ?? 0;
    public string Birim => Kalem?.Birim ?? "";

    public List<FirmaSecenegi> Firmalar { get; }

    public Guid? OnerilenTeklifId
    {
        get => _onerilenTeklifId;
        set
        {
            if (_onerilenTeklifId == value)
                return;

            _onerilenTeklifId = value;
            if (Kalem is not null)
                Kalem.OnerilenTeklifId = value;

            SatinalmaOneriYardimcisi.KalemOnerisiGuncelle(Talep);
            Degisti?.Invoke();
            OnPropertyChanged();
            OnPropertyChanged(nameof(OneriDurumu));
            OnPropertyChanged(nameof(OneriToplamMetni));
        }
    }

    public string OneriDurumu => OnerilenTeklifId is null
        ? "Seçilmedi"
        : Firmalar.FirstOrDefault(f => f.Id == OnerilenTeklifId)?.Ad ?? "Seçildi";

    public string OneriToplamMetni
    {
        get
        {
            if (OnerilenTeklifId is null)
                return "—";

            var fiyat = SatinalmaOneriYardimcisi.KalemOneriFiyati(Talep, Kalem);
            return fiyat is null
                ? "—"
                : $"{fiyat.ToplamKdvDahil.ToString("N2", Tr)} ₺";
        }
    }

    private string FirmaEtiketi(SatinalmaTeklif teklif, SatinalmaTalepKalemi kalem)
    {
        var ad = string.IsNullOrWhiteSpace(teklif.FirmaAdi) ? "Firma" : teklif.FirmaAdi;
        var fiyat = teklif.Fiyatlar.FirstOrDefault(f => f.KalemId == kalem.Id);
        if (fiyat is null || fiyat.BirimFiyat <= 0)
            return ad;

        return $"{ad} — {fiyat.BirimFiyatGosterim(teklif.UsdKuru, teklif.EurKuru)}";
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

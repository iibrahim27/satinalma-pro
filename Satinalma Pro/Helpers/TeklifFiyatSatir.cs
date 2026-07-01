using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using SatinalmaPro.Models;

namespace SatinalmaPro.Helpers;

public class TeklifFiyatSatir : INotifyPropertyChanged
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    public Guid KalemId { get; init; }
    public string Malzeme { get; init; } = "";
    public double Miktar { get; init; }
    public string Birim { get; init; } = "";

    private string _marka = "";
    private string _paraBirimi = ParaBirimleri.Try;
    private decimal _birimFiyat;
    private string _birimFiyatMetni = "";
    private double _kdvOrani = 20;
    private string _kdvOraniMetni = "";
    private string _satirToplamMetni = "0,00 ₺";

    public string Marka
    {
        get => _marka;
        set { _marka = value ?? ""; OnChanged(); }
    }

    public string ParaBirimi
    {
        get => _paraBirimi;
        set
        {
            var yeni = string.IsNullOrWhiteSpace(value) ? ParaBirimleri.Try : value.Trim().ToUpperInvariant();
            if (_paraBirimi == yeni)
                return;
            _paraBirimi = yeni;
            OnChanged();
        }
    }

    public decimal BirimFiyat
    {
        get => _birimFiyat;
        set
        {
            _birimFiyat = value;
            _birimFiyatMetni = value > 0 ? SayiMetniYardimcisi.OndalikGoster(value) : "";
            OnChanged(nameof(BirimFiyatMetni));
        }
    }

    public string BirimFiyatMetni
    {
        get => _birimFiyatMetni;
        set
        {
            _birimFiyatMetni = value ?? "";
            if (SayiMetniYardimcisi.OndalikOku(_birimFiyatMetni, out var sonuc))
                _birimFiyat = sonuc;
            OnChanged();
        }
    }

    public double KdvOrani
    {
        get => _kdvOrani;
        set
        {
            _kdvOrani = value;
            _kdvOraniMetni = SayiMetniYardimcisi.CiftGoster(value);
            OnChanged(nameof(KdvOraniMetni));
        }
    }

    public string KdvOraniMetni
    {
        get => _kdvOraniMetni;
        set
        {
            _kdvOraniMetni = value ?? "";
            if (SayiMetniYardimcisi.CiftOku(_kdvOraniMetni, out var sonuc))
                _kdvOrani = sonuc;
            OnChanged();
        }
    }

    public decimal UsdKuru { get; set; }
    public decimal EurKuru { get; set; }

    public string MiktarMetni => $"{Miktar:G} {Birim}";

    public string SatirToplamMetni
    {
        get => _satirToplamMetni;
        private set
        {
            if (_satirToplamMetni == value)
                return;
            _satirToplamMetni = value;
            OnChanged();
        }
    }

    public decimal SatirToplamTl
    {
        get
        {
            var fiyat = new SatinalmaTeklifFiyati
            {
                KalemId = KalemId,
                BirimFiyat = BirimFiyat,
                ParaBirimi = ParaBirimi,
                KdvOrani = KdvOrani
            };
            fiyat.Hesapla(Miktar, UsdKuru, EurKuru);
            return fiyat.ToplamKdvDahil;
        }
    }

    public void KurlariGuncelle(decimal usd, decimal eur)
    {
        UsdKuru = usd;
        EurKuru = eur;
    }

    public void GuncelleSatirToplam() =>
        SatirToplamMetni = SatirToplamTl.ToString("N2", Tr) + " ₺";

    public void MetinleriBaslat(decimal birimFiyat, double kdvOrani)
    {
        _birimFiyat = birimFiyat;
        _birimFiyatMetni = birimFiyat > 0 ? SayiMetniYardimcisi.OndalikGoster(birimFiyat) : "";
        _kdvOrani = kdvOrani;
        _kdvOraniMetni = SayiMetniYardimcisi.CiftGoster(kdvOrani);
        GuncelleSatirToplam();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

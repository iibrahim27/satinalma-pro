using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace SatinalmaPro.Models;

public sealed class TeklifsizFirmaFiyatSatiri : INotifyPropertyChanged
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
    private string _firmaAdi = "";
    private string _birimFiyatMetin = "";

    public TeklifsizFirmaFiyatSatiri(SatinalmaTalepKalemi kalem) => Kalem = kalem;

    public SatinalmaTalepKalemi Kalem { get; }

    public string Malzeme => Kalem.Malzeme;
    public double Miktar => Kalem.Miktar;
    public string Birim => Kalem.Birim;

    public string FirmaAdi
    {
        get => _firmaAdi;
        set { _firmaAdi = value?.Trim() ?? ""; OnPropertyChanged(); }
    }

    public string BirimFiyatMetin
    {
        get => _birimFiyatMetin;
        set { _birimFiyatMetin = value?.Trim() ?? ""; OnPropertyChanged(); }
    }

    public bool GecerliMi(out decimal birimFiyat)
    {
        birimFiyat = 0;
        if (string.IsNullOrWhiteSpace(FirmaAdi))
            return false;

        if (string.IsNullOrWhiteSpace(BirimFiyatMetin))
            return false;

        return decimal.TryParse(BirimFiyatMetin, NumberStyles.Any, Tr, out birimFiyat) ||
               decimal.TryParse(BirimFiyatMetin.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out birimFiyat);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

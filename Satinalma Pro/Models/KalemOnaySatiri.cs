using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SatinalmaPro.Models;

public sealed class KalemOnaySatiri : INotifyPropertyChanged
{
    private Guid? _onaylananTeklifId;

    public         KalemOnaySatiri(SatinalmaTalepKalemi kalem, IEnumerable<SatinalmaTeklif> teklifler)
    {
        Kalem = kalem;
        Firmalar =
        [
            new FirmaSecenegi(null, "— Seçiniz —"),
            .. teklifler.Where(t => t is not null)
                .Select(t => new FirmaSecenegi(t.Id, string.IsNullOrWhiteSpace(t.FirmaAdi) ? "Firma" : t.FirmaAdi))
        ];
        _onaylananTeklifId = kalem.OnaylananTeklifId;
    }

    public SatinalmaTalepKalemi Kalem { get; }

    public string Malzeme => Kalem?.Malzeme ?? "";
    public double Miktar => Kalem?.Miktar ?? 0;
    public string Birim => Kalem?.Birim ?? "";

    public List<FirmaSecenegi> Firmalar { get; }

    public Guid? OnaylananTeklifId
    {
        get => _onaylananTeklifId;
        set
        {
            if (_onaylananTeklifId == value) return;
            _onaylananTeklifId = value;
            if (Kalem is not null)
                Kalem.OnaylananTeklifId = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(OnayDurumu));
        }
    }

    public string OnayDurumu => OnaylananTeklifId == null
        ? "Bekliyor"
        : Firmalar.FirstOrDefault(f => f.Id == OnaylananTeklifId)?.Ad ?? "Onaylı";

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public sealed record FirmaSecenegi(Guid? Id, string Ad);

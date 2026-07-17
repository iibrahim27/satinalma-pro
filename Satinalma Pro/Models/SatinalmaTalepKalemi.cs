using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SatinalmaPro.Models;

public class SatinalmaTalepKalemi : INotifyPropertyChanged
{
    private string _malzeme = "";
    private double _miktar;
    private string _birim = "Adet";
    private string _aciklama = "";

    public Guid Id { get; set; } = Guid.NewGuid();
    public int SiraNo { get; set; }

    public string Malzeme
    {
        get => _malzeme;
        set { _malzeme = value; OnPropertyChanged(); }
    }

    public double Miktar
    {
        get => _miktar;
        set
        {
            if (Math.Abs(_miktar - value) < 0.0001) return;
            _miktar = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(MiktarGosterim));
        }
    }

    public string Birim
    {
        get => _birim;
        set { _birim = value; OnPropertyChanged(); }
    }

    public string Aciklama
    {
        get => _aciklama;
        set { _aciklama = value; OnPropertyChanged(); }
    }

  public Guid? OnaylananTeklifId { get; set; }
    /// <summary>Satınalma önerisi — yönetime gönderilmeden önce kalem bazlı firma/fiyat seçimi.</summary>
    public Guid? OnerilenTeklifId { get; set; }
    /// <summary>Yönetim onayında miktarın firmalara bölünmesi. Boşsa OnaylananTeklifId tek firma demektir.</summary>
    public List<KalemFirmaAtamasi> FirmaAtamalari { get; set; } = [];
    public double KabulEdilenMiktar { get; set; }
    public bool SiparisTamamlandi { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public double KalanMiktar => Math.Max(0, Miktar - KabulEdilenMiktar);

    [System.Text.Json.Serialization.JsonIgnore]
    public string MiktarGosterim => Miktar.ToString("G", System.Globalization.CultureInfo.CurrentCulture);

    [System.Text.Json.Serialization.JsonIgnore]
    public string KabulDurumu => SiparisTamamlandi || KabulEdilenMiktar >= Miktar
        ? "Tamamlandı"
        : KabulEdilenMiktar > 0
            ? "Kısmi"
            : "Bekliyor";

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

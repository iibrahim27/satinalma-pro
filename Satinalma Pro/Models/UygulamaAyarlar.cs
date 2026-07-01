namespace SatinalmaPro.Models;

public class UygulamaAyarlar
{
    public string FirmaAdi { get; set; } = "";
    /// <summary>PDF ve belgelerde kullanılan firma logosu (göreli yol: logos/...).</summary>
    public string LogoDosyaYolu { get; set; } = "";
    /// <summary>Ana panelde modüllerin üstünde gösterilen logo (göreli yol: logos/...).</summary>
    public string AnasayfaLogoDosyaYolu { get; set; } = "";
    /// <summary>Alınan malzemeler modülünde kullanılan kategori listesi.</summary>
    public List<string> MalzemeKategorileri { get; set; } = [];
    /// <summary>Stok ve malzeme kayıtlarında kullanılan birim terimleri.</summary>
    public List<string> MalzemeBirimleri { get; set; } = [];
    /// <summary>Araç zimmet formunda manuel maddeler (her satır bir madde).</summary>
    public List<string> FiloZimmetFormMaddeleri { get; set; } = [];
}

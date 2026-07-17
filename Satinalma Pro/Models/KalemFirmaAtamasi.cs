namespace SatinalmaPro.Models;

/// <summary>Kalem miktarının bir firmaya ayrılan kısmı (yönetim bölünmüş onayı).</summary>
public class KalemFirmaAtamasi
{
    public Guid TeklifId { get; set; }
    public double Miktar { get; set; }
    public double KabulEdilenMiktar { get; set; }
    public bool SiparisTamamlandi { get; set; }
}

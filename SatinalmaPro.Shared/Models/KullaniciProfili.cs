namespace SatinalmaPro.Shared.Models;

public class KullaniciProfili
{
    public string Uid { get; set; } = "";
    public string Eposta { get; set; } = "";
    public string AdSoyad { get; set; } = "";
    public string Rol { get; set; } = KullaniciRolleri.Saha;
    public bool Aktif { get; set; } = true;
    public string? Saha { get; set; }
    public string? FcmToken { get; set; }
    public List<string> Moduller { get; set; } = [];
}

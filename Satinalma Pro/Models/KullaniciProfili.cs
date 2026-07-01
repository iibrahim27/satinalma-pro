namespace SatinalmaPro.Models;

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
    public List<ModulYetkiKaydi> ModulYetkileri { get; set; } = [];

    public string ModulOzeti
    {
        get
        {
            if (ModulYetkileri.Count > 0)
            {
                var okuma = ModulYetkileri.Count(y => y.Okuma);
                var yazma = ModulYetkileri.Count(y => y.Yazma);
                return $"{okuma} okuma, {yazma} yazma";
            }

            return Moduller.Count > 0 ? $"{Moduller.Count} modül seçili" : "Rol varsayılanı";
        }
    }
}

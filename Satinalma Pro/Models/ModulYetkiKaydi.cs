namespace SatinalmaPro.Models;

public class ModulYetkiKaydi
{
    public string Modul { get; set; } = "";
    public bool Okuma { get; set; }
    public bool Yazma { get; set; }
    public List<string> Sekmeler { get; set; } = [];
}

namespace SatinalmaPro.Models;

public class FiloVeriPaketi
{
    public List<FiloAracKaydi> Araclar { get; set; } = [];
    public List<FiloGiderKaydi> Giderler { get; set; } = [];
    public List<FiloZimmetKaydi> Zimmetler { get; set; } = [];
}

namespace SatinalmaPro.Models;

public class StokSecimOgesi
{
    public StokKaydi Kayit { get; }

    public StokSecimOgesi(StokKaydi kayit) => Kayit = kayit;

    public string SecimMetni =>
        $"{Kayit.MalzemeAdi} — {Kayit.DepoSaha} ({Kayit.MevcutMiktar:N2} {Kayit.Birim})";
}

using System.Globalization;
using SatinalmaPro.Models;

namespace SatinalmaPro.Views.Modules.Satinalma.Part1;

public sealed class OnaylananTeklifGecmisSatiri
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
    private readonly SatinalmaTeklif _teklif;

    public OnaylananTeklifGecmisSatiri(SatinalmaTeklif teklif) => _teklif = teklif;

    public string DurumMetni => _teklif.Onaylandi ? "Onaylı" : "—";
    public string FirmaAdi => string.IsNullOrWhiteSpace(_teklif.FirmaAdi) ? "—" : _teklif.FirmaAdi;
    public string MarkaOzeti => _teklif.MarkaOzeti;
    public string VadeGosterim => _teklif.VadeGunu > 0 ? $"{_teklif.VadeGunu} gün" : "—";
    public string TeslimSuresi => string.IsNullOrWhiteSpace(_teklif.TeslimSuresi) ? "—" : _teklif.TeslimSuresi;
    public string OdemeSekli => string.IsNullOrWhiteSpace(_teklif.OdemeSekli) ? "—" : _teklif.OdemeSekli;
    public string KdvHaricMetni => TutarMetni(_teklif.AraToplam);
    public string KdvDahilMetni => TutarMetni(_teklif.GenelToplam);

    private static string TutarMetni(decimal tutar) =>
        tutar.ToString("N2", Tr) + " ₺";
}

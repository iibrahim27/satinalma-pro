using System.Globalization;
using SatinalmaPro.Models;

namespace SatinalmaPro.Helpers;

public class TeklifGirisSatiri
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    private readonly SatinalmaTalep _talep;
    private readonly SatinalmaTeklif _teklif;

    public TeklifGirisSatiri(SatinalmaTalep talep, SatinalmaTeklif teklif)
    {
        _talep = talep;
        _teklif = teklif;
    }

    public SatinalmaTeklif Teklif => _teklif;

    public bool OneriMi =>
        _talep.SatinalmaOnerisiElleSecildi
        && _talep.YonetimOnerilenTeklifId == _teklif.Id;

    public string OneriMetni => OneriMi ? "★" : "";

    public string FirmaAdi => _teklif.FirmaAdi;
    public string VadeGosterim => _teklif.VadeGunu > 0 ? $"{_teklif.VadeGunu} gün" : "—";
    public string TeslimSuresi => string.IsNullOrWhiteSpace(_teklif.TeslimSuresi) ? "—" : _teklif.TeslimSuresi;
    public string OdemeSekli => string.IsNullOrWhiteSpace(_teklif.OdemeSekli) ? "—" : _teklif.OdemeSekli;
    public string MarkaOzeti => _teklif.MarkaOzeti;

    public string KdvHaricMetni => TutarMetni(_teklif.AraToplam);
    public string KdvTutarMetni => TutarMetni(_teklif.KdvTutari);
    public string KdvDahilMetni => TutarMetni(_teklif.GenelToplam);

    private static string TutarMetni(decimal tutar) =>
        tutar.ToString("N2", Tr) + " ₺";
}
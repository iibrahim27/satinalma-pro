using SatinalmaPro.Shared.Helpers;
using SatinalmaPro.Shared.Models;
using SatinalmaPro.Shared.Procurement;
using SatinalmaPro.Shared.Services;
using SharedTalep = SatinalmaPro.Shared.Models.SatinalmaTalep;

namespace SatinalmaPro.Services;

/// <summary>Masaüstü talep listesi — enterprise route filtreleri ile dashboard sorguları.</summary>
public sealed class MasaustuDashboardSorgu : ISatinalmaDashboardSorgu
{
    private IReadOnlyList<SharedTalep> _talepler = [];

    public void TalepleriGuncelle(IReadOnlyList<SharedTalep> talepler) => _talepler = talepler;

    public IEnumerable<SharedTalep> KullaniciTalepleri(string uid)
    {
        var ad = OturumYoneticisi.AktifKullanici?.AdSoyad;
        return _talepler.Where(t => t.OlusturanUid == uid || t.TalepEden == ad);
    }

    public IEnumerable<SharedTalep> YonetimTalepleri() =>
        RouteIle(SatinalmaRoutes.YonetimGelenTalepler);

    public IEnumerable<SharedTalep> YonetimTeklifOnayiBekleyenleri() =>
        RouteIle(SatinalmaRoutes.YonetimTeklifGirilen);

    public IEnumerable<SharedTalep> YonetimOnaylananTeklifleri() =>
        RouteIle(SatinalmaRoutes.YonetimOnaylananTeklifler)
            .Where(t => !t.TeklifsizYonetimOnayi);

    public IEnumerable<SharedTalep> YonetimOnaylananTalepleri() =>
        RouteIle(SatinalmaRoutes.SatinalmaOnaylanan)
            .Where(t => t.TeklifsizYonetimOnayi);

    public IEnumerable<SharedTalep> YonetimReddedilenleri() =>
        RouteIle(SatinalmaRoutes.YonetimRedVerilen);

    public IEnumerable<SharedTalep> YonetimGecmisTalepleri() =>
        RouteIle(SatinalmaRoutes.YonetimGecmis);

    public IEnumerable<SharedTalep> YonetimGecmisTeklifliOnaylari() =>
        RouteIle(SatinalmaRoutes.YonetimOnayGecmisi)
            .Where(t => !t.TeklifsizYonetimOnayi);

    public IEnumerable<SharedTalep> TeklifGirisiBekleyenleri() =>
        RouteIle(SatinalmaRoutes.SatinalmaTeklifGirilen);

    public IEnumerable<SharedTalep> KarsilastirmaBekleyenleri() =>
        RouteIle(SatinalmaRoutes.SatinalmaKarsilastirma);

    public IEnumerable<SharedTalep> OnayBekleyenTalepler()
    {
        var rol = OturumYoneticisi.AktifKullanici?.Rol;
        if (TabFilterManager.RequiresRequesterScope(rol))
            return RouteIle(SatinalmaRoutes.Taleplerim)
                .Where(t =>
                {
                    var status = ProcurementStatusResolver.Resolve(t);
                    return status is ProcurementStatus.Submitted
                        or ProcurementStatus.QuoteRequested
                        or ProcurementStatus.QuoteEntry
                        or ProcurementStatus.Comparison
                        or ProcurementStatus.ManagementQuoteReview;
                });

        return RouteIle(SatinalmaRoutes.SatinalmaTeklifGirilen)
            .Concat(RouteIle(SatinalmaRoutes.SatinalmaKarsilastirma))
            .DistinctBy(t => t.Id);
    }

    public IEnumerable<SharedTalep> KayitliTalepler() =>
        SatinalmaTalepKuyrugu.Filtrele(_talepler, SatinalmaTalepKuyrugu.KayitliTalep);

    public IEnumerable<SharedTalep> OnaylanmisTalepler() =>
        RouteIle(SatinalmaRoutes.OnaylananTaleplerSaha);

    public IEnumerable<SharedTalep> TeklifsizFirmaFiyatBekleyenleri() =>
        _talepler.Where(t => t.TeklifsizFirmaFiyatBekliyor);

    public List<OnaylananMalzemeSatiri> OnaylananMalzemeleriOlustur() =>
        OnaylananMalzemeOlusturucu.Olustur(_talepler);

    public int MalKabulBekleyenSayisi() =>
        RouteIle(SatinalmaRoutes.SatinalmaSiparis).Count();

    private IEnumerable<SharedTalep> RouteIle(string route)
    {
        var rol = OturumYoneticisi.AktifKullanici?.Rol;
        var uid = OturumYoneticisi.AktifKullanici?.Uid;
        return _talepler.Where(t => ProcurementRouteMatcher.Matches(route, t, rol, uid));
    }
}

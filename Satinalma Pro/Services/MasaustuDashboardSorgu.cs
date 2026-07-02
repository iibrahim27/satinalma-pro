using SatinalmaPro.Shared.Helpers;
using SatinalmaPro.Shared.Models;
using SatinalmaPro.Shared.Services;
using SharedTalep = SatinalmaPro.Shared.Models.SatinalmaTalep;

namespace SatinalmaPro.Services;

/// <summary>Masaüstü talep listesi üzerinde mobil ile aynı kuyruk sorgularını çalıştırır.</summary>
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
        SatinalmaTalepKuyrugu.Filtrele(_talepler, SatinalmaTalepKuyrugu.YonetimTalepler);

    public IEnumerable<SharedTalep> YonetimTeklifOnayiBekleyenleri() =>
        SatinalmaTalepKuyrugu.Filtrele(_talepler, SatinalmaTalepKuyrugu.YonetimTeklifler);

    public IEnumerable<SharedTalep> YonetimOnaylananTeklifleri() =>
        SatinalmaTalepKuyrugu.Filtrele(_talepler, SatinalmaTalepKuyrugu.OnaylananTeklif);

    public IEnumerable<SharedTalep> YonetimOnaylananTalepleri() =>
        SatinalmaTalepKuyrugu.Filtrele(_talepler, SatinalmaTalepKuyrugu.OnaylananTalep);

    public IEnumerable<SharedTalep> YonetimReddedilenleri() =>
        SatinalmaTalepKuyrugu.Filtrele(_talepler, SatinalmaTalepKuyrugu.Reddedildi);

    public IEnumerable<SharedTalep> YonetimGecmisTalepleri() =>
        SatinalmaTalepKuyrugu.Filtrele(_talepler, SatinalmaTalepKuyrugu.YonetimGecmisTalep);

    public IEnumerable<SharedTalep> YonetimGecmisTeklifliOnaylari() =>
        SatinalmaTalepKuyrugu.Filtrele(_talepler, SatinalmaTalepKuyrugu.YonetimGecmisTeklifli);

    public IEnumerable<SharedTalep> TeklifGirisiBekleyenleri() =>
        SatinalmaTalepKuyrugu.Filtrele(_talepler, SatinalmaTalepKuyrugu.SatinalmaTeklifGirisiAktif);

    public IEnumerable<SharedTalep> KarsilastirmaBekleyenleri() =>
        SatinalmaTalepKuyrugu.Filtrele(_talepler, SatinalmaTalepKuyrugu.SatinalmaKarsilastirma);

    public IEnumerable<SharedTalep> OnayBekleyenTalepler()
    {
        var rol = OturumYoneticisi.AktifKullanici?.Rol;
        var uid = OturumYoneticisi.AktifKullanici?.Uid;
        var ad = OturumYoneticisi.AktifKullanici?.AdSoyad;
        var sadeceKendi = MobilYetkiServisi.SatinalmaSadeceTalepModu(rol);

        return SatinalmaTalepKuyrugu.Filtrele(_talepler, SatinalmaTalepKuyrugu.OnayBekleyen)
            .Where(t => !sadeceKendi || SatinalmaTalepKuyrugu.KullanicininTalebi(t, uid, ad));
    }

    public IEnumerable<SharedTalep> OnaylanmisTalepler() =>
        SatinalmaTalepKuyrugu.Filtrele(_talepler, SatinalmaTalepKuyrugu.Onaylanmis);

    public IEnumerable<SharedTalep> TeklifsizFirmaFiyatBekleyenleri() =>
        _talepler.Where(t => t.TeklifsizFirmaFiyatBekliyor);

    public List<OnaylananMalzemeSatiri> OnaylananMalzemeleriOlustur()
    {
        var liste = new List<OnaylananMalzemeSatiri>();

        foreach (var talep in _talepler.Where(t => t.HerhangiKalemOnayli))
        {
            talep.FirmaSiparisNolari ??= [];

            foreach (var kalem in talep.Kalemler.Where(k => k.OnaylananTeklifId != null).OrderBy(k => k.SiraNo))
            {
                var teklif = talep.KalemOnayTeklifi(kalem);
                if (teklif is null)
                    continue;

                teklif.FiyatlariHesapla(talep.Kalemler);
                var fiyat = teklif.Fiyatlar.FirstOrDefault(f => f.KalemId == kalem.Id);
                if (fiyat is null)
                    continue;

                var siparisNo = talep.FirmaSiparisNolari.TryGetValue(teklif.Id, out var no) && !string.IsNullOrWhiteSpace(no)
                    ? no
                    : talep.SiparisNo;

                liste.Add(new OnaylananMalzemeSatiri
                {
                    TalepId = talep.Id,
                    KalemId = kalem.Id,
                    TeklifId = teklif.Id,
                    TalepNo = talep.TalepNo,
                    SiparisNo = siparisNo,
                    Tarih = talep.Tarih,
                    Durum = talep.Durum,
                    Firma = teklif.FirmaAdi,
                    Marka = string.IsNullOrWhiteSpace(fiyat.Marka) ? teklif.Marka : fiyat.Marka,
                    Malzeme = kalem.Malzeme,
                    SiparisMiktari = kalem.Miktar,
                    KabulEdilenMiktar = kalem.KabulEdilenMiktar,
                    SiparisTamamlandi = kalem.SiparisTamamlandi,
                    Birim = kalem.Birim,
                    BirimFiyati = fiyat.TlBirimFiyat(teklif.UsdKuru, teklif.EurKuru),
                    ToplamTutar = fiyat.ToplamTutar,
                    KalemAciklamasi = kalem.Aciklama,
                    VadeGunu = teklif.VadeGunu
                });
            }
        }

        return liste;
    }

    public int MalKabulBekleyenSayisi() =>
        OnaylananMalzemeleriOlustur().Count(s => !s.SiparisTamamlandi && s.KalanMiktar > 0.0001);
}

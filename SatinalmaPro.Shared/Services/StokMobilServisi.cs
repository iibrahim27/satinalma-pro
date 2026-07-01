using SatinalmaPro.Shared.Helpers;
using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Shared.Services;

public sealed class StokMobilServisi
{
    private readonly MobilVeriDeposu _depo;

    public StokMobilServisi(MobilVeriDeposu depo) => _depo = depo;

    public StokKaydi? StokBul(string malzeme, string depo) =>
        _depo.Stok.FirstOrDefault(s =>
            s.MalzemeAdi.Equals(malzeme.Trim(), StringComparison.OrdinalIgnoreCase) &&
            s.DepoSaha.Equals(depo.Trim(), StringComparison.OrdinalIgnoreCase));

    public StokKaydi StokBulVeyaOlustur(string malzeme, string kategori, string birim, string depo, decimal birimMaliyet = 0)
    {
        var mevcut = StokBul(malzeme, depo);
        if (mevcut is not null)
            return mevcut;

        mevcut = new StokKaydi
        {
            MalzemeAdi = malzeme.Trim(),
            Kategori = kategori.Trim(),
            Birim = birim.Trim(),
            DepoSaha = depo.Trim(),
            BirimMaliyet = birimMaliyet,
            SonGuncelleme = Bugun()
        };
        _depo.Stok.Add(mevcut);
        return mevcut;
    }

    public async Task GirisYapAsync(
        string tarih, IEnumerable<StokIslemSatirKaydi> satirlar,
        string belgeNo, string teslimEden, string teslimEdilen,
        CancellationToken iptal = default)
    {
        foreach (var satir in satirlar)
        {
            var stok = StokBulVeyaOlustur(satir.Malzeme, satir.Kategori, satir.Birim, satir.DepoSaha, satir.BirimFiyat);
            stok.MevcutMiktar += satir.Miktar;
            if (satir.BirimFiyat > 0)
                stok.BirimMaliyet = satir.BirimFiyat;
            stok.SonGuncelleme = tarih;
            stok.ToplamDegerHesapla();

            _depo.StokHareketleri.Add(new StokHareketKaydi
            {
                Tarih = tarih,
                HareketTipi = StokHareketTipleri.Giris,
                MalzemeAdi = stok.MalzemeAdi,
                Kategori = stok.Kategori,
                Birim = stok.Birim,
                Miktar = satir.Miktar,
                DepoSaha = stok.DepoSaha,
                BirimMaliyet = stok.BirimMaliyet,
                BelgeNo = belgeNo,
                IslemYapan = teslimEden,
                TeslimEdilen = teslimEdilen
            });
        }

        await _depo.StokKaydetAsync(iptal);
        await _depo.StokHareketKaydetAsync(iptal);
    }

    public async Task CikisYapAsync(
        string tarih, IEnumerable<StokIslemSatirKaydi> satirlar,
        string belgeNo, string teslimEden, string teslimEdilen,
        CancellationToken iptal = default)
    {
        foreach (var satir in satirlar)
        {
            var stok = StokBul(satir.Malzeme, satir.DepoSaha)
                ?? throw new InvalidOperationException($"{satir.Malzeme} stokta bulunamadı.");

            if (satir.Miktar > stok.MevcutMiktar)
                throw new InvalidOperationException($"{satir.Malzeme} için yetersiz stok.");

            stok.MevcutMiktar -= satir.Miktar;
            stok.SonGuncelleme = tarih;
            stok.ToplamDegerHesapla();

            _depo.StokHareketleri.Add(new StokHareketKaydi
            {
                Tarih = tarih,
                HareketTipi = StokHareketTipleri.Cikis,
                MalzemeAdi = stok.MalzemeAdi,
                Kategori = stok.Kategori,
                Birim = stok.Birim,
                Miktar = satir.Miktar,
                DepoSaha = stok.DepoSaha,
                BirimMaliyet = stok.BirimMaliyet,
                BelgeNo = belgeNo,
                IslemYapan = teslimEden,
                TeslimEdilen = teslimEdilen
            });
        }

        await _depo.StokKaydetAsync(iptal);
        await _depo.StokHareketKaydetAsync(iptal);
    }

    public IEnumerable<string> MalzemeListesi() =>
        _depo.Stok.Select(s => s.MalzemeAdi).Distinct(StringComparer.CurrentCultureIgnoreCase).OrderBy(s => s);

    public IEnumerable<string> StokMalzemeAra(string? arama, bool sadeceMevcutStok = true)
    {
        var kaynak = _depo.Stok
            .Where(s => !sadeceMevcutStok || s.MevcutMiktar > 0)
            .Select(s => s.MalzemeAdi);
        return MalzemeAdiOneriYardimcisi.Filtrele(kaynak, arama);
    }

    public IEnumerable<string> DepoListesi() =>
        _depo.Stok.Select(s => s.DepoSaha).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().OrderBy(s => s);

    public async Task SayimYapAsync(
        string malzeme, string depo, double sayimMiktari, string islemYapan,
        CancellationToken iptal = default)
    {
        var stok = StokBul(malzeme, depo)
            ?? throw new InvalidOperationException($"{malzeme} stokta bulunamadı.");

        var fark = sayimMiktari - stok.MevcutMiktar;
        if (Math.Abs(fark) < 0.0001)
            return;

        var tarih = Bugun();
        stok.MevcutMiktar = sayimMiktari;
        stok.SonGuncelleme = tarih;
        stok.ToplamDegerHesapla();

        _depo.StokHareketleri.Add(new StokHareketKaydi
        {
            Tarih = tarih,
            HareketTipi = StokHareketTipleri.Sayim,
            MalzemeAdi = stok.MalzemeAdi,
            Kategori = stok.Kategori,
            Birim = stok.Birim,
            Miktar = Math.Abs(fark),
            DepoSaha = stok.DepoSaha,
            BirimMaliyet = stok.BirimMaliyet,
            BelgeNo = _depo.YeniBelgeNo("SY"),
            IslemYapan = islemYapan,
            Aciklama = fark > 0 ? "Sayım fazlası" : "Sayım eksiği"
        });

        await _depo.StokKaydetAsync(iptal);
        await _depo.StokHareketKaydetAsync(iptal);
    }

    private static string Bugun() => DateTime.Now.ToString("dd.MM.yyyy");
}

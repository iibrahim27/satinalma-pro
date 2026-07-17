using SatinalmaPro.Shared.Helpers;
using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Shared.Services;

public sealed class BildirimServisi
{
    private readonly MobilVeriDeposu _depo;
    private readonly FcmPushServisi? _fcm;
    private readonly SemaphoreSlim _yazmaKilidi = new(1, 1);

    public BildirimServisi(MobilVeriDeposu depo, FcmPushServisi? fcm = null)
    {
        _depo = depo;
        _fcm = fcm;
    }

    public async Task EkleAsync(BildirimKaydi bildirim, CancellationToken iptal = default)
    {
        await CokluEkleAsync([bildirim], iptal);
    }

    public async Task CokluEkleAsync(IReadOnlyList<BildirimKaydi> bildirimler, CancellationToken iptal = default)
    {
        if (bildirimler.Count == 0)
            return;

        var gonderilecekler = bildirimler
            .Where(BildirimRolPolitikasi.KayitGonderilmeli)
            .GroupBy(BildirimMantikAnahtari.Olustur, StringComparer.Ordinal)
            .Select(grup => grup.First())
            .ToList();
        if (gonderilecekler.Count == 0)
            return;

        await _yazmaKilidi.WaitAsync(iptal);
        try
        {
            await _depo.BildirimleriYukleAsync(iptal);
            var mevcutAnahtarlar = _depo.Bildirimler
                .Select(BildirimMantikAnahtari.Olustur)
                .ToHashSet(StringComparer.Ordinal);
            var yeniKayitlar = gonderilecekler
                .Where(b => !mevcutAnahtarlar.Contains(BildirimMantikAnahtari.Olustur(b)))
                .ToList();
            if (yeniKayitlar.Count == 0)
                return;

            foreach (var b in yeniKayitlar)
            {
                BildirimBirlestirme.Dokun(b);
                _depo.Bildirimler.Insert(0, b);
            }

            await _depo.BildirimleriKaydetAsync(iptal);

            foreach (var b in yeniKayitlar)
                await FcmGonderAsync(b, iptal);
        }
        finally
        {
            _yazmaKilidi.Release();
        }
    }

    public IEnumerable<BildirimKaydi> KullaniciBildirimleri(KullaniciProfili? kullanici) =>
        BildirimFiltreleme.KullaniciBildirimleri(_depo.Bildirimler, kullanici);

    public int OkunmamisSayisi(KullaniciProfili? kullanici) =>
        BildirimFiltreleme.OkunmamisSayisi(_depo.Bildirimler, kullanici, _depo.Talepler);

    public async Task OkunduIsaretleAsync(BildirimKaydi bildirim, CancellationToken iptal = default)
    {
        bildirim.Okundu = true;
        BildirimBirlestirme.Dokun(bildirim);
        await _depo.BildirimleriKaydetAsync(iptal);
    }

    public async Task TumunuOkunduIsaretleAsync(KullaniciProfili kullanici, CancellationToken iptal = default)
    {
        foreach (var b in KullaniciBildirimleri(kullanici))
        {
            b.Okundu = true;
            BildirimBirlestirme.Dokun(b);
        }
        await _depo.BildirimleriKaydetAsync(iptal);
    }

    public async Task GecersizleriSilAsync(CancellationToken iptal = default)
    {
        await _depo.BildirimleriYukleAsync(iptal);
        await _depo.TalepleriYukleAsync(iptal);

        var silinecek = _depo.Bildirimler
            .Where(b => !BildirimFiltreleme.GecerliMi(b, _depo.Talepler))
            .ToList();
        if (silinecek.Count > 0)
        {
            var silinecekIdler = silinecek.Select(b => b.Id).ToHashSet();
            _depo.Bildirimler.RemoveAll(b => silinecekIdler.Contains(b.Id));
            await _depo.BildirimleriArsivleAsync(silinecek, iptal);
            await _depo.BildirimleriKaydetAsync(iptal);
        }
    }

    public async Task GecersizleriOkunduYapAsync(CancellationToken iptal = default)
    {
        await _depo.BildirimleriYukleAsync(iptal);
        await _depo.TalepleriYukleAsync(iptal);

        var degisti = false;
        foreach (var b in _depo.Bildirimler)
        {
            if (b.Okundu || BildirimFiltreleme.GecerliMi(b, _depo.Talepler))
                continue;

            b.Okundu = true;
            BildirimBirlestirme.Dokun(b);
            degisti = true;
        }

        if (degisti)
            await _depo.BildirimleriKaydetAsync(iptal);
    }

    public async Task TemizleAsync(KullaniciProfili kullanici, CancellationToken iptal = default)
    {
        await _depo.TalepleriYukleAsync(iptal);
        var silinecek = KullaniciBildirimleri(kullanici)
            .Where(b => !BildirimFiltreleme.Temizlenmemeli(b, _depo.Talepler))
            .ToList();

        var silinecekIdler = silinecek.Select(b => b.Id).ToHashSet();
        _depo.Bildirimler.RemoveAll(b => silinecekIdler.Contains(b.Id));
        await _depo.BildirimleriArsivleAsync(silinecek, iptal);
        await _depo.BildirimleriKaydetAsync(iptal);
    }

    public async Task TalepBildirimleriniSilAsync(Guid talepId, CancellationToken iptal = default)
    {
        await _depo.BildirimleriYukleAsync(iptal);
        var silinecek = _depo.Bildirimler.Where(b => b.TalepId == talepId).ToList();
        _depo.Bildirimler.RemoveAll(b => b.TalepId == talepId);
        await _depo.BildirimleriArsivleAsync(silinecek, iptal);
        await _depo.BildirimleriKaydetAsync(iptal);
    }

    private async Task FcmGonderAsync(BildirimKaydi bildirim, CancellationToken iptal)
    {
        if (_fcm?.Aktif != true)
            return;

        try
        {
            await _fcm.BildirimGonderAsync(bildirim, iptal);
        }
        catch
        {
            // push isteğe bağlı
        }
    }
}

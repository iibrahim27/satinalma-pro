using SatinalmaPro.Shared.Helpers;
using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Shared.Services;

public sealed class BildirimServisi
{
    private readonly MobilVeriDeposu _depo;
    private readonly FcmPushServisi? _fcm;

    public BildirimServisi(MobilVeriDeposu depo, FcmPushServisi? fcm = null)
    {
        _depo = depo;
        _fcm = fcm;
    }

    public async Task EkleAsync(BildirimKaydi bildirim, CancellationToken iptal = default)
    {
        BildirimBirlestirme.Dokun(bildirim);
        await _depo.BildirimleriYukleAsync(iptal);
        _depo.Bildirimler.Insert(0, bildirim);
        await _depo.BildirimleriKaydetAsync(iptal);
        await FcmGonderAsync(bildirim, iptal);
    }

    public async Task CokluEkleAsync(IReadOnlyList<BildirimKaydi> bildirimler, CancellationToken iptal = default)
    {
        if (bildirimler.Count == 0)
            return;

        foreach (var b in bildirimler)
            BildirimBirlestirme.Dokun(b);

        await _depo.BildirimleriYukleAsync(iptal);
        foreach (var b in bildirimler)
            _depo.Bildirimler.Insert(0, b);
        await _depo.BildirimleriKaydetAsync(iptal);

        foreach (var b in bildirimler)
            await FcmGonderAsync(b, iptal);
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

        var kalacak = _depo.Bildirimler
            .Where(b => BildirimFiltreleme.GecerliMi(b, _depo.Talepler))
            .ToList();
        if (kalacak.Count != _depo.Bildirimler.Count)
        {
            _depo.Bildirimler.Clear();
            _depo.Bildirimler.AddRange(kalacak);
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
            .Select(b => b.Id)
            .ToHashSet();

        _depo.Bildirimler.RemoveAll(b => silinecek.Contains(b.Id));
        await _depo.BildirimleriKaydetAsync(iptal);
    }

    public async Task TalepBildirimleriniSilAsync(Guid talepId, CancellationToken iptal = default)
    {
        await _depo.BildirimleriYukleAsync(iptal);
        _depo.Bildirimler.RemoveAll(b => b.TalepId == talepId);
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

using SatinalmaPro.Shared.Models;
using SatinalmaPro.Shared.Services;

namespace SatinalmaPro.Mobile.Services;

public sealed class BildirimDinleyici : IDisposable
{
    private readonly OturumServisi _oturum;
    private Timer? _zamanlayici;
    private bool _kontrolEdiliyor;

    public event Action? BildirimlerDegisti;

    public BildirimDinleyici(OturumServisi oturum) => _oturum = oturum;

    public int OkunmamisSayisi =>
        _oturum.Bildirimler.OkunmamisSayisi(_oturum.Depo.AktifKullanici);

    public void Baslat()
    {
        Durdur();
        _zamanlayici = new Timer(_ => _ = KontrolEtAsync(), null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15));
    }

    public void Durdur()
    {
        _zamanlayici?.Dispose();
        _zamanlayici = null;
    }

    public Task IlkKontrolAsync() => KontrolEtAsync(bildirimGoster: !BildirimGosterimKaydi.FcmAktif, agYenile: true);

    public Task SenkronizeVeGosterAsync() =>
        KontrolEtAsync(bildirimGoster: !BildirimGosterimKaydi.FcmAktif, agYenile: true);

    public Task SenkronizeSessizAsync() => KontrolEtAsync(bildirimGoster: false, agYenile: true);

    private async Task KontrolEtAsync(bool bildirimGoster = true, bool agYenile = true)
    {
        if (_kontrolEdiliyor || !_oturum.GirisYapildi)
            return;

        _kontrolEdiliyor = true;
        try
        {
            if (agYenile)
            {
                try
                {
                    await _oturum.Depo.HizliSenkronAsync();
                }
                catch
                {
                    // Kota / ağ hatası — mevcut önbellek ile devam
                }
            }

            await _oturum.Bildirimler.GecersizleriOkunduYapAsync();

            var kullanici = _oturum.Depo.AktifKullanici;
            if (kullanici is null)
                return;

            var toastGoster = bildirimGoster && !BildirimGosterimKaydi.FcmAktif;

            foreach (var bildirim in _oturum.Bildirimler.KullaniciBildirimleri(kullanici))
            {
                if (!toastGoster
                    || !BildirimFiltreleme.ToastGosterilmeli(bildirim, kullanici, _oturum.Depo.Talepler))
                    continue;

                if (!BildirimGosterimKaydi.IlkGosterimMi(bildirim.Id))
                    continue;

                BildirimGosterimKaydi.Isaretle(bildirim.Id);

                MainThread.BeginInvokeOnMainThread(() =>
                    YerelBildirimGosterici.Goster(bildirim, kullanici.Rol));
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                _oturum.VeriGuncellendiBildir();
                BildirimlerDegisti?.Invoke();
            });
        }
        catch
        {
            // ağ hatası sessiz
        }
        finally
        {
            _kontrolEdiliyor = false;
        }
    }

    public void Dispose() => Durdur();
}

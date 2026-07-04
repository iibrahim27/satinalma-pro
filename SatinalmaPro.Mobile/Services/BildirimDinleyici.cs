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
        _zamanlayici = new Timer(
            _ => _ = KontrolEtAsync(yalnizcaBildirim: true),
            null,
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(5));
    }

    public void Durdur()
    {
        _zamanlayici?.Dispose();
        _zamanlayici = null;
    }

    public Task IlkKontrolAsync() => KontrolEtAsync(bildirimGoster: true, agYenile: true);

    public Task SenkronizeVeGosterAsync() =>
        KontrolEtAsync(bildirimGoster: true, agYenile: true);

    /// <summary>Arka plan servisi — yalnızca bildirim belgesi (1 Firestore isteği).</summary>
    public Task BildirimPollAsync() =>
        KontrolEtAsync(bildirimGoster: true, agYenile: true, yalnizcaBildirim: true);

    public Task SenkronizeSessizAsync() => KontrolEtAsync(bildirimGoster: false, agYenile: true);

    private async Task KontrolEtAsync(
        bool bildirimGoster = true,
        bool agYenile = true,
        bool yalnizcaBildirim = false)
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
                    if (yalnizcaBildirim)
                        await _oturum.Depo.BildirimleriSenkronizeEtAsync();
                    else
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

            var toastGoster = bildirimGoster;

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

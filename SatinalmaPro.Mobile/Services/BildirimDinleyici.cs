using SatinalmaPro.Shared.Models;
using SatinalmaPro.Shared.Services;

namespace SatinalmaPro.Mobile.Services;

public sealed class BildirimDinleyici : IDisposable
{
    private readonly OturumServisi _oturum;
    private Timer? _zamanlayici;
    private int _kontrolEdiliyor;
    private bool _ilkSenkronTamam;

    public event Action? BildirimlerDegisti;

    public BildirimDinleyici(OturumServisi oturum) => _oturum = oturum;

    public int OkunmamisSayisi =>
        _oturum.Bildirimler.OkunmamisSayisi(_oturum.Depo.AktifKullanici);

    public void Baslat()
    {
        Durdur();
        _ilkSenkronTamam = false;
        _zamanlayici = new Timer(
            _ => _ = KontrolEtAsync(yalnizcaBildirim: true),
            null,
            TimeSpan.FromSeconds(2),
            TimeSpan.FromMinutes(1));
    }

    public void Durdur()
    {
        _zamanlayici?.Dispose();
        _zamanlayici = null;
        _ilkSenkronTamam = false;
    }

    /// <summary>İlk yükleme yalnızca gelen kutusunu eşitler; geçmiş kayıtlar yeniden toast edilmez.</summary>
    public Task IlkKontrolAsync() => KontrolEtAsync(bildirimGoster: false, agYenile: true);

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
        if (!_oturum.GirisYapildi || Interlocked.CompareExchange(ref _kontrolEdiliyor, 1, 0) != 0)
            return;

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
            var ilkSenkron = !_ilkSenkronTamam;

            foreach (var bildirim in _oturum.Bildirimler.KullaniciBildirimleri(kullanici))
            {
                // Uygulama yeniden açıldığında var olan bildirimleri yalnızca bilinen olarak işaretle.
                // Sonraki poll/FCM turunda yalnız gerçekten yeni kayıtlar toast olur.
                if (ilkSenkron)
                {
                    BildirimGosterimKaydi.Isaretle(bildirim.Id);
                    continue;
                }

                if (!toastGoster
                    || !BildirimFiltreleme.ToastGosterilmeli(bildirim, kullanici, _oturum.Depo.Talepler))
                    continue;

                if (!BildirimGosterimKaydi.IlkGosterimMi(bildirim.Id))
                    continue;

                BildirimGosterimKaydi.Isaretle(bildirim.Id);

                MainThread.BeginInvokeOnMainThread(() =>
                    YerelBildirimGosterici.Goster(bildirim, kullanici.Rol));
            }

            _ilkSenkronTamam = true;

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
            Volatile.Write(ref _kontrolEdiliyor, 0);
        }
    }

    public void Dispose() => Durdur();
}

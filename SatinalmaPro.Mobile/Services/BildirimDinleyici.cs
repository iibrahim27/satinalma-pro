using SatinalmaPro.Shared.Models;
using SatinalmaPro.Shared.Services;

namespace SatinalmaPro.Mobile.Services;

public sealed class BildirimDinleyici : IDisposable
{
    private const string GosterilenAnahtar = "bildirim_gosterilen";

    private readonly OturumServisi _oturum;
    private readonly HashSet<Guid> _gosterilen = [];
    private Timer? _zamanlayici;
    private bool _kontrolEdiliyor;

    public event Action? BildirimlerDegisti;

    public BildirimDinleyici(OturumServisi oturum)
    {
        _oturum = oturum;
        GosterilenleriYukle();
    }

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

    public Task IlkKontrolAsync() => KontrolEtAsync(bildirimGoster: true, agYenile: true);

    public Task SenkronizeVeGosterAsync() => KontrolEtAsync(bildirimGoster: true, agYenile: true);

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

            foreach (var bildirim in _oturum.Bildirimler.KullaniciBildirimleri(kullanici))
            {
                if (!bildirimGoster
                    || !BildirimFiltreleme.ToastGosterilmeli(bildirim, kullanici, _oturum.Depo.Talepler))
                    continue;

                if (!_gosterilen.Add(bildirim.Id))
                    continue;

                GosterilenleriKaydet();

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

    private void GosterilenleriYukle()
    {
        _gosterilen.Clear();
        var ham = Preferences.Default.Get(GosterilenAnahtar, "");
        foreach (var parca in ham.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            if (Guid.TryParse(parca, out var id))
                _gosterilen.Add(id);
        }
    }

    private void GosterilenleriKaydet()
    {
        if (_gosterilen.Count == 0)
        {
            Preferences.Default.Remove(GosterilenAnahtar);
            return;
        }

        Preferences.Default.Set(GosterilenAnahtar, string.Join(',', _gosterilen));
    }

    public void Dispose() => Durdur();
}


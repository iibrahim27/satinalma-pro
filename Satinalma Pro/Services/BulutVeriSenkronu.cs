using System.IO;
using System.Threading;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Threading;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Shared;
using SatinalmaPro.Shared.SaaS;

namespace SatinalmaPro.Services;

public static class BulutVeriSenkronu
{
    private static readonly JsonSerializerOptions JsonSecenekleri = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly SemaphoreSlim BulutYazmaKilidi = new(1, 1);
    private static readonly HashSet<string> BekleyenKayitlar = new(StringComparer.Ordinal);
    private static readonly HashSet<string> BosYazmayaIzinliAnahtarlar = new(StringComparer.Ordinal);
    private static DispatcherTimer? _zamanlayici;
    private static bool _senkronYukleniyor;
    private static DispatcherTimer? _yoklamaZamanlayici;
    private static int _yoklamaDongusu;
    private static bool _sifirlamaAktif;

    private static readonly string[] SikYoklamaAnahtarlari =
        ["satinalma_ayarlar", "satinalma_talepler", "uygulama_ayarlar"];
    private static readonly TimeSpan YoklamaAraligi = TimeSpan.FromSeconds(30);

    public static bool BuluttanYuklendi { get; private set; }
    public static bool SifirlamaAktif => _sifirlamaAktif;

    /// <summary>
    /// Tam sıfırlama sırasında yoklama/merge/planla kapalı — eski bulut verisi yereli doldurmasın.
    /// </summary>
    public static void SifirlamaKapisiniAc()
    {
        _sifirlamaAktif = true;
        lock (BekleyenKayitlar)
            BekleyenKayitlar.Clear();
        _zamanlayici?.Stop();
        YoklamayiDurdur();
    }

    public static void SifirlamaKapisiniKapat()
    {
        _sifirlamaAktif = false;
        if (OturumYoneticisi.GirisYapildi)
            YoklamayiBaslat();
    }

    /// <summary>Firma değişiminde bekleyen yazmaları ve senkron durumunu sıfırlar.</summary>
    public static void KiraciDegisti()
    {
        lock (BekleyenKayitlar)
            BekleyenKayitlar.Clear();

        lock (BosYazmayaIzinliAnahtarlar)
            BosYazmayaIzinliAnahtarlar.Clear();

        _zamanlayici?.Stop();
        YoklamayiDurdur();
        BuluttanYuklendi = false;
        _senkronYukleniyor = false;
        _sifirlamaAktif = false;
        _yoklamaDongusu = 0;
    }

    public static void Planla(string anahtar)
    {
        if (_sifirlamaAktif)
            return;
        if (!OturumYoneticisi.GirisYapildi || OturumYoneticisi.Firestore is null)
            return;

        lock (BekleyenKayitlar)
        {
            BekleyenKayitlar.Add(anahtar);
        }

        _zamanlayici ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1200) };
        _zamanlayici.Tick -= ZamanlayiciTik;
        _zamanlayici.Tick += ZamanlayiciTik;
        _zamanlayici.Stop();
        _zamanlayici.Start();
    }

    public static async Task IkiliSenkronAsync(
        IProgress<(int tamamlanan, int toplam, string adim)>? ilerleme = null,
        CancellationToken iptal = default)
    {
        if (!OturumYoneticisi.GirisYapildi || OturumYoneticisi.Firestore is null)
            return;
        if (_sifirlamaAktif)
            return;

        var senkronTenantId = KiracıOturumu.TenantId;
        if (string.IsNullOrWhiteSpace(senkronTenantId))
            return;

        _senkronYukleniyor = true;
        var toplam = BelgeHaritasi.Count + 1;
        var tamamlanan = 0;

        try
        {
            foreach (var (anahtar, yol) in BelgeHaritasiSirali())
            {
                iptal.ThrowIfCancellationRequested();
                if (!string.Equals(senkronTenantId, KiracıOturumu.TenantId, StringComparison.Ordinal))
                    return;

                ilerleme?.Report((tamamlanan, toplam, SenkronAdimMetni(anahtar)));

                try
                {
                    var (bulutJson, _) = await OturumYoneticisi.Firestore
                        .BelgeOkuAsync(yol, iptal)
                        .ConfigureAwait(false);

                    if (!string.Equals(senkronTenantId, KiracıOturumu.TenantId, StringComparison.Ordinal))
                        return;

                    var yerelJson = YerelJsonOku(anahtar);

                    // Belge Firestore'da varsa (boş [] dahil) bulutu uygula — aksi halde PC2 eski veriyi geri yükler
                    var bulutBelgesiVar = bulutJson is not null;
                    var yereldeVar = JsonAnlamliMi(yerelJson, anahtar);
                    var buluttaVar = JsonAnlamliMi(bulutJson, anahtar);

                    // Bilinçli sıfırlama damgası varken boş bulutu yerel ile doldurma.
                    var sifirlamaDamgasi = SatinalmaDepo.Ayarlar.VeriSifirlamaUtc > 0
                        || anahtar is "satinalma_talepler" or "satinalma_ayarlar";

                    if (bulutBelgesiVar && !buluttaVar && yereldeVar && yerelJson is not null
                        && ListeModuluMu(anahtar)
                        && SatinalmaDepo.Ayarlar.VeriSifirlamaUtc <= 0
                        && anahtar != "satinalma_talepler")
                    {
                        // Boş bulut + dolu yerel: mal kabul kaybını önlemek için yereli yükle
                        await OturumYoneticisi.Firestore.BelgeJsonYazAsync(
                            yol, yerelJson, OturumYoneticisi.Auth?.Uid, iptal)
                            .ConfigureAwait(false);
                        BulutSenkronZamani.Kaydet(anahtar, DateTime.UtcNow);
                    }
                    else if (bulutBelgesiVar && !buluttaVar && sifirlamaDamgasi)
                    {
                        // Boş bulut otoriter — yereli geri yükleme.
                        await UiThreaddeCalistirAsync(() =>
                        {
                            if (!string.Equals(senkronTenantId, KiracıOturumu.TenantId, StringComparison.Ordinal))
                                return;
                            Uygula(anahtar, bulutJson!);
                            YerelBirlesikDurumuKaydet(anahtar);
                        });
                        BulutSenkronZamani.Kaydet(anahtar, DateTime.UtcNow);
                    }
                    else if (bulutBelgesiVar)
                    {
                        await UiThreaddeCalistirAsync(() =>
                        {
                            if (!string.Equals(senkronTenantId, KiracıOturumu.TenantId, StringComparison.Ordinal))
                                return;
                            Uygula(anahtar, bulutJson!);
                            YerelBirlesikDurumuKaydet(anahtar);
                        });
                        BulutSenkronZamani.Kaydet(anahtar, DateTime.UtcNow);
                    }
                    else if (yereldeVar && yerelJson is not null)
                    {
                        // Yeni firmanın boş imza ayarını buluta şablon olarak yazma.
                        if (anahtar == "satinalma_ayarlar" && SatinalmaAyarlariBosMu(yerelJson))
                        {
                            // Yerelde temiz iskelet varsa buluta yazma; firma kendi ayarını kaydedince gider.
                        }
                        else
                        {
                            await OturumYoneticisi.Firestore.BelgeJsonYazAsync(
                                yol, yerelJson, OturumYoneticisi.Auth?.Uid, iptal)
                                .ConfigureAwait(false);
                        }
                    }
                    else if (yerelJson is not null)
                    {
                        // Yerel sıfırlandı, bulutta kayıt yok — boş durumu buluta yaz
                        if (anahtar != "satinalma_ayarlar" || !SatinalmaAyarlariBosMu(yerelJson))
                        {
                            await OturumYoneticisi.Firestore.BelgeJsonYazAsync(
                                yol, yerelJson, OturumYoneticisi.Auth?.Uid, iptal)
                                .ConfigureAwait(false);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    // Tek kayıt hatası tüm senkronu durdurmasın
                    HataGunlugu.Kaydet(ex, $"BulutVeriSenkronu.IkiliSenkron.{anahtar}");
                }

                tamamlanan++;
            }

            iptal.ThrowIfCancellationRequested();
            ilerleme?.Report((tamamlanan, toplam, "Logolar senkronize ediliyor..."));

            try
            {
                await MedyaBulutSenkronu.SenkronizeEtAsync(iptal).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                HataGunlugu.Kaydet(ex, "BulutVeriSenkronu.MedyaSenkron");
            }

            tamamlanan++;
            ilerleme?.Report((tamamlanan, toplam, "Tamamlandı"));
            BuluttanYuklendi = true;
        }
        finally
        {
            _senkronYukleniyor = false;
        }
    }

    private static Task UiThreaddeCalistirAsync(Action islem)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            islem();
            return Task.CompletedTask;
        }

        return dispatcher.InvokeAsync(islem).Task;
    }

    private static string SenkronAdimMetni(string anahtar) => anahtar switch
    {
        "malzeme" => "Malzeme verileri alınıyor...",
        "stok" => "Stok verileri alınıyor...",
        "stok_hareket" => "Stok hareketleri alınıyor...",
        "agrega" => "Agrega verileri alınıyor...",
        "cimento" => "Çimento verileri alınıyor...",
        "akaryakit" => "Akaryakıt verileri alınıyor...",
        "filo" => "Filo verileri alınıyor...",
        "satinalma_talepler" => "Satınalma talepleri alınıyor...",
        "satinalma_ayarlar" => "Satınalma ayarları alınıyor...",
        "finansman" => "Finansman verileri alınıyor...",
        "uygulama_ayarlar" => "Uygulama ayarları alınıyor...",
        _ => "Veriler senkronize ediliyor..."
    };

    /// <param name="sifirlamaModu">
    /// true: bilinçli tam sıfırlama — boş []/{} yazılır, bekleyen birleştirme senkronu iptal edilir,
    /// medya bulutu temizlenir. Yetki yoksa sessizce çıkmak yerine hata fırlatır.
    /// </param>
    public static async Task TumVerileriBulutaGonderAsync(
        CancellationToken iptal = default,
        bool sifirlamaModu = false)
    {
        if (!OturumYoneticisi.GirisYapildi || OturumYoneticisi.Firestore is null)
        {
            if (sifirlamaModu)
                throw new InvalidOperationException("Buluta yazmak için oturum gerekli.");
            return;
        }

        if (!KullaniciYetkileri.Duzenleyebilir)
        {
            if (sifirlamaModu)
                throw new InvalidOperationException("Buluta sıfırlama yazmak için düzenleme yetkisi gerekli.");
            return;
        }

        if (sifirlamaModu)
        {
            // Gecikmeli Planla/merge eski talepleri buluta geri yazmasın.
            lock (BekleyenKayitlar)
                BekleyenKayitlar.Clear();
            _zamanlayici?.Stop();

            lock (BosYazmayaIzinliAnahtarlar)
            {
                foreach (var anahtar in BelgeHaritasi.Keys)
                    BosYazmayaIzinliAnahtarlar.Add(anahtar);
            }

            // Android/diğer istemcilerin offline cache temizlemesi için damga.
            if (SatinalmaDepo.Ayarlar.VeriSifirlamaUtc <= 0)
            {
                SatinalmaDepo.Ayarlar.VeriSifirlamaUtc =
                    DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                SatinalmaDepo.Kaydet();
            }
        }

        _senkronYukleniyor = true;
        try
        {
            await BulutYazmaKilidi.WaitAsync(iptal).ConfigureAwait(false);
            try
            {
                foreach (var (anahtar, yol) in BelgeHaritasiSirali())
                {
                    var json = Olustur(anahtar);
                    // Normal senkron: boş bellek yazımını yerelden tamamla.
                    // Sıfırlama: boş liste bilinçlidir — eski yerel/cache ile ezilmesin.
                    if (!sifirlamaModu && (string.IsNullOrWhiteSpace(json) || json is "[]" or "{}"))
                        json = YerelJsonOku(anahtar) ?? json;

                    await OturumYoneticisi.Firestore.BelgeJsonYazAsync(
                        yol, json, OturumYoneticisi.Auth?.Uid, iptal).ConfigureAwait(false);
                    YerelOnbellegeYaz(anahtar, json);
                    BulutSenkronZamani.Kaydet(anahtar, DateTime.UtcNow);
                }

                if (sifirlamaModu)
                {
                    await MedyaBulutSenkronu.BulutuTemizleAsync(iptal).ConfigureAwait(false);
                    // Paylaşılan bildirim blob'unu da boşalt (inbox kullanıcı bazlı ayrı temizlenir).
                    await OturumYoneticisi.Firestore.BelgeJsonYazAsync(
                        FirestoreYollari.Bildirimler(),
                        "[]",
                        OturumYoneticisi.Auth?.Uid,
                        iptal).ConfigureAwait(false);
                }
                else
                    await MedyaBulutSenkronu.BulutaYukleAsync(iptal).ConfigureAwait(false);
            }
            finally
            {
                BulutYazmaKilidi.Release();
            }
        }
        finally
        {
            _senkronYukleniyor = false;
            if (sifirlamaModu)
            {
                lock (BekleyenKayitlar)
                    BekleyenKayitlar.Clear();
                lock (BosYazmayaIzinliAnahtarlar)
                    BosYazmayaIzinliAnahtarlar.Clear();
            }
        }
    }

    public static async Task AnahtarBulutaGonderAsync(string anahtar, CancellationToken iptal = default)
    {
        if (!OturumYoneticisi.GirisYapildi || OturumYoneticisi.Firestore is null)
            return;

        if (!BelgeHaritasi.TryGetValue(anahtar, out var relYol))
            return;

        var yol = KiraciliBulutYolu(relYol);

        await BulutYazmaKilidi.WaitAsync(iptal);
        try
        {
            var bilincliBos = BilincliBosYazmaIzinliMi(anahtar);
            var json = Olustur(anahtar);
            // satinalma_talepler: boş [] asla eski disk ile doldurulmaz (sıfırlama geri dönüşü).
            if (!bilincliBos
                && anahtar != "satinalma_talepler"
                && (string.IsNullOrEmpty(json) || json is "[]" or "{}"))
                json = YerelJsonOku(anahtar) ?? json;

            string? talepBirlesikJson = null;
            // Sıfırlama: buluttaki eski talepleri birleştirip geri yükleme.
            if (anahtar == "satinalma_talepler" && !bilincliBos)
            {
                (json, talepBirlesikJson) = await TalepleriBulutaHazirlaAsync(yol, iptal);
            }
            else if (anahtar == "satinalma_ayarlar" && !bilincliBos)
            {
                json = await AyarlariBulutaHazirlaAsync(iptal);
            }
            else if (!bilincliBos &&
                     await BosListeBulutuEzmesinAsync(anahtar, json, yol, iptal).ConfigureAwait(false))
            {
                return;
            }

            await OturumYoneticisi.Firestore.BelgeJsonYazAsync(
                yol, json, OturumYoneticisi.Auth?.Uid, iptal);
            YerelOnbellegeYaz(anahtar, json);
            BulutSenkronZamani.Kaydet(anahtar, DateTime.UtcNow);
            BilincliBosYazmaIzniniTuket(anahtar);
            if (talepBirlesikJson is not null)
                SatinalmaDepo.TalepleriBirlestirVeYukle(talepBirlesikJson);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            HataGunlugu.Kaydet(ex, $"BulutVeriSenkronu.AnahtarBulutaGonder.{anahtar}");
        }
        finally
        {
            BulutYazmaKilidi.Release();
        }
    }

    public static string? DosyaAdindanAnahtar(string dosyaAdi) => dosyaAdi switch
    {
        "alinan_malzemeler.json" => "malzeme",
        "stok.json" => "stok",
        "stok_hareketleri.json" => "stok_hareket",
        "agrega.json" => "agrega",
        "cimento.json" => "cimento",
        "akaryakit.json" => "akaryakit",
        "filo.json" => "filo",
        "satinalma_talepler.json" => "satinalma_talepler",
        "satinalma_ayarlar.json" => "satinalma_ayarlar",
        "finansman_gelir.json" => "finansman",
        "uygulama_ayarlar.json" => "uygulama_ayarlar",
        _ => null
    };

    public static void SifirlemeyiBulutaPlanla(string dosyaAdi)
    {
        var anahtar = DosyaAdindanAnahtar(dosyaAdi);
        if (anahtar is null)
            return;

        lock (BosYazmayaIzinliAnahtarlar)
            BosYazmayaIzinliAnahtarlar.Add(anahtar);
        Planla(anahtar);
    }

    public static async Task BuluttanYukleAsync(CancellationToken iptal = default)
    {
        if (!OturumYoneticisi.GirisYapildi || OturumYoneticisi.Firestore is null)
            return;

        _senkronYukleniyor = true;
        try
        {
            foreach (var (anahtar, yol) in BelgeHaritasiSirali())
            {
                var (json, _) = await OturumYoneticisi.Firestore.BelgeOkuAsync(yol, iptal);
                if (json is null)
                    continue;

                await UiThreaddeCalistirAsync(() =>
                {
                    Uygula(anahtar, json);
                    var bos = json.Trim() is "[]" or "{}" or "null";
                    if (bos || _sifirlamaAktif)
                        YerelOnbellegeYaz(anahtar, json);
                    else
                        YerelBirlesikDurumuKaydet(anahtar);
                });
            }

            BuluttanYuklendi = true;
        }
        finally
        {
            _senkronYukleniyor = false;
        }
    }

    public static void YoklamayiBaslat()
    {
        if (!OturumYoneticisi.GirisYapildi)
            return;

        _yoklamaZamanlayici ??= new DispatcherTimer { Interval = YoklamaAraligi };
        _yoklamaZamanlayici.Tick -= YoklamaTik;
        _yoklamaZamanlayici.Tick += YoklamaTik;
        _yoklamaZamanlayici.Start();
    }

    public static void YoklamayiDurdur()
    {
        _yoklamaZamanlayici?.Stop();
    }

    public static async Task SimdiYenileAsync(CancellationToken iptal = default)
    {
        if (!OturumYoneticisi.GirisYapildi || OturumYoneticisi.Firestore is null)
            return;
        if (_sifirlamaAktif)
            return;

        await BuluttanCekAsync(SikYoklamaAnahtarlari, iptal);
        await BildirimYoneticisi.BildirimleriKontrolEtAsync();
    }

    /// <summary>Talep listesini buluttan anında çeker — bildirim kontrolünden önce.</summary>
    public static async Task TalepleriBuluttanCekAsync(CancellationToken iptal = default)
    {
        if (!OturumYoneticisi.GirisYapildi || OturumYoneticisi.Firestore is null)
            return;

        await BuluttanCekAsync(["satinalma_ayarlar", "satinalma_talepler"], iptal);
    }

    private static async void YoklamaTik(object? sender, EventArgs e)
    {
        if (_sifirlamaAktif || _senkronYukleniyor || OturumYoneticisi.Firestore is null)
            return;

        try
        {
            if (!LisansHalaGecerliMi())
            {
                YoklamayiDurdur();
                var dispatcher = Application.Current?.Dispatcher;
                if (dispatcher is null || dispatcher.CheckAccess())
                    LisansSuresiDolduCikis();
                else
                    _ = dispatcher.InvokeAsync(LisansSuresiDolduCikis);
                return;
            }

            var tamTarama = ++_yoklamaDongusu % 5 == 0;
            var anahtarlar = tamTarama
                ? BelgeHaritasi.Keys.ToArray()
                : SikYoklamaAnahtarlari;

            await BuluttanCekAsync(anahtarlar);
            _ = BildirimYoneticisi.BildirimleriKontrolEtAsync();
        }
        catch (Exception ex)
        {
            _senkronYukleniyor = false;
            HataGunlugu.Kaydet(ex, "BulutVeriSenkronu.YoklamaTik");
        }
    }

    private static bool LisansHalaGecerliMi()
    {
        var lisans = KiracıOturumu.Lisans;
        if (lisans is null)
            return true;
        lisans.KalanGunHesapla();
        return !lisans.SuresiDoldu && lisans.Aktif;
    }

    private static void LisansSuresiDolduCikis()
    {
        try
        {
            MessageBox.Show(
                "Firmanızın lisans süresi doldu. Oturum kapatılıyor.\nSatınalma Yönetici üzerinden lisans yenileyin.",
                "Lisans süresi doldu",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        catch
        {
            // UI yoksa sessiz çıkış
        }

        OturumYoneticisi.CikisYap();
    }

    private static async Task BuluttanCekAsync(IEnumerable<string> anahtarlar, CancellationToken iptal = default)
    {
        if (_sifirlamaAktif)
            return;

        foreach (var anahtar in anahtarlar.OrderBy(a => BulutGonderimOnceligi(a)))
        {
            try
            {
                if (!BelgeHaritasi.TryGetValue(anahtar, out var relYol))
                    continue;

                var yol = KiraciliBulutYolu(relYol);

                if (anahtar == "satinalma_talepler")
                    await SatinalmaAyarlariniBuluttanOncelikleAsync(iptal);

                var (json, guncelleme) = await OturumYoneticisi.Firestore!.BelgeOkuAsync(yol, iptal);
                if (json is null)
                    continue;

                var bosBulut = json.Trim() is "[]" or "{}" or "null";
                // Boş bulut her zaman uygulanır (sıfırlama); dolu belgede zaman damgası kontrolü kalır.
                if (!bosBulut && !BulutSenkronZamani.YeniVeriVar(anahtar, guncelleme))
                    continue;

                _senkronYukleniyor = true;
                await UiThreaddeCalistirAsync(() =>
                {
                    Uygula(anahtar, json);
                    // Boş bulutta birleşik bellek yazımı eski veriyi diske geri basmasın.
                    if (bosBulut)
                        YerelOnbellegeYaz(anahtar, json);
                    else
                        YerelBirlesikDurumuKaydet(anahtar);
                    if (guncelleme.HasValue)
                        BulutSenkronZamani.Kaydet(anahtar, guncelleme.Value);
                });
                _senkronYukleniyor = false;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _senkronYukleniyor = false;
                HataGunlugu.Kaydet(ex, $"BulutVeriSenkronu.BuluttanCek.{anahtar}");
            }
        }
    }

    private static async void ZamanlayiciTik(object? sender, EventArgs e)
    {
        _zamanlayici?.Stop();
        try
        {
            if (_senkronYukleniyor)
            {
                // Senkron sürerken bekleyen yazmalar kaybolmasın — kısa sonra tekrar dene.
                _zamanlayici ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1200) };
                _zamanlayici.Start();
                return;
            }

            await BulutaGonderAsync();
        }
        catch (Exception ex)
        {
            HataGunlugu.Kaydet(ex, "BulutVeriSenkronu.ZamanlayiciTik");
        }
    }

    public static async Task SilmeSonrasiGonderAsync(CancellationToken iptal = default)
    {
        if (!OturumYoneticisi.GirisYapildi || !KullaniciYetkileri.Duzenleyebilir
            || OturumYoneticisi.Firestore is null)
            return;

        await AnahtarBulutaGonderAsync("satinalma_ayarlar", iptal);
        await AnahtarBulutaGonderAsync("satinalma_talepler", iptal);
    }

    public static async Task TalepleriHemenGonderAsync(CancellationToken iptal = default)
    {
        if (!OturumYoneticisi.GirisYapildi || !KullaniciYetkileri.Duzenleyebilir
            || OturumYoneticisi.Firestore is null)
            return;

        await AnahtarBulutaGonderAsync("satinalma_ayarlar", iptal);
        await AnahtarBulutaGonderAsync("satinalma_talepler", iptal);
    }

    public static async Task AyarlariHemenGonderAsync(CancellationToken iptal = default)
    {
        if (!OturumYoneticisi.GirisYapildi || !KullaniciYetkileri.Duzenleyebilir
            || OturumYoneticisi.Firestore is null)
            return;

        await AnahtarBulutaGonderAsync("satinalma_ayarlar", iptal);
    }

    /// <summary>Mal kabul sonrası Alınan Malzemeler + stok belgelerini anında buluta yazar.</summary>
    public static async Task MalKabulSonrasiHemenGonderAsync(CancellationToken iptal = default)
    {
        if (!OturumYoneticisi.GirisYapildi || OturumYoneticisi.Firestore is null)
            return;

        await AnahtarBulutaGonderAsync("satinalma_ayarlar", iptal);
        await AnahtarBulutaGonderAsync("satinalma_talepler", iptal);
        await AnahtarBulutaGonderAsync("malzeme", iptal);
        await AnahtarBulutaGonderAsync("stok", iptal);
        await AnahtarBulutaGonderAsync("stok_hareket", iptal);
    }

    public static async Task BulutaGonderAsync(CancellationToken iptal = default)
    {
        if (_senkronYukleniyor || !OturumYoneticisi.GirisYapildi || !KullaniciYetkileri.Duzenleyebilir)
            return;

        string[] anahtarlar;
        lock (BekleyenKayitlar)
        {
            anahtarlar = BekleyenKayitlar.ToArray();
            BekleyenKayitlar.Clear();
        }

        if (anahtarlar.Length == 0 || OturumYoneticisi.Firestore is null)
            return;

        Array.Sort(anahtarlar, (a, b) => BulutGonderimOnceligi(a).CompareTo(BulutGonderimOnceligi(b)));

        foreach (var anahtar in anahtarlar)
        {
            if (!BelgeHaritasi.TryGetValue(anahtar, out var relYol))
                continue;

            var yol = KiraciliBulutYolu(relYol);
            var bilincliBos = BilincliBosYazmaIzinliMi(anahtar);

            var json = Olustur(anahtar);
            if (!bilincliBos
                && anahtar != "satinalma_talepler"
                && (string.IsNullOrEmpty(json) || json is "[]" or "{}"))
                json = YerelJsonOku(anahtar) ?? json;

            string? talepBirlesikJson = null;
            if (anahtar == "satinalma_talepler" && !bilincliBos)
            {
                (json, talepBirlesikJson) = await TalepleriBulutaHazirlaAsync(yol, iptal);
            }
            else if (anahtar == "satinalma_ayarlar" && !bilincliBos)
            {
                json = await AyarlariBulutaHazirlaAsync(iptal);
            }
            else if (!bilincliBos &&
                     await BosListeBulutuEzmesinAsync(anahtar, json, yol, iptal).ConfigureAwait(false))
            {
                continue;
            }

            await OturumYoneticisi.Firestore.BelgeJsonYazAsync(
                yol, json, OturumYoneticisi.Auth?.Uid, iptal);
            YerelOnbellegeYaz(anahtar, json);
            BulutSenkronZamani.Kaydet(anahtar, DateTime.UtcNow);
            BilincliBosYazmaIzniniTuket(anahtar);
            if (talepBirlesikJson is not null)
                SatinalmaDepo.TalepleriBirlestirVeYukle(talepBirlesikJson);
        }
    }

    private static readonly Dictionary<string, string> BelgeHaritasi = new(StringComparer.Ordinal)
    {
        ["malzeme"] = "veri/alinan_malzemeler",
        ["stok"] = "veri/stok",
        ["stok_hareket"] = "veri/stok_hareketleri",
        ["agrega"] = "veri/agrega",
        ["cimento"] = "veri/cimento",
        ["akaryakit"] = "veri/akaryakit",
        ["filo"] = "veri/filo",
        ["satinalma_talepler"] = "veri/satinalma_talepler",
        ["satinalma_ayarlar"] = "veri/satinalma_ayarlar",
        ["finansman"] = "veri/finansman_gelir",
        ["uygulama_ayarlar"] = "veri/uygulama_ayarlar",
        ["iade_kayitlari"] = "veri/iade_kayitlari"
    };

    private static string YerelDosyaYolu(string anahtar)
    {
        var tid = KiracıOturumu.TenantId;
        string Dosya(string ad)
        {
            if (string.IsNullOrWhiteSpace(tid))
                return SatinalmaProKlasor.DosyaYolu(ad);
            var ad2 = $"{Path.GetFileNameWithoutExtension(ad)}_{tid}{Path.GetExtension(ad)}";
            return SatinalmaProKlasor.DosyaYolu(ad2);
        }

        return anahtar switch
        {
            "malzeme" => Dosya("alinan_malzemeler.json"),
            "stok" => Dosya("stok.json"),
            "stok_hareket" => Dosya("stok_hareketleri.json"),
            "agrega" => Dosya("agrega.json"),
            "cimento" => Dosya("cimento.json"),
            "akaryakit" => Dosya("akaryakit.json"),
            "filo" => Dosya("filo.json"),
            "satinalma_talepler" => Dosya("satinalma_talepler.json"),
            "satinalma_ayarlar" => Dosya("satinalma_ayarlar.json"),
            "finansman" => Dosya("finansman_gelir.json"),
            "uygulama_ayarlar" => Dosya("uygulama_ayarlar.json"),
            "iade_kayitlari" => Dosya("iade_kayitlari.json"),
            _ => Dosya($"{anahtar}.json")
        };
    }

    /// <summary>Bulut verisi uygulandıktan sonra birleşik bellek durumunu diske yazar.</summary>
    private static void YerelBirlesikDurumuKaydet(string anahtar)
    {
        var yol = YerelDosyaYolu(anahtar);
        SatinalmaProKlasor.Olustur();
        File.WriteAllText(yol, Olustur(anahtar));
    }

    private static void YerelOnbellegeYaz(string anahtar, string json)
    {
        var yol = YerelDosyaYolu(anahtar);
        SatinalmaProKlasor.Olustur();
        File.WriteAllText(yol, json);
    }

    private static string Olustur(string anahtar) => anahtar switch
    {
        "malzeme" => JsonSerializer.Serialize(ModulVeriDeposu.AlinanMalzemeler.ToList(), JsonSecenekleri),
        "stok" => JsonSerializer.Serialize(ModulVeriDeposu.Stok.ToList(), JsonSecenekleri),
        "stok_hareket" => JsonSerializer.Serialize(ModulVeriDeposu.StokHareketleri.ToList(), JsonSecenekleri),
        "agrega" => JsonSerializer.Serialize(ModulVeriDeposu.Agrega.ToList(), JsonSecenekleri),
        "cimento" => JsonSerializer.Serialize(ModulVeriDeposu.Cimento.ToList(), JsonSecenekleri),
        "akaryakit" => JsonSerializer.Serialize(ModulVeriDeposu.Akaryakit.ToList(), JsonSecenekleri),
        "filo" => JsonSerializer.Serialize(new FiloVeriPaketi
        {
            Araclar = ModulVeriDeposu.FiloAraclari.ToList(),
            Giderler = ModulVeriDeposu.FiloGiderleri.ToList(),
            Zimmetler = ModulVeriDeposu.FiloZimmetleri.ToList()
        }, JsonSecenekleri),
        "satinalma_talepler" => JsonSerializer.Serialize(SatinalmaDepo.Talepler.ToList(), JsonSecenekleri),
        "satinalma_ayarlar" => JsonSerializer.Serialize(SatinalmaDepo.Ayarlar, JsonSecenekleri),
        "finansman" => JsonSerializer.Serialize(FinansmanVeriDeposu.Gelirler.ToList(), JsonSecenekleri),
        "uygulama_ayarlar" => JsonSerializer.Serialize(UygulamaAyarDeposu.Ayarlar, JsonSecenekleri),
        "iade_kayitlari" => IadeDeposu.JsonOlustur(),
        _ => "[]"
    };

    private static void Uygula(string anahtar, string json)
    {
        ModulVeriDeposu.BulutYuklemesiBaslat();
        try
        {
            switch (anahtar)
            {
                case "malzeme":
                    ModulVeriDeposu.AlinanMalzemeleriYukle(json);
                    break;
                case "stok":
                    ModulVeriDeposu.StokYukle(json);
                    break;
                case "stok_hareket":
                    ModulVeriDeposu.StokHareketleriYukle(json);
                    break;
                case "agrega":
                    ModulVeriDeposu.AgregaYukle(json);
                    break;
                case "cimento":
                    ModulVeriDeposu.CimentoYukle(json);
                    break;
                case "akaryakit":
                    ModulVeriDeposu.AkaryakitYukle(json);
                    break;
                case "filo":
                    ModulVeriDeposu.FiloYukle(json);
                    break;
                case "satinalma_talepler":
                    SatinalmaAyarlariniDisktenYenile();
                    // Boş bulut veya sıfırlama: birleştirme yok — disk/bellek eski talepleri geri getirmesin.
                    var talepBos = string.IsNullOrWhiteSpace(json) || json.Trim() is "[]";
                    SatinalmaDepo.TalepleriBirlestirVeYukle(
                        json,
                        yerelBirlestir: !talepBos && !_sifirlamaAktif);
                    break;
                case "satinalma_ayarlar":
                    // Bulut bu kiracının kaynağı — birleştirme diğer firmanın imza isimlerini sızdırıyordu.
                    SatinalmaDepo.AyarlarYukle(json, birlestir: false);
                    break;
                case "finansman":
                    FinansmanVeriDeposu.GelirleriYukle(json);
                    break;
                case "uygulama_ayarlar":
                    UygulamaAyarDeposu.BuluttanYukle(json);
                    break;
                case "iade_kayitlari":
                    IadeDeposu.BuluttanYukle(json);
                    break;
            }
        }
        finally
        {
            ModulVeriDeposu.BulutYuklemesiBitir();
        }
    }

    private static async Task<SatinalmaAyarlar?> BulutAyarlariniOkuAsync(CancellationToken iptal = default)
    {
        if (OturumYoneticisi.Firestore is null)
            return null;

        try
        {
            var (json, _, docStamp) = await OturumYoneticisi.Firestore
                .BelgeOkuDetayAsync(KiraciliBulutYolu(BelgeHaritasi["satinalma_ayarlar"]), iptal);
            if (string.IsNullOrWhiteSpace(json) && docStamp <= 0)
                return null;

            var ayar = string.IsNullOrWhiteSpace(json)
                ? new SatinalmaAyarlar()
                : JsonSerializer.Deserialize<SatinalmaAyarlar>(json, JsonSecenekleri) ?? new SatinalmaAyarlar();
            ayar.VeriSifirlamaUtc = Math.Max(ayar.VeriSifirlamaUtc, docStamp);
            return ayar;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Sıfırlama damgası sonrası eski yerel talepleri buluta geri yazmaz.
    /// Boş bulutta yalnızca post-reset kayıtlar (ör. yeni açılan talep) gönderilir.
    /// </summary>
    private static async Task<(string json, string? birlesikJson)> TalepleriBulutaHazirlaAsync(
        string yol, CancellationToken iptal)
    {
        var bulutAyarlar = await BulutAyarlariniOkuAsync(iptal);
        var resetUtc = Math.Max(
            SatinalmaDepo.Ayarlar.VeriSifirlamaUtc,
            bulutAyarlar?.VeriSifirlamaUtc ?? 0);
        if (resetUtc > SatinalmaDepo.Ayarlar.VeriSifirlamaUtc)
            SatinalmaDepo.Ayarlar.VeriSifirlamaUtc = resetUtc;

        var yerel = SatinalmaDepo.Talepler
            .Where(t => resetUtc <= 0 || t.GuncellemeUtc >= resetUtc)
            .ToList();

        var (bulutJson, _, talepDocStamp) = await OturumYoneticisi.Firestore!
            .BelgeOkuDetayAsync(yol, iptal);
        if (talepDocStamp > resetUtc)
        {
            resetUtc = talepDocStamp;
            SatinalmaDepo.Ayarlar.VeriSifirlamaUtc = Math.Max(
                SatinalmaDepo.Ayarlar.VeriSifirlamaUtc, resetUtc);
            yerel = SatinalmaDepo.Talepler
                .Where(t => t.GuncellemeUtc >= resetUtc)
                .ToList();
        }

        var bulutBos = string.IsNullOrWhiteSpace(bulutJson) || bulutJson.Trim() is "[]";
        if (bulutBos)
        {
            var json = JsonSerializer.Serialize(yerel, JsonSecenekleri);
            return (json, json);
        }

        var bulut = JsonSerializer.Deserialize<List<SatinalmaTalep>>(bulutJson!, JsonSecenekleri) ?? [];
        if (resetUtc > 0)
            bulut = bulut.Where(t => t.GuncellemeUtc >= resetUtc).ToList();
        var silinen = SatinalmaTalepSenkronYardimcisi.SilinenleriBirlestir(
            SatinalmaDepo.Ayarlar.SilinenTalepIdleri,
            bulutAyarlar?.SilinenTalepIdleri);
        SatinalmaDepo.Ayarlar.SilinenTalepIdleri = silinen;
        var birlesik = SatinalmaTalepBirlestirme.Birlestir(yerel, bulut, silinen);
        var birlesikJson = JsonSerializer.Serialize(birlesik, JsonSecenekleri);
        return (birlesikJson, birlesikJson);
    }

    /// <summary>Ayar yazarken sıfırlama damgasının düşürülmesini engeller.</summary>
    private static async Task<string> AyarlariBulutaHazirlaAsync(CancellationToken iptal)
    {
        var bulutAyarlar = await BulutAyarlariniOkuAsync(iptal);
        if (bulutAyarlar is not null)
        {
            SatinalmaDepo.Ayarlar.SilinenTalepIdleri = SatinalmaTalepSenkronYardimcisi.SilinenleriBirlestir(
                SatinalmaDepo.Ayarlar.SilinenTalepIdleri,
                bulutAyarlar.SilinenTalepIdleri);
            SatinalmaDepo.Ayarlar.SonTalepSira = Math.Max(
                SatinalmaDepo.Ayarlar.SonTalepSira, bulutAyarlar.SonTalepSira);
            SatinalmaDepo.Ayarlar.SonSiparisSira = Math.Max(
                SatinalmaDepo.Ayarlar.SonSiparisSira, bulutAyarlar.SonSiparisSira);
            SatinalmaDepo.Ayarlar.SonIadeSira = Math.Max(
                SatinalmaDepo.Ayarlar.SonIadeSira, bulutAyarlar.SonIadeSira);
            SatinalmaDepo.Ayarlar.VeriSifirlamaUtc = Math.Max(
                SatinalmaDepo.Ayarlar.VeriSifirlamaUtc, bulutAyarlar.VeriSifirlamaUtc);
        }

        return JsonSerializer.Serialize(SatinalmaDepo.Ayarlar, JsonSecenekleri);
    }

    private static string? YerelJsonOku(string anahtar)
    {
        var yol = YerelDosyaYolu(anahtar);
        return File.Exists(yol) ? File.ReadAllText(yol) : null;
    }

    private static bool JsonAnlamliMi(string? json, string anahtar)
    {
        if (string.IsNullOrWhiteSpace(json))
            return false;

        var trimmed = json.Trim();
        if (trimmed is "[]" or "{}" or "null")
            return false;

        if (anahtar == "uygulama_ayarlar")
        {
            try
            {
                var ayarlar = JsonSerializer.Deserialize<UygulamaAyarlar>(json, JsonSecenekleri);
                return ayarlar is not null && (
                    !string.IsNullOrWhiteSpace(ayarlar.FirmaAdi) ||
                    !string.IsNullOrWhiteSpace(ayarlar.LogoDosyaYolu) ||
                    !string.IsNullOrWhiteSpace(ayarlar.AnasayfaLogoDosyaYolu) ||
                    ayarlar.MalzemeKategorileri.Count > 0 ||
                    ayarlar.MalzemeBirimleri.Count > 0);
            }
            catch
            {
                return false;
            }
        }

        if (anahtar == "satinalma_ayarlar")
            return !SatinalmaAyarlariBosMu(json);

        return trimmed.Length > 2;
    }

    /// <summary>Yeni firma iskeleti: imza/şartname/sayaç yok — buluta şablon yazılmasın.</summary>
    private static bool SatinalmaAyarlariBosMu(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return true;

        try
        {
            var ayar = JsonSerializer.Deserialize<SatinalmaAyarlar>(json, JsonSecenekleri);
            if (ayar is null)
                return true;

            if (ayar.ImzaAyarleriTemiz)
                return ayar.SonTalepSira <= 0
                       && ayar.SonSiparisSira <= 0
                       && ayar.SonIadeSira <= 0
                       && (ayar.SilinenTalepIdleri is null || ayar.SilinenTalepIdleri.Count == 0)
                       && string.IsNullOrWhiteSpace(ayar.SartnameMetni)
                       && string.IsNullOrWhiteSpace(ayar.TeklifIstemeSartnameleri);

            var sef = ayar.SefImzalari?.Count ?? 0;
            var yonetim = ayar.YonetimImzalari?.Count ?? 0;
            return sef == 0 && yonetim == 0
                   && ayar.SonTalepSira <= 0
                   && ayar.SonSiparisSira <= 0
                   && ayar.SonIadeSira <= 0
                   && (ayar.SilinenTalepIdleri is null || ayar.SilinenTalepIdleri.Count == 0)
                   && string.IsNullOrWhiteSpace(ayar.SartnameMetni)
                   && string.IsNullOrWhiteSpace(ayar.TeklifIstemeSartnameleri);
        }
        catch
        {
            return false;
        }
    }

    private static bool ListeModuluMu(string anahtar) =>
        anahtar is "malzeme" or "stok" or "stok_hareket" or "agrega" or "cimento"
            or "akaryakit" or "filo" or "finansman";

    private static bool BilincliBosYazmaIzinliMi(string anahtar)
    {
        lock (BosYazmayaIzinliAnahtarlar)
            return BosYazmayaIzinliAnahtarlar.Contains(anahtar);
    }

    private static void BilincliBosYazmaIzniniTuket(string anahtar)
    {
        lock (BosYazmayaIzinliAnahtarlar)
            BosYazmayaIzinliAnahtarlar.Remove(anahtar);
    }

    /// <summary>
    /// Bellek/yerel boşken dolu bulut belgesini [] ile ezmeyi engeller.
    /// Modül sıfırlama (SifirlemeyiBulutaPlanla) bilinçli boş yazmaya izin verir.
    /// </summary>
    private static async Task<bool> BosListeBulutuEzmesinAsync(
        string anahtar, string json, string yol, CancellationToken iptal)
    {
        if (!ListeModuluMu(anahtar) || JsonAnlamliMi(json, anahtar))
            return false;

        lock (BosYazmayaIzinliAnahtarlar)
        {
            if (BosYazmayaIzinliAnahtarlar.Contains(anahtar))
                return false;
        }

        try
        {
            var (bulutJson, _) = await OturumYoneticisi.Firestore!
                .BelgeOkuAsync(yol, iptal)
                .ConfigureAwait(false);
            if (JsonAnlamliMi(bulutJson, anahtar))
                return true;
        }
        catch
        {
            // okuma hatasında yazmayı engelleme
        }

        return false;
    }

    private static string KiraciliBulutYolu(string veriYolu) =>
        $"{FirestoreYollari.TenantKok(KiracıOturumu.ZorunluTenantId())}/{veriYolu}";

    private static IEnumerable<KeyValuePair<string, string>> BelgeHaritasiSirali()
    {
        string[] oncelik = ["satinalma_ayarlar", "satinalma_talepler"];
        var islenen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var anahtar in oncelik)
        {
            if (BelgeHaritasi.TryGetValue(anahtar, out var yol))
            {
                islenen.Add(anahtar);
                yield return new KeyValuePair<string, string>(anahtar, KiraciliBulutYolu(yol));
            }
        }

        foreach (var kv in BelgeHaritasi)
        {
            if (islenen.Contains(kv.Key))
                continue;
            yield return new KeyValuePair<string, string>(kv.Key, KiraciliBulutYolu(kv.Value));
        }
    }

    private static int BulutGonderimOnceligi(string? anahtar) => anahtar switch
    {
        "satinalma_ayarlar" => 0,
        "satinalma_talepler" => 1,
        _ => 2
    };

    private static void SatinalmaAyarlariniDisktenYenile()
    {
        var yol = YerelDosyaYolu("satinalma_ayarlar");
        if (!File.Exists(yol))
            return;

        try
        {
            SatinalmaDepo.AyarlarYukle(File.ReadAllText(yol), birlestir: false);
        }
        catch
        {
            // disk okunamazsa bellekteki ayarlarla devam
        }
    }

    private static async Task SatinalmaAyarlariniBuluttanOncelikleAsync(CancellationToken iptal)
    {
        if (OturumYoneticisi.Firestore is null)
            return;

        try
        {
            var (json, _) = await OturumYoneticisi.Firestore
                .BelgeOkuAsync(KiraciliBulutYolu(BelgeHaritasi["satinalma_ayarlar"]), iptal);
            if (!string.IsNullOrWhiteSpace(json))
                SatinalmaDepo.AyarlarYukle(json, birlestir: false);
        }
        catch
        {
            SatinalmaAyarlariniDisktenYenile();
        }
    }
}

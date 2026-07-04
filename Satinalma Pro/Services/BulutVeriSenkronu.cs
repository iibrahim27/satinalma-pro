using System.IO;
using System.Threading;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Threading;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;

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
    private static DispatcherTimer? _zamanlayici;
    private static bool _senkronYukleniyor;
    private static DispatcherTimer? _yoklamaZamanlayici;
    private static int _yoklamaDongusu;

    private static readonly string[] SikYoklamaAnahtarlari = ["satinalma_ayarlar", "satinalma_talepler"];
    private static readonly TimeSpan YoklamaAraligi = TimeSpan.FromSeconds(30);

    public static bool BuluttanYuklendi { get; private set; }

    public static void Planla(string anahtar)
    {
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

        _senkronYukleniyor = true;
        var toplam = BelgeHaritasi.Count + 1;
        var tamamlanan = 0;

        try
        {
            foreach (var (anahtar, yol) in BelgeHaritasiSirali())
            {
                iptal.ThrowIfCancellationRequested();
                ilerleme?.Report((tamamlanan, toplam, SenkronAdimMetni(anahtar)));

                try
                {
                    var (bulutJson, _) = await OturumYoneticisi.Firestore
                        .BelgeOkuAsync(yol, iptal)
                        .ConfigureAwait(false);
                    var yerelJson = YerelJsonOku(anahtar);

                    // Belge Firestore'da varsa (boş [] dahil) bulutu uygula — aksi halde PC2 eski veriyi geri yükler
                    var bulutBelgesiVar = bulutJson is not null;
                    var yereldeVar = JsonAnlamliMi(yerelJson, anahtar);

                    if (bulutBelgesiVar)
                    {
                        await UiThreaddeCalistirAsync(() =>
                        {
                            Uygula(anahtar, bulutJson!);
                            YerelBirlesikDurumuKaydet(anahtar);
                        });
                        BulutSenkronZamani.Kaydet(anahtar, DateTime.UtcNow);
                    }
                    else if (yereldeVar && yerelJson is not null)
                    {
                        await OturumYoneticisi.Firestore.BelgeJsonYazAsync(
                            yol, yerelJson, OturumYoneticisi.Auth?.Uid, iptal)
                            .ConfigureAwait(false);
                    }
                    else if (yerelJson is not null)
                    {
                        // Yerel sıfırlandı, bulutta kayıt yok — boş durumu buluta yaz
                        await OturumYoneticisi.Firestore.BelgeJsonYazAsync(
                            yol, yerelJson, OturumYoneticisi.Auth?.Uid, iptal)
                            .ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch
                {
                    // Tek kayıt hatası tüm senkronu durdurmasın
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
            catch
            {
                // logo senkronu isteğe bağlı
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

    public static async Task TumVerileriBulutaGonderAsync(CancellationToken iptal = default)
    {
        if (!OturumYoneticisi.GirisYapildi || !KullaniciYetkileri.Duzenleyebilir)
            return;

        _senkronYukleniyor = true;
        try
        {
            foreach (var (anahtar, yol) in BelgeHaritasiSirali())
            {
                var json = Olustur(anahtar);
                if (string.IsNullOrWhiteSpace(json) || json is "[]" or "{}")
                    json = YerelJsonOku(anahtar) ?? json;

                await OturumYoneticisi.Firestore!.BelgeJsonYazAsync(
                    yol, json, OturumYoneticisi.Auth?.Uid, iptal);
            }

            await MedyaBulutSenkronu.BulutaYukleAsync(iptal);
        }
        finally
        {
            _senkronYukleniyor = false;
        }
    }

    public static async Task AnahtarBulutaGonderAsync(string anahtar, CancellationToken iptal = default)
    {
        if (!OturumYoneticisi.GirisYapildi || OturumYoneticisi.Firestore is null)
            return;

        if (!BelgeHaritasi.TryGetValue(anahtar, out var yol))
            return;

        await BulutYazmaKilidi.WaitAsync(iptal);
        try
        {
            var json = Olustur(anahtar);
            if (string.IsNullOrEmpty(json) || json is "[]" or "{}")
                json = YerelJsonOku(anahtar) ?? json;

            string? talepBirlesikJson = null;
            if (anahtar == "satinalma_talepler")
            {
                var (bulutJson, _) = await OturumYoneticisi.Firestore.BelgeOkuAsync(yol, iptal);
                if (!string.IsNullOrWhiteSpace(bulutJson))
                {
                    var bulut = JsonSerializer.Deserialize<List<SatinalmaTalep>>(bulutJson, JsonSecenekleri) ?? [];
                    var bulutAyarlar = await BulutAyarlariniOkuAsync(iptal);
                    var silinen = SatinalmaTalepSenkronYardimcisi.SilinenleriBirlestir(
                        SatinalmaDepo.Ayarlar.SilinenTalepIdleri,
                        bulutAyarlar?.SilinenTalepIdleri);
                    SatinalmaDepo.Ayarlar.SilinenTalepIdleri = silinen;
                    var birlesik = SatinalmaTalepBirlestirme.Birlestir(SatinalmaDepo.Talepler, bulut, silinen);
                    talepBirlesikJson = JsonSerializer.Serialize(birlesik, JsonSecenekleri);
                    json = talepBirlesikJson;
                }
            }
            else if (anahtar == "satinalma_ayarlar")
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
                    json = JsonSerializer.Serialize(SatinalmaDepo.Ayarlar, JsonSecenekleri);
                }
            }

            await OturumYoneticisi.Firestore.BelgeJsonYazAsync(
                yol, json, OturumYoneticisi.Auth?.Uid, iptal);
            YerelOnbellegeYaz(anahtar, json);
            BulutSenkronZamani.Kaydet(anahtar, DateTime.UtcNow);
            if (talepBirlesikJson is not null)
                SatinalmaDepo.TalepleriYukle(talepBirlesikJson);
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
        if (anahtar is not null)
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

                Uygula(anahtar, json);
                YerelBirlesikDurumuKaydet(anahtar);
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
        if (_senkronYukleniyor || OturumYoneticisi.Firestore is null)
            return;

        try
        {
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

    private static async Task BuluttanCekAsync(IEnumerable<string> anahtarlar, CancellationToken iptal = default)
    {
        foreach (var anahtar in anahtarlar.OrderBy(a => BulutGonderimOnceligi(a)))
        {
            if (!BelgeHaritasi.TryGetValue(anahtar, out var yol))
                continue;

            if (anahtar == "satinalma_talepler")
                await SatinalmaAyarlariniBuluttanOncelikleAsync(iptal);

            var (json, guncelleme) = await OturumYoneticisi.Firestore!.BelgeOkuAsync(yol, iptal);
            if (json is null)
                continue;

            if (!BulutSenkronZamani.YeniVeriVar(anahtar, guncelleme))
                continue;

            _senkronYukleniyor = true;
            await UiThreaddeCalistirAsync(() =>
            {
                Uygula(anahtar, json);
                YerelBirlesikDurumuKaydet(anahtar);
                if (guncelleme.HasValue)
                    BulutSenkronZamani.Kaydet(anahtar, guncelleme.Value);
            });
            _senkronYukleniyor = false;
        }
    }

    private static async void ZamanlayiciTik(object? sender, EventArgs e)
    {
        _zamanlayici?.Stop();
        try
        {
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
            if (!BelgeHaritasi.TryGetValue(anahtar, out var yol))
                continue;

            var json = Olustur(anahtar);
            string? talepBirlesikJson = null;
            if (anahtar == "satinalma_talepler")
            {
                var (bulutJson, _) = await OturumYoneticisi.Firestore.BelgeOkuAsync(yol, iptal);
                if (!string.IsNullOrWhiteSpace(bulutJson))
                {
                    var bulut = JsonSerializer.Deserialize<List<SatinalmaTalep>>(bulutJson, JsonSecenekleri) ?? [];
                    var bulutAyarlar = await BulutAyarlariniOkuAsync(iptal);
                    var silinen = SatinalmaTalepSenkronYardimcisi.SilinenleriBirlestir(
                        SatinalmaDepo.Ayarlar.SilinenTalepIdleri,
                        bulutAyarlar?.SilinenTalepIdleri);
                    SatinalmaDepo.Ayarlar.SilinenTalepIdleri = silinen;
                    var birlesik = SatinalmaTalepBirlestirme.Birlestir(SatinalmaDepo.Talepler, bulut, silinen);
                    talepBirlesikJson = JsonSerializer.Serialize(birlesik, JsonSecenekleri);
                    json = talepBirlesikJson;
                }
            }
            else if (anahtar == "satinalma_ayarlar")
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
                    json = JsonSerializer.Serialize(SatinalmaDepo.Ayarlar, JsonSecenekleri);
                }
            }

            await OturumYoneticisi.Firestore.BelgeJsonYazAsync(
                yol, json, OturumYoneticisi.Auth?.Uid, iptal);
            YerelOnbellegeYaz(anahtar, json);
            BulutSenkronZamani.Kaydet(anahtar, DateTime.UtcNow);
            if (talepBirlesikJson is not null)
                SatinalmaDepo.TalepleriYukle(talepBirlesikJson);
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
        ["uygulama_ayarlar"] = "veri/uygulama_ayarlar"
    };

    private static string YerelDosyaYolu(string anahtar) => anahtar switch
    {
        "malzeme" => SatinalmaProKlasor.DosyaYolu("alinan_malzemeler.json"),
        "stok" => SatinalmaProKlasor.DosyaYolu("stok.json"),
        "stok_hareket" => SatinalmaProKlasor.DosyaYolu("stok_hareketleri.json"),
        "agrega" => SatinalmaProKlasor.DosyaYolu("agrega.json"),
        "cimento" => SatinalmaProKlasor.DosyaYolu("cimento.json"),
        "akaryakit" => SatinalmaProKlasor.DosyaYolu("akaryakit.json"),
        "filo" => SatinalmaProKlasor.DosyaYolu("filo.json"),
        "satinalma_talepler" => SatinalmaProKlasor.DosyaYolu("satinalma_talepler.json"),
        "satinalma_ayarlar" => SatinalmaProKlasor.DosyaYolu("satinalma_ayarlar.json"),
        "finansman" => SatinalmaProKlasor.DosyaYolu("finansman_gelir.json"),
        "uygulama_ayarlar" => SatinalmaProKlasor.DosyaYolu("uygulama_ayarlar.json"),
        _ => SatinalmaProKlasor.DosyaYolu($"{anahtar}.json")
    };

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
                    SatinalmaDepo.TalepleriBirlestirVeYukle(json);
                    break;
                case "satinalma_ayarlar":
                    SatinalmaDepo.AyarlarYukle(json);
                    break;
                case "finansman":
                    FinansmanVeriDeposu.GelirleriYukle(json);
                    break;
                case "uygulama_ayarlar":
                    UygulamaAyarDeposu.BuluttanYukle(json);
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
            var (json, _) = await OturumYoneticisi.Firestore
                .BelgeOkuAsync(BelgeHaritasi["satinalma_ayarlar"], iptal);
            if (string.IsNullOrWhiteSpace(json))
                return null;

            return JsonSerializer.Deserialize<SatinalmaAyarlar>(json, JsonSecenekleri);
        }
        catch
        {
            return null;
        }
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
                    ayarlar.MalzemeKategorileri.Count > 0);
            }
            catch
            {
                return false;
            }
        }

        return trimmed.Length > 2;
    }

    private static IEnumerable<KeyValuePair<string, string>> BelgeHaritasiSirali()
    {
        string[] oncelik = ["satinalma_ayarlar", "satinalma_talepler"];
        var islenen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var anahtar in oncelik)
        {
            if (BelgeHaritasi.TryGetValue(anahtar, out var yol))
            {
                islenen.Add(anahtar);
                yield return new KeyValuePair<string, string>(anahtar, yol);
            }
        }

        foreach (var kv in BelgeHaritasi)
        {
            if (islenen.Contains(kv.Key))
                continue;
            yield return kv;
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
            SatinalmaDepo.AyarlarYukle(File.ReadAllText(yol));
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
                .BelgeOkuAsync(BelgeHaritasi["satinalma_ayarlar"], iptal);
            if (!string.IsNullOrWhiteSpace(json))
                SatinalmaDepo.AyarlarYukle(json);
        }
        catch
        {
            SatinalmaAyarlariniDisktenYenile();
        }
    }
}

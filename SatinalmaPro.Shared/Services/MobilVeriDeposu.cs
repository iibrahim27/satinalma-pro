using System.Text.Json;
using System.Text.Json.Serialization;
using SatinalmaPro.Shared.Helpers;
using SatinalmaPro.Shared.Models;
using SatinalmaPro.Shared.Services.Firebase;

namespace SatinalmaPro.Shared.Services;

public sealed class MobilVeriDeposu
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly TimeSpan TamSenkronBekleme = TimeSpan.FromSeconds(45);
    private static readonly SemaphoreSlim TalepBulutYazmaKilidi = new(1, 1);
    private static readonly SemaphoreSlim BildirimBulutYazmaKilidi = new(1, 1);

    private readonly FirestoreVeriServisi _firestore;
    private readonly FirebaseAuthServisi _auth;
    private DateTime? _sonTamSenkron;

    public List<SatinalmaTalep> Talepler { get; } = [];
    public SatinalmaAyarlar Ayarlar { get; private set; } = new();
    public List<StokKaydi> Stok { get; } = [];
    public List<StokHareketKaydi> StokHareketleri { get; } = [];
    public List<BildirimKaydi> Bildirimler { get; } = [];
    public List<string> AlinanMalzemeAdlari { get; } = [];

    public KullaniciProfili? AktifKullanici { get; private set; }

    public bool OfflineMod { get; private set; }
    public bool SonSenkronBasarili { get; private set; } = true;
    public DateTime? SonSenkronZamani { get; private set; }
    public string? SonSenkronHata { get; private set; }

    public MobilVeriDeposu(FirestoreVeriServisi firestore, FirebaseAuthServisi auth)
    {
        _firestore = firestore;
        _auth = auth;
    }

    public void AktifKullaniciyiAyarla(KullaniciProfili? profil) => AktifKullanici = profil;

    public async Task SenkronizeEtAsync(bool zorla = false, CancellationToken iptal = default)
    {
        if (!zorla
            && _sonTamSenkron.HasValue
            && DateTime.Now - _sonTamSenkron.Value < TamSenkronBekleme)
            return;

        try
        {
            await AyarlariYukleAsync(iptal);
            await TalepleriYukleAsync(iptal);
            await StokYukleAsync(iptal);
            await StokHareketYukleAsync(iptal);
            await AlinanMalzemeleriYukleAsync(iptal);
            await BildirimleriYukleAsync(iptal);
            await YerelOnbellegeKaydetAsync();
            OfflineMod = false;
            SonSenkronBasarili = true;
            SonSenkronHata = null;
            SonSenkronZamani = DateTime.Now;
            _sonTamSenkron = DateTime.Now;
        }
        catch (Exception ex)
        {
            if (await YerelOnbellektenYukleAsync())
            {
                OfflineMod = true;
                SonSenkronBasarili = false;
                SonSenkronHata = AgHataMesaji.Turkcele(ex.Message);
            }
            else
                throw;
        }
    }

    /// <summary>Giriş sonrası tam senkron başarısız olsa bile oturumu açık tutar.</summary>
    public async Task<bool> GirisSonrasiSenkronizeEtAsync(CancellationToken iptal = default)
    {
        try
        {
            await SenkronizeEtAsync(zorla: true, iptal);
            return true;
        }
        catch (Exception ex)
        {
            if (await YerelOnbellektenYukleAsync())
            {
                OfflineMod = true;
                SonSenkronBasarili = false;
                SonSenkronHata = AgHataMesaji.Turkcele(ex.Message);
                return false;
            }

            OfflineMod = true;
            SonSenkronBasarili = false;
            SonSenkronHata = AgHataMesaji.Turkcele(ex.Message);
            return false;
        }
    }

    /// <summary>Arka plan kontrolü — yalnızca bildirim belgesini okur (1 Firestore isteği).</summary>
    public async Task BildirimleriSenkronizeEtAsync(CancellationToken iptal = default)
    {
        try
        {
            await BildirimleriYukleAsync(iptal);
            await YerelOnbellegeKaydetAsync();
            SonSenkronHata = null;
        }
        catch (Exception ex)
        {
            SonSenkronHata = AgHataMesaji.Turkcele(ex.Message);
            throw;
        }
    }

    public async Task TalepleriYukleAsync(CancellationToken iptal = default)
    {
        var json = await _firestore.BelgeJsonOkuAsync(FirestoreYollari.SatinalmaTalepler, iptal);
        if (string.IsNullOrWhiteSpace(json))
        {
            SilinenTalepleriTemizle();
            return;
        }

        var gelen = JsonSerializer.Deserialize<List<SatinalmaTalep>>(json, Json) ?? [];
        var yerel = Talepler.ToList();
        var birlesik = yerel.Count > 0
            ? SatinalmaTalepBirlestirme.Birlestir(yerel, gelen, Ayarlar.SilinenTalepIdleri)
            : gelen.Where(t => !SatinalmaTalepSenkronYardimcisi.SilinenKumesi(Ayarlar.SilinenTalepIdleri).Contains(t.Id)).ToList();

        Talepler.Clear();
        foreach (var talep in birlesik)
        {
            TalepHazirla(talep);
            Talepler.Add(talep);
        }

        SilinenTalepleriTemizle();

        if (SatinalmaTalepYardimcisi.BosTaslaklariTemizle(Talepler))
            await TalepleriKaydetAsync(iptal, ayarlariKaydet: false);

        if (SatinalmaTalepYardimcisi.TaslaklariNormalizeEt(Talepler, silBosTaslaklari: false))
            await TalepleriKaydetAsync(iptal);
        else if (SatinalmaTalepYardimcisi.YonetimOnayMiraslariniGuncelle(Talepler))
            await TalepleriKaydetAsync(iptal);
    }

    public async Task TalepleriKaydetAsync(CancellationToken iptal = default, bool ayarlariKaydet = true)
    {
        await TalepBulutYazmaKilidi.WaitAsync(iptal);
        try
        {
            try
            {
                var bulutJson = await _firestore.BelgeJsonOkuAsync(FirestoreYollari.SatinalmaTalepler, iptal);
                var bulutAyarJson = await _firestore.BelgeJsonOkuAsync(FirestoreYollari.SatinalmaAyarlar, iptal);
                if (!string.IsNullOrWhiteSpace(bulutAyarJson))
                {
                    var bulutAyarlar = JsonSerializer.Deserialize<SatinalmaAyarlar>(bulutAyarJson, Json);
                    if (bulutAyarlar is not null)
                        SatinalmaTalepSenkronYardimcisi.AyarlariBirlestir(Ayarlar, bulutAyarlar);
                }

                if (!string.IsNullOrWhiteSpace(bulutJson))
                {
                    var bulut = JsonSerializer.Deserialize<List<SatinalmaTalep>>(bulutJson, Json) ?? [];
                    var birlesik = SatinalmaTalepBirlestirme.Birlestir(Talepler, bulut, Ayarlar.SilinenTalepIdleri);
                    Talepler.Clear();
                    foreach (var talep in birlesik)
                    {
                        TalepHazirla(talep);
                        Talepler.Add(talep);
                    }
                }
            }
            catch
            {
                // Bulut okunamazsa yerel kayıtla devam et
            }

            SilinenTalepleriTemizle();

            if (ayarlariKaydet)
                await AyarlariKaydetAsync(iptal);

            var json = JsonSerializer.Serialize(Talepler, Json);
            await _firestore.BelgeJsonYazAsync(FirestoreYollari.SatinalmaTalepler, json, _auth.Uid, iptal);
        }
        finally
        {
            TalepBulutYazmaKilidi.Release();
        }
    }

    /// <summary>Talepler + bildirimler — arka plan ve bildirim dinleyicisi için.</summary>
    public async Task HizliSenkronAsync(CancellationToken iptal = default)
    {
        await AyarlariYukleAsync(iptal);
        await TalepleriYukleAsync(iptal);
        await BildirimleriYukleAsync(iptal);
        await YerelOnbellegeKaydetAsync();
    }

    private void SilinenTalepleriTemizle() =>
        SatinalmaTalepSenkronYardimcisi.SilinenleriListedenCikar(Talepler, Ayarlar.SilinenTalepIdleri);

    public async Task AyarlariYukleAsync(CancellationToken iptal = default)
    {
        var json = await _firestore.BelgeJsonOkuAsync(FirestoreYollari.SatinalmaAyarlar, iptal);
        if (string.IsNullOrWhiteSpace(json))
            return;

        var gelen = JsonSerializer.Deserialize<SatinalmaAyarlar>(json, Json) ?? new SatinalmaAyarlar();
        SatinalmaTalepSenkronYardimcisi.AyarlariBirlestir(Ayarlar, gelen);
        SilinenTalepleriTemizle();
    }

    public async Task AyarlariKaydetAsync(CancellationToken iptal = default)
    {
        string? bulutJson = null;
        try
        {
            bulutJson = await _firestore.BelgeJsonOkuAsync(FirestoreYollari.SatinalmaAyarlar, iptal);
            if (!string.IsNullOrWhiteSpace(bulutJson))
            {
                var bulutAyarlar = JsonSerializer.Deserialize<SatinalmaAyarlar>(bulutJson, Json);
                if (bulutAyarlar is not null)
                    SatinalmaTalepSenkronYardimcisi.AyarlariBirlestir(Ayarlar, bulutAyarlar);
            }
        }
        catch
        {
            // bulut okunamazsa yerel ayarlarla devam
        }

        var json = SatinalmaAyarlarSenkronYardimcisi.MobilGuncellemesiniUygula(bulutJson, Ayarlar, Json);
        await _firestore.BelgeJsonYazAsync(FirestoreYollari.SatinalmaAyarlar, json, _auth.Uid, iptal);
    }

    public async Task StokYukleAsync(CancellationToken iptal = default)
    {
        var json = await _firestore.BelgeJsonOkuAsync(FirestoreYollari.Stok, iptal);
        Stok.Clear();
        if (string.IsNullOrWhiteSpace(json))
            return;
        Stok.AddRange(JsonSerializer.Deserialize<List<StokKaydi>>(json, Json) ?? []);
    }

    public async Task StokKaydetAsync(CancellationToken iptal = default)
    {
        try
        {
            var bulutJson = await _firestore.BelgeJsonOkuAsync(FirestoreYollari.Stok, iptal);
            if (!string.IsNullOrWhiteSpace(bulutJson))
            {
                var bulut = JsonSerializer.Deserialize<List<StokKaydi>>(bulutJson, Json) ?? [];
                var birlesik = StokBirlestirme.Birlestir(Stok, bulut);
                Stok.Clear();
                Stok.AddRange(birlesik);
            }
        }
        catch
        {
            // bulut okunamazsa yerel ile devam
        }

        var json = JsonSerializer.Serialize(Stok, Json);
        await _firestore.BelgeJsonYazAsync(FirestoreYollari.Stok, json, _auth.Uid, iptal);
    }

    public async Task StokHareketYukleAsync(CancellationToken iptal = default)
    {
        var json = await _firestore.BelgeJsonOkuAsync(FirestoreYollari.StokHareket, iptal);
        StokHareketleri.Clear();
        if (string.IsNullOrWhiteSpace(json))
            return;
        StokHareketleri.AddRange(JsonSerializer.Deserialize<List<StokHareketKaydi>>(json, Json) ?? []);
    }

    public async Task StokHareketKaydetAsync(CancellationToken iptal = default)
    {
        try
        {
            var bulutJson = await _firestore.BelgeJsonOkuAsync(FirestoreYollari.StokHareket, iptal);
            if (!string.IsNullOrWhiteSpace(bulutJson))
            {
                var bulut = JsonSerializer.Deserialize<List<StokHareketKaydi>>(bulutJson, Json) ?? [];
                var birlesik = StokBirlestirme.HareketleriBirlestir(StokHareketleri, bulut);
                StokHareketleri.Clear();
                StokHareketleri.AddRange(birlesik);
            }
        }
        catch
        {
            // bulut okunamazsa yerel ile devam
        }

        var json = JsonSerializer.Serialize(StokHareketleri, Json);
        await _firestore.BelgeJsonYazAsync(FirestoreYollari.StokHareket, json, _auth.Uid, iptal);
    }

    public async Task BildirimleriYukleAsync(CancellationToken iptal = default)
    {
        var json = await _firestore.BelgeJsonOkuAsync(FirestoreYollari.Bildirimler, iptal);
        var bulut = BildirimleriOku(json);
        var birlesik = BildirimBirlestirme.Birlestir(Bildirimler, bulut);
        Bildirimler.Clear();
        Bildirimler.AddRange(birlesik);
    }

    public async Task BildirimleriKaydetAsync(CancellationToken iptal = default)
    {
        await BildirimBulutYazmaKilidi.WaitAsync(iptal);
        try
        {
            var bulutJson = await _firestore.BelgeJsonOkuAsync(FirestoreYollari.Bildirimler, iptal);
            var bulut = BildirimleriOku(bulutJson);
            var birlesik = BildirimBirlestirme.Birlestir(Bildirimler, bulut);
            Bildirimler.Clear();
            Bildirimler.AddRange(birlesik);

            var json = JsonSerializer.Serialize(Bildirimler, Json);
            await _firestore.BelgeJsonYazAsync(FirestoreYollari.Bildirimler, json, _auth.Uid, iptal);
        }
        finally
        {
            BildirimBulutYazmaKilidi.Release();
        }
    }

    private static List<BildirimKaydi> BildirimleriOku(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        return JsonSerializer.Deserialize<List<BildirimKaydi>>(json, Json) ?? [];
    }

    public async Task AlinanMalzemeleriYukleAsync(CancellationToken iptal = default)
    {
        AlinanMalzemeAdlari.Clear();
        var json = await _firestore.BelgeJsonOkuAsync(FirestoreYollari.AlinanMalzemeler, iptal);
        if (string.IsNullOrWhiteSpace(json))
            return;

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return;

            foreach (var kayit in doc.RootElement.EnumerateArray())
            {
                string? ad = null;
                if (kayit.TryGetProperty("malzemeHizmet", out var camel))
                    ad = camel.GetString();
                else if (kayit.TryGetProperty("MalzemeHizmet", out var pascal))
                    ad = pascal.GetString();

                if (!string.IsNullOrWhiteSpace(ad))
                    AlinanMalzemeAdlari.Add(ad.Trim());
            }
        }
        catch
        {
            // Öneri kaynağı isteğe bağlı
        }
    }

    public IEnumerable<string> MalzemeAdiOneriAra(string? arama)
    {
        IEnumerable<string> kaynaklar = AlinanMalzemeAdlari
            .Concat(Stok.Select(s => s.MalzemeAdi))
            .Concat(Talepler.SelectMany(t => t.Kalemler?.Select(k => k.Malzeme) ?? []));

        return MalzemeAdiOneriYardimcisi.Filtrele(kaynaklar, arama);
    }

    public string YeniTalepNoOlustur()
    {
        var yil = DateTime.Now.Year;
        Ayarlar.SonTalepSira++;
        return $"TLP-{yil}-{Ayarlar.SonTalepSira:D4}";
    }

    public string YeniSiparisNoOlustur()
    {
        var yil = DateTime.Now.Year;
        Ayarlar.SonSiparisSira++;
        return $"SIP-{yil}-{Ayarlar.SonSiparisSira:D4}";
    }

    public string YeniBelgeNo(string onEk)
    {
        var yil = DateTime.Now.Year;
        var sira = StokHareketleri.Count(h => h.BelgeNo.StartsWith($"{onEk}-{yil}", StringComparison.Ordinal)) + 1;
        return $"{onEk}-{yil}-{sira:D4}";
    }

    private static void TalepHazirla(SatinalmaTalep talep)
    {
        talep.Kalemler ??= [];
        talep.Teklifler ??= [];
        talep.FirmaSiparisNolari ??= [];
        if (string.IsNullOrWhiteSpace(talep.TalepTuru))
            talep.TalepTuru = TalepTurleri.Normal;

        foreach (var teklif in talep.Teklifler)
            teklif.Fiyatlar ??= [];
    }

    private static string OnbellekYolu =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SatinalmaPro", "mobil_onbellek.json");

    private async Task YerelOnbellegeKaydetAsync()
    {
        try
        {
            var paket = new OnbellekPaketi
            {
                Talepler = Talepler,
                Ayarlar = Ayarlar,
                Stok = Stok,
                StokHareketleri = StokHareketleri,
                AlinanMalzemeAdlari = AlinanMalzemeAdlari,
                Bildirimler = Bildirimler,
                KayitZamani = DateTime.Now
            };
            var yol = OnbellekYolu;
            Directory.CreateDirectory(Path.GetDirectoryName(yol)!);
            await File.WriteAllTextAsync(yol, JsonSerializer.Serialize(paket, Json));
        }
        catch
        {
            // Önbellek isteğe bağlı
        }
    }

    private async Task<bool> YerelOnbellektenYukleAsync()
    {
        try
        {
            var yol = OnbellekYolu;
            if (!File.Exists(yol))
                return false;

            var paket = JsonSerializer.Deserialize<OnbellekPaketi>(await File.ReadAllTextAsync(yol), Json);
            if (paket is null)
                return false;

            Talepler.Clear();
            foreach (var talep in paket.Talepler)
            {
                TalepHazirla(talep);
                Talepler.Add(talep);
            }

            Ayarlar = paket.Ayarlar ?? new SatinalmaAyarlar();
            Stok.Clear();
            Stok.AddRange(paket.Stok);
            StokHareketleri.Clear();
            StokHareketleri.AddRange(paket.StokHareketleri);
            AlinanMalzemeAdlari.Clear();
            AlinanMalzemeAdlari.AddRange(paket.AlinanMalzemeAdlari);
            Bildirimler.Clear();
            Bildirimler.AddRange(paket.Bildirimler);
            SonSenkronZamani = paket.KayitZamani;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private sealed class OnbellekPaketi
    {
        public List<SatinalmaTalep> Talepler { get; set; } = [];
        public SatinalmaAyarlar? Ayarlar { get; set; }
        public List<StokKaydi> Stok { get; set; } = [];
        public List<StokHareketKaydi> StokHareketleri { get; set; } = [];
        public List<string> AlinanMalzemeAdlari { get; set; } = [];
        public List<BildirimKaydi> Bildirimler { get; set; } = [];
        public DateTime KayitZamani { get; set; }
    }
}

using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;

namespace SatinalmaPro.Services;

public static class SatinalmaDepo
{
    private static readonly string Klasor = SatinalmaProKlasor.Yol;

    private static readonly string TalepDosyasi = SatinalmaProKlasor.DosyaYolu("satinalma_talepler.json");
    private static readonly string AyarDosyasi = SatinalmaProKlasor.DosyaYolu("satinalma_ayarlar.json");

    private static readonly JsonSerializerOptions JsonSecenekleri = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static ObservableCollection<SatinalmaTalep> Talepler { get; } = [];
    public static SatinalmaAyarlar Ayarlar { get; private set; } = SatinalmaAyarlar.VarsayilanOlustur();

    /// <summary>Formda düzenlenen boş taslak — senkron sırasında silinmez.</summary>
    public static Guid? KorunanBosTaslakId { get; set; }

    private static bool _yuklendi;

    /// <summary>Bulut senkronu veya toplu yükleme sonrası UI listesini yenilemek için.</summary>
    public static event Action? TaleplerGuncellendi;

    public static void YenidenYukle()
    {
        _yuklendi = false;
        Talepler.Clear();
        Yukle();
    }

    public static void TumTalepleriSifirla()
    {
        Talepler.Clear();
        Ayarlar.SonTalepSira = 0;
        Ayarlar.SonSiparisSira = 0;
        Kaydet();
    }

    public static void AyarlariSifirla()
    {
        Ayarlar = SatinalmaAyarlar.VarsayilanOlustur();
        ImzalariHazirla(Ayarlar);
        Kaydet();
    }

    public static void Yukle()
    {
        if (_yuklendi)
            return;

        _yuklendi = true;
        Directory.CreateDirectory(Klasor);

        if (File.Exists(AyarDosyasi))
        {
            try
            {
                var json = File.ReadAllText(AyarDosyasi);
                Ayarlar = JsonSerializer.Deserialize<SatinalmaAyarlar>(json, JsonSecenekleri)
                          ?? SatinalmaAyarlar.VarsayilanOlustur();
            }
            catch
            {
                Ayarlar = SatinalmaAyarlar.VarsayilanOlustur();
            }
        }

        ImzalariHazirla(Ayarlar);
        SartnameMetniniGocEt(Ayarlar);

        if (File.Exists(TalepDosyasi))
        {
            try
            {
                var json = File.ReadAllText(TalepDosyasi);
                var liste = JsonSerializer.Deserialize<List<SatinalmaTalep>>(json, JsonSecenekleri) ?? [];
                foreach (var talep in liste)
                {
                    TalebiHazirla(talep);
                    KalemOnaylariniGocEt(talep);
                    Talepler.Add(talep);
                }

                if (SatinalmaTalepYardimcisi.TaslaklariNormalizeEt(Talepler))
                    Kaydet();
                else if (SatinalmaTalepYardimcisi.YonetimOnayMiraslariniGuncelle(Talepler))
                    Kaydet();
            }
            catch
            {
                // boş başla
            }
        }

        SilinenTalepleriTemizle();

        if (!File.Exists(TalepDosyasi) && !OturumYoneticisi.BulutAktif)
            OrnekTalepEkle();

        SahiplikGocEt();
    }

    public static void Kaydet()
    {
        Directory.CreateDirectory(Klasor);
        var talepJson = JsonSerializer.Serialize(Talepler.ToList(), JsonSecenekleri);
        File.WriteAllText(TalepDosyasi, talepJson);

        KaydetAyarlar();
        BulutVeriSenkronu.Planla("satinalma_talepler");
    }

    public static void KaydetAyarlar()
    {
        Directory.CreateDirectory(Klasor);
        var ayarJson = JsonSerializer.Serialize(Ayarlar, JsonSecenekleri);
        File.WriteAllText(AyarDosyasi, ayarJson);
        BulutVeriSenkronu.Planla("satinalma_ayarlar");
    }

    public static void TalepleriYukle(string json) =>
        TalepleriBirlestirVeYukle(json, yerelBirlestir: false);

    /// <summary>Bulut verisini yerel ile birleştirir — kayıp talep olmaz.</summary>
    public static void TalepleriBirlestirVeYukle(string json, bool yerelBirlestir = true)
    {
        var gelen = JsonSerializer.Deserialize<List<SatinalmaTalep>>(json, JsonSecenekleri) ?? [];
        var yerel = yerelBirlestir ? Talepler.ToList() : [];

        if (yerelBirlestir && File.Exists(TalepDosyasi))
        {
            try
            {
                var disk = JsonSerializer.Deserialize<List<SatinalmaTalep>>(
                    File.ReadAllText(TalepDosyasi), JsonSecenekleri) ?? [];
                yerel = SatinalmaTalepBirlestirme.Birlestir(yerel, disk, Ayarlar.SilinenTalepIdleri);
            }
            catch
            {
                // disk okunamazsa bellekteki yerel ile devam
            }
        }

        var birlesik = yerelBirlestir
            ? SatinalmaTalepBirlestirme.Birlestir(yerel, gelen, Ayarlar.SilinenTalepIdleri)
            : gelen.Where(t => !SatinalmaTalepSenkronYardimcisi.SilinenKumesi(Ayarlar.SilinenTalepIdleri).Contains(t.Id)).ToList();

        Talepler.Clear();
        var talepNoAtandi = false;
        foreach (var talep in birlesik)
        {
            TalebiHazirla(talep);
            if (string.IsNullOrWhiteSpace(talep.TalepNo)
                && SatinalmaTalepKuyrugu.KayitliTalep(talep))
            {
                TalepNoAtaIfNeeded(talep);
                talepNoAtandi = true;
            }
            KalemOnaylariniGocEt(talep);
            Talepler.Add(talep);
        }

        var kaydet = talepNoAtandi;
        if (SatinalmaTalepYardimcisi.TaslaklariNormalizeEt(Talepler, silBosTaslaklari: false))
            kaydet = true;
        else if (SatinalmaTalepYardimcisi.YonetimOnayMiraslariniGuncelle(Talepler))
            kaydet = true;
        else if (yerelBirlestir && birlesik.Count > gelen.Count)
            kaydet = true;

        SilinenTalepleriTemizle();

        if (BosTaslaklariTemizle())
            kaydet = true;

        if (kaydet)
            Kaydet();

        SahiplikGocEt();

        TaleplerGuncellendi?.Invoke();
    }

    private static void SahiplikGocEt()
    {
        if (!OturumYoneticisi.GirisYapildi || OturumYoneticisi.AktifKullanici is not { } kullanici)
            return;

        if (SatinalmaTalepSahiplikYardimcisi.OlusturanUidGocEt(Talepler, kullanici) > 0)
            Kaydet();
    }

    /// <summary>İçeriksiz taslakları kaldırır; formda açık olan korunur.</summary>
    public static bool BosTaslaklariTemizle(Guid? korunanId = null) =>
        SatinalmaTalepYardimcisi.BosTaslaklariTemizle(Talepler, korunanId ?? KorunanBosTaslakId);

    public static void TalepNoAtaIfNeeded(SatinalmaTalep talep)
    {
        if (!string.IsNullOrWhiteSpace(talep.TalepNo))
            return;

        talep.TalepNo = YeniTalepNoOlustur();
        KaydetAyarlar();
    }

    public static void TalebiHazirla(SatinalmaTalep talep)
    {
        talep.Kalemler ??= [];
        talep.Teklifler ??= [];
        talep.FirmaSiparisNolari ??= [];
        foreach (var teklif in talep.Teklifler)
        {
            teklif.Fiyatlar ??= [];
            foreach (var fiyat in teklif.Fiyatlar)
                fiyat.Marka ??= "";
            MarkalariKalemeTasi(teklif);
        }
    }

    public static SatinalmaAyarlar AyarlariHazirla(SatinalmaAyarlar? ayarlar = null)
    {
        ayarlar ??= Ayarlar ?? SatinalmaAyarlar.VarsayilanOlustur();
        ayarlar.SefImzalari ??= [];
        ayarlar.YonetimImzalari ??= [];
        ayarlar.Sartnameler ??= [];
        ImzalariHazirla(ayarlar);
        return ayarlar;
    }

    public static void AyarlarYukle(string json, bool birlestir = true)
    {
        var gelen = JsonSerializer.Deserialize<SatinalmaAyarlar>(json, JsonSecenekleri)
                    ?? SatinalmaAyarlar.VarsayilanOlustur();

        if (birlestir)
        {
            Ayarlar.SilinenTalepIdleri = SatinalmaTalepSenkronYardimcisi.SilinenleriBirlestir(
                Ayarlar.SilinenTalepIdleri, gelen.SilinenTalepIdleri);
            Ayarlar.SonTalepSira = Math.Max(Ayarlar.SonTalepSira, gelen.SonTalepSira);
            Ayarlar.SonSiparisSira = Math.Max(Ayarlar.SonSiparisSira, gelen.SonSiparisSira);
            if (Ayarlar.VarsayilanUsdKuru <= 0 && gelen.VarsayilanUsdKuru > 0)
                Ayarlar.VarsayilanUsdKuru = gelen.VarsayilanUsdKuru;
            if (Ayarlar.VarsayilanEurKuru <= 0 && gelen.VarsayilanEurKuru > 0)
                Ayarlar.VarsayilanEurKuru = gelen.VarsayilanEurKuru;
            if (string.IsNullOrWhiteSpace(Ayarlar.FirmaAdi) && !string.IsNullOrWhiteSpace(gelen.FirmaAdi))
                Ayarlar.FirmaAdi = gelen.FirmaAdi;

            ImzaListeleriniBirlestir(Ayarlar.SefImzalari, gelen.SefImzalari);
            ImzaListeleriniBirlestir(Ayarlar.YonetimImzalari, gelen.YonetimImzalari);

            if (string.IsNullOrWhiteSpace(Ayarlar.SartnameMetni) && !string.IsNullOrWhiteSpace(gelen.SartnameMetni))
                Ayarlar.SartnameMetni = gelen.SartnameMetni;
            if (string.IsNullOrWhiteSpace(Ayarlar.TeklifIstemeSartnameleri) &&
                !string.IsNullOrWhiteSpace(gelen.TeklifIstemeSartnameleri))
                Ayarlar.TeklifIstemeSartnameleri = gelen.TeklifIstemeSartnameleri;
        }
        else
            Ayarlar = gelen;

        AyarlariHazirla(Ayarlar);
        SilinenTalepleriTemizle();
    }

    private static void ImzaListeleriniBirlestir(
        ObservableCollection<ImzaAyari> hedef,
        IEnumerable<ImzaAyari>? kaynak)
    {
        if (kaynak is null)
            return;

        foreach (var kaynakImza in kaynak)
        {
            if (string.IsNullOrWhiteSpace(kaynakImza.Unvan))
                continue;

            var mevcut = hedef.FirstOrDefault(h =>
                string.Equals(h.Unvan?.Trim(), kaynakImza.Unvan.Trim(), StringComparison.OrdinalIgnoreCase));

            if (mevcut is not null)
            {
                if (string.IsNullOrWhiteSpace(mevcut.AdSoyad) && !string.IsNullOrWhiteSpace(kaynakImza.AdSoyad))
                    mevcut.AdSoyad = kaynakImza.AdSoyad.Trim();

                mevcut.Unvan = kaynakImza.Unvan.Trim();
                mevcut.Aktif = mevcut.Aktif || kaynakImza.Aktif;
            }
            else
            {
                hedef.Add(new ImzaAyari
                {
                    Unvan = kaynakImza.Unvan.Trim(),
                    AdSoyad = kaynakImza.AdSoyad?.Trim() ?? "",
                    Aktif = kaynakImza.Aktif
                });
            }
        }
    }

    public static void SilinenTalepleriTemizle()
    {
        var once = Talepler.Count;
        SatinalmaTalepSenkronYardimcisi.SilinenleriListedenCikar(Talepler, Ayarlar.SilinenTalepIdleri);
        if (Talepler.Count != once)
            TaleplerGuncellendi?.Invoke();
    }

    public static string YeniTalepNoOlustur()
    {
        var yil = DateTime.Now.Year;
        Ayarlar.SonTalepSira++;
        return $"TLP-{yil}-{Ayarlar.SonTalepSira:D4}";
    }

    public static string YeniSiparisNoOlustur()
    {
        var yil = DateTime.Now.Year;
        Ayarlar.SonSiparisSira++;
        return $"SIP-{yil}-{Ayarlar.SonSiparisSira:D4}";
    }

    public static string SiparisNoAl(SatinalmaTalep talep, Guid teklifId)
    {
        talep.FirmaSiparisNolari ??= [];
        if (!talep.FirmaSiparisNolari.TryGetValue(teklifId, out var no) || string.IsNullOrWhiteSpace(no))
        {
            no = YeniSiparisNoOlustur();
            talep.FirmaSiparisNolari[teklifId] = no;
        }

        return no;
    }

    public static SatinalmaTalep YeniTalepOlustur(bool talepNoVer = false)
    {
        var talep = new SatinalmaTalep
        {
            TalepNo = talepNoVer ? YeniTalepNoOlustur() : "",
            Tarih = DateTime.Now.ToString("dd.MM.yyyy"),
            Durum = SatinalmaTalepDurumlari.Taslak
        };
        talep.Kalemler.Add(new SatinalmaTalepKalemi { SiraNo = 1, Birim = "Adet" });
        return talep;
    }

    public static void TalepKalemleriniTekliflerleSenkronla(SatinalmaTalep talep) =>
        SatinalmaTalepYardimcisi.TalepKalemleriniTekliflerleSenkronla(talep);

    public static void TeklifDegisikligiIsle(SatinalmaTalep talep) =>
        SatinalmaTalepYardimcisi.TeklifDegisikligiIsle(talep);

    public static void TeklifFiyatlariniHazirla(SatinalmaTalep talep, SatinalmaTeklif teklif)
    {
        teklif.Fiyatlar.Clear();
        foreach (var kalem in talep.Kalemler.OrderBy(k => k.SiraNo))
        {
            teklif.Fiyatlar.Add(new SatinalmaTeklifFiyati
            {
                KalemId = kalem.Id,
                KdvOrani = teklif.KdvOrani > 0 ? teklif.KdvOrani : 20
            });
        }
    }

    public static void TeklifsizFirmaFiyatKaydet(SatinalmaTalep talep, IEnumerable<TeklifsizFirmaFiyatSatiri> satirlar)
    {
        talep.Kalemler ??= [];
        talep.Teklifler ??= [];
        talep.FirmaSiparisNolari ??= [];

        var girisler = new List<(Guid kalemId, string firma, decimal birimFiyat)>();
        foreach (var satir in satirlar)
        {
            if (!satir.GecerliMi(out var birimFiyat))
                throw new InvalidOperationException($"'{satir.Malzeme}' için firma ve geçerli birim fiyat girin.");

            girisler.Add((satir.Kalem.Id, satir.FirmaAdi, birimFiyat));
        }

        if (girisler.Count == 0)
            throw new InvalidOperationException("Kaydedilecek kalem bulunamadı.");

        foreach (var grup in girisler.GroupBy(g => g.firma, StringComparer.OrdinalIgnoreCase))
        {
            var teklif = new SatinalmaTeklif
            {
                FirmaAdi = grup.Key,
                Onaylandi = true,
                Aciklama = "Yönetim onayı sonrası firma/fiyat girişi",
                UsdKuru = Ayarlar.VarsayilanUsdKuru,
                EurKuru = Ayarlar.VarsayilanEurKuru
            };
            TeklifFiyatlariniHazirla(talep, teklif);

            foreach (var (kalemId, _, birimFiyat) in grup)
            {
                var kalem = talep.Kalemler.First(k => k.Id == kalemId);
                var fiyat = teklif.Fiyatlar.First(f => f.KalemId == kalemId);
                fiyat.BirimFiyat = birimFiyat;
                fiyat.ParaBirimi = ParaBirimleri.Try;
                fiyat.Hesapla(kalem.Miktar, teklif.UsdKuru, teklif.EurKuru);
                kalem.OnaylananTeklifId = teklif.Id;
            }

            teklif.FiyatlariHesapla(talep.Kalemler);
            talep.Teklifler.Add(teklif);
            SiparisNoAl(talep, teklif.Id);
        }

        var anaTeklifId = girisler
            .GroupBy(g => g.firma, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .First()
            .Select(g => talep.Kalemler.First(k => k.Id == g.kalemId).OnaylananTeklifId!.Value)
            .First();

        talep.OnaylananTeklifId = anaTeklifId;
        talep.SiparisNo = SiparisNoAl(talep, anaTeklifId);
        talep.Durum = SatinalmaTalepDurumlari.Onaylandi;
        Kaydet();
    }

    public static List<OnaylananMalzemeSatiri> OnaylananMalzemeleriOlustur()
    {
        var liste = new List<OnaylananMalzemeSatiri>();

        foreach (var talep in Talepler.Where(t => t.HerhangiKalemOnayli))
        {
            TalebiHazirla(talep);
            talep.FirmaSiparisNolari ??= [];

            foreach (var kalem in talep.Kalemler.Where(k => k.OnaylananTeklifId != null).OrderBy(k => k.SiraNo))
            {
                var teklif = talep.KalemOnayTeklifi(kalem);
                if (teklif == null)
                    continue;

                teklif.FiyatlariHesapla(talep.Kalemler);
                teklif.Fiyatlar ??= [];
                var fiyat = teklif.Fiyatlar.FirstOrDefault(f => f.KalemId == kalem.Id);
                if (fiyat == null)
                    continue;

                var siparisNo = talep.FirmaSiparisNolari.TryGetValue(teklif.Id, out var no) && !string.IsNullOrWhiteSpace(no)
                    ? no
                    : talep.SiparisNo;

                liste.Add(new OnaylananMalzemeSatiri
                {
                    TalepId = talep.Id,
                    KalemId = kalem.Id,
                    TeklifId = teklif.Id,
                    TalepNo = talep.TalepNo,
                    SiparisNo = siparisNo,
                    Tarih = talep.Tarih,
                    Durum = talep.Durum,
                    Firma = teklif.FirmaAdi,
                    Marka = string.IsNullOrWhiteSpace(fiyat.Marka) ? teklif.MarkaOzeti : fiyat.Marka,
                    Malzeme = kalem.Malzeme,
                    SiparisMiktari = kalem.Miktar,
                    KabulEdilenMiktar = kalem.KabulEdilenMiktar,
                    SiparisTamamlandi = kalem.SiparisTamamlandi,
                    Birim = kalem.Birim,
                    BirimFiyati = fiyat.TlBirimFiyat(teklif.UsdKuru, teklif.EurKuru),
                    ToplamTutar = fiyat.ToplamTutar,
                    VadeGunu = teklif.VadeGunu,
                    KalemAciklamasi = kalem.Aciklama
                });
            }
        }

        return liste;
    }

    public static SatinalmaTalepKalemi? KalemBul(Guid talepId, Guid kalemId) =>
        Talepler.FirstOrDefault(t => t.Id == talepId)?.Kalemler.FirstOrDefault(k => k.Id == kalemId);

    private static void KalemOnaylariniGocEt(SatinalmaTalep talep)
    {
        if (talep.OnaylananTeklifId is not { } eskiTeklifId)
            return;

        foreach (var kalem in talep.Kalemler.Where(k => k.OnaylananTeklifId == null))
            kalem.OnaylananTeklifId = eskiTeklifId;

        if (!string.IsNullOrWhiteSpace(talep.SiparisNo) &&
            !talep.FirmaSiparisNolari.ContainsKey(eskiTeklifId))
            talep.FirmaSiparisNolari[eskiTeklifId] = talep.SiparisNo;
    }

    private static void SartnameMetniniGocEt(SatinalmaAyarlar ayarlar)
    {
        if (!string.IsNullOrWhiteSpace(ayarlar.SartnameMetni))
            return;

        var metinler = ayarlar.Sartnameler
            .Select(s => s.Metin)
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .ToList();

        if (metinler.Count > 0)
            ayarlar.SartnameMetni = string.Join(Environment.NewLine + Environment.NewLine, metinler);
    }

    private static void OrnekTalepEkle()
    {
        var talep = new SatinalmaTalep
        {
            TalepNo = "TLP-2026-0001",
            Tarih = "18.06.2026",
            TalepAciklamasi = "Merkez şantiye tünel bölümü için acil malzeme talebi",
            Durum = SatinalmaTalepDurumlari.TeklifGirisi
        };
        Ayarlar.SonTalepSira = 1;

        var k1 = new SatinalmaTalepKalemi
        {
            SiraNo = 1, Malzeme = "HDPE Boru Ø160", Miktar = 120, Birim = "Metre",
            Aciklama = "SN8"
        };
        var k2 = new SatinalmaTalepKalemi
        {
            SiraNo = 2, Malzeme = "Beton Prizma K-350", Miktar = 45, Birim = "m³",
            Aciklama = "Tünel girişi"
        };
        talep.Kalemler.Add(k1);
        talep.Kalemler.Add(k2);

        var teklif1 = new SatinalmaTeklif
        {
            FirmaAdi = "Delta Yapı Malzeme", VadeGunu = 60,
            TeslimSuresi = "7 gün", OdemeSekli = "Çek", KdvOrani = 20
        };
        TeklifFiyatlariniHazirla(talep, teklif1);
        teklif1.Fiyatlar.First(f => f.KalemId == k1.Id).Marka = "Pipelife";
        teklif1.Fiyatlar.First(f => f.KalemId == k2.Id).Marka = "Akçansa";
        teklif1.Fiyatlar.First(f => f.KalemId == k1.Id).BirimFiyat = 285;
        teklif1.Fiyatlar.First(f => f.KalemId == k2.Id).BirimFiyat = 4200;
        teklif1.FiyatlariHesapla(talep.Kalemler);

        var teklif2 = new SatinalmaTeklif
        {
            FirmaAdi = "ABC İnşaat Tedarik", VadeGunu = 45,
            TeslimSuresi = "5 gün", OdemeSekli = "Havale", KdvOrani = 20
        };
        TeklifFiyatlariniHazirla(talep, teklif2);
        teklif2.Fiyatlar.First(f => f.KalemId == k1.Id).Marka = "Wavin";
        teklif2.Fiyatlar.First(f => f.KalemId == k2.Id).Marka = "Çimsa";
        teklif2.Fiyatlar.First(f => f.KalemId == k1.Id).BirimFiyat = 310;
        teklif2.Fiyatlar.First(f => f.KalemId == k2.Id).BirimFiyat = 3950;
        teklif2.FiyatlariHesapla(talep.Kalemler);

        talep.Teklifler.Add(teklif1);
        talep.Teklifler.Add(teklif2);
        Talepler.Add(talep);

        var onayli = new SatinalmaTalep
        {
            TalepNo = "TLP-2026-0002",
            Tarih = "10.06.2026",
            TalepAciklamasi = "Demir ve bağlantı malzemeleri",
            Durum = SatinalmaTalepDurumlari.SiparisOlusturuldu,
            SiparisNo = "SIP-2026-0001"
        };
        Ayarlar.SonTalepSira = 2;
        Ayarlar.SonSiparisSira = 1;

        var d1 = new SatinalmaTalepKalemi
        {
            SiraNo = 1, Malzeme = "Demir Ø12", Miktar = 2.5, Birim = "Ton", Aciklama = "Nervürlü"
        };
        var d2 = new SatinalmaTalepKalemi
        {
            SiraNo = 2, Malzeme = "Demir Ø16", Miktar = 1.8, Birim = "Ton", Aciklama = "Kesimli"
        };
        onayli.Kalemler.Add(d1);
        onayli.Kalemler.Add(d2);

        var onayTeklif = new SatinalmaTeklif
        {
            FirmaAdi = "Çelik A.Ş.", VadeGunu = 30,
            TeslimSuresi = "3 gün", OdemeSekli = "Havale", KdvOrani = 20, Onaylandi = true
        };
        TeklifFiyatlariniHazirla(onayli, onayTeklif);
        onayTeklif.Fiyatlar.First(f => f.KalemId == d1.Id).Marka = "Kaptan Demir";
        onayTeklif.Fiyatlar.First(f => f.KalemId == d2.Id).Marka = "İçdaş";
        onayTeklif.Fiyatlar.First(f => f.KalemId == d1.Id).BirimFiyat = 46500;
        onayTeklif.Fiyatlar.First(f => f.KalemId == d2.Id).BirimFiyat = 47200;
        onayTeklif.FiyatlariHesapla(onayli.Kalemler);
        onayli.Teklifler.Add(onayTeklif);
        onayli.OnaylananTeklifId = onayTeklif.Id;
        d1.OnaylananTeklifId = onayTeklif.Id;
        d2.OnaylananTeklifId = onayTeklif.Id;
        onayli.FirmaSiparisNolari[onayTeklif.Id] = onayli.SiparisNo;
        Talepler.Add(onayli);

        Kaydet();
    }

    private static void MarkalariKalemeTasi(SatinalmaTeklif teklif)
    {
        teklif.Fiyatlar ??= [];
        if (string.IsNullOrWhiteSpace(teklif.Marka))
            return;

        foreach (var fiyat in teklif.Fiyatlar)
        {
            if (string.IsNullOrWhiteSpace(fiyat.Marka))
                fiyat.Marka = teklif.Marka;
        }

        teklif.Marka = "";
    }

    public static void ImzalariHazirla(SatinalmaAyarlar? ayarlar)
    {
        if (ayarlar is null)
            return;

        ayarlar.SefImzalari ??= [];
        ayarlar.YonetimImzalari ??= [];

        if (ayarlar.SefImzalari.Count == 0 && ayarlar.YonetimImzalari.Count == 0 && ayarlar.Imzalar is { Count: > 0 })
        {
            foreach (var imza in ayarlar.Imzalar)
            {
                if (YonetimImzasiMi(imza.Unvan))
                    ayarlar.YonetimImzalari.Add(imza);
                else
                    ayarlar.SefImzalari.Add(imza);
            }
        }

        if (ayarlar.SefImzalari.Count == 0 && ayarlar.YonetimImzalari.Count == 0)
        {
            var varsayilan = SatinalmaAyarlar.VarsayilanOlustur();
            ayarlar.SefImzalari = varsayilan.SefImzalari;
            ayarlar.YonetimImzalari = varsayilan.YonetimImzalari;
        }

        ImzaVarsayilanlariniKontrolEt(ayarlar);
        ayarlar.Imzalar = null;
    }

    private static bool YonetimImzasiMi(string? unvan) =>
        !string.IsNullOrWhiteSpace(unvan) && (
            unvan.Contains("Yönetim", StringComparison.OrdinalIgnoreCase) ||
            unvan.Contains("Proje Müdürü", StringComparison.OrdinalIgnoreCase) ||
            unvan.Contains("Proje Muduru", StringComparison.OrdinalIgnoreCase));

    private static void ImzaVarsayilanlariniKontrolEt(SatinalmaAyarlar ayarlar)
    {
        if (ayarlar.SefImzalari.Count == 0)
        {
            foreach (var unvan in new[] { "Satınalma", "Tünel Şefi", "Şantiye Şefi" })
                ayarlar.SefImzalari.Add(new ImzaAyari { Unvan = unvan, Aktif = true });
        }

        if (ayarlar.YonetimImzalari.Count == 0)
            ayarlar.YonetimImzalari.Add(new ImzaAyari { Unvan = "Proje Müdürü", Aktif = true });
    }
}

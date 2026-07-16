using SatinalmaPro.Shared.Helpers;
using SatinalmaPro.Shared.Models;
using SatinalmaPro.Shared.Procurement;
using SatinalmaPro.Shared.Services;

namespace SatinalmaPro.Shared.Services;

public sealed class SatinalmaMobilServisi : ISatinalmaDashboardSorgu
{
    private readonly MobilVeriDeposu _depo;
    private readonly BildirimServisi _bildirimler;
    private readonly StokMobilServisi? _stok;

    public SatinalmaMobilServisi(MobilVeriDeposu depo, BildirimServisi bildirimler, StokMobilServisi? stok = null)
    {
        _depo = depo;
        _bildirimler = bildirimler;
        _stok = stok;
    }

    public SatinalmaTalep YeniTalepOlustur()
    {
        var kullanici = _depo.AktifKullanici;
        return new SatinalmaTalep
        {
            Tarih = DateTime.Now.ToString("dd.MM.yyyy"),
            TalepEden = kullanici?.AdSoyad ?? "",
            OlusturanUid = kullanici?.Uid ?? "",
            OlusturanRol = kullanici?.Rol ?? "",
            Durum = SatinalmaTalepDurumlari.Taslak,
            TalepTuru = TalepTurleri.Normal,
            Kalemler = [new SatinalmaTalepKalemi { SiraNo = 1, Birim = "Adet" }]
        };
    }

    public async Task TalepKaydetAsync(SatinalmaTalep talep, CancellationToken iptal = default)
    {
        if (talep.Durum == SatinalmaTalepDurumlari.Taslak)
        {
            if (!SatinalmaTalepYardimcisi.IcerikVar(talep))
                throw new InvalidOperationException("Kaydetmek için en az bir kalem veya açıklama girin.");
            SatinalmaTalepYardimcisi.KayitOncesiHazirla(talep);
        }

        if (string.IsNullOrWhiteSpace(talep.TalepNo))
            talep.TalepNo = _depo.YeniTalepNoOlustur();

        SatinalmaTalepSenkronYardimcisi.Dokun(talep);
        OlusturanRolunuTamamla(talep);

        var mevcut = _depo.Talepler.FirstOrDefault(t => t.Id == talep.Id);
        if (mevcut is null)
            _depo.Talepler.Add(talep);
        else
        {
            _depo.Talepler.Remove(mevcut);
            _depo.Talepler.Add(talep);
        }

        await _depo.AyarlariKaydetAsync(iptal);
        await _depo.TalepleriKaydetAsync(iptal);
    }

    public async Task TalepSilAsync(SatinalmaTalep talep, CancellationToken iptal = default)
    {
        if (!SatinalmaOnayYetkisi.TalepSilebilir(_depo.AktifKullanici, talep))
            throw new InvalidOperationException("Talep silme yetkiniz yok.");

        var mevcut = _depo.Talepler.FirstOrDefault(t => t.Id == talep.Id)
            ?? throw new InvalidOperationException("Talep bulunamadı.");

        _depo.Talepler.Remove(mevcut);
        SatinalmaTalepSenkronYardimcisi.SilindiIsaretle(talep.Id, _depo.Ayarlar);
        await _bildirimler.TalepBildirimleriniSilAsync(talep.Id, iptal);
        await _depo.AyarlariKaydetAsync(iptal);
        await _depo.TalepleriKaydetAsync(iptal, ayarlariKaydet: false);
    }

    public async Task YonetimeGonderAsync(SatinalmaTalep talep, CancellationToken iptal = default)
    {
        if ((talep.Teklifler?.Count ?? 0) > 0)
            throw new InvalidOperationException(
                "Teklif girilmiş talepler «Karşılaştırma» ekranından yönetime gönderilmelidir.");

        if (talep.Durum == SatinalmaTalepDurumlari.Taslak)
        {
            if (!SatinalmaTalepYardimcisi.IcerikVar(talep))
                throw new InvalidOperationException("Göndermek için en az bir kalem girin.");
            SatinalmaTalepYardimcisi.KayitOncesiHazirla(talep);
        }

        OlusturanRolunuTamamla(talep);
        talep.Durum = SatinalmaTalepDurumlari.ImzaSurecinde;
        SatinalmaTalepSenkronYardimcisi.Dokun(talep);
        await TalepKaydetAsync(talep, iptal);

        await _bildirimler.CokluEkleAsync(
            BildirimRolPolitikasi.YonetimeGonderildiHedefleri()
                .Select(h => BildirimKaydiOlustur(BildirimTipleri.YonetimeGonderildi, talep, h.HedefRol, h.HedefUid))
                .ToList(),
            iptal);
    }

    public async Task YonetimAcilOnaylaAsync(SatinalmaTalep talep, CancellationToken iptal = default)
    {
        YetkiKontrol(SatinalmaOnayYetkisi.YonetimKararVerebilir(_depo.AktifKullanici), "Acil onay yetkiniz yok.");

        if (talep.TalepTuru != TalepTurleri.Acil)
            throw new InvalidOperationException("Bu işlem yalnızca acil talepler için geçerlidir.");

        talep.Durum = SatinalmaTalepDurumlari.Onaylandi;
        talep.TeklifsizYonetimOnayi = true;
        talep.OnaylananTeklifId = null;
        talep.YonetimOnerilenTeklifId = null;
        talep.SatinalmaOnerisiElleSecildi = false;

        foreach (var kalem in talep.Kalemler)
            kalem.OnaylananTeklifId = null;

        YonetimOnayiKaydet(talep);
        await TalepKaydetAsync(talep, iptal);

        await _bildirimler.CokluEkleAsync(
            OnaylandiKayitlari(talep), iptal);
    }

    public async Task YonetimOnaylaAsync(SatinalmaTalep talep, bool teklifIste, CancellationToken iptal = default)
    {
        YetkiKontrol(SatinalmaOnayYetkisi.YonetimKararVerebilir(_depo.AktifKullanici), "Yönetim onay yetkiniz yok.");

        if (talep.TalepTuru == TalepTurleri.Acil)
        {
            await YonetimAcilOnaylaAsync(talep, iptal);
            return;
        }

        if (!teklifIste)
        {
            talep.Durum = SatinalmaTalepDurumlari.Onaylandi;
            talep.TeklifsizYonetimOnayi = true;
            YonetimOnayiKaydet(talep);
            await TalepKaydetAsync(talep, iptal);
            await _bildirimler.GecersizleriOkunduYapAsync(iptal);

            await _bildirimler.CokluEkleAsync(OnaylandiKayitlari(talep), iptal);
            return;
        }

        talep.Durum = SatinalmaTalepDurumlari.TeklifGirisi;
        await TalepKaydetAsync(talep, iptal);
        await _bildirimler.GecersizleriOkunduYapAsync(iptal);

        await _bildirimler.EkleAsync(
            BildirimKaydiOlustur(BildirimTipleri.TeklifIstendi, talep, hedefRol: KullaniciRolleri.Satinalma),
            iptal);

        if (!string.IsNullOrWhiteSpace(talep.OlusturanUid))
        {
            await _bildirimler.EkleAsync(
                BildirimKaydiOlustur(
                    BildirimTipleri.TeklifIstendi,
                    talep,
                    hedefUid: talep.OlusturanUid,
                    ek: OnayBildirimYardimcisi.TeklifIstemeBildirimEk(_depo.AktifKullanici?.Rol)),
                iptal);
        }
    }

    public async Task YonetimReddetAsync(SatinalmaTalep talep, string? gerekce, CancellationToken iptal = default)
    {
        YetkiKontrol(SatinalmaOnayYetkisi.YonetimKararVerebilir(_depo.AktifKullanici), "Red yetkiniz yok.");

        talep.Durum = SatinalmaTalepDurumlari.Reddedildi;
        talep.RedGerekcesi = string.IsNullOrWhiteSpace(gerekce) ? "" : gerekce.Trim();
        await TalepKaydetAsync(talep, iptal);
        await _bildirimler.GecersizleriOkunduYapAsync(iptal);

        try
        {
            var actorUid = _depo.AktifKullanici?.Uid;
            await _bildirimler.CokluEkleAsync(
                BildirimRolPolitikasi.ReddedildiHedefleri(talep.OlusturanUid, actorUid)
                    .Select(h => BildirimKaydiOlustur(BildirimTipleri.Reddedildi, talep, h.HedefRol, h.HedefUid, ek: gerekce))
                    .ToList(),
                iptal);
        }
        catch
        {
            // talep reddedildi; bildirim isteğe bağlı
        }
    }

    public async Task TeklifGeriGonderAsync(SatinalmaTalep talep, string? gerekce, CancellationToken iptal = default)
    {
        YetkiKontrol(SatinalmaOnayYetkisi.YonetimKararVerebilir(_depo.AktifKullanici), "Geri gönderme yetkiniz yok.");

        if (!SatinalmaTalepYardimcisi.TeklifYonetimOnayiBekliyor(talep))
            throw new InvalidOperationException("Bu talep için geri gönderilecek teklif onayı bulunamadı.");

        talep.Durum = SatinalmaTalepDurumlari.Karsilastirma;
        talep.TeklifDuzeltmeNotu = string.IsNullOrWhiteSpace(gerekce) ? "" : gerekce.Trim();
        talep.OnaylananTeklifId = null;
        talep.Kalemler ??= [];
        foreach (var kalem in talep.Kalemler)
            kalem.OnaylananTeklifId = null;

        await TalepKaydetAsync(talep, iptal);
        await _bildirimler.EkleAsync(
            BildirimKaydiOlustur(BildirimTipleri.TeklifDuzeltmeIstendi, talep, hedefRol: KullaniciRolleri.Satinalma, ek: talep.TeklifDuzeltmeNotu),
            iptal);
        await _bildirimler.GecersizleriOkunduYapAsync(iptal);
    }

    public Task TeklifReddetAsync(SatinalmaTalep talep, string? gerekce, CancellationToken iptal = default) =>
        YonetimReddetAsync(talep, gerekce, iptal);

    public async Task YonetimeTeklifOnayGonderAsync(SatinalmaTalep talep, CancellationToken iptal = default)
    {
        var oneri = SatinalmaIsAkisi.YonetimeTeklifGonderiminiDogrula(talep);
        SatinalmaIsAkisi.YonetimeTeklifGonderiminiHazirla(talep, oneri);
        await TalepKaydetAsync(talep, iptal);

        await _bildirimler.CokluEkleAsync(
            BildirimRolPolitikasi.TeklifOnaydaHedefleri()
                .Select(h => BildirimKaydiOlustur(BildirimTipleri.TeklifOnayda, talep, h.HedefRol, h.HedefUid))
                .ToList(),
            iptal);
        await _bildirimler.GecersizleriOkunduYapAsync(iptal);
    }

    public async Task TeklifEkleAsync(SatinalmaTalep talep, SatinalmaTeklif teklif, CancellationToken iptal = default)
    {
        if (!SatinalmaIsAkisi.TeklifEklenebilir(talep, _depo.AktifKullanici))
            throw new InvalidOperationException(SatinalmaIsAkisi.TeklifEklemeEngelMesaji(talep, _depo.AktifKullanici));

        teklif.FiyatlariHesapla(talep.Kalemler);
        var mevcut = talep.Teklifler.FirstOrDefault(t => t.Id == teklif.Id);
        if (mevcut is not null)
            talep.Teklifler.Remove(mevcut);
        talep.Teklifler.Add(teklif);

        if (talep.Durum is SatinalmaTalepDurumlari.TeklifGirisi
            or SatinalmaTalepDurumlari.Hazirlaniyor
            or SatinalmaTalepDurumlari.ImzaSurecinde)
            talep.Durum = SatinalmaTalepDurumlari.Karsilastirma;

        SatinalmaTalepYardimcisi.TeklifDegisikligiIsle(talep);
        await TalepKaydetAsync(talep, iptal);
    }

    public async Task TeklifOnaylaAsync(SatinalmaTalep talep, Guid teklifId, CancellationToken iptal = default)
    {
        if (!talep.Teklifler.Any(t => t.Id == teklifId))
            throw new InvalidOperationException("Teklif bulunamadı.");

        foreach (var kalem in talep.Kalemler)
            kalem.OnaylananTeklifId = teklifId;

        await KalemBazliOnaylaAsync(talep, iptal);
    }

    private bool FirmaOnayiSaltOkunur(SatinalmaTalep talep) =>
        SatinalmaOnayYetkisi.FirmaOnayiSaltOkunur(talep, _depo.AktifKullanici);

    public async Task FirmaOnaylariniGeriAlAsync(SatinalmaTalep talep, CancellationToken iptal = default)
    {
        if (FirmaOnayiSaltOkunur(talep))
            throw new InvalidOperationException("Bu onayı geri alma yetkiniz yok.");

        if (talep.Durum == SatinalmaTalepDurumlari.SiparisOlusturuldu)
            throw new InvalidOperationException("Sipariş verilmiş taleplerde onay geri alınamaz.");

        if (!talep.HerhangiKalemOnayli
            && !talep.YonetimOnayKilitli
            && talep.Durum != SatinalmaTalepDurumlari.Onaylandi)
            throw new InvalidOperationException("Geri alınacak onay bulunamadı.");

        talep.Kalemler ??= [];
        talep.Teklifler ??= [];

        foreach (var kalem in talep.Kalemler)
        {
            kalem.OnaylananTeklifId = null;
            kalem.KabulEdilenMiktar = 0;
            kalem.SiparisTamamlandi = false;
        }

        foreach (var teklif in talep.Teklifler)
            teklif.Onaylandi = false;

        talep.OnaylananTeklifId = null;
        talep.FirmaSiparisNolari?.Clear();
        talep.SiparisNo = "";
        talep.TeklifsizYonetimOnayi = false;
        talep.YonetimOnayKilitli = false;
        talep.YonetimOnaylayanAd = "";
        talep.YonetimOnaylayanEposta = "";
        talep.YonetimOnayTarihi = "";

        if ((talep.Teklifler?.Count ?? 0) > 0)
        {
            talep.Durum = SatinalmaTalepDurumlari.Karsilastirma;
            talep.Status = ProcurementStatus.Comparison;
        }
        else
        {
            talep.Durum = SatinalmaTalepDurumlari.TeklifGirisi;
            talep.Status = ProcurementStatus.QuoteEntry;
        }

        await TalepKaydetAsync(talep, iptal);
    }

    public async Task KalemTeklifiAtaAsync(SatinalmaTalep talep, Guid kalemId, Guid? teklifId, CancellationToken iptal = default)
    {
        if (FirmaOnayiSaltOkunur(talep))
            throw new InvalidOperationException("Onay kilitli taleplerde değişiklik yapılamaz.");

        var kalem = talep.Kalemler.FirstOrDefault(k => k.Id == kalemId)
            ?? throw new InvalidOperationException("Kalem bulunamadı.");

        if (teklifId is not null && !talep.Teklifler.Any(t => t.Id == teklifId))
            throw new InvalidOperationException("Teklif bulunamadı.");

        kalem.OnaylananTeklifId = teklifId;
        await TalepKaydetAsync(talep, iptal);
    }

    public async Task TumKalemlereTeklifAtaAsync(SatinalmaTalep talep, Guid teklifId, CancellationToken iptal = default)
    {
        if (FirmaOnayiSaltOkunur(talep))
            throw new InvalidOperationException("Onay kilitli taleplerde değişiklik yapılamaz.");

        if (!talep.Teklifler.Any(t => t.Id == teklifId))
            throw new InvalidOperationException("Teklif bulunamadı.");

        foreach (var kalem in talep.Kalemler)
            kalem.OnaylananTeklifId = teklifId;

        await TalepKaydetAsync(talep, iptal);
    }

    public async Task KalemBazliOnaylaAsync(SatinalmaTalep talep, CancellationToken iptal = default)
    {
        YetkiKontrol(SatinalmaOnayYetkisi.TeklifOnayVerebilir(_depo.AktifKullanici), "Teklif onay yetkiniz yok.");

        if (FirmaOnayiSaltOkunur(talep))
            throw new InvalidOperationException("Onay kilitli talepte onay değiştirilemez.");

        var onayliKalemler = talep.Kalemler.Where(k => k.OnaylananTeklifId != null).ToList();
        if (onayliKalemler.Count == 0)
            throw new InvalidOperationException("En az bir malzeme için firma seçin.");

        foreach (var teklif in talep.Teklifler)
            teklif.Onaylandi = onayliKalemler.Any(k => k.OnaylananTeklifId == teklif.Id);

        var anaTeklifId = onayliKalemler
            .GroupBy(k => k.OnaylananTeklifId!.Value)
            .OrderByDescending(g => g.Count())
            .First().Key;

        talep.OnaylananTeklifId = anaTeklifId;
        talep.YonetimOnerilenTeklifId = anaTeklifId;
        talep.Durum = SatinalmaTalepDurumlari.Onaylandi;

        talep.FirmaSiparisNolari ??= [];
        foreach (var teklifId in onayliKalemler.Select(k => k.OnaylananTeklifId!.Value).Distinct())
        {
            if (!talep.FirmaSiparisNolari.ContainsKey(teklifId))
                talep.FirmaSiparisNolari[teklifId] = _depo.YeniSiparisNoOlustur();
        }

        talep.SiparisNo = talep.FirmaSiparisNolari[anaTeklifId];

        YonetimOnayiKaydet(talep);
        await TalepKaydetAsync(talep, iptal);

        var anaTeklif = talep.Teklifler.FirstOrDefault(t => t.Id == anaTeklifId);
        var firmaSayisi = onayliKalemler.Select(k => k.OnaylananTeklifId).Distinct().Count();
        var firmaAdi = firmaSayisi == 1 ? anaTeklif?.FirmaAdi : null;

        var onayKayitlari = OnaylandiKayitlari(talep, firmaAdi);
        await _bildirimler.CokluEkleAsync(onayKayitlari, iptal);

        await _bildirimler.GecersizleriOkunduYapAsync(iptal);
    }

    private void YonetimOnayiKaydet(SatinalmaTalep talep)
    {
        var k = _depo.AktifKullanici;
        talep.YonetimOnaylayanUid = k?.Uid ?? "";
        talep.YonetimOnaylayanAd = k?.AdSoyad ?? "";
        talep.YonetimOnaylayanEposta = k?.Eposta ?? "";
        talep.YonetimOnayTarihi = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
        talep.YonetimOnayKilitli = true;
    }

    public void TeklifFiyatlariniHazirla(SatinalmaTalep talep, SatinalmaTeklif teklif)
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

    public IEnumerable<SatinalmaTalep> KullaniciTalepleri(string uid) =>
        _depo.Talepler.Where(t => SatinalmaTalepSahiplikYardimcisi.KullanicininTalebi(
            t, uid, _depo.AktifKullanici?.AdSoyad, _depo.AktifKullanici?.Eposta));

    /// <summary>Yönetim Talepler sekmesi — karar bekleyen yeni talepler.</summary>
    public IEnumerable<SatinalmaTalep> YonetimTalepleri() =>
        SatinalmaTalepKuyrugu.Filtrele(_depo.Talepler, SatinalmaTalepKuyrugu.YonetimTalepler);

    public IEnumerable<SatinalmaTalep> YonetimTeklifBekleyenleri() =>
        SatinalmaTalepKuyrugu.Filtrele(_depo.Talepler, SatinalmaTalepKuyrugu.YonetimTeklifBekleyen);

    public IEnumerable<SatinalmaTalep> YonetimTeklifOnayiBekleyenleri() =>
        SatinalmaTalepKuyrugu.Filtrele(_depo.Talepler, SatinalmaTalepKuyrugu.YonetimTeklifler);

    public IEnumerable<SatinalmaTalep> YonetimOnaylananTeklifleri() =>
        SatinalmaTalepKuyrugu.Filtrele(_depo.Talepler, SatinalmaTalepKuyrugu.OnaylananTeklif);

    public IEnumerable<SatinalmaTalep> YonetimOnaylananTalepleri() =>
        SatinalmaTalepKuyrugu.Filtrele(_depo.Talepler, SatinalmaTalepKuyrugu.OnaylananTalep);

    public IEnumerable<SatinalmaTalep> YonetimReddedilenleri() =>
        SatinalmaTalepKuyrugu.Filtrele(_depo.Talepler, SatinalmaTalepKuyrugu.Reddedildi);

    public IEnumerable<SatinalmaTalep> YonetimBekleyenleri() => YonetimTalepleri();

    /// <summary>Yönetim Geçmiş Onaylar — sipariş tamamlanmış arşiv.</summary>
    public IEnumerable<SatinalmaTalep> YonetimOnayGecmisi(string uid) =>
        _depo.Talepler.Where(t => SatinalmaTalepKuyrugu.YonetimGecmis(t, uid));

    /// <summary>Yönetim teklifsiz / acil onay geçmişi.</summary>
    public IEnumerable<SatinalmaTalep> YonetimGecmisTalepleri() =>
        SatinalmaTalepKuyrugu.Filtrele(_depo.Talepler, SatinalmaTalepKuyrugu.YonetimGecmisTalep);

    /// <summary>Yönetim teklifli onay geçmişi.</summary>
    public IEnumerable<SatinalmaTalep> YonetimGecmisTeklifliOnaylari() =>
        SatinalmaTalepKuyrugu.Filtrele(_depo.Talepler, SatinalmaTalepKuyrugu.YonetimGecmisTeklifli);

    public static string OnayTipiMetni(SatinalmaTalep talep)
    {
        if (talep.TalepTuru == TalepTurleri.Acil)
            return "Acil Onay";
        if (talep.TeklifsizYonetimOnayi)
            return "Teklifsiz Onay";
        if (talep.OnaylananTeklifId is not null)
            return "Teklif Onayı";
        return "Yönetim Onayı";
    }

    public IEnumerable<SatinalmaTalep> TeklifGirisiBekleyenleri() =>
        SatinalmaTalepKuyrugu.Filtrele(_depo.Talepler, SatinalmaTalepKuyrugu.SatinalmaTeklifGirisiAktif);

    public IEnumerable<SatinalmaTalep> KarsilastirmaBekleyenleri() =>
        SatinalmaTalepKuyrugu.Filtrele(_depo.Talepler, SatinalmaTalepKuyrugu.SatinalmaKarsilastirma);

    /// <summary>Mobil satınalma — tüm kayıtlı talepler (boş taslak hariç).</summary>
    public IEnumerable<SatinalmaTalep> TumKayitliTalepler() =>
        SatinalmaTalepKuyrugu.Filtrele(_depo.Talepler, SatinalmaTalepKuyrugu.KayitliTalep);

    public IEnumerable<SatinalmaTalep> KayitliTalepler() => TumKayitliTalepler();

    /// <summary>Onay bekleyen talepler — tüm roller tüm kayıtları takip eder.</summary>
    public IEnumerable<SatinalmaTalep> OnayBekleyenTalepler()
    {
        var rol = _depo.AktifKullanici?.Rol;
        var genis = KullaniciRolleri.OnayBekleyenGenisListe(rol);

        return SatinalmaTalepKuyrugu.Filtrele(_depo.Talepler, t => SatinalmaTalepKuyrugu.OnayBekleyenListede(t, genis));
    }

    /// <summary>Onaylanmış talepler — süreç takibi.</summary>
    public IEnumerable<SatinalmaTalep> OnaylanmisTalepler() =>
        SatinalmaTalepKuyrugu.Filtrele(_depo.Talepler, SatinalmaTalepKuyrugu.Onaylanmis);

    /// <summary>Yönetim onay bekleyen toplam (gelen + teklif) — masaüstü Onay Bekleyen ile uyumlu.</summary>
    public IEnumerable<SatinalmaTalep> YonetimKararBekleyenleri() =>
        SatinalmaTalepKuyrugu.Filtrele(_depo.Talepler, SatinalmaTalepKuyrugu.YonetimKararBekleyen);

    private static void YetkiKontrol(bool izin, string mesaj)
    {
        if (!izin)
            throw new InvalidOperationException(mesaj);
    }

    private void OlusturanRolunuTamamla(SatinalmaTalep talep)
    {
        if (!string.IsNullOrWhiteSpace(talep.OlusturanRol))
            return;

        if (!string.IsNullOrWhiteSpace(_depo.AktifKullanici?.Rol)
            && !string.IsNullOrWhiteSpace(talep.OlusturanUid)
            && talep.OlusturanUid == _depo.AktifKullanici.Uid)
            talep.OlusturanRol = _depo.AktifKullanici.Rol;
    }

    public IEnumerable<SatinalmaTalep> TeklifsizFirmaFiyatBekleyenleri() =>
        _depo.Talepler.Where(t => t.TeklifsizFirmaFiyatBekliyor);

    public async Task TeklifsizFirmaFiyatKaydetAsync(
        SatinalmaTalep talep,
        IEnumerable<TeklifsizFirmaFiyatGirdisi> girdiler,
        CancellationToken iptal = default)
    {
        talep.Kalemler ??= [];
        talep.Teklifler ??= [];
        talep.FirmaSiparisNolari ??= [];

        var liste = girdiler.Where(g => g.Gecerli).ToList();
        if (liste.Count == 0)
            throw new InvalidOperationException("En az bir kalem için firma ve birim fiyat girin.");

        foreach (var kalem in talep.Kalemler)
        {
            if (!liste.Any(g => g.KalemId == kalem.Id))
                throw new InvalidOperationException($"'{kalem.Malzeme}' için firma ve fiyat girilmedi.");
        }

        talep.Teklifler.RemoveAll(t => t.Aciklama.Contains("Yönetim onayı sonrası", StringComparison.Ordinal));

        foreach (var grup in liste.GroupBy(g => g.FirmaAdi.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            var teklif = new SatinalmaTeklif
            {
                FirmaAdi = grup.Key,
                Onaylandi = true,
                Aciklama = "Yönetim onayı sonrası firma/fiyat girişi",
                UsdKuru = _depo.Ayarlar.VarsayilanUsdKuru,
                EurKuru = _depo.Ayarlar.VarsayilanEurKuru
            };
            TeklifFiyatlariniHazirla(talep, teklif);

            foreach (var g in grup)
            {
                var kalem = talep.Kalemler.First(k => k.Id == g.KalemId);
                var fiyat = teklif.Fiyatlar.First(f => f.KalemId == g.KalemId);
                fiyat.BirimFiyat = g.BirimFiyat;
                fiyat.ParaBirimi = "TRY";
                fiyat.Hesapla(kalem.Miktar, teklif.UsdKuru, teklif.EurKuru);
                kalem.OnaylananTeklifId = teklif.Id;
            }

            teklif.FiyatlariHesapla(talep.Kalemler);
            talep.Teklifler.Add(teklif);

            if (string.IsNullOrWhiteSpace(talep.SiparisNo))
                talep.SiparisNo = _depo.YeniSiparisNoOlustur();
            talep.FirmaSiparisNolari[teklif.Id] = talep.SiparisNo;
        }

        var anaTeklifId = liste
            .GroupBy(g => g.FirmaAdi.Trim(), StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .First()
            .Select(g => talep.Kalemler.First(k => k.Id == g.KalemId).OnaylananTeklifId!.Value)
            .First();

        talep.OnaylananTeklifId = anaTeklifId;
        talep.Durum = SatinalmaTalepDurumlari.Onaylandi;
        await TalepKaydetAsync(talep, iptal);
    }

    public async Task TeklifSilAsync(SatinalmaTalep talep, Guid teklifId, CancellationToken iptal = default)
    {
        if (talep.YonetimOnayKilitli)
            throw new InvalidOperationException("Onay kilitli taleplerde teklif silinemez.");

        var teklif = talep.Teklifler.FirstOrDefault(t => t.Id == teklifId)
            ?? throw new InvalidOperationException("Teklif bulunamadı.");

        talep.Teklifler.Remove(teklif);
        foreach (var kalem in talep.Kalemler.Where(k => k.OnaylananTeklifId == teklifId))
            kalem.OnaylananTeklifId = null;

        if (talep.SatinalmaOnerisiElleSecildi && talep.YonetimOnerilenTeklifId == teklifId)
        {
            talep.YonetimOnerilenTeklifId = null;
            talep.SatinalmaOnerisiElleSecildi = false;
        }

        if (talep.Teklifler.Count == 0 && talep.Durum == SatinalmaTalepDurumlari.Karsilastirma)
            talep.Durum = SatinalmaTalepDurumlari.TeklifGirisi;

        await TalepKaydetAsync(talep, iptal);
    }

    public async Task TeklifGuncelleAsync(SatinalmaTalep talep, SatinalmaTeklif teklif, CancellationToken iptal = default)
    {
        if (talep.YonetimOnayKilitli)
            throw new InvalidOperationException("Onay kilitli taleplerde teklif güncellenemez.");

        teklif.FiyatlariHesapla(talep.Kalemler);
        var mevcut = talep.Teklifler.FirstOrDefault(t => t.Id == teklif.Id);
        if (mevcut is not null)
            talep.Teklifler.Remove(mevcut);
        talep.Teklifler.Add(teklif);

        if (talep.Durum == SatinalmaTalepDurumlari.TeklifGirisi)
            talep.Durum = SatinalmaTalepDurumlari.Karsilastirma;

        await TalepKaydetAsync(talep, iptal);
    }

    public async Task SatinalmaOnerisiSecAsync(SatinalmaTalep talep, Guid teklifId, CancellationToken iptal = default)
    {
        if (FirmaOnayiSaltOkunur(talep))
            throw new InvalidOperationException("Onay kilitli taleplerde öneri değiştirilemez.");

        if (!talep.Teklifler.Any(t => t.Id == teklifId))
            throw new InvalidOperationException("Teklif bulunamadı.");

        talep.YonetimOnerilenTeklifId = teklifId;
        talep.SatinalmaOnerisiElleSecildi = true;
        await TalepKaydetAsync(talep, iptal);
    }

    public async Task SatinalmaOnerisiOtomatigeAlAsync(SatinalmaTalep talep, CancellationToken iptal = default)
    {
        if (FirmaOnayiSaltOkunur(talep))
            throw new InvalidOperationException("Onay kilitli taleplerde öneri değiştirilemez.");

        talep.YonetimOnerilenTeklifId = null;
        talep.SatinalmaOnerisiElleSecildi = false;
        await TalepKaydetAsync(talep, iptal);
    }

    public List<OnaylananMalzemeSatiri> OnaylananMalzemeleriOlustur() =>
        OnaylananMalzemeOlusturucu.Olustur(_depo.Talepler);

    public int MalKabulBekleyenSayisi() =>
        OnaylananMalzemeleriOlustur().Count(OnaylananMalzemeOlusturucu.MalKabulBekleyen);

    public SatinalmaTalepKalemi? KalemBul(Guid talepId, Guid kalemId) =>
        _depo.Talepler.FirstOrDefault(t => t.Id == talepId)?.Kalemler.FirstOrDefault(k => k.Id == kalemId);

    public async Task MalKabulAsync(Guid talepId, Guid kalemId, double miktar, CancellationToken iptal = default)
    {
        YetkiKontrol(
            MobilYetkiServisi.MalKabulVeStokAktarYapabilir(_depo.AktifKullanici?.Rol),
            "Mal kabul işlemi yalnızca Admin veya Satınalma rolü tarafından yapılabilir.");

        if (miktar <= 0)
            throw new InvalidOperationException("Miktar sıfırdan büyük olmalıdır.");

        var talep = _depo.Talepler.First(t => t.Id == talepId);
        var kalem = talep.Kalemler.FirstOrDefault(k => k.Id == kalemId)
            ?? throw new InvalidOperationException("Kalem bulunamadı.");

        kalem.KabulEdilenMiktar += miktar;

        if (kalem.KabulEdilenMiktar > kalem.Miktar + 0.0001)
        {
            kalem.Miktar = kalem.KabulEdilenMiktar;
            talep.Teklifler ??= [];
            foreach (var teklif in talep.Teklifler)
                teklif.FiyatlariHesapla(talep.Kalemler);
        }

        if (kalem.KabulEdilenMiktar >= kalem.Miktar - 0.0001)
            kalem.SiparisTamamlandi = true;

        await TalepKaydetAsync(talep, iptal);
        var ozet = $"{kalem.Malzeme} · {miktar:N2} {kalem.Birim}";
        var actorUid = _depo.AktifKullanici?.Uid;
        await _bildirimler.CokluEkleAsync(
            BildirimRolPolitikasi.MalKabulEdildiHedefleri(talep.OlusturanUid, actorUid)
                .Select(h => BildirimKaydiOlustur(BildirimTipleri.MalKabulEdildi, talep, h.HedefRol, h.HedefUid, ek: ozet))
                .ToList(),
            iptal);
    }

    public async Task SiparisTamamlaAsync(Guid talepId, Guid kalemId, CancellationToken iptal = default)
    {
        YetkiKontrol(
            MobilYetkiServisi.MalKabulVeStokAktarYapabilir(_depo.AktifKullanici?.Rol),
            "Sipariş tamamlama yalnızca Admin veya Satınalma rolü tarafından yapılabilir.");

        var talep = _depo.Talepler.FirstOrDefault(t => t.Id == talepId)
            ?? throw new InvalidOperationException("Talep bulunamadı.");
        var kalem = talep.Kalemler.FirstOrDefault(k => k.Id == kalemId)
            ?? throw new InvalidOperationException("Kalem bulunamadı.");

        kalem.SiparisTamamlandi = true;

        if (talep.Kalemler.Where(k => k.OnaylananTeklifId != null).All(k => k.SiparisTamamlandi))
            talep.Durum = SatinalmaTalepDurumlari.SiparisOlusturuldu;

        await TalepKaydetAsync(talep, iptal);
    }

    public async Task SiparisNoAtaAsync(Guid talepId, CancellationToken iptal = default)
    {
        YetkiKontrol(
            MobilYetkiServisi.MalKabulVeStokAktarYapabilir(_depo.AktifKullanici?.Rol),
            "Sipariş numarası atama yalnızca Admin veya Satınalma rolü tarafından yapılabilir.");

        var talep = _depo.Talepler.FirstOrDefault(t => t.Id == talepId)
            ?? throw new InvalidOperationException("Talep bulunamadı.");

        if (string.IsNullOrWhiteSpace(talep.SiparisNo))
            talep.SiparisNo = _depo.YeniSiparisNoOlustur();

        talep.FirmaSiparisNolari ??= [];
        foreach (var teklifId in talep.Kalemler
                     .Where(k => k.OnaylananTeklifId != null)
                     .Select(k => k.OnaylananTeklifId!.Value)
                     .Distinct())
        {
            if (!talep.FirmaSiparisNolari.ContainsKey(teklifId))
                talep.FirmaSiparisNolari[teklifId] = talep.SiparisNo;
        }

        talep.Durum = SatinalmaTalepDurumlari.SiparisOlusturuldu;
        await TalepKaydetAsync(talep, iptal);
        var ek = $"Sipariş No: {talep.SiparisNo}";
        var actorUid = _depo.AktifKullanici?.Uid;
        await _bildirimler.CokluEkleAsync(
            BildirimRolPolitikasi.SiparisOlusturulduHedefleri(talep.OlusturanUid, actorUid)
                .Select(h => BildirimKaydiOlustur(BildirimTipleri.SiparisOlusturuldu, talep, h.HedefRol, h.HedefUid, ek: ek))
                .ToList(),
            iptal);
    }

    public bool StogaDahaOnceAktarildi(OnaylananMalzemeSatiri satir)
    {
        var belgeNo = satir.AktarimBelgeNo();
        return _depo.StokHareketleri.Any(h =>
            h.HareketTipi == StokHareketTipleri.Giris &&
            h.BelgeNo.Equals(belgeNo, StringComparison.OrdinalIgnoreCase) &&
            h.MalzemeAdi.Equals(satir.Malzeme, StringComparison.OrdinalIgnoreCase));
    }

    public async Task StogaAktarAsync(
        OnaylananMalzemeSatiri satir,
        double miktar,
        string kategori,
        string depoSaha,
        string teslimAlan,
        CancellationToken iptal = default)
    {
        YetkiKontrol(
            MobilYetkiServisi.MalKabulVeStokAktarYapabilir(_depo.AktifKullanici?.Rol),
            "Stoğa aktarım yalnızca Admin veya Satınalma rolü tarafından yapılabilir.");

        if (_stok is null)
            throw new InvalidOperationException("Stok servisi yapılandırılmamış.");

        if (miktar <= 0)
            throw new InvalidOperationException("Miktar sıfırdan büyük olmalıdır.");

        if (StogaDahaOnceAktarildi(satir))
            throw new InvalidOperationException(
                $"{satir.Malzeme} daha önce stoğa aktarılmış ({satir.AktarimBelgeNo()}).");

        var belgeNo = satir.AktarimBelgeNo();
        var tarih = DateTime.Now.ToString("dd.MM.yyyy");
        var islemYapan = _depo.AktifKullanici?.AdSoyad ?? "";

        await _stok.GirisYapAsync(
            tarih,
            [new StokIslemSatirKaydi
            {
                Malzeme = satir.Malzeme,
                Kategori = string.IsNullOrWhiteSpace(kategori) ? "Malzeme" : kategori,
                Miktar = miktar,
                Birim = satir.Birim,
                DepoSaha = depoSaha,
                BirimFiyat = satir.BirimFiyati
            }],
            belgeNo,
            islemYapan,
            teslimAlan,
            iptal);

        var kalem = KalemBul(satir.TalepId, satir.KalemId);
        if (kalem is not null && kalem.KabulEdilenMiktar < miktar)
        {
            kalem.KabulEdilenMiktar = Math.Max(kalem.KabulEdilenMiktar, miktar);
            if (kalem.KabulEdilenMiktar >= kalem.Miktar)
                kalem.SiparisTamamlandi = true;
            await TalepKaydetAsync(_depo.Talepler.First(t => t.Id == satir.TalepId), iptal);
        }

        var talep = _depo.Talepler.First(t => t.Id == satir.TalepId);
        var ek = $"{satir.Malzeme} · {miktar:N2} {satir.Birim} stoğa aktarıldı";
        var actorUid = _depo.AktifKullanici?.Uid;
        await _bildirimler.CokluEkleAsync(
            BildirimRolPolitikasi.MalKabulEdildiHedefleri(talep.OlusturanUid, actorUid)
                .Select(h => BildirimKaydiOlustur(BildirimTipleri.MalKabulEdildi, talep, h.HedefRol, h.HedefUid, ek: ek))
                .ToList(),
            iptal);
    }

    private List<BildirimKaydi> OnaylandiKayitlari(SatinalmaTalep talep, string? firmaAdi = null)
    {
        var kayitlar = new List<BildirimKaydi>();
        foreach (var (hedefRol, hedefUid) in OnayBildirimYardimcisi.OnaylandiHedefleri(talep.OlusturanUid, _depo.AktifKullanici?.Rol))
            kayitlar.Add(BildirimKaydiOlustur(BildirimTipleri.Onaylandi, talep, hedefRol, hedefUid, firmaAdi));
        return kayitlar;
    }

    private BildirimKaydi BildirimKaydiOlustur(
        string tip,
        SatinalmaTalep talep,
        string? hedefRol = null,
        string? hedefUid = null,
        string? firmaAdi = null,
        string? ek = null)
    {
        var (baslik, mesaj) = BildirimMetniOlusturucu.Olustur(
            tip, talep, firmaAdi, ek, _depo.AktifKullanici?.Rol);
        return new BildirimKaydi
        {
            Baslik = baslik,
            Mesaj = mesaj,
            Tip = tip,
            TalepId = talep.Id,
            HedefRol = hedefRol,
            HedefUid = hedefUid,
            OlusturanUid = _depo.AktifKullanici?.Uid ?? "",
            OlusturanAd = _depo.AktifKullanici?.AdSoyad ?? "",
            OlusturmaTarihi = DateTime.Now.ToString("dd.MM.yyyy HH:mm"),
            GuncellemeUtc = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
    }
}

using SatinalmaPro.Shared.Helpers;
using SatinalmaPro.Shared.Models;
using SatinalmaPro.Shared.Procurement;
using SatinalmaPro.Shared.Services;

namespace SatinalmaPro.E2eTest;

/// <summary>
/// Her enterprise status için ilgili rollerin en az bir sekmede talebi görebildiğini doğrular.
/// "Gidecek yeri olmayan" talep/teklif durumu taraması.
/// </summary>
public static class ProcurementAkisKapsamDenetimi
{
    private static readonly (string Rol, string Uid)[] Roller =
    [
        (KullaniciRolleri.Admin, "audit-admin"),
        (KullaniciRolleri.Yonetim, "audit-yonetim"),
        (KullaniciRolleri.Satinalma, "audit-satinalma"),
        (KullaniciRolleri.Sef, "audit-sef"),
        (KullaniciRolleri.Saha, "audit-saha"),
        (KullaniciRolleri.Depo, "audit-depo"),
        (KullaniciRolleri.Atolye, "audit-atolye")
    ];

    /// <summary>Status → bu role sahip kullanıcıların görmesi beklenen roller (boş = kimse listelemez).</summary>
    private static readonly Dictionary<string, string[]> BeklenenGorunurluk = new(StringComparer.OrdinalIgnoreCase)
    {
        [ProcurementStatus.Draft] = [KullaniciRolleri.Sef, KullaniciRolleri.Saha],
        [ProcurementStatus.Submitted] = [KullaniciRolleri.Yonetim, KullaniciRolleri.Satinalma, KullaniciRolleri.Sef, KullaniciRolleri.Saha],
        [ProcurementStatus.QuoteRequested] = [KullaniciRolleri.Yonetim, KullaniciRolleri.Satinalma, KullaniciRolleri.Sef, KullaniciRolleri.Saha],
        [ProcurementStatus.QuoteEntry] = [KullaniciRolleri.Satinalma, KullaniciRolleri.Sef, KullaniciRolleri.Saha],
        [ProcurementStatus.Comparison] = [KullaniciRolleri.Satinalma, KullaniciRolleri.Sef, KullaniciRolleri.Saha],
        [ProcurementStatus.ManagementQuoteReview] = [KullaniciRolleri.Yonetim, KullaniciRolleri.Satinalma, KullaniciRolleri.Sef, KullaniciRolleri.Saha],
        [ProcurementStatus.Approved] = [KullaniciRolleri.Yonetim, KullaniciRolleri.Satinalma, KullaniciRolleri.Sef, KullaniciRolleri.Saha],
        [ProcurementStatus.Ordered] = [KullaniciRolleri.Yonetim, KullaniciRolleri.Satinalma, KullaniciRolleri.Sef, KullaniciRolleri.Saha],
        [ProcurementStatus.Rejected] = [KullaniciRolleri.Yonetim, KullaniciRolleri.Satinalma, KullaniciRolleri.Sef, KullaniciRolleri.Saha],
        [ProcurementStatus.Completed] = [KullaniciRolleri.Yonetim, KullaniciRolleri.Satinalma, KullaniciRolleri.Sef, KullaniciRolleri.Saha]
    };

    public static E2eTestSonuc Calistir()
    {
        var sonuc = new E2eTestSonuc();
        sonuc.Adim("=== KAPSAM DENETİMİ: Status × Rol × Sekme matrisi ===");

        foreach (var status in ProcurementStatus.All)
        {
            sonuc.Adim($"Durum: {status}");

            if (!BeklenenGorunurluk.TryGetValue(status, out var beklenenRoller))
                beklenenRoller = [];

            foreach (var (rol, uid) in Roller)
            {
                var ornek = OrnekTalep(status, uid);
                var menu = ProcurementRouteMatcher.GetFlatMenu(TabFilterManager.NormalizeRole(rol));
                var gorunenRoute = menu
                    .Where(m => ProcurementRouteMatcher.Matches(m.Route, ornek, rol, uid))
                    .Select(m => m.Route)
                    .ToList();

                var rolBekleniyor = beklenenRoller.Contains(rol, StringComparer.OrdinalIgnoreCase);

                if (rolBekleniyor)
                {
                    sonuc.Bekle(gorunenRoute.Count > 0,
                        $"{rol}: en az 1 sekmede görünür ({string.Join(", ", gorunenRoute)})",
                        $"KRİTİK: {rol} için {status} durumunda liste sekmesi YOK (yetim talep riski)");
                }
                else if (gorunenRoute.Count > 0
                         && rol is not KullaniciRolleri.Admin
                         && rol is not KullaniciRolleri.Satinalma)
                {
                    sonuc.Bekle(false,
                        "",
                        $"{rol} beklenmeyen şekilde {status} görüyor: {string.Join(", ", gorunenRoute)}");
                }
            }
        }

        // Geçiş zinciri: her adım sonrası bir sonraki rol için sekme var mı
        sonuc.Adim("=== Geçiş zinciri doğrulaması (normal akış) ===");
        var ortam = new AutomasyonTestOrtami();
        var talep = ortam.TalepOlustur(AutomasyonTestOrtami.Sef);
        sonuc.Bekle(SekmeVar(talep, KullaniciRolleri.Yonetim, SatinalmaRoutes.YonetimGelenTalepler, AutomasyonTestOrtami.Yonetim.Uid),
            "submitted → yönetim Gelen Talepler", "submitted yönetimde görünmüyor");

        ortam.DetayAksiyonUygula(talep, Shared.Procurement.Detail.PurchaseRequestDetailAction.StartQuoteProcess, AutomasyonTestOrtami.Yonetim);
        talep = ortam.GuncelTalep(talep.Id);
        sonuc.Bekle(SekmeVar(talep, KullaniciRolleri.Satinalma, SatinalmaRoutes.SatinalmaTeklifIstenen, AutomasyonTestOrtami.Satinalma.Uid),
            "quote_requested → satınalma Teklif İstemi", "quote_requested satınalmada görünmüyor");

        ortam.EnterpriseTeklifEkle(talep, "A", 100, AutomasyonTestOrtami.Satinalma);
        talep = ortam.GuncelTalep(talep.Id);
        sonuc.Bekle(SekmeVar(talep, KullaniciRolleri.Satinalma, SatinalmaRoutes.SatinalmaTeklifGirilen, AutomasyonTestOrtami.Satinalma.Uid),
            "quote_entry → satınalma Teklif Girişi Bekleyenler", "quote_entry sekmesi yok");

        ortam.YonetimeTeklifGonder(talep, AutomasyonTestOrtami.Satinalma);
        talep = ortam.GuncelTalep(talep.Id);
        sonuc.Bekle(SekmeVar(talep, KullaniciRolleri.Yonetim, SatinalmaRoutes.YonetimTeklifGirilen, AutomasyonTestOrtami.Yonetim.Uid),
            "management_quote_review → yönetim Teklif İnceleme", "yönetim inceleme sekmesi yok");
        sonuc.Bekle(SekmeVar(talep, KullaniciRolleri.Satinalma, SatinalmaRoutes.YonetimTeklifGirilen, AutomasyonTestOrtami.Satinalma.Uid),
            "management_quote_review → satınalma Teklif İnceleme & Onay", "satınalma inceleme sekmesi yok");

        ortam.Temizle();
        return sonuc;
    }

    /// <summary>Rol kapsamının masaüstü route ve Android sekmelerinde aynı kalmasını doğrular.</summary>
    public static E2eTestSonuc PlatformMenuUyumu()
    {
        var sonuc = new E2eTestSonuc();
        sonuc.Adim("=== Platform menü uyumu (Desktop vs TabFilterManager) ===");

        foreach (var (rol, _) in Roller)
        {
            var desktop = ProcurementRouteMatcher.GetFlatMenu(TabFilterManager.NormalizeRole(rol))
                .Select(i => i.Route).OrderBy(r => r).ToList();
            var androidTabs = TabFilterManager.GetVisibleTabs(rol);
            if (rol is KullaniciRolleri.Sef or KullaniciRolleri.Saha)
            {
                var desktopHasTaleplerim = desktop.Contains(SatinalmaRoutes.Taleplerim);
                var androidHasManagement = androidTabs.Contains(ProcurementTab.ManagementApproval);
                sonuc.Bekle(desktopHasTaleplerim && !androidHasManagement
                             && !TabFilterManager.RequiresRequesterScope(rol),
                    $"{rol}: tüm talepleri izleme kapsamı masaüstü ve Android'de uyumlu",
                    $"{rol}: liste görünürlüğü hâlâ talep sahibiyle sınırlandırılmış veya onay sekmesine erişebiliyor");
            }

            if (rol == KullaniciRolleri.Depo)
            {
                sonuc.Bekle(!desktop.Any()
                             && androidTabs.SequenceEqual([ProcurementTab.StockStatus, ProcurementTab.StockMovements]),
                    "Depo: satınalma listeleri kapalı, yalnız stok sekmeleri açık",
                    "Depo: satınalma sürecine veya yanlış stok kapsamına erişebiliyor");
            }

            if (rol == KullaniciRolleri.Atolye)
            {
                sonuc.Bekle(!desktop.Any()
                             && androidTabs.SequenceEqual([ProcurementTab.StockStatus]),
                    "Atölye: yalnız stok durumu erişimi açık",
                    "Atölye: stok durumu dışındaki ekrana erişebiliyor");
            }
        }

        return sonuc;
    }

    /// <summary>Kullanıcı tarafından tanımlanan masaüstü modül ve stok sekmesi matrisi.</summary>
    public static E2eTestSonuc MasaustuModulMatrisi()
    {
        var sonuc = new E2eTestSonuc();
        sonuc.Adim("=== Masaüstü rol / modül matrisi ===");

        var tumOperasyonModulleri = new[]
        {
            "Alınan Malzemeler", "Stok Yönetimi", "Agrega", "Çimento", "Akaryakıt Takip",
            "Araç Filo Takip", "Finansman Raporlama", "Satınalma", "Raporlamalar"
        };

        var beklentiler = new Dictionary<string, IReadOnlyList<string>>
        {
            [KullaniciRolleri.Yonetim] = tumOperasyonModulleri,
            [KullaniciRolleri.Sef] = tumOperasyonModulleri,
            [KullaniciRolleri.Satinalma] = [.. tumOperasyonModulleri, "Ayarlar"],
            [KullaniciRolleri.Saha] = ["Satınalma", "Stok Yönetimi"],
            [KullaniciRolleri.Depo] = ["Stok Yönetimi"],
            [KullaniciRolleri.Atolye] = ["Stok Yönetimi"]
        };

        foreach (var (rol, beklenen) in beklentiler)
        {
            var gercek = MasaustuRolHaritasi.MasaustuModulleri(rol);
            sonuc.Bekle(
                gercek.OrderBy(x => x).SequenceEqual(beklenen.OrderBy(x => x), StringComparer.OrdinalIgnoreCase),
                $"{rol}: masaüstü modül kapsamı doğru",
                $"{rol}: masaüstü modül kapsamı beklenen rolle uyuşmuyor");
        }

        sonuc.Bekle(
            MasaustuRolHaritasi.StokSekmeleri(KullaniciRolleri.Saha)
                .SequenceEqual(["Stok Durumu", "Stok Hareketleri"]),
            "Saha: stok durumu ve stok hareketleri açık",
            "Saha: stok yönetiminde yetkisiz sekme açık");
        sonuc.Bekle(
            MasaustuRolHaritasi.StokSekmeleri(KullaniciRolleri.Sef)
                .SequenceEqual(["Stok Durumu", "Stok Hareketleri"]),
            "Şef: stok durumu ve stok hareketleri açık",
            "Şef: stok yönetiminde yetkisiz sekme açık");
        sonuc.Bekle(
            MasaustuRolHaritasi.StokSekmeleri(KullaniciRolleri.Atolye)
                .SequenceEqual(["Stok Durumu"]),
            "Atölye: yalnız stok durumu açık",
            "Atölye: stok yönetiminde yetkisiz sekme açık");

        return sonuc;
    }

    private static bool SekmeVar(SatinalmaTalep talep, string rol, string route, string uid) =>
        ProcurementRouteMatcher.IsRouteVisibleForRole(route, rol)
        && ProcurementRouteMatcher.Matches(route, talep, rol, uid);

    private static SatinalmaTalep OrnekTalep(string status, string requesterUid)
    {
        var talep = new SatinalmaTalep
        {
            Id = Guid.NewGuid(),
            OlusturanUid = requesterUid,
            TalepEden = "Denetim",
            Status = status,
            Kalemler = [new SatinalmaTalepKalemi { Id = Guid.NewGuid(), Malzeme = "Test", Miktar = 1 }]
        };

        talep.Durum = status switch
        {
            ProcurementStatus.Draft => SatinalmaTalepDurumlari.Taslak,
            ProcurementStatus.Submitted => SatinalmaTalepDurumlari.ImzaSurecinde,
            ProcurementStatus.QuoteRequested => SatinalmaTalepDurumlari.TeklifGirisi,
            ProcurementStatus.QuoteEntry => SatinalmaTalepDurumlari.TeklifGirisi,
            ProcurementStatus.Comparison => SatinalmaTalepDurumlari.Karsilastirma,
            ProcurementStatus.ManagementQuoteReview => SatinalmaTalepDurumlari.YonetimOnayinda,
            ProcurementStatus.Approved => SatinalmaTalepDurumlari.Onaylandi,
            ProcurementStatus.Ordered => SatinalmaTalepDurumlari.SiparisOlusturuldu,
            ProcurementStatus.Rejected => SatinalmaTalepDurumlari.Reddedildi,
            ProcurementStatus.Completed => SatinalmaTalepDurumlari.SiparisOlusturuldu,
            _ => SatinalmaTalepDurumlari.ImzaSurecinde
        };

        if (status == ProcurementStatus.QuoteEntry)
            talep.Teklifler.Add(new SatinalmaTeklif { Id = Guid.NewGuid(), FirmaAdi = "X", Fiyatlar = [new SatinalmaTeklifFiyati { BirimFiyat = 1 }] });

        if (status == ProcurementStatus.ManagementQuoteReview)
            talep.Teklifler.Add(new SatinalmaTeklif { Id = Guid.NewGuid(), FirmaAdi = "Y", Fiyatlar = [new SatinalmaTeklifFiyati { BirimFiyat = 1 }] });

        if (status == ProcurementStatus.Comparison)
            talep.TeklifDuzeltmeNotu = "revize";

        if (status == ProcurementStatus.Completed)
            talep.Kalemler[0].KabulEdilenMiktar = 1;

        if (status == ProcurementStatus.Approved || status == ProcurementStatus.Ordered)
        {
            talep.YonetimOnayKilitli = true;
            talep.Teklifler.Add(new SatinalmaTeklif { Id = Guid.NewGuid(), FirmaAdi = "Z", Onaylandi = true });
        }

        return talep;
    }
}

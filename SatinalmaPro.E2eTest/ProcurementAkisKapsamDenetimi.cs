using SatinalmaPro.Shared.Helpers;
using SatinalmaPro.Shared.Models;
using SatinalmaPro.Shared.Procurement;

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
        [ProcurementStatus.Submitted] = [KullaniciRolleri.Yonetim, KullaniciRolleri.Sef, KullaniciRolleri.Saha],
        [ProcurementStatus.QuoteRequested] = [KullaniciRolleri.Yonetim, KullaniciRolleri.Satinalma],
        [ProcurementStatus.QuoteEntry] = [KullaniciRolleri.Satinalma],
        [ProcurementStatus.Comparison] = [KullaniciRolleri.Satinalma],
        [ProcurementStatus.ManagementQuoteReview] = [KullaniciRolleri.Yonetim],
        [ProcurementStatus.Approved] = [KullaniciRolleri.Yonetim, KullaniciRolleri.Satinalma, KullaniciRolleri.Sef, KullaniciRolleri.Saha],
        [ProcurementStatus.Ordered] = [KullaniciRolleri.Yonetim, KullaniciRolleri.Satinalma, KullaniciRolleri.Depo, KullaniciRolleri.Sef, KullaniciRolleri.Saha, KullaniciRolleri.Atolye],
        [ProcurementStatus.Rejected] = [KullaniciRolleri.Yonetim, KullaniciRolleri.Satinalma, KullaniciRolleri.Sef, KullaniciRolleri.Saha],
        [ProcurementStatus.Completed] = [KullaniciRolleri.Yonetim, KullaniciRolleri.Satinalma, KullaniciRolleri.Admin]
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
                    sonuc.Uyar($"{rol} beklenmeyen şekilde {status} görüyor: {string.Join(", ", gorunenRoute)}");
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

        ortam.Temizle();
        return sonuc;
    }

    /// <summary>TabFilterManager (Android) ile masaüstü menü farklarını raporla.</summary>
    public static E2eTestSonuc PlatformMenuUyumu()
    {
        var sonuc = new E2eTestSonuc();
        sonuc.Adim("=== Platform menü uyumu (Desktop vs TabFilterManager) ===");

        foreach (var (rol, _) in Roller)
        {
            var desktop = ProcurementRouteMatcher.GetFlatMenu(TabFilterManager.NormalizeRole(rol))
                .Select(i => i.Route).OrderBy(r => r).ToList();
            var androidTabs = TabFilterManager.GetVisibleTabs(rol);
            // Android TabFilterManager sef/saha'ya full tabs veriyor — desktop kısıtlı
            if (rol is KullaniciRolleri.Sef or KullaniciRolleri.Saha)
            {
                var desktopHasTaleplerim = desktop.Contains(SatinalmaRoutes.Taleplerim);
                var androidHasManagement = androidTabs.Contains(ProcurementTab.ManagementApproval);
                sonuc.Bekle(desktopHasTaleplerim && !androidHasManagement == false,
                    $"{rol}: desktop Taleplerim={desktopHasTaleplerim}, Android ManagementApproval={androidHasManagement}",
                    "");
                if (androidHasManagement && desktop.All(r => r != SatinalmaRoutes.YonetimGelenTalepler))
                    sonuc.Uyar($"{rol}: Android TabFilterManager yönetim sekmeleri gösteriyor, masaüstü göstermiyor (bilinen fark — Android shell ayrı menü kullanabilir)");
            }

            if (rol == KullaniciRolleri.Atolye)
            {
                var desktopYoldaki = desktop.Contains(SatinalmaRoutes.SatinalmaSiparis);
                var androidYoldaki = androidTabs.Contains(ProcurementTab.ApprovedOrders);
                if (desktopYoldaki && !androidYoldaki)
                    sonuc.Uyar("Atölye: masaüstünde Yoldaki Malzemeler var, Android TabFilterManager'da ApprovedOrders yok — Android AtolyeShell kontrol edilmeli");
            }
        }

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

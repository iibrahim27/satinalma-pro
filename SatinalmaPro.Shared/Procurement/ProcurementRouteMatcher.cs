using SatinalmaPro.Shared.Helpers;
using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Shared.Procurement;

/// <summary>
/// Rol × route × enterprise status eşleştirmesi — Android ve masaüstü ortak kaynak.
/// </summary>
public static class ProcurementRouteMatcher
{
    private sealed record RouteRule(
        string Route,
        IReadOnlyList<string> StatusIn,
        bool RequiresReturnFlag = false,
        bool ExcludeMalKabulComplete = false);

    public static bool Matches(
        string route,
        SatinalmaTalep talep,
        string? role,
        string? currentUid)
    {
        if (!IsRouteVisibleForRole(route, role))
            return false;

        if (route == SatinalmaRoutes.SatinalmaIade)
        {
            return ProcurementStatusResolver.Resolve(talep)
                       .Equals(ProcurementStatus.Completed, StringComparison.OrdinalIgnoreCase)
                   && ProcurementRequestMapper.HasReturnFlag(talep);
        }

        if (RequiresRequesterScope(role))
        {
            if (string.IsNullOrWhiteSpace(currentUid))
                return false;
            if (!string.Equals(talep.OlusturanUid, currentUid, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        var rule = FindRule(route);
        if (rule is not null)
            return MatchesRule(rule, talep);

        return MatchesLegacyArchive(route, talep);
    }

    public static IReadOnlyList<string> StatusesForRoute(string route) =>
        FindRule(route)?.StatusIn ?? [];

    private static bool MatchesRule(RouteRule rule, SatinalmaTalep talep)
    {
        var status = ProcurementStatusResolver.Resolve(talep);
        if (!rule.StatusIn.Contains(status, StringComparer.OrdinalIgnoreCase))
            return false;

        if (rule.RequiresReturnFlag && !ProcurementRequestMapper.HasReturnFlag(talep))
            return false;

        if (rule.ExcludeMalKabulComplete && MalKabulTamamlandi(talep))
            return false;

        return true;
    }

    private static bool MatchesLegacyArchive(string route, SatinalmaTalep talep) => route switch
    {
        SatinalmaRoutes.YonetimDirekOnaylanan =>
            ProcurementStatusResolver.Resolve(talep) == ProcurementStatus.Approved
            && talep.TeklifsizYonetimOnayi,

        SatinalmaRoutes.YonetimOnayGecmisi or SatinalmaRoutes.SatinalmaOnayGecmisi =>
            SatinalmaTalepKuyrugu.YonetimOnayGecmisinde(talep),

        SatinalmaRoutes.YonetimGecmis =>
            SatinalmaTalepKuyrugu.YonetimGecmis(talep)
            || SatinalmaTalepKuyrugu.YonetimGecmisTeklifli(talep)
            || ProcurementStatusResolver.Resolve(talep) == ProcurementStatus.Completed,

        SatinalmaRoutes.SatinalmaTeklifDuzeltme =>
            ProcurementStatusResolver.Resolve(talep) == ProcurementStatus.Comparison
            && !string.IsNullOrWhiteSpace(talep.TeklifDuzeltmeNotu),

        SatinalmaRoutes.OnayBekleyen =>
            ProcurementStatusResolver.Resolve(talep) is ProcurementStatus.Submitted
                or ProcurementStatus.QuoteRequested
                or ProcurementStatus.QuoteEntry
                or ProcurementStatus.Comparison
                or ProcurementStatus.ManagementQuoteReview,

        _ => false
    };

    public static bool IsRouteVisibleForRole(string route, string? role)
    {
        var roleKey = TabFilterManager.NormalizeRole(role);
        return route switch
        {
            "stok-durum" => roleKey is "admin" or "satinalma" or "depo" or "atolye" or "sef" or "saha",
            _ => GetFlatMenu(roleKey).Any(i => i.Route == route)
        };
    }

    public static IReadOnlyList<ProcurementMenuItem> GetFlatMenu(string? roleKey) =>
        GetMenuGroups(roleKey).SelectMany(g => g.Items).ToList();

    public static IReadOnlyList<ProcurementMenuGroup> GetMenuGroups(string? role)
    {
        var roleKey = TabFilterManager.NormalizeRole(role);
        var gruplar = new List<ProcurementMenuGroup>();

        if (roleKey == "atolye")
        {
            gruplar.Add(new ProcurementMenuGroup(null,
            [
                new("Güncel Stok Durumu", "stok-durum"),
                new("Yoldaki Malzemeler", SatinalmaRoutes.SatinalmaSiparis)
            ]));
            return gruplar;
        }

        if (roleKey == "depo")
        {
            gruplar.Add(new ProcurementMenuGroup(null,
            [
                new("Yoldaki Malzemeler", SatinalmaRoutes.SatinalmaSiparis)
            ]));
            return gruplar;
        }

        var yonetimItems = new List<ProcurementMenuItem>();

        if (roleKey is "admin" or "yonetim")
        {
            if (roleKey == "admin")
                yonetimItems.Add(new("Satınalma Panosu", SatinalmaRoutes.Panosu));

            yonetimItems.AddRange(
            [
                new("Gelen Talepler", SatinalmaRoutes.YonetimGelenTalepler),
                new("Teklif İstenenler", SatinalmaRoutes.YonetimTeklifBekleyen),
                new("Teklif İnceleme & Onay", SatinalmaRoutes.YonetimTeklifGirilen),
                new("Onaylananlar", SatinalmaRoutes.YonetimOnaylananTeklifler),
                new("Reddedilenler", SatinalmaRoutes.YonetimRedVerilen)
            ]);

            if (roleKey == "admin")
            {
                yonetimItems.Add(new("Yönetim Onay Geçmişi", SatinalmaRoutes.YonetimOnayGecmisi));
                yonetimItems.Add(new("Direk Onaylanan Talepler", SatinalmaRoutes.YonetimDirekOnaylanan));
                yonetimItems.Add(new("Talep ve Onaylanan Teklifler Geçmişi", SatinalmaRoutes.YonetimGecmis));
            }
        }

        if (roleKey is "admin" or "satinalma")
        {
            if (roleKey == "admin")
                gruplar.Add(new ProcurementMenuGroup("Yönetim", yonetimItems));

            var satinalmaItems = new List<ProcurementMenuItem>
            {
                new("Satınalma Panosu", SatinalmaRoutes.Panosu),
                new("Gelen Talepler", SatinalmaRoutes.YonetimGelenTalepler),
                new("Teklif İstemi Yapılanlar", SatinalmaRoutes.SatinalmaTeklifIstenen),
                new("Teklif Girişi Bekleyenler", SatinalmaRoutes.SatinalmaTeklifGirilen),
                new("Teklif İnceleme & Onay", SatinalmaRoutes.YonetimTeklifGirilen),
                new("Fiyat Karşılaştırma", SatinalmaRoutes.SatinalmaKarsilastirma),
                new("Onaylananlar", SatinalmaRoutes.SatinalmaOnaylanan),
                new("Geçmiş Onaylananlar", SatinalmaRoutes.SatinalmaOnayGecmisi),
                new("Reddedilenler", SatinalmaRoutes.YonetimRedVerilen),
                new("Sipariş Yönetimi", SatinalmaRoutes.SatinalmaSiparis),
                new("Mal Kabul & Sevkiyat", SatinalmaRoutes.SatinalmaMalKabul),
                new("İade İşlemleri", SatinalmaRoutes.SatinalmaIade)
            };

            if (roleKey == "admin")
                satinalmaItems.Add(new("Tedarikçiler", SatinalmaRoutes.SatinalmaTedarikciler));

            gruplar.Add(new ProcurementMenuGroup(roleKey == "admin" ? "Satınalma" : null, satinalmaItems));
            return gruplar;
        }

        if (roleKey is "sef" or "saha")
        {
            gruplar.Add(new ProcurementMenuGroup(null,
            [
                new("Satınalma Panosu", SatinalmaRoutes.Panosu),
                new("Talep Oluşturma", SatinalmaRoutes.TalepForm),
                new("Taleplerim", SatinalmaRoutes.Taleplerim),
                new("Onaylananlar", SatinalmaRoutes.OnaylananTaleplerSaha),
                new("Reddedilenler", SatinalmaRoutes.YonetimRedVerilen),
                new("Yoldaki Malzemeler", SatinalmaRoutes.SatinalmaSiparis),
                new("Güncel Stok Durumu", "stok-durum")
            ]));
            return gruplar;
        }

        if (roleKey == "yonetim")
            gruplar.Add(new ProcurementMenuGroup(null, yonetimItems));

        return gruplar;
    }

    public static string FirstRoute(string? role)
    {
        var flat = GetFlatMenu(TabFilterManager.NormalizeRole(role));
        if (flat.Any(i => i.Route == SatinalmaRoutes.Panosu))
            return SatinalmaRoutes.Panosu;
        return flat.FirstOrDefault()?.Route ?? SatinalmaRoutes.Taleplerim;
    }

    public static bool RequiresRequesterScope(string? role) =>
        TabFilterManager.RequiresRequesterScope(role);

    private static RouteRule? FindRule(string route) => route switch
    {
        SatinalmaRoutes.YonetimGelenTalepler
            => new(route, [ProcurementStatus.Submitted]),

        SatinalmaRoutes.YonetimTeklifBekleyen or SatinalmaRoutes.SatinalmaTeklifIstenen
            => new(route, [ProcurementStatus.QuoteRequested]),

        SatinalmaRoutes.YonetimTeklifGirilen
            => new(route, [ProcurementStatus.ManagementQuoteReview]),

        SatinalmaRoutes.SatinalmaTeklifGirilen
            => new(route, [ProcurementStatus.QuoteEntry]),

        SatinalmaRoutes.SatinalmaKarsilastirma
            => new(route, [ProcurementStatus.Comparison]),

        SatinalmaRoutes.SatinalmaOnaylanan or SatinalmaRoutes.OnaylananTaleplerSaha
            => new(route, [ProcurementStatus.Approved]),

        SatinalmaRoutes.YonetimOnaylananTeklifler
            => new(route,
            [
                ProcurementStatus.Approved,
                ProcurementStatus.Ordered,
                ProcurementStatus.Completed
            ]),

        SatinalmaRoutes.YonetimRedVerilen
            => new(route, [ProcurementStatus.Rejected]),

        SatinalmaRoutes.SatinalmaSiparis
            => new(route, [ProcurementStatus.Ordered], ExcludeMalKabulComplete: true),

        SatinalmaRoutes.SatinalmaMalKabul
            => new(route, [ProcurementStatus.Completed]),

        SatinalmaRoutes.Taleplerim
            => new(route, [ProcurementStatus.Draft, ProcurementStatus.Submitted]),

        SatinalmaRoutes.SatinalmaIade
            => new(route, [ProcurementStatus.Completed], RequiresReturnFlag: true),

        _ => null
    };

    private static bool MalKabulTamamlandi(SatinalmaTalep talep)
    {
        talep.Kalemler ??= [];
        var kalemler = talep.Kalemler.Where(k => !string.IsNullOrWhiteSpace(k.Malzeme)).ToList();
        if (kalemler.Count == 0)
            return false;

        return kalemler.All(k =>
            k.SiparisTamamlandi || k.KabulEdilenMiktar >= k.Miktar - 0.0001);
    }
}

public sealed record ProcurementMenuItem(string Baslik, string Route);

public sealed record ProcurementMenuGroup(string? Baslik, IReadOnlyList<ProcurementMenuItem> Items);

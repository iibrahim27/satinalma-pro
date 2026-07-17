using System.Text;
using SatinalmaPro.E2eTest;

var ortam = new BellekTestOrtami();
var tumSonuclar = new List<E2eTestSonuc>();

Console.OutputEncoding = Encoding.UTF8;

try
{
    Console.WriteLine("Satınalma E2E Akış Testi başlıyor...\n");

    Console.WriteLine("=== PurchaseModuleAutomationTest (Enterprise) ===\n");
    var otomasyonOrtami = new AutomasyonTestOrtami();
    var otomasyonSonuclari = PurchaseModuleAutomationTest.TumSenaryolariCalistir(otomasyonOrtami);
    tumSonuclar.AddRange(otomasyonSonuclari);

    Console.WriteLine("\n=== Kapsam Denetimi (status × rol × sekme) ===\n");
    tumSonuclar.Add(ProcurementAkisKapsamDenetimi.Calistir());
    tumSonuclar.Add(ProcurementAkisKapsamDenetimi.PlatformMenuUyumu());
    tumSonuclar.Add(ProcurementAkisKapsamDenetimi.MasaustuModulMatrisi());

    var otomasyonEksik = otomasyonSonuclari.Sum(s => s.Eksikler.Count);
    Console.WriteLine($"\n=== Otomasyon Özet: {otomasyonEksik} eksik ===\n");

    Console.WriteLine("=== Legacy E2E Testleri ===\n");
    tumSonuclar.Add(E2eAkisTestleri.TamTeklifliAkis(ortam));
    ortam.Temizle();

    tumSonuclar.Add(E2eAkisTestleri.SahaTalepSahiplik(ortam));
    ortam.Temizle();

    tumSonuclar.Add(E2eAkisTestleri.TeklifsizOnay(ortam));
    ortam.Temizle();

    tumSonuclar.Add(E2eAkisTestleri.BildirimDuzeltmeAkisi(ortam));
    ortam.Temizle();

    tumSonuclar.Add(E2eAkisTestleri.KalemMiktarFirmaBolme(ortam));
    ortam.Temizle();

    var raporYolu = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "E2E_TEST_RAPORU.md");
    raporYolu = Path.GetFullPath(raporYolu);
    var rapor = RaporOlustur(tumSonuclar);
    File.WriteAllText(raporYolu, rapor, Encoding.UTF8);

    // Proje köküne de yaz
    var projeRapor = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "E2E_TEST_RAPORU.md"));
    File.WriteAllText(projeRapor, rapor, Encoding.UTF8);

    Console.WriteLine(rapor);

    var toplamEksik = tumSonuclar.Sum(s => s.Eksikler.Count);
    var toplamUyari = tumSonuclar.Sum(s => s.Uyarilar.Count);
    Console.WriteLine($"\n=== ÖZET: {toplamEksik} eksik, {toplamUyari} uyarı ===");
    Console.WriteLine($"PurchaseModuleAutomationTest: {(otomasyonEksik == 0 ? "BAŞARILI" : "HATALI")}");
    Console.WriteLine($"Kapsam denetimi: {(toplamEksik == 0 ? "BAŞARILI" : "HATALI")}");

    Environment.Exit(toplamEksik > 0 ? 1 : 0);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Test hatası: {ex}");
    Environment.Exit(2);
}

static string RaporOlustur(List<E2eTestSonuc> sonuclar)
{
    var sb = new StringBuilder();
    sb.AppendLine("# Satınalma E2E Test Raporu");
    sb.AppendLine($"Tarih: {DateTime.Now:yyyy-MM-dd HH:mm}");
    sb.AppendLine();
    sb.AppendLine("Test verileri bellek içi simülasyon ile oluşturuldu; Firebase'e yazılmadı.");
    sb.AppendLine("Akış: PurchaseModuleAutomationTest (enterprise status, FCM topic, Firestore güvenlik) + legacy E2E.");
    sb.AppendLine("Android karşılığı: `PurchaseModuleAutomationTest` (JVM) + `PurchaseModuleAutomationInstrumentedTest` (Logcat).");
    sb.AppendLine();

    var tumEksikler = new List<string>();
    var tumUyarilar = new List<string>();

    foreach (var s in sonuclar)
    {
        sb.AppendLine($"## {s.Adimlar.FirstOrDefault() ?? "Senaryo"}");
        sb.AppendLine();
        foreach (var adim in s.Adimlar.Skip(1))
            sb.AppendLine($"- {adim}");
        sb.AppendLine();

        if (s.Basarilar.Count > 0)
        {
            sb.AppendLine("### Geçen kontroller");
            foreach (var ok in s.Basarilar)
                sb.AppendLine($"- ✅ {ok}");
            sb.AppendLine();
        }

        if (s.Eksikler.Count > 0)
        {
            sb.AppendLine("### Eksik / Hatalı");
            foreach (var e in s.Eksikler)
            {
                sb.AppendLine($"- ❌ {e}");
                tumEksikler.Add(e);
            }
            sb.AppendLine();
        }

        if (s.Uyarilar.Count > 0)
        {
            sb.AppendLine("### Bilinen platform farkları (uyarı)");
            foreach (var u in s.Uyarilar)
            {
                sb.AppendLine($"- ⚠️ {u}");
                tumUyarilar.Add(u);
            }
            sb.AppendLine();
        }
    }

    sb.AppendLine("---");
    sb.AppendLine("## Genel Eksiklik Özeti");
    sb.AppendLine();

    if (tumEksikler.Count == 0)
        sb.AppendLine("Kritik eksik bulunamadı — simülasyon akışı tamamlandı.");
    else
    {
        sb.AppendLine("| # | Eksik |");
        sb.AppendLine("|---|-------|");
        for (var i = 0; i < tumEksikler.Count; i++)
            sb.AppendLine($"| {i + 1} | {tumEksikler[i]} |");
    }

    sb.AppendLine();
    sb.AppendLine("## Masaüstü vs Android — Bilinen Kalan Farklar (PDF/UX)");
    sb.AppendLine();
    sb.AppendLine("- Şartname editörü + sipariş PDF birleştirme (masaüstünde var, Android basit PDF)");
    sb.AppendLine("- İmza blokları / ayarlardan imza (masaüstünde var)");
    sb.AppendLine("- Sipariş Onay Formu çift imza PDF (masaüstünde var)");
    sb.AppendLine("- Malzeme katalog penceresi browse-all (masaüstünde var, Android autocomplete)");
    sb.AppendLine("- Yönetim geçmişi tek menü vs Android iki liste");
    sb.AppendLine("- Masaüstü bildirim tıklaması `MasaustuHedef` (liste ekranı), FCM/push `HedefRoute` (detay ekranı) — bilinçli UX ayrımı");
    sb.AppendLine();
    sb.AppendLine("## Test Verisi");
    sb.AppendLine();
    sb.AppendLine("Tüm test verileri bellek içi çalıştırıldı ve `Temizle()` ile silindi. Firebase/local dosyaya yazılmadı.");

    return sb.ToString();
}

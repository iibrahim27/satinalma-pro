using System.IO;
using SatinalmaPro.Services;

namespace SatinalmaPro.Views.Modules.Satinalma;

/// <summary>Masaüstü modül yeniden yazımı — yerel talep verisini bir kez sıfırlar (Android/bulut koduna dokunmaz).</summary>
public static class SatinalmaMasaustuSifirlama
{
    private const string BayrakDosyasi = "satinalma_masaustu_rewrite_v1.flag";

    public static void IlkAcilistaUygula()
    {
        var bayrak = Path.Combine(SatinalmaProKlasor.Yol, BayrakDosyasi);
        if (File.Exists(bayrak))
            return;

        SatinalmaDepo.TumTalepleriSifirla();

        Directory.CreateDirectory(SatinalmaProKlasor.Yol);
        File.WriteAllText(bayrak, DateTime.UtcNow.ToString("O"));
    }
}

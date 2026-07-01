using System.IO;
using SatinalmaPro.Helpers;

namespace SatinalmaPro.Services;

public static class SatinalmaProKlasor
{
    public static string Yol { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        UygulamaBilgisi.VeriKlasoruAdi);

    public static void Olustur() => Directory.CreateDirectory(Yol);

    public static string DosyaYolu(string dosyaAdi) => Path.Combine(Yol, dosyaAdi);

    public static string LogolarKlasoruYolu() => Path.Combine(Yol, "logos");
}

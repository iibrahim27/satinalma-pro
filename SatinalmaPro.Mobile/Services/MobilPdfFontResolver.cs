using PdfSharpCore.Fonts;

namespace SatinalmaPro.Mobile.Services;

/// <summary>
/// PdfSharpCore Android/MAUI icin zorunlu font cozucu.
/// Sadece PDF olusturulurken baslatilir (uygulama acilisinda degil).
/// </summary>
public sealed class MobilPdfFontResolver : IFontResolver
{
    private const string RegularFace = "OpenSans#Regular";
    private const string BoldFace = "OpenSans#Bold";
    private const string RegularPaket = "pdf_OpenSans-Regular.ttf";
    private const string BoldPaket = "pdf_OpenSans-Semibold.ttf";

    private static int _baslatildi;
    private static byte[]? _regular;
    private static byte[]? _bold;

    public string DefaultFontName => RegularFace;

    public static void Baslat()
    {
        if (Interlocked.Exchange(ref _baslatildi, 1) == 1)
            return;

        // Getter'a dokunma — PdfSharpCore Utils.FontResolver static ctor patlar.
        GlobalFontSettings.FontResolver = new MobilPdfFontResolver();
    }

    public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        _ = familyName;
        _ = isItalic;
        return new FontResolverInfo(isBold ? BoldFace : RegularFace);
    }

    public byte[] GetFont(string faceName) =>
        faceName == BoldFace ? BoldYukle() : RegularYukle();

    private static byte[] RegularYukle() =>
        _regular ??= PakettenOku(RegularPaket);

    private static byte[] BoldYukle() =>
        _bold ??= PakettenOku(BoldPaket);

    private static byte[] PakettenOku(string paketAdi)
    {
        using var stream = FileSystem.OpenAppPackageFileAsync(paketAdi).GetAwaiter().GetResult();
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }
}

namespace SatinalmaPro.Mobile.Services;

public static class MobilBelgePaylasServisi
{
    public static async Task PdfPaylasAsync(byte[] pdf, string dosyaAdi, string baslik = "PDF Paylaş")
    {
        if (pdf is null or { Length: 0 })
            throw new InvalidOperationException("PDF oluşturulamadı (boş dosya).");

        if (!dosyaAdi.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            dosyaAdi += ".pdf";

        dosyaAdi = string.Join("_", dosyaAdi.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        if (string.IsNullOrWhiteSpace(dosyaAdi))
            dosyaAdi = "belge.pdf";

        var yol = Path.Combine(FileSystem.CacheDirectory, dosyaAdi);
        await File.WriteAllBytesAsync(yol, pdf);

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = baslik,
                File = new ShareFile(yol, "application/pdf")
            });
        });
    }

    public static async Task PdfOlusturVePaylasAsync(
        Func<byte[]> pdfOlustur,
        string dosyaAdi,
        string baslik = "PDF Paylaş")
    {
        byte[] pdf;
        try
        {
            // PdfSharpCore font cozucusu ana thread'de guvenli calisir (Android).
            pdf = await MainThread.InvokeOnMainThreadAsync(pdfOlustur);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"PDF hazırlanamadı: {ex.Message}", ex);
        }

        await PdfPaylasAsync(pdf, dosyaAdi, baslik);
    }
}

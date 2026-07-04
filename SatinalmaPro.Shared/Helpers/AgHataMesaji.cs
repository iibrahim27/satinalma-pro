namespace SatinalmaPro.Shared.Helpers;

public static class AgHataMesaji
{
    public static string Turkcele(string? mesaj)
    {
        if (string.IsNullOrWhiteSpace(mesaj))
            return "Bağlantı hatası.";

        if (mesaj.Contains("quota", StringComparison.OrdinalIgnoreCase)
            || mesaj.Contains("RESOURCE_EXHAUSTED", StringComparison.OrdinalIgnoreCase))
        {
            return "Firebase günlük okuma kotası doldu. Birkaç saat sonra tekrar deneyin. "
                   + "Yönetici Firebase kotasını kontrol etmeli.";
        }

        if (mesaj.Contains("connection", StringComparison.OrdinalIgnoreCase)
            || mesaj.Contains("network", StringComparison.OrdinalIgnoreCase)
            || mesaj.Contains("SERVICE_NOT_AVAILABLE", StringComparison.OrdinalIgnoreCase)
            || mesaj.Contains("Unable to resolve host", StringComparison.OrdinalIgnoreCase)
            || mesaj.Contains("No address associated with hostname", StringComparison.OrdinalIgnoreCase)
            || mesaj.Contains("timed out", StringComparison.OrdinalIgnoreCase)
            || mesaj.Contains("SSL", StringComparison.OrdinalIgnoreCase))
        {
            return "İnternet bağlantısı kurulamadı. Bağlantınızı kontrol edip tekrar deneyin.";
        }

        return mesaj;
    }
}

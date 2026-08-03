namespace TalepPro.Helpers;

public static class TalepProArgumanlari
{
    public static (Guid? TalepId, string? Sekme) Coz(IEnumerable<string>? args)
    {
        Guid? talepId = null;
        string? sekme = null;
        foreach (var a in args ?? [])
        {
            if (a.StartsWith("--talep=", StringComparison.OrdinalIgnoreCase) &&
                Guid.TryParse(a["--talep=".Length..], out var id))
                talepId = id;
            else if (a.StartsWith("--sekme=", StringComparison.OrdinalIgnoreCase))
                sekme = a["--sekme=".Length..];
        }
        return (talepId, sekme);
    }

    public static string[] Olustur(Guid? talepId, string? sekme)
    {
        var list = new List<string>();
        if (talepId is { } id)
            list.Add($"--talep={id:D}");
        if (!string.IsNullOrWhiteSpace(sekme))
            list.Add($"--sekme={sekme}");
        return list.ToArray();
    }
}

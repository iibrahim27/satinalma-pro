using System.Text;
using System.Text.RegularExpressions;

namespace SatinalmaPro.Helpers;

public static partial class ZimmetMaddeYardimcisi
{
    [GeneratedRegex(@"^\d+[\.\)]\s", RegexOptions.Compiled)]
    private static partial Regex MaddeBaslangicRegex();

    /// <summary>
    /// Metni zimmet maddelerine ayırır. Numaralı satırlar (1. / 2)) yeni madde başlatır;
    /// alt satırlar önceki maddeye eklenir.
    /// </summary>
    public static List<string> Ayikla(string metin)
    {
        var lines = metin
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        if (lines.Count == 0)
            return [];

        var maddeler = new List<string>();
        var sb = new StringBuilder();

        foreach (var line in lines)
        {
            if (MaddeBaslangicRegex().IsMatch(line) && sb.Length > 0)
            {
                maddeler.Add(sb.ToString().Trim());
                sb.Clear();
            }

            if (sb.Length > 0)
                sb.Append(' ');

            sb.Append(line);
        }

        if (sb.Length > 0)
            maddeler.Add(sb.ToString().Trim());

        return maddeler;
    }
}

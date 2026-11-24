using System.Text;
using ImageEditor.Core.Models;

namespace ImageEditor.Core.Services;

public static class NameFormatter
{
    public static string FormatDisplayName(string rawName, int maxWords = 2, bool applyTitleCase = true)
    {
        if (string.IsNullOrWhiteSpace(rawName))
        {
            return string.Empty;
        }

        IEnumerable<string> words = rawName
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

        if (maxWords > 0)
        {
            words = words.Take(maxWords);
        }

        if (!applyTitleCase)
        {
            return string.Join(" ", words);
        }

        return string.Join(" ", words.Select(FormatWord));
    }

    private static string FormatWord(string word)
    {
        if (string.IsNullOrEmpty(word))
        {
            return string.Empty;
        }

        // Preserve initials separated by dots (e.g. "m." -> "M.", "t.k" -> "T.K")
        if (word.Contains('.'))
        {
            string[] segments = word.Split('.');
            for (int i = 0; i < segments.Length; i++)
            {
                if (segments[i].Length > 0)
                {
                    segments[i] = CapitalizeSegment(segments[i]);
                }
            }

            return string.Join(".", segments);
        }

        return CapitalizeSegment(word);
    }

    private static string CapitalizeSegment(string segment)
    {
        if (segment.Length == 1)
        {
            return char.ToUpperInvariant(segment[0]).ToString();
        }

        return char.ToUpperInvariant(segment[0]) + segment[1..].ToLowerInvariant();
    }

    public static string SanitizeFileName(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return "unnamed";
        }

        char[] invalidChars = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(input.Length);
        bool lastWasSeparator = false;

        foreach (char c in input.Trim())
        {
            if (invalidChars.Contains(c) || char.IsControl(c))
            {
                if (!lastWasSeparator)
                {
                    sb.Append('_');
                    lastWasSeparator = true;
                }
            }
            else if (char.IsWhiteSpace(c))
            {
                if (!lastWasSeparator)
                {
                    sb.Append(' ');
                    lastWasSeparator = true;
                }
            }
            else
            {
                sb.Append(c);
                lastWasSeparator = false;
            }
        }

        string cleaned = sb.ToString().Trim(' ', '.', '_');
        return string.IsNullOrEmpty(cleaned) ? "unnamed" : cleaned;
    }

    public static string BuildUniqueFileName(
        AttendeeRecord record,
        string formattedName,
        HashSet<string> usedNames)
    {
        string baseName = SanitizeFileName(
            string.IsNullOrWhiteSpace(formattedName) ? record.Name : formattedName);

        string candidate = baseName;
        int suffix = 2;
        while (!usedNames.Add(candidate))
        {
            candidate = $"{baseName}_{suffix}";
            suffix++;
        }

        return $"{candidate}.png";
    }
}

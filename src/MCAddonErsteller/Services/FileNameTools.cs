using System.Text;
using System.Text.RegularExpressions;

namespace MCAddonErsteller.Services;

public static class FileNameTools
{
    private static readonly Regex MultiUnderscoreRegex = new("_+", RegexOptions.Compiled);

    public static string ToSafeFileName(string? value, string fallback)
    {
        string text = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

        foreach (char invalid in Path.GetInvalidFileNameChars())
            text = text.Replace(invalid, '_');

        StringBuilder builder = new(text.Length);
        foreach (char c in text)
        {
            if (char.IsLetterOrDigit(c) || c is '_' or '-' or '.')
                builder.Append(c);
            else if (char.IsWhiteSpace(c))
                builder.Append('_');
            else
                builder.Append('_');
        }

        string safe = MultiUnderscoreRegex.Replace(builder.ToString(), "_").Trim('_', '.', ' ');
        return string.IsNullOrWhiteSpace(safe) ? fallback : safe;
    }

    public static string NormalizeVersion(string? value)
    {
        string text = string.IsNullOrWhiteSpace(value) ? "1.0.0" : value.Trim();
        if (text.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            text = text[1..];

        text = text.Replace('_', '.').Replace('-', '.');
        string[] parts = text.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        List<int> numbers = new();

        foreach (string part in parts)
        {
            string digits = new(part.Where(char.IsDigit).ToArray());
            if (int.TryParse(digits, out int number))
                numbers.Add(number);
        }

        while (numbers.Count < 3)
            numbers.Add(0);

        return string.Join('.', numbers.Take(4));
    }

    public static string VersionForFileName(string version)
    {
        return NormalizeVersion(version).Replace('.', '_');
    }
}

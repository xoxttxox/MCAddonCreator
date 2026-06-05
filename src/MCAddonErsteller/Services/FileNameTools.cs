using System.Text;
using System.Text.RegularExpressions;

namespace MCAddonCreator.Services;

public static partial class FileNameTools
{
  [GeneratedRegex("_+")]
  private static partial Regex MultiUnderscoreRegex();

  public static string ToSafeFileName(string? value, string fallback = "Addon")
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

    string safe = MultiUnderscoreRegex()
      .Replace(builder.ToString(), "_")
      .Trim('_', '.', ' ');

    return string.IsNullOrWhiteSpace(safe) ? "Addon" : safe;
  }

  public static string NormalizeVersion(string? value)
  {
    string text = string.IsNullOrWhiteSpace(value) ? "1.0.0" : value.Trim();

    if (text.StartsWith("v", StringComparison.OrdinalIgnoreCase))
      text = text[1..];

    text = text.Replace('_', '.').Replace('-', '.');

    string[] parts = text.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    List<int> numbers = [];

    foreach (string part in parts)
    {
      string digits = string.Concat(part.Where(char.IsDigit));

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
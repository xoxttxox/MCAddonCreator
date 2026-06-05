using System.Text.Json;
using MCAddonCreator.Models;

namespace MCAddonCreator.Services;

// This comment ensures the file is touched so consumers that rely on the new Log API will load this assembly.
public static class ManifestReader
{
  public static ManifestInfo Read(string manifestPath, Action<string, MCAddonCreator.Models.LogLevel>? log = null)
  {
    log?.Invoke($"Reading manifest: {manifestPath}", MCAddonCreator.Models.LogLevel.Info);

    if (string.IsNullOrWhiteSpace(manifestPath))
    {
      log?.Invoke("Manifest path must not be empty.", MCAddonCreator.Models.LogLevel.Error);
      throw new ArgumentException("Manifest path must not be empty.", nameof(manifestPath));
    }

    if (!File.Exists(manifestPath))
    {
      log?.Invoke($"Manifest file was not found: {manifestPath}", MCAddonCreator.Models.LogLevel.Error);
      throw new FileNotFoundException("Manifest file was not found.", manifestPath);
    }

    using FileStream stream = File.OpenRead(manifestPath);
    using JsonDocument document = JsonDocument.Parse(stream, new JsonDocumentOptions
    {
      AllowTrailingCommas = true,
      CommentHandling = JsonCommentHandling.Skip
    });

    JsonElement root = document.RootElement;

    if (root.ValueKind != JsonValueKind.Object)
      throw new InvalidDataException("manifest.json is invalid.");

    JsonElement header = root.TryGetProperty("header", out JsonElement h) && h.ValueKind == JsonValueKind.Object
      ? h
      : root;

    var info = new ManifestInfo
    {
      Name = ReadString(header, "name", "Unbekannt"),
      Description = ReadString(header, "description", string.Empty),
      Uuid = ReadString(header, "uuid", string.Empty),
      Version = ReadVersion(header, "version", "1.0.0"),
      Kind = ReadKind(root)
    };

    log?.Invoke($"Manifest parsed: {info.Name} v{info.Version} ({info.Kind})", MCAddonCreator.Models.LogLevel.Info);

    return info;
  }

  private static string ReadString(JsonElement element, string propertyName, string fallback)
  {
    if (element.ValueKind != JsonValueKind.Object)
      return fallback;

    if (!element.TryGetProperty(propertyName, out JsonElement value))
      return fallback;

    return value.ValueKind == JsonValueKind.String
      ? value.GetString() ?? fallback
      : fallback;
  }

  private static string ReadVersion(JsonElement element, string propertyName, string fallback)
  {
    if (element.ValueKind != JsonValueKind.Object)
      return fallback;

    if (!element.TryGetProperty(propertyName, out JsonElement value))
      return fallback;

    if (value.ValueKind == JsonValueKind.Array)
    {
      List<int> parts = [];

      foreach (JsonElement part in value.EnumerateArray())
      {
        if (part.ValueKind == JsonValueKind.Number && part.TryGetInt32(out int number))
          parts.Add(Math.Max(0, number));
      }

      if (parts.Count > 0)
        return FileNameTools.NormalizeVersion(string.Join('.', parts));
    }

    if (value.ValueKind == JsonValueKind.String)
      return FileNameTools.NormalizeVersion(value.GetString());

    return FileNameTools.NormalizeVersion(fallback);
  }

  private static string ReadKind(JsonElement root)
  {
    if (!root.TryGetProperty("modules", out JsonElement modules) || modules.ValueKind != JsonValueKind.Array)
      return "unknown";

    bool hasResources = false;
    bool hasDataOrScript = false;

    foreach (JsonElement module in modules.EnumerateArray())
    {
      if (module.ValueKind != JsonValueKind.Object)
        continue;

      if (!module.TryGetProperty("type", out JsonElement typeElement) || typeElement.ValueKind != JsonValueKind.String)
        continue;

      string? type = typeElement.GetString()?.Trim().ToLowerInvariant();

      if (type == "resources")
        hasResources = true;
      else if (type is "data" or "script")
        hasDataOrScript = true;
    }

    if (hasResources && hasDataOrScript)
      return "mixed";

    if (hasResources)
      return "resource";

    if (hasDataOrScript)
      return "behavior";

    return "unknown";
  }
}
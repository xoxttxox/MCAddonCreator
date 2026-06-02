using System.Text.Json;
using MCAddonErsteller.Models;

namespace MCAddonErsteller.Services;

public static class ManifestReader
{
    public static ManifestInfo Read(string manifestPath)
    {
        using FileStream stream = File.OpenRead(manifestPath);
        using JsonDocument document = JsonDocument.Parse(stream, new JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip
        });

        JsonElement root = document.RootElement;
        JsonElement header = root.TryGetProperty("header", out JsonElement h) ? h : root;

        return new ManifestInfo
        {
            Name = ReadString(header, "name", "Unbekannt"),
            Description = ReadString(header, "description", string.Empty),
            Uuid = ReadString(header, "uuid", string.Empty),
            Version = ReadVersion(header, "version", "1.0.0"),
            Kind = ReadKind(root)
        };
    }

    private static string ReadString(JsonElement element, string propertyName, string fallback)
    {
        if (element.TryGetProperty(propertyName, out JsonElement value) && value.ValueKind == JsonValueKind.String)
            return value.GetString() ?? fallback;

        return fallback;
    }

    private static string ReadVersion(JsonElement element, string propertyName, string fallback)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement value))
            return fallback;

        if (value.ValueKind == JsonValueKind.Array)
        {
            List<int> parts = new();
            foreach (JsonElement part in value.EnumerateArray())
            {
                if (part.ValueKind == JsonValueKind.Number && part.TryGetInt32(out int number))
                    parts.Add(number);
            }

            if (parts.Count > 0)
                return string.Join('.', parts);
        }

        if (value.ValueKind == JsonValueKind.String)
            return value.GetString() ?? fallback;

        return fallback;
    }

    private static string ReadKind(JsonElement root)
    {
        if (!root.TryGetProperty("modules", out JsonElement modules) || modules.ValueKind != JsonValueKind.Array)
            return "unknown";

        bool hasResources = false;
        bool hasDataOrScript = false;

        foreach (JsonElement module in modules.EnumerateArray())
        {
            if (!module.TryGetProperty("type", out JsonElement typeElement) || typeElement.ValueKind != JsonValueKind.String)
                continue;

            string? type = typeElement.GetString()?.ToLowerInvariant();
            if (type == "resources")
                hasResources = true;
            if (type is "data" or "script")
                hasDataOrScript = true;
        }

        if (hasResources && !hasDataOrScript)
            return "resource";
        if (hasDataOrScript && !hasResources)
            return "behavior";
        if (hasDataOrScript && hasResources)
            return "mixed";

        return "unknown";
    }
}

namespace MCAddonErsteller.Models;

public sealed class ManifestInfo
{
    public string Name { get; init; } = "Unbekannt";
    public string Description { get; init; } = string.Empty;
    public string Version { get; init; } = "1.0.0";
    public string Uuid { get; init; } = string.Empty;
    public string Kind { get; init; } = "unknown";
}

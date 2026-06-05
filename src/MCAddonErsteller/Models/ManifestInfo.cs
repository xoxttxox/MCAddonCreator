namespace MCAddonCreator.Models;

public sealed class ManifestInfo
{
  public required string Name { get; init; }
  public required string Description { get; init; }
  public required string Version { get; init; }
  public required string Uuid { get; init; }
  public required string Kind { get; init; }
}
namespace MCAddonCreator.Models;

public sealed class UpdateResult
{
  public string CurrentVersion { get; init; } = string.Empty;
  public string LatestVersion { get; init; } = string.Empty;
  public bool IsUpdateAvailable { get; init; }
  public string ReleaseUrl { get; init; } = string.Empty;
}
namespace MCAddonCreator.Models;

public sealed class ResolvedPack
{
  public required string SourcePath { get; init; }
  public required string RootDirectory { get; init; }
  public required string ArchiveFolderName { get; init; }
  public required ManifestInfo Manifest { get; init; }
  public string? TemporaryDirectory { get; init; }
}
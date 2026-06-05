namespace MCAddonCreator.Models;

public sealed class BuildOptions
{
  public string? BehaviorPackPath { get; init; }
  public string? ResourcePackPath { get; init; }
  public bool IncludeBehaviorPack { get; init; }
  public bool IncludeResourcePack { get; init; }
  public required string PackageName { get; init; }
  public required string Version { get; init; }
  public required string OutputDirectory { get; init; }
  public Action<string, LogLevel>? Log { get; init; }
  public Action<string>? Status { get; init; }
  public IProgress<double>? Progress { get; init; }
  public int StepDelayMilliseconds { get; init; } = 180;
}
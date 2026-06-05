using System.IO.Compression;
using MCAddonCreator.Models;

namespace MCAddonCreator.Services;

public static class McAddonBuilder
{
  public static Task<string> BuildAsync(BuildOptions options, CancellationToken cancellationToken = default)
  {
    return Task.Run(() => Build(options, cancellationToken), cancellationToken);
  }

  private static string Build(BuildOptions options, CancellationToken cancellationToken)
  {
    ValidateOptions(options);

    List<ResolvedPack> packs = [];
    List<string> tempDirectories = [];

    try
    {
      Report(options, 0, "Preparing build ...");
      StepDelay(options, cancellationToken);

      options.Log?.Invoke("Checking selection ...", MCAddonCreator.Models.LogLevel.Info);
      StepDelay(options, cancellationToken);

      if (options.IncludeBehaviorPack)
      {
        Report(options, 8, "Reading Behavior Pack ...");
        options.Log?.Invoke("Reading BP manifest.json ...", MCAddonCreator.Models.LogLevel.Info);

        ResolvedPack bp = PackResolver.Resolve(options.BehaviorPackPath!, "BP", options.Log);
        packs.Add(bp);

        if (bp.TemporaryDirectory is not null)
          tempDirectories.Add(bp.TemporaryDirectory);

        options.Log?.Invoke($"BP: {bp.Manifest.Name} | Version {bp.Manifest.Version} | Type {bp.Manifest.Kind}", MCAddonCreator.Models.LogLevel.Info);
        WarnIfKindMismatch(bp, expected: "behavior", options.Log);

        StepDelay(options, cancellationToken);
      }

      if (options.IncludeResourcePack)
      {
        Report(options, 16, "Reading Resource Pack ...");
        options.Log?.Invoke("Reading RP manifest.json ...", MCAddonCreator.Models.LogLevel.Info);

        ResolvedPack rp = PackResolver.Resolve(options.ResourcePackPath!, "RP", options.Log);
        packs.Add(rp);

        if (rp.TemporaryDirectory is not null)
          tempDirectories.Add(rp.TemporaryDirectory);

        options.Log?.Invoke($"RP: {rp.Manifest.Name} | Version {rp.Manifest.Version} | Type {rp.Manifest.Kind}", MCAddonCreator.Models.LogLevel.Info);
        WarnIfKindMismatch(rp, expected: "resource", options.Log);

        StepDelay(options, cancellationToken);
      }

      Report(options, 24, "Preparing output ...");
      Directory.CreateDirectory(options.OutputDirectory);

      string safeName = FileNameTools.ToSafeFileName(options.PackageName, "MeinAddon");
      string safeVersion = FileNameTools.VersionForFileName(options.Version);
      string outputPath = Path.Combine(options.OutputDirectory, $"{safeName}_v{safeVersion}.mcaddon");

      if (File.Exists(outputPath))
      {
        options.Log?.Invoke("Existing file will be replaced ...", MCAddonCreator.Models.LogLevel.Warning);
        File.Delete(outputPath);
      }

      options.Log?.Invoke($"Output: {outputPath}", MCAddonCreator.Models.LogLevel.Info);
      StepDelay(options, cancellationToken);

      Report(options, 30, "Counting files ...");

      List<PackFileList> packFileLists =
      [
        .. packs.Select(pack => new PackFileList(pack, [.. EnumeratePackFiles(pack.RootDirectory)]))
      ];

      int totalFiles = packFileLists.Sum(pack => pack.Files.Count);
      options.Log?.Invoke($"Files found: {totalFiles}", MCAddonCreator.Models.LogLevel.Info);
      StepDelay(options, cancellationToken);

      using FileStream stream = File.Create(outputPath);
      using ZipArchive archive = new(stream, ZipArchiveMode.Create);

      HashSet<string> usedFolderNames = new(StringComparer.OrdinalIgnoreCase);
      int processedFiles = 0;

      foreach (PackFileList packFiles in packFileLists)
      {
        cancellationToken.ThrowIfCancellationRequested();

        ResolvedPack pack = packFiles.Pack;
        string archiveFolderName = GetUniqueFolderName(pack.ArchiveFolderName, usedFolderNames);
        usedFolderNames.Add(archiveFolderName);

        Report(options, CalculateFileProgress(processedFiles, totalFiles), $"Packing {archiveFolderName} ...");
          options.Log?.Invoke($"Packing {archiveFolderName} ({packFiles.Files.Count} files) ...", MCAddonCreator.Models.LogLevel.Info);
        StepDelay(options, cancellationToken);

        foreach (string filePath in packFiles.Files)
        {
          cancellationToken.ThrowIfCancellationRequested();

          string relativePath = Path.GetRelativePath(pack.RootDirectory, filePath)
            .Replace(Path.DirectorySeparatorChar, '/')
            .Replace(Path.AltDirectorySeparatorChar, '/');

          string entryName = archiveFolderName + "/" + relativePath;
          archive.CreateEntryFromFile(filePath, entryName, CompressionLevel.Optimal);

          processedFiles++;
          Report(options, CalculateFileProgress(processedFiles, totalFiles), $"Packing files {processedFiles}/{totalFiles} ...");

          if (ShouldLogFileProgress(processedFiles, totalFiles))
            options.Log?.Invoke($"Packing files: {processedFiles}/{totalFiles}", MCAddonCreator.Models.LogLevel.Info);
        }

        options.Log?.Invoke($"{archiveFolderName} packed.", MCAddonCreator.Models.LogLevel.Success);
        StepDelay(options, cancellationToken);
      }

      Report(options, 96, "Finalizing MCADDON ...");
      options.Log?.Invoke("Writing ZIP structure as .mcaddon ...", MCAddonCreator.Models.LogLevel.Info);
      StepDelay(options, cancellationToken);

      Report(options, 100, "Done.");
      options.Log?.Invoke("Done. MCADDON was created successfully.", MCAddonCreator.Models.LogLevel.Success);

      return outputPath;
    }
    finally
    {
      foreach (string tempDirectory in tempDirectories.Distinct(StringComparer.OrdinalIgnoreCase))
        TryDeleteDirectory(tempDirectory);
    }
  }

  private static void ValidateOptions(BuildOptions options)
  {
    if (!options.IncludeBehaviorPack && !options.IncludeResourcePack)
      throw new InvalidOperationException("Please select at least BP or RP.");

    if (options.IncludeBehaviorPack && string.IsNullOrWhiteSpace(options.BehaviorPackPath))
      throw new InvalidOperationException("BP is enabled but no BP source was selected.");

    if (options.IncludeResourcePack && string.IsNullOrWhiteSpace(options.ResourcePackPath))
      throw new InvalidOperationException("RP is enabled but no RP source was selected.");

    if (string.IsNullOrWhiteSpace(options.PackageName))
      throw new InvalidOperationException("Please enter an addon name.");

    if (string.IsNullOrWhiteSpace(options.OutputDirectory))
      throw new InvalidOperationException("Please choose an output directory.");
  }

  private static void WarnIfKindMismatch(ResolvedPack pack, string expected, Action<string, MCAddonCreator.Models.LogLevel>? log)
  {
    if (pack.Manifest.Kind is "unknown" or "mixed")
      return;

    if (!pack.Manifest.Kind.Equals(expected, StringComparison.OrdinalIgnoreCase))
    {
      string expectedLabel = expected == "behavior" ? "Behavior Pack" : "Resource Pack";
      log?.Invoke($"Warning: {pack.ArchiveFolderName} does not look like a {expectedLabel} according to manifest.json.", MCAddonCreator.Models.LogLevel.Warning);
    }
  }

  private static IEnumerable<string> EnumeratePackFiles(string rootDirectory)
  {
    return Directory
      .EnumerateFiles(rootDirectory, "*", SearchOption.AllDirectories)
      .Where(path => !IsIgnored(path))
      .OrderBy(path => path, StringComparer.OrdinalIgnoreCase);
  }

  private static bool IsIgnored(string filePath)
  {
    string normalized = filePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

    string[] ignoredSegments =
    [
      $"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}",
      $"{Path.DirectorySeparatorChar}.vs{Path.DirectorySeparatorChar}",
      $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
      $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
      $"{Path.DirectorySeparatorChar}__MACOSX{Path.DirectorySeparatorChar}",
    ];

    if (ignoredSegments.Any(segment => normalized.Contains(segment, StringComparison.OrdinalIgnoreCase)))
      return true;

    string fileName = Path.GetFileName(filePath);

    return fileName.Equals("Thumbs.db", StringComparison.OrdinalIgnoreCase)
      || fileName.Equals(".DS_Store", StringComparison.OrdinalIgnoreCase);
  }

  private static string GetUniqueFolderName(string folderName, HashSet<string> used)
  {
    if (!used.Contains(folderName))
      return folderName;

    int index = 2;
    string candidate;

    do
    {
      candidate = folderName + "_" + index;
      index++;
    }
    while (used.Contains(candidate));

    return candidate;
  }

  private static double CalculateFileProgress(int processedFiles, int totalFiles)
  {
    if (totalFiles <= 0)
      return 95;

    const double start = 34;
    const double end = 94;

    double fileProgress = processedFiles / (double)totalFiles;
    return start + fileProgress * (end - start);
  }

  private static bool ShouldLogFileProgress(int processedFiles, int totalFiles)
  {
    if (totalFiles <= 0)
      return false;

    if (processedFiles == totalFiles)
      return true;

    if (totalFiles <= 20)
      return true;

    return processedFiles % 25 == 0;
  }

  private static void Report(BuildOptions options, double progress, string status)
  {
    options.Progress?.Report(progress);
    options.Status?.Invoke(status);
  }

  private static void StepDelay(BuildOptions options, CancellationToken cancellationToken)
  {
    int delay = Math.Clamp(options.StepDelayMilliseconds, 0, 1000);

    if (delay <= 0)
      return;

    cancellationToken.WaitHandle.WaitOne(delay);
    cancellationToken.ThrowIfCancellationRequested();
  }

  private static void TryDeleteDirectory(string directory)
  {
    try
    {
      if (Directory.Exists(directory))
        Directory.Delete(directory, recursive: true);
    }
    catch
    {
      // Temp cleanup best effort only.
    }
  }

  private sealed record PackFileList(ResolvedPack Pack, List<string> Files);
}
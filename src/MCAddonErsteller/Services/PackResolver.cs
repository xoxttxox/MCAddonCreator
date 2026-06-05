using System.IO.Compression;
using MCAddonCreator.Models;

namespace MCAddonCreator.Services;

public static class PackResolver
{
  private static readonly string[] IgnoredDirectoryNames =
  [
    "__MACOSX",
    ".git",
    ".github",
    "bin",
    "obj"
  ];

  public static ResolvedPack Resolve(string sourcePath, string role, Action<string, MCAddonCreator.Models.LogLevel>? log = null)
  {
    if (string.IsNullOrWhiteSpace(sourcePath))
    {
      log?.Invoke($"{role}: No path provided.", MCAddonCreator.Models.LogLevel.Error);
      throw new InvalidOperationException($"{role}: No path provided.");
    }

    if (Directory.Exists(sourcePath))
      return ResolveDirectory(sourcePath, role, temporaryDirectory: null, log: log);

    if (File.Exists(sourcePath))
      return ResolveZipLikeFile(sourcePath, role, log);

    log?.Invoke($"{role}: Source not found: {sourcePath}", MCAddonCreator.Models.LogLevel.Error);
    throw new FileNotFoundException($"{role}: Source not found.", sourcePath);
  }

  public static void DeleteTemporaryDirectory(ResolvedPack pack)
  {
    if (pack.TemporaryDirectory is null)
      return;

    TryDeleteDirectory(pack.TemporaryDirectory);
  }

  private static ResolvedPack ResolveZipLikeFile(string sourcePath, string role, Action<string, MCAddonCreator.Models.LogLevel>? log = null)
  {
    string extension = Path.GetExtension(sourcePath).ToLowerInvariant();

    if (extension is not ".zip" and not ".mcpack" and not ".mcaddon")
    {
      log?.Invoke($"{role}: Unsupported extension {extension}", MCAddonCreator.Models.LogLevel.Error);
      throw new InvalidOperationException($"{role}: Only .zip, .mcpack, .mcaddon or folders are supported.");
    }

    string tempRoot = Path.Combine(Path.GetTempPath(), "MCAddonCreator_" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(tempRoot);

    try
    {
      ZipFile.ExtractToDirectory(sourcePath, tempRoot, overwriteFiles: true);
      return ResolveDirectory(tempRoot, role, temporaryDirectory: tempRoot, originalSourceFile: sourcePath, log: log);
    }
    catch (InvalidDataException ex)
    {
      TryDeleteDirectory(tempRoot);
      log?.Invoke($"{role}: Invalid archive file: {ex.Message}", MCAddonCreator.Models.LogLevel.Error);
      throw new InvalidOperationException($"{role}: The file is not a valid ZIP/MCPACK/MCADDON archive.", ex);
    }
    catch
    {
      TryDeleteDirectory(tempRoot);
      throw;
    }
  }

  private static ResolvedPack ResolveDirectory(string sourceDirectory, string role, string? temporaryDirectory, string? originalSourceFile = null, Action<string, MCAddonCreator.Models.LogLevel>? log = null)
  {
    string fullPath = Path.GetFullPath(sourceDirectory);
    string manifestPath = FindManifest(fullPath);
    string rootDirectory = Path.GetDirectoryName(manifestPath) ?? fullPath;
    ManifestInfo manifest = ManifestReader.Read(manifestPath, log);

    string folderName = Path.GetFileName(rootDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

    if (string.IsNullOrWhiteSpace(folderName) && originalSourceFile is not null)
      folderName = Path.GetFileNameWithoutExtension(originalSourceFile);

    folderName = FileNameTools.ToSafeFileName(folderName, role);

    return new ResolvedPack
    {
      SourcePath = originalSourceFile ?? fullPath,
      RootDirectory = rootDirectory,
      ArchiveFolderName = folderName,
      Manifest = manifest,
      TemporaryDirectory = temporaryDirectory
    };
  }

  private static string FindManifest(string directory)
  {
    string rootManifest = Path.Combine(directory, "manifest.json");

    if (File.Exists(rootManifest))
      return rootManifest;

    string[] directChildManifests =
    [
      .. Directory.GetDirectories(directory)
      .Select(path => Path.Combine(path, "manifest.json"))
      .Where(File.Exists)
      .Where(path => !IsIgnoredPath(path))
    ];

    if (directChildManifests.Length == 1)
      return directChildManifests[0];

    string[] allManifests =
    [
      .. Directory
      .EnumerateFiles(directory, "manifest.json", SearchOption.AllDirectories)
      .Where(path => !IsIgnoredPath(path))
      .OrderBy(GetDirectoryDepth)
    ];

    if (allManifests.Length == 0)
      throw new InvalidOperationException("No manifest.json found. Please select the correct BP/RP folder or a matching ZIP.");

    return allManifests[0];
  }

  private static bool IsIgnoredPath(string path)
  {
    string fullPath = Path.GetFullPath(path);

    string[] parts = fullPath.Split(
      [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
      StringSplitOptions.RemoveEmptyEntries
    );

    return parts.Any(part => IgnoredDirectoryNames.Contains(part, StringComparer.OrdinalIgnoreCase));
  }

  private static int GetDirectoryDepth(string path)
  {
    return Path.GetFullPath(path).Count(c => c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar);
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
      // Temp cleanup must never hide the original error.
    }
  }
}
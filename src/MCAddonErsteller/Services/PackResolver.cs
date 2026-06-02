using System.IO.Compression;
using MCAddonErsteller.Models;

namespace MCAddonErsteller.Services;

public static class PackResolver
{
    public static ResolvedPack Resolve(string sourcePath, string role)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
            throw new InvalidOperationException($"{role}: Kein Pfad angegeben.");

        if (Directory.Exists(sourcePath))
            return ResolveDirectory(sourcePath, role, temporaryDirectory: null);

        if (File.Exists(sourcePath))
            return ResolveZipLikeFile(sourcePath, role);

        throw new FileNotFoundException($"{role}: Quelle wurde nicht gefunden.", sourcePath);
    }

    public static void DeleteTemporaryDirectory(ResolvedPack pack)
    {
        if (pack.TemporaryDirectory is null)
            return;

        TryDeleteDirectory(pack.TemporaryDirectory);
    }

    private static ResolvedPack ResolveZipLikeFile(string sourcePath, string role)
    {
        string extension = Path.GetExtension(sourcePath).ToLowerInvariant();
        if (extension is not ".zip" and not ".mcpack" and not ".mcaddon")
            throw new InvalidOperationException($"{role}: Nur .zip, .mcpack, .mcaddon oder Ordner werden unterstützt.");

        string tempRoot = Path.Combine(Path.GetTempPath(), "MCAddonErsteller_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            ZipFile.ExtractToDirectory(sourcePath, tempRoot, overwriteFiles: true);
            return ResolveDirectory(tempRoot, role, tempRoot, sourcePath);
        }
        catch
        {
            TryDeleteDirectory(tempRoot);
            throw;
        }
    }

    private static ResolvedPack ResolveDirectory(string sourceDirectory, string role, string? temporaryDirectory, string? originalSourceFile = null)
    {
        string fullPath = Path.GetFullPath(sourceDirectory);
        string manifestPath = FindManifest(fullPath);
        string rootDirectory = Path.GetDirectoryName(manifestPath) ?? fullPath;
        ManifestInfo manifest = ManifestReader.Read(manifestPath);

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

        string[] directChildManifests = Directory.GetDirectories(directory)
            .Select(path => Path.Combine(path, "manifest.json"))
            .Where(File.Exists)
            .ToArray();

        if (directChildManifests.Length == 1)
            return directChildManifests[0];

        string[] allManifests = Directory.EnumerateFiles(directory, "manifest.json", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}__MACOSX{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path.Count(c => c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar))
            .ToArray();

        if (allManifests.Length == 0)
            throw new InvalidOperationException("Keine manifest.json gefunden. Bitte den richtigen BP/RP Ordner oder eine passende ZIP auswählen.");

        return allManifests[0];
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

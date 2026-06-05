using System.Diagnostics;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using MCAddonCreator.Models;

namespace MCAddonCreator.Services;

public static class UpdateChecker
{
  private const string LatestReleaseUrl = "https://api.github.com/repos/xoxttxox/MC-Addon-Ersteller/releases/latest";
  private const string ReleasesPageUrl = "https://github.com/xoxttxox/MC-Addon-Ersteller/releases/latest";

  public static async Task<UpdateResult> CheckForUpdateAsync(Action<string, MCAddonCreator.Models.LogLevel>? log = null)
  {
    // Use Warning level for this status so the UI shows it as progress/yellow (avoid duplicate blue info)
    log?.Invoke("Checking for updates...", MCAddonCreator.Models.LogLevel.Warning);
    string currentVersionText = GetCurrentVersion();

    using HttpClient client = new();

    client.DefaultRequestHeaders.UserAgent.Add(
      new ProductInfoHeaderValue("MCAddonErsteller", currentVersionText)
    );

    using HttpResponseMessage response = await client.GetAsync(LatestReleaseUrl);
    response.EnsureSuccessStatusCode();

    await using Stream stream = await response.Content.ReadAsStreamAsync();
    using JsonDocument doc = await JsonDocument.ParseAsync(stream);

    string tagName = doc.RootElement.GetProperty("tag_name").GetString() ?? "";
    string latestVersionText = tagName.TrimStart('v', 'V');

    if (!Version.TryParse(currentVersionText, out Version? current))
      current = new Version(1, 0, 0);

    if (!Version.TryParse(latestVersionText, out Version? latest))
      latest = current;

    log?.Invoke($"Update check complete: current={currentVersionText}, latest={latest}", MCAddonCreator.Models.LogLevel.Info);

    return new UpdateResult
    {
      CurrentVersion = current.ToString(),
      LatestVersion = latest.ToString(),
      IsUpdateAvailable = latest > current,
      ReleaseUrl = ReleasesPageUrl
    };
  }

  public static void OpenReleasePage()
  {
    Process.Start(new ProcessStartInfo
    {
      FileName = ReleasesPageUrl,
      UseShellExecute = true
    });
  }

  private static string GetCurrentVersion()
  {
    return Assembly
      .GetExecutingAssembly()
      .GetName()
      .Version?
      .ToString(3) ?? "1.0.3";
  }
}
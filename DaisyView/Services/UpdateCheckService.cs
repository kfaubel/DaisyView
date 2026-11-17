using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using DaisyView.Models;

namespace DaisyView.Services;

/// <summary>
/// Checks for application updates from GitHub Releases
/// When updates are found, a dialog prompts the user to update
/// Users can skip updates, which will be checked again on next startup
/// </summary>
public class UpdateCheckService
{
    private readonly LoggingService _loggingService;
    private readonly string _githubOwner;
    private readonly string _githubRepo;
    private const string GitHubApiUrl = "https://api.github.com";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public UpdateCheckService(LoggingService loggingService, string githubOwner = "kfaubel", string githubRepo = "DaisyView")
    {
        _loggingService = loggingService;
        _githubOwner = string.IsNullOrWhiteSpace(githubOwner) ? "kfaubel" : githubOwner;
        _githubRepo = string.IsNullOrWhiteSpace(githubRepo) ? "DaisyView" : githubRepo;
    }

    /// <summary>
    /// Checks for a new release on GitHub
    /// Returns the latest release info if available
    /// </summary>
    public async Task<GitHubRelease?> CheckForUpdatesAsync()
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "DaisyView");
            client.Timeout = TimeSpan.FromSeconds(2); // 2 second timeout to avoid blocking startup

            var url = $"{GitHubApiUrl}/repos/{_githubOwner}/{_githubRepo}/releases/latest";
            
            var response = await client.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                _loggingService.LogInfo("GitHub API returned status {StatusCode} when checking for updates", response.StatusCode);
                return null;
            }

            var contentStream = await response.Content.ReadAsStreamAsync();
            var release = await JsonSerializer.DeserializeAsync<GitHubRelease>(contentStream, JsonOptions);
            
            if (release != null)
            {
                _loggingService.LogInfo("Found latest release: {TagName}", release.TagName);
            }

            return release;
        }
        catch (TaskCanceledException)
        {
            _loggingService.LogInfo("Update check timed out (network issue or slow connection)");
            return null;
        }
        catch (Exception ex)
        {
            _loggingService.LogWarning("Failed to check for updates: {Message}", ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Compares two version strings (e.g., "1.0.0" vs "1.0.1")
    /// Returns true if newVersion is greater than currentVersion
    /// </summary>
    public static bool IsNewerVersion(string currentVersion, string newVersion)
    {
        try
        {
            var current = Version.Parse(NormalizeVersion(currentVersion));
            var newer = Version.Parse(NormalizeVersion(newVersion));
            return newer > current;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Normalizes a version string by removing 'v' prefix if present
    /// </summary>
    private static string NormalizeVersion(string version)
    {
        return version.StartsWith("v", StringComparison.OrdinalIgnoreCase) 
            ? version[1..] 
            : version;
    }

    /// <summary>
    /// Gets the release page URL for a specific release
    /// </summary>
    public string GetReleasePageUrl(string tagName)
    {
        return $"https://github.com/{_githubOwner}/{_githubRepo}/releases/tag/{tagName}";
    }

    /// <summary>
    /// Gets the preferred direct download URL for a release when possible.
    /// Falls back to the release page URL.
    /// </summary>
    public string GetPreferredDownloadUrl(GitHubRelease release)
    {
        if (release == null)
            throw new ArgumentNullException(nameof(release));

        if (release.Assets.Count > 0)
        {
            // Prefer installer-like assets for one-click update.
            var preferred = release.Assets.FirstOrDefault(asset =>
                !string.IsNullOrWhiteSpace(asset.BrowserDownloadUrl)
                && HasPreferredInstallerExtension(asset.Name));

            if (preferred?.BrowserDownloadUrl != null)
                return preferred.BrowserDownloadUrl;

            var firstUsable = release.Assets.FirstOrDefault(asset => !string.IsNullOrWhiteSpace(asset.BrowserDownloadUrl));
            if (firstUsable?.BrowserDownloadUrl != null)
                return firstUsable.BrowserDownloadUrl;
        }

        if (!string.IsNullOrWhiteSpace(release.HtmlUrl))
            return release.HtmlUrl;

        return GetReleasePageUrl(release.TagName ?? string.Empty);
    }

    private static bool HasPreferredInstallerExtension(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        return fileName.EndsWith(".msi", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".msix", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".msixbundle", StringComparison.OrdinalIgnoreCase);
    }
}

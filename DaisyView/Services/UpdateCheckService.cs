using System;
using System.Net.Http;
using System.Net.Http.Json;
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
    private readonly string _githubOwner = "ken";  // TODO: Update with actual GitHub owner
    private readonly string _githubRepo = "DaisyView";
    private const string GitHubApiUrl = "https://api.github.com";

    public UpdateCheckService(LoggingService loggingService)
    {
        _loggingService = loggingService;
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
            var release = System.Text.Json.JsonSerializer.DeserializeAsync<GitHubRelease>(contentStream).Result;
            
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
    /// Gets the download URL for a specific release
    /// </summary>
    public string GetDownloadUrl(string tagName)
    {
        return $"https://github.com/{_githubOwner}/{_githubRepo}/releases/tag/{tagName}";
    }
}

using System;
using System.Collections.Generic;

namespace DaisyView.Models;

/// <summary>
/// Represents information about a GitHub release for update checking
/// </summary>
public class GitHubRelease
{
    public string? TagName { get; set; }
    public string? Name { get; set; }
    public string? Body { get; set; }
    public string? HtmlUrl { get; set; }
    public bool Prerelease { get; set; }
    public DateTime PublishedAt { get; set; }
    public List<GitHubReleaseAsset> Assets { get; set; } = new();
}

/// <summary>
/// Represents a downloadable asset attached to a GitHub release.
/// </summary>
public class GitHubReleaseAsset
{
    public string? Name { get; set; }
    public string? BrowserDownloadUrl { get; set; }
    public string? ContentType { get; set; }
    public long Size { get; set; }
}

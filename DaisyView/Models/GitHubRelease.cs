using System;

namespace DaisyView.Models;

/// <summary>
/// Represents information about a GitHub release for update checking
/// </summary>
public class GitHubRelease
{
    public string? TagName { get; set; }
    public string? Name { get; set; }
    public string? Body { get; set; }
    public bool Prerelease { get; set; }
    public DateTime PublishedAt { get; set; }
}

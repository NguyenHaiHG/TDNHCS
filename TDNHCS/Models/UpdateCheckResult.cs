namespace TDNHCS.Models;

public class UpdateCheckResult
{
    public bool HasUpdate { get; init; }
    public string LatestVersion { get; init; } = string.Empty;
    public string CurrentVersion { get; init; } = string.Empty;
    public string? ReleaseNotes { get; init; }
    public string? DownloadUrl { get; init; }
    public string? ReleasePageUrl { get; init; }
}

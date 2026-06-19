using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using TDNHCS.Models;

namespace TDNHCS.Services;

public class GitHubUpdateService
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    static GitHubUpdateService()
    {
        HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("QLVBNHCS-Updater/1.0");
        HttpClient.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
    }

    public string CurrentVersion
    {
        get
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            return version == null ? "1.0.0" : $"{version.Major}.{version.Minor}.{version.Build}";
        }
    }

    public async Task<UpdateCheckResult> CheckForUpdateAsync(CancellationToken cancellationToken = default)
    {
        var currentVersion = CurrentVersion;

        if (!UpdateConfig.IsConfigured)
        {
            return new UpdateCheckResult
            {
                HasUpdate = false,
                CurrentVersion = currentVersion
            };
        }

        var apiUrl = $"https://api.github.com/repos/{UpdateConfig.GitHubOwner}/{UpdateConfig.GitHubRepo}/releases/latest";
        using var response = await HttpClient.GetAsync(apiUrl, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return new UpdateCheckResult
            {
                HasUpdate = false,
                ReleaseAvailable = false,
                CurrentVersion = currentVersion
            };
        }

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var release = await JsonSerializer.DeserializeAsync<GitHubRelease>(stream, cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Không đọc được thông tin phiên bản mới.");

        var latestVersion = ParseVersion(release.TagName);
        var installedVersion = ParseVersion(currentVersion);
        var asset = release.Assets.FirstOrDefault(item =>
            item.Name.Equals(UpdateConfig.SetupAssetName, StringComparison.OrdinalIgnoreCase));

        return new UpdateCheckResult
        {
            HasUpdate = latestVersion > installedVersion,
            ReleaseAvailable = true,
            LatestVersion = latestVersion.ToString(3),
            CurrentVersion = currentVersion,
            ReleaseNotes = release.Body,
            DownloadUrl = asset?.BrowserDownloadUrl
        };
    }

    public async Task<string> DownloadInstallerAsync(string downloadUrl, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        var destination = Path.Combine(Path.GetTempPath(), UpdateConfig.SetupAssetName);
        if (File.Exists(destination))
        {
            File.Delete(destination);
        }

        using var downloadClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(15)
        };
        downloadClient.DefaultRequestHeaders.UserAgent.ParseAdd("QLVBNHCS-Updater/1.0");

        using var response = await downloadClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new InvalidOperationException("Không tìm thấy file cập nhật.");
        }

        response.EnsureSuccessStatusCode();

        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var output = File.Create(destination);

        var totalBytes = response.Content.Headers.ContentLength ?? -1;
        var buffer = new byte[81920];
        long downloadedBytes = 0;
        int read;
        var lastReported = -1;

        progress?.Report(0);

        while ((read = await input.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            downloadedBytes += read;

            if (totalBytes > 0)
            {
                var percent = (int)(downloadedBytes * 100 / totalBytes);
                if (percent != lastReported)
                {
                    lastReported = percent;
                    progress?.Report(percent);
                }
            }
        }

        progress?.Report(100);
        return destination;
    }

    public void RunInstaller(string installerPath)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = installerPath,
            Arguments = "/SILENT /CLOSEAPPLICATIONS /UPDATE=1",
            UseShellExecute = true
        });

        Application.Current.Shutdown();
    }

    private static Version ParseVersion(string value)
    {
        value = value.Trim().TrimStart('v', 'V');
        return Version.TryParse(value, out var version) ? version : new Version(0, 0, 0);
    }

    private sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = string.Empty;

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; } = string.Empty;

        [JsonPropertyName("body")]
        public string? Body { get; set; }

        [JsonPropertyName("assets")]
        public List<GitHubReleaseAsset> Assets { get; set; } = [];
    }

    private sealed class GitHubReleaseAsset
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; set; } = string.Empty;
    }
}

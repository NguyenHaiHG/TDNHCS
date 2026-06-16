namespace TDNHCS;

/// <summary>
/// Cấu hình cập nhật từ GitHub Releases.
/// Một tài khoản GitHub có thể dùng cho nhiều app — mỗi app nên có repo riêng.
/// </summary>
public static class UpdateConfig
{
    /// <summary>
    /// Tên tài khoản hoặc tổ chức GitHub (ví dụ: "nguyenhai").
    /// </summary>
    public const string GitHubOwner = "YOUR_GITHUB_USERNAME";

    /// <summary>
    /// Tên repository chứa release của app này (ví dụ: "QLVBNHCS").
    /// </summary>
    public const string GitHubRepo = "QLVBNHCS";

    /// <summary>
    /// Tên file setup upload lên GitHub Release.
    /// </summary>
    public const string SetupAssetName = "TDNHCS_Setup.exe";

    public static bool IsConfigured =>
        !string.IsNullOrWhiteSpace(GitHubOwner)
        && GitHubOwner != "YOUR_GITHUB_USERNAME"
        && !string.IsNullOrWhiteSpace(GitHubRepo);
}

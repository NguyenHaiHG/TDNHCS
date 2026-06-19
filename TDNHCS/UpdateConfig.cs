namespace TDNHCS;

/// <summary>
/// Cấu hình cập nhật từ xa — hardcode nội bộ, không hiển thị cho người dùng.
/// Repo GitHub cần để Public để app tự kiểm tra phiên bản mới.
/// </summary>
internal static class UpdateConfig
{
    internal const string GitHubOwner = "NguyenHaiHG";
    internal const string GitHubRepo = "TDNHCS";
    internal const string SetupAssetName = "TDNHCS_Setup.exe";

    internal static bool IsConfigured =>
        !string.IsNullOrWhiteSpace(GitHubOwner)
        && !string.IsNullOrWhiteSpace(GitHubRepo);
}

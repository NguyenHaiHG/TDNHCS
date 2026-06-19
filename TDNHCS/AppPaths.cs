using System.IO;

namespace TDNHCS;

/// <summary>
/// Quản lý tập trung đường dẫn lưu trữ dữ liệu của app trên ổ D:\
/// Thư mục được đặt Hidden để không hiện trong File Explorer thông thường.
/// </summary>
public static class AppPaths
{
    // Thư mục gốc — đặt tên không gợi ý nội dung, ẩn đi
    private const string RootFolderName = "SysCache_QLVB";

    public static string RootFolder => Path.Combine("D:\\", RootFolderName);

    public static string DatabasePath => Path.Combine(RootFolder, "data.db");

    // File đính kèm lưu với tên GUID để người ngoài không đọc được
    public static string AttachmentsFolder => Path.Combine(RootFolder, "store");

    public static bool IsInitialized => File.Exists(DatabasePath);

    /// <summary>
    /// Chuyển đường dẫn tuyệt đối (nếu nằm trong thư mục dữ liệu) sang dạng lưu tương đối.
    /// </summary>
    public static string ToStoredPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        if (!Path.IsPathRooted(path))
        {
            return path.Replace('/', Path.DirectorySeparatorChar);
        }

        var normalized = Path.GetFullPath(path);
        var root = Path.GetFullPath(RootFolder.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar);

        if (normalized.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            return Path.GetRelativePath(Path.GetFullPath(RootFolder), normalized)
                .Replace('/', Path.DirectorySeparatorChar);
        }

        return path;
    }

    /// <summary>
    /// Đọc đường dẫn đã lưu (tương đối hoặc tuyệt đối cũ) thành đường dẫn đầy đủ trên máy hiện tại.
    /// </summary>
    public static string ResolveStoredPath(string? storedPath)
    {
        if (string.IsNullOrWhiteSpace(storedPath))
        {
            return string.Empty;
        }

        if (Path.IsPathRooted(storedPath))
        {
            return Path.GetFullPath(storedPath);
        }

        return Path.GetFullPath(Path.Combine(RootFolder, storedPath));
    }

    /// <summary>
    /// Tạo toàn bộ thư mục cần thiết và đặt thuộc tính Hidden
    /// </summary>
    public static void EnsureDirectories()
    {
        // Tạo thư mục gốc
        Directory.CreateDirectory(RootFolder);
        HideFolder(RootFolder);

        // Tạo thư mục chứa file đính kèm
        Directory.CreateDirectory(AttachmentsFolder);
        HideFolder(AttachmentsFolder);
    }

    private static void HideFolder(string path)
    {
        var info = new DirectoryInfo(path);
        if (!info.Attributes.HasFlag(FileAttributes.Hidden))
            info.Attributes |= FileAttributes.Hidden | FileAttributes.System;
    }
}

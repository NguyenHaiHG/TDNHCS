using System.IO;
using System.IO.Compression;

namespace TDNHCS.Services;

/// <summary>
/// Sao lưu / khôi phục toàn bộ dữ liệu (data.db + file đính kèm) ra file ZIP.
/// </summary>
public class BackupRestoreService
{
    public Task BackupAsync(string destinationZipPath, CancellationToken cancellationToken = default)
    {
        if (!AppPaths.IsInitialized)
        {
            throw new InvalidOperationException("Chưa có dữ liệu để sao lưu.");
        }

        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var directory = Path.GetDirectoryName(destinationZipPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (File.Exists(destinationZipPath))
            {
                File.Delete(destinationZipPath);
            }

            ZipFile.CreateFromDirectory(
                AppPaths.RootFolder,
                destinationZipPath,
                CompressionLevel.Optimal,
                includeBaseDirectory: false);
        }, cancellationToken);
    }

    public Task RestoreAsync(string sourceZipPath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(sourceZipPath))
        {
            throw new FileNotFoundException("Không tìm thấy file sao lưu.", sourceZipPath);
        }

        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var tempDir = Path.Combine(Path.GetTempPath(), "QLVB_Restore_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                ZipFile.ExtractToDirectory(sourceZipPath, tempDir, overwriteFiles: true);
                cancellationToken.ThrowIfCancellationRequested();

                var databaseFile = Directory.GetFiles(tempDir, "data.db", SearchOption.AllDirectories)
                    .OrderBy(path => path.Count(c => c is '/' or '\\'))
                    .FirstOrDefault()
                    ?? throw new InvalidOperationException("File sao lưu không chứa data.db.");

                var sourceRoot = Directory.GetParent(databaseFile)!.FullName;

                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

                AppPaths.EnsureDirectories();
                ClearDirectoryContents(AppPaths.RootFolder);

                CopyDirectory(sourceRoot, AppPaths.RootFolder);
                AppPaths.EnsureDirectories();
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }, cancellationToken);
    }

    private static void ClearDirectoryContents(string targetRoot)
    {
        if (!Directory.Exists(targetRoot))
        {
            return;
        }

        foreach (var file in Directory.GetFiles(targetRoot))
        {
            File.Delete(file);
        }

        foreach (var directory in Directory.GetDirectories(targetRoot))
        {
            Directory.Delete(directory, true);
        }
    }

    private static void CopyDirectory(string sourceRoot, string destinationRoot)
    {
        Directory.CreateDirectory(destinationRoot);

        foreach (var file in Directory.GetFiles(sourceRoot))
        {
            var fileName = Path.GetFileName(file);
            File.Copy(file, Path.Combine(destinationRoot, fileName), true);
        }

        foreach (var directory in Directory.GetDirectories(sourceRoot))
        {
            var directoryName = Path.GetFileName(directory);
            CopyDirectory(directory, Path.Combine(destinationRoot, directoryName));
        }
    }
}

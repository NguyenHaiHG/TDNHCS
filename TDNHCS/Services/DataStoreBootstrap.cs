using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TDNHCS.Data;

namespace TDNHCS.Services;

/// <summary>
/// Khởi tạo thư mục D:\SysCache_QLVB và database khi người dùng lưu văn bản đầu tiên.
/// </summary>
public class DataStoreBootstrap
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public DataStoreBootstrap(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public bool IsInitialized => AppPaths.IsInitialized;

    public async Task EnsureInitializedAsync()
    {
        if (IsInitialized)
        {
            return;
        }

        await _initLock.WaitAsync();
        try
        {
            if (IsInitialized)
            {
                return;
            }

            AppPaths.EnsureDirectories();

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DocumentDbContext>();
            await context.Database.EnsureCreatedAsync();
            await EnsureDocumentContentColumnAsync(context);
        }
        finally
        {
            _initLock.Release();
        }
    }

    private static async Task EnsureDocumentContentColumnAsync(DocumentDbContext context)
    {
        try
        {
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE Documents ADD COLUMN Content TEXT");
        }
        catch
        {
            // Database mới đã có cột này, database cũ chỉ cần thêm một lần.
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using TDNHCS.Data;
using TDNHCS.Services;
using TDNHCS.ViewModels;
using TDNHCS.Views;

namespace TDNHCS;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Ngăn WPF tự tắt khi LoginWindow đóng
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // Cấu hình Dependency Injection
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        // Khởi tạo database
        await InitializeDatabaseAsync();

        // Dùng scope để resolve các scoped services cho màn đăng nhập
        using var loginScope = _serviceProvider.CreateScope();
        var loginViewModel = loginScope.ServiceProvider.GetRequiredService<LoginViewModel>();
        var loginWindow = new LoginWindow(loginViewModel);

        if (loginWindow.ShowDialog() == true)
        {
            // Đăng nhập thành công -> Hiển thị MainWindow trong scope mới
            var mainScope = _serviceProvider.CreateScope();
            var mainWindow = mainScope.ServiceProvider.GetRequiredService<MainWindow>();
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            MainWindow = mainWindow;
            mainWindow.Show();
        }
        else
        {
            // Hủy đăng nhập -> Thoát ứng dụng
            Shutdown();
        }
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Database
        services.AddDbContext<DocumentDbContext>();

        // Services
        services.AddScoped<DocumentService>();
        services.AddScoped<UserService>();
        services.AddSingleton<ExportService>();
        services.AddSingleton<PrintService>();
        services.AddSingleton<GitHubUpdateService>();

        // ViewModels
        services.AddScoped<MainViewModel>();
        services.AddTransient<DocumentDetailViewModel>();
        services.AddScoped<LoginViewModel>();

        // Views
        services.AddScoped<MainWindow>();
    }

    private async Task InitializeDatabaseAsync()
    {
        using var scope = _serviceProvider!.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DocumentDbContext>();

        // Tạo database nếu chưa có
        await context.Database.EnsureCreatedAsync();
        await EnsureDocumentContentColumnAsync(context);
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

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}

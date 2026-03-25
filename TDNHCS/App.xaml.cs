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

        // Hiển thị màn hình đăng nhập
        var loginViewModel = _serviceProvider.GetRequiredService<LoginViewModel>();
        var loginWindow = new LoginWindow(loginViewModel);

        if (loginWindow.ShowDialog() == true)
        {
            // Đăng nhập thành công -> Hiển thị MainWindow
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
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
        services.AddSingleton<DocumentService>();
        services.AddSingleton<ExportService>();
        services.AddSingleton<PrintService>();

        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddTransient<DocumentDetailViewModel>();
        services.AddTransient<LoginViewModel>();

        // Views
        services.AddSingleton<MainWindow>();
    }

    private async Task InitializeDatabaseAsync()
    {
        using var scope = _serviceProvider!.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DocumentDbContext>();

        // Tạo database nếu chưa có
        await context.Database.EnsureCreatedAsync();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}

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

        // Bắt mọi exception chưa được xử lý
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            var msg = args.ExceptionObject?.ToString() ?? "Unknown error";
            var logPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "TDNHCS_error.txt");
            System.IO.File.WriteAllText(logPath, msg);
            MessageBox.Show($"Lỗi nghiêm trọng:\n\n{(args.ExceptionObject as Exception)?.Message}\n\nChi tiết đã được lưu tại:\n{logPath}",
                "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        };

        DispatcherUnhandledException += (_, args) =>
        {
            var logPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "TDNHCS_error.txt");
            System.IO.File.WriteAllText(logPath, args.Exception.ToString());
            MessageBox.Show($"Lỗi:\n\n{args.Exception.Message}\n\nChi tiết đã được lưu tại:\n{logPath}",
                "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        // Ngăn WPF tự tắt khi LoginWindow đóng
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // Cấu hình Dependency Injection
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        // Không tạo database lúc khởi động — chờ người dùng thêm văn bản đầu tiên.

        // Dùng scope để resolve các scoped services cho màn đăng nhập
        using var loginScope = _serviceProvider.CreateScope();
        var loginViewModel = loginScope.ServiceProvider.GetRequiredService<LoginViewModel>();
        var loginWindow = new LoginWindow(loginViewModel);

        if (loginWindow.ShowDialog() == true)
        {
            try
            {
                // Đăng nhập thành công -> Hiển thị MainWindow trong scope mới
                var mainScope = _serviceProvider.CreateScope();
                var mainWindow = mainScope.ServiceProvider.GetRequiredService<MainWindow>();
                ShutdownMode = ShutdownMode.OnMainWindowClose;
                MainWindow = mainWindow;
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Không thể mở cửa sổ chính:\n\n{ex.Message}\n\n{ex.InnerException?.Message}",
                    "Lỗi khởi động",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown();
            }
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
        services.AddSingleton<DataStoreBootstrap>();
        services.AddScoped<DocumentService>();
        services.AddScoped<UserService>();
        services.AddSingleton<ExportService>();
        services.AddSingleton<PrintService>();
        services.AddSingleton<GitHubUpdateService>();
        services.AddSingleton<BackupRestoreService>();
        services.AddSingleton<OcrService>();
        services.AddSingleton<DocumentTextService>();
        services.AddSingleton<TextComparisonService>();

        // ViewModels
        services.AddScoped<MainViewModel>();
        services.AddScoped<DocumentCompareViewModel>();
        services.AddTransient<DocumentDetailViewModel>();
        services.AddScoped<LoginViewModel>();

        // Views
        services.AddScoped<MainWindow>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using TDNHCS;
using TDNHCS.Models;
using TDNHCS.Services;
using TDNHCS.Views;

namespace TDNHCS.ViewModels;

/// <summary>
/// ViewModel chính cho MainWindow
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly DocumentService _documentService;
    private readonly ExportService _exportService;
    private readonly PrintService _printService;
    private readonly UserService _userService;
    private readonly GitHubUpdateService _updateService;
    private readonly BackupRestoreService _backupRestoreService;
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private ObservableCollection<Document> _documents = new();

    [ObservableProperty]
    private ObservableCollection<Category> _categories = new();

    [ObservableProperty]
    private ObservableCollection<Document> _chatbotResults = new();

    [ObservableProperty]
    private Document? _selectedDocument;

    [ObservableProperty]
    private string _chatbotSearchText = string.Empty;

    [ObservableProperty]
    private string _chatbotMessage = "Bạn muốn tôi tìm văn bản nào?";

    [ObservableProperty]
    private bool _isChatbotExpanded;

    [ObservableProperty]
    private bool _isLoading;

    public string AppVersion => _updateService.CurrentVersion;

    private bool _hasPromptedDefaultPasswordChange;

    public MainViewModel(
        DocumentService documentService,
        ExportService exportService,
        PrintService printService,
        UserService userService,
        GitHubUpdateService updateService,
        BackupRestoreService backupRestoreService,
        IServiceProvider serviceProvider)
    {
        _documentService = documentService;
        _exportService = exportService;
        _printService = printService;
        _userService = userService;
        _updateService = updateService;
        _backupRestoreService = backupRestoreService;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Khởi tạo - Load dữ liệu
    /// </summary>
    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsLoading = true;

            if (AppPaths.IsInitialized)
            {
                await _documentService.MigrateStoredPathsAsync();
            }

            var docs = await _documentService.GetAllDocumentsAsync();
            Documents = new ObservableCollection<Document>(docs);

            var cats = await _documentService.GetAllCategoriesAsync();
            Categories = new ObservableCollection<Category>(cats);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi tải dữ liệu: {ex.Message}", "Lỗi", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ChatbotSearchAsync()
    {
        if (string.IsNullOrWhiteSpace(ChatbotSearchText))
        {
            ChatbotMessage = "Bạn hãy nhập tiêu đề hoặc nội dung văn bản cần tìm nhé.";
            ChatbotResults.Clear();
            return;
        }

        try
        {
            IsLoading = true;

            if (!AppPaths.IsInitialized)
            {
                ChatbotResults.Clear();
                ChatbotMessage = "Chưa có dữ liệu. Bạn hãy thêm văn bản đầu tiên trước khi tìm kiếm nhé.";
                return;
            }

            var docs = await _documentService.SearchDocumentsAsync(ChatbotSearchText.Trim());
            ChatbotResults = new ObservableCollection<Document>(docs);
            ChatbotMessage = docs.Count == 0
                ? "Tôi chưa tìm thấy văn bản phù hợp. Bạn thử nhập từ khóa khác nhé."
                : $"Tôi tìm thấy {docs.Count} văn bản liên quan. Bạn bấm vào văn bản bên dưới để đọc nhé.";
        }
        catch (Exception ex)
        {
            ChatbotMessage = $"Không tìm được văn bản: {ex.Message}";
            ChatbotResults.Clear();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ToggleChatbot()
    {
        IsChatbotExpanded = !IsChatbotExpanded;
    }

    [RelayCommand]
    private async Task OpenChatbotDocumentAsync(Document? document)
    {
        if (document == null) return;

        var fullDocument = await _documentService.GetDocumentByIdAsync(document.Id);
        if (fullDocument == null) return;

        SelectedDocument = fullDocument;
        var window = new DocumentViewWindow(fullDocument)
        {
            Owner = Application.Current.MainWindow
        };
        window.ShowDialog();
    }

    public void PromptDefaultPasswordChange()
    {
        if (_hasPromptedDefaultPasswordChange
            || !_userService.CanChangePassword
            || !_userService.IsUsingDefaultAdminPassword(LoginViewModel.CurrentUser))
        {
            return;
        }

        _hasPromptedDefaultPasswordChange = true;
        var result = MessageBox.Show(
            "Bạn đang dùng mật khẩu admin mặc định (Admin@123). Bạn có muốn đổi mật khẩu ngay bây giờ không?",
            "Đổi mật khẩu",
            MessageBoxButton.YesNo,
            MessageBoxImage.Information);

        if (result == MessageBoxResult.Yes)
        {
            ChangePassword();
        }
    }

    /// <summary>
    /// Thêm văn bản mới
    /// </summary>
    [RelayCommand]
    private void AddDocument()
    {
        var viewModel = _serviceProvider.GetRequiredService<DocumentDetailViewModel>();
        var window = new DocumentDetailWindow(viewModel);

        if (window.ShowDialog() == true)
        {
            _ = LoadDataAsync();
        }
    }

    /// <summary>
    /// Sửa văn bản
    /// </summary>
    [RelayCommand]
    private async Task EditDocumentAsync()
    {
        if (SelectedDocument == null)
        {
            MessageBox.Show("Vui lòng chọn văn bản cần sửa!", "Thông báo",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var documentToEdit = await _documentService.GetDocumentByIdAsync(SelectedDocument.Id);
        if (documentToEdit == null) return;

        var viewModel = new DocumentDetailViewModel(_documentService, documentToEdit);
        var window = new DocumentDetailWindow(viewModel);

        if (window.ShowDialog() == true)
        {
            await LoadDataAsync();
        }
    }

    /// <summary>
    /// Xóa văn bản
    /// </summary>
    [RelayCommand]
    private async Task DeleteDocumentAsync()
    {
        if (SelectedDocument == null)
        {
            MessageBox.Show("Vui lòng chọn văn bản cần xóa!", "Thông báo",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var result = MessageBox.Show(
            $"Bạn có chắc muốn xóa văn bản '{SelectedDocument.Title}'?",
            "Xác nhận xóa",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                await _documentService.DeleteDocumentAsync(SelectedDocument.Id);
                await LoadDataAsync();

                MessageBox.Show("Xóa văn bản thành công!", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi xóa văn bản: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    /// <summary>
    /// Export Excel
    /// </summary>
    [RelayCommand]
    private void ExportExcel()
    {
        if (!Documents.Any())
        {
            MessageBox.Show("Không có dữ liệu để xuất!", "Thông báo",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "Excel Files (*.xlsx)|*.xlsx",
            FileName = $"DanhSachVanBan_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                _exportService.ExportToExcel(Documents.ToList(), dialog.FileName);

                var result = MessageBox.Show(
                    "Xuất Excel thành công! Bạn có muốn mở file?",
                    "Thành công",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = dialog.FileName,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi xuất Excel: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    /// <summary>
    /// In văn bản
    /// </summary>
    [RelayCommand]
    private void Print()
    {
        if (!Documents.Any())
        {
            MessageBox.Show("Không có dữ liệu để in!", "Thông báo",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            _printService.PrintDocuments(Documents.ToList());
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi in: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Hiển thị thống kê
    /// </summary>
    [RelayCommand]
    private void ShowStatistics()
    {
        var viewModel = new StatisticsViewModel(_documentService);
        var window = new StatisticsWindow(viewModel);
        window.ShowDialog();
    }

    /// <summary>
    /// Xem nội dung văn bản trong app (không mở Word/PDF bên ngoài)
    /// </summary>
    [RelayCommand]
    private async Task ViewDocumentAsync()
    {
        if (SelectedDocument == null)
        {
            MessageBox.Show("Vui lòng chọn văn bản cần xem!", "Thông báo",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var fullDocument = await _documentService.GetDocumentByIdAsync(SelectedDocument.Id);
        if (fullDocument == null) return;

        SelectedDocument = fullDocument;
        var window = new DocumentViewWindow(fullDocument)
        {
            Owner = Application.Current.MainWindow
        };
        window.ShowDialog();
    }

    [RelayCommand]
    private async Task BackupDataAsync()
    {
        if (!AppPaths.IsInitialized)
        {
            MessageBox.Show(
                "Chưa có dữ liệu để sao lưu.\n\nHãy thêm văn bản đầu tiên trước.",
                "Sao lưu dữ liệu",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Lưu file sao lưu",
            Filter = "File sao lưu (*.zip)|*.zip",
            FileName = $"QLVB_SaoLuu_{DateTime.Now:yyyyMMdd_HHmmss}.zip",
            DefaultExt = ".zip"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            IsLoading = true;
            await _backupRestoreService.BackupAsync(dialog.FileName);
            MessageBox.Show(
                $"Sao lưu thành công!\n\nFile: {dialog.FileName}\n\nBạn có thể copy file này sang máy khác, cài app và dùng Khôi phục để mở lại dữ liệu.",
                "Sao lưu dữ liệu",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể sao lưu: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RestoreDataAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Chọn file sao lưu",
            Filter = "File sao lưu (*.zip)|*.zip"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var confirm = MessageBox.Show(
            "Khôi phục sẽ ghi đè toàn bộ dữ liệu hiện tại bằng dữ liệu trong file sao lưu.\n\nBạn có chắc muốn tiếp tục?",
            "Khôi phục dữ liệu",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            IsLoading = true;
            await _backupRestoreService.RestoreAsync(dialog.FileName);

            MessageBox.Show(
                "Khôi phục thành công!\n\nỨng dụng sẽ mở lại để tải dữ liệu mới.",
                "Khôi phục dữ liệu",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            RestartApplication();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể khôi phục: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static void RestartApplication()
    {
        var exePath = Environment.ProcessPath
            ?? Process.GetCurrentProcess().MainModule?.FileName;

        if (!string.IsNullOrWhiteSpace(exePath))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true
            });
        }

        Application.Current.Shutdown();
    }

    /// <summary>
    /// Đổi mật khẩu người dùng hiện tại
    /// </summary>
    [RelayCommand]
    private void ChangePassword()
    {
        if (!_userService.CanChangePassword)
        {
            MessageBox.Show(
                "Hệ thống chưa khởi tạo lưu trữ.\n\nVui lòng thêm văn bản đầu tiên trước khi đổi mật khẩu.",
                "Đổi mật khẩu",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var viewModel = new ChangePasswordViewModel(_userService);
        var window = new ChangePasswordWindow(viewModel)
        {
            Owner = Application.Current.MainWindow
        };
        window.ShowDialog();
    }

    [RelayCommand]
    private async Task CheckForUpdatesAsync()
    {
        await CheckForUpdatesInternalAsync(showNoUpdateMessage: true);
    }

    public async Task CheckForUpdatesSilentlyAsync()
    {
        await CheckForUpdatesInternalAsync(showNoUpdateMessage: false);
    }

    private async Task CheckForUpdatesInternalAsync(bool showNoUpdateMessage)
    {
        if (!UpdateConfig.IsConfigured)
        {
            return;
        }

        try
        {
            IsLoading = true;
            var result = await _updateService.CheckForUpdateAsync();

            if (!result.ReleaseAvailable)
            {
                if (showNoUpdateMessage)
                {
                    MessageBox.Show(
                        "Chưa có bản cập nhật trên máy chủ.\n\nVui lòng thử lại sau hoặc liên hệ quản trị viên.",
                        "Cập nhật phần mềm",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }

                return;
            }

            if (!result.HasUpdate)
            {
                if (showNoUpdateMessage)
                {
                    MessageBox.Show(
                        $"Bạn đang dùng phiên bản mới nhất ({result.CurrentVersion}).",
                        "Cập nhật",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }

                return;
            }

            if (string.IsNullOrWhiteSpace(result.DownloadUrl))
            {
                MessageBox.Show(
                    $"Có phiên bản mới {result.LatestVersion} nhưng chưa tải được gói cập nhật.\n\nPhần mềm sẽ thoát.",
                    "Cập nhật",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                Application.Current.Shutdown();
                return;
            }

            var notes = string.IsNullOrWhiteSpace(result.ReleaseNotes)
                ? "Không có ghi chú phiên bản."
                : result.ReleaseNotes;

            var confirm = MessageBox.Show(
                $"Có phiên bản mới: {result.LatestVersion}\nPhiên bản hiện tại: {result.CurrentVersion}\n\n{notes}\n\nBạn cần cập nhật để tiếp tục sử dụng.\nChọn Có để tải và cài đặt.\nChọn Không sẽ thoát phần mềm.",
                "Cập nhật phần mềm",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
                return;
            }

            IsLoading = false;

            var progressWindow = new UpdateProgressWindow
            {
                Owner = Application.Current.MainWindow
            };
            progressWindow.Show();

            try
            {
                var progress = new Progress<int>(percent => progressWindow.UpdateProgress(percent));
                var installerPath = await _updateService.DownloadInstallerAsync(result.DownloadUrl, progress);
                progressWindow.Close();
                _updateService.RunInstaller(installerPath);
            }
            catch
            {
                progressWindow.Close();
                throw;
            }
        }
        catch (Exception ex)
        {
            if (showNoUpdateMessage)
            {
                MessageBox.Show(
                    $"Không thể kiểm tra cập nhật: {ex.Message}",
                    "Cập nhật phần mềm",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }
}

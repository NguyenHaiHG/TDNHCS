using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
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
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private ObservableCollection<Document> _documents = new();

    [ObservableProperty]
    private ObservableCollection<Category> _categories = new();

    [ObservableProperty]
    private Document? _selectedDocument;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    public MainViewModel(
        DocumentService documentService, 
        ExportService exportService,
        PrintService printService,
        IServiceProvider serviceProvider)
    {
        _documentService = documentService;
        _exportService = exportService;
        _printService = printService;
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

    /// <summary>
    /// Tìm kiếm văn bản
    /// </summary>
    [RelayCommand]
    private async Task SearchAsync()
    {
        try
        {
            IsLoading = true;

            if (string.IsNullOrWhiteSpace(SearchText))
            {
                var docs = await _documentService.GetAllDocumentsAsync();
                Documents = new ObservableCollection<Document>(docs);
            }
            else
            {
                var docs = await _documentService.SearchDocumentsAsync(SearchText);
                Documents = new ObservableCollection<Document>(docs);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi tìm kiếm: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
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
}


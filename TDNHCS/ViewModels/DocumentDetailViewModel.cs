using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using TDNHCS;
using TDNHCS.Models;
using TDNHCS.Services;

namespace TDNHCS.ViewModels;

public partial class DocumentDetailViewModel : ObservableObject
{
    private readonly DocumentService _documentService;
    private readonly DocumentTextService _documentTextService;
    private readonly bool _isEditMode;

    [ObservableProperty]
    private Document _document = new();

    [ObservableProperty]
    private ObservableCollection<Category> _categories = new();

    public event Action<bool>? CloseRequested;

    public DocumentDetailViewModel(
        DocumentService documentService,
        DocumentTextService documentTextService,
        Document? document = null)
    {
        _documentService = documentService;
        _documentTextService = documentTextService;
        _isEditMode = document != null;
        
        Document = document ?? new Document
        {
            DocumentNumber = "VB-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"),
            IssueDate = DateTime.Now,
            ReceivedDate = DateTime.Now,
            CreatedBy = Environment.UserName,
            Type = DocumentType.Incoming
        };

        LoadCategoriesAsync();
    }

    private async void LoadCategoriesAsync()
    {
        var cats = await _documentService.GetAllCategoriesAsync();
        Categories = new ObservableCollection<Category>(cats);
        
        if (!_isEditMode && Categories.Any())
        {
            Document.CategoryId = Categories.First().Id;
        }
    }

    [RelayCommand]
    private void BrowseFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Chọn file đính kèm",
            Filter = "Tất cả file hỗ trợ (*.pdf;*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff;*.docx;*.txt)|*.pdf;*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff;*.docx;*.txt|PDF và ảnh scan (*.pdf;*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff)|*.pdf;*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff|Word và Text (*.docx;*.txt)|*.docx;*.txt|Tất cả file (*.*)|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            var originalFileName = Path.GetFileName(dialog.FileName);
            Document.FilePath = dialog.FileName;
            Document.OriginalFileName = originalFileName;

            MessageBox.Show("Đã chọn file đính kèm. File sẽ được lưu khi bạn bấm Lưu văn bản.",
                "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    [RelayCommand]
    private async Task ExtractContentAsync()
    {
        var filePath = Document.ResolvedFilePath;
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            MessageBox.Show(
                "Vui lòng chọn file PDF scan hoặc file ảnh trước.",
                "Đọc nội dung OCR",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        try
        {
            var text = await Task.Run(() => _documentTextService.ReadFile(filePath));
            if (string.IsNullOrWhiteSpace(text))
            {
                MessageBox.Show(
                    "OCR hoàn tất nhưng không nhận diện được nội dung.",
                    "Đọc nội dung OCR",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            Document.Content = text;
            OnPropertyChanged(nameof(Document));
            MessageBox.Show(
                "Đã đọc nội dung và điền vào ô Nội dung văn bản.",
                "Đọc nội dung OCR",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Không thể đọc nội dung file: {ex.Message}",
                "Lỗi OCR",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void OpenFile()
    {
        if (string.IsNullOrWhiteSpace(Document.ResolvedFilePath))
        {
            MessageBox.Show("Chưa có file đính kèm!", "Thông báo",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!File.Exists(Document.ResolvedFilePath))
        {
            MessageBox.Show("File không tồn tại!", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = Document.ResolvedFilePath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi mở file: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Document.DocumentNumber))
        {
            MessageBox.Show("Vui lòng nhập số văn bản!", "Thông báo",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(Document.Title))
        {
            MessageBox.Show("Vui lòng nhập tiêu đề!", "Thông báo",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            if (_isEditMode)
            {
                await _documentService.UpdateDocumentAsync(Document);
                MessageBox.Show("Cập nhật văn bản thành công!", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                var isFirstDocument = !AppPaths.IsInitialized;
                await _documentService.AddDocumentAsync(Document);
                var message = isFirstDocument
                    ? "Thêm văn bản thành công!\n\nHệ thống đã tạo thư mục lưu trữ tại D:\\SysCache_QLVB."
                    : "Thêm văn bản thành công!";
                MessageBox.Show(message, "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }

            CloseRequested?.Invoke(true);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi lưu văn bản: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke(false);
    }
}

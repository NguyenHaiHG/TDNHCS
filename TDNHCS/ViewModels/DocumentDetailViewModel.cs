using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using TDNHCS.Models;
using TDNHCS.Services;

namespace TDNHCS.ViewModels;

public partial class DocumentDetailViewModel : ObservableObject
{
    private readonly DocumentService _documentService;
    private readonly bool _isEditMode;

    [ObservableProperty]
    private Document _document = new();

    [ObservableProperty]
    private ObservableCollection<Category> _categories = new();

    public event Action<bool>? CloseRequested;

    public DocumentDetailViewModel(DocumentService documentService, Document? document = null)
    {
        _documentService = documentService;
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
            Filter = "All Files (*.*)|*.*|PDF Files (*.pdf)|*.pdf|Word Files (*.docx;*.doc)|*.docx;*.doc|Excel Files (*.xlsx;*.xls)|*.xlsx;*.xls"
        };

        if (dialog.ShowDialog() == true)
        {
            var fileName = Path.GetFileName(dialog.FileName);
            var documentsFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "TDNHCS",
                "Attachments",
                DateTime.Now.Year.ToString(),
                DateTime.Now.Month.ToString("D2")
            );

            Directory.CreateDirectory(documentsFolder);
            var destinationPath = Path.Combine(documentsFolder, fileName);

            try
            {
                if (File.Exists(destinationPath))
                {
                    var result = MessageBox.Show(
                        "File đã tồn tại. Bạn có muốn ghi đè?",
                        "Xác nhận",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result != MessageBoxResult.Yes)
                        return;
                }

                File.Copy(dialog.FileName, destinationPath, true);
                Document.FilePath = destinationPath;
                
                MessageBox.Show("Đính kèm file thành công!", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi sao chép file: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    [RelayCommand]
    private void OpenFile()
    {
        if (string.IsNullOrWhiteSpace(Document.FilePath))
        {
            MessageBox.Show("Chưa có file đính kèm!", "Thông báo",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!File.Exists(Document.FilePath))
        {
            MessageBox.Show("File không tồn tại!", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = Document.FilePath,
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
                await _documentService.AddDocumentAsync(Document);
                MessageBox.Show("Thêm văn bản thành công!", "Thông báo",
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

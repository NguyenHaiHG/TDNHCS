using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Windows;
using TDNHCS.Models;
using TDNHCS.Services;

namespace TDNHCS.ViewModels;

public partial class DocumentCompareViewModel : ObservableObject
{
    private readonly DocumentService _documentService;
    private readonly DocumentTextService _documentTextService;
    private readonly TextComparisonService _comparisonService;

    [ObservableProperty]
    private ObservableCollection<Document> _documents = new();

    [ObservableProperty]
    private Document? _selectedLeftDocument;

    [ObservableProperty]
    private Document? _selectedRightDocument;

    [ObservableProperty]
    private string _leftText = string.Empty;

    [ObservableProperty]
    private string _rightText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<DiffRow> _diffRows = new();

    [ObservableProperty]
    private string _similarityDisplay = "Chưa so sánh";

    [ObservableProperty]
    private string _summaryDisplay = "Nhập hoặc chọn hai văn bản, sau đó bấm So sánh.";

    public DocumentCompareViewModel(
        DocumentService documentService,
        DocumentTextService documentTextService,
        TextComparisonService comparisonService)
    {
        _documentService = documentService;
        _documentTextService = documentTextService;
        _comparisonService = comparisonService;
    }

    [RelayCommand]
    private async Task LoadDocumentsAsync()
    {
        try
        {
            var documents = await _documentService.GetAllDocumentsAsync();
            Documents = new ObservableCollection<Document>(documents);
        }
        catch (Exception ex)
        {
            SummaryDisplay = $"Không tải được danh sách văn bản: {ex.Message}";
        }
    }

    partial void OnSelectedLeftDocumentChanged(Document? value)
    {
        if (value != null)
        {
            LoadSelectedDocument(value, isLeft: true);
        }
    }

    partial void OnSelectedRightDocumentChanged(Document? value)
    {
        if (value != null)
        {
            LoadSelectedDocument(value, isLeft: false);
        }
    }

    private async void LoadSelectedDocument(Document document, bool isLeft)
    {
        try
        {
            SummaryDisplay = "Đang đọc nội dung văn bản, vui lòng chờ...";
            var text = await Task.Run(() => _documentTextService.GetText(document));
            if (isLeft)
            {
                LeftText = text;
            }
            else
            {
                RightText = text;
            }

            SummaryDisplay = "Đã đọc nội dung. Bấm So sánh để xem kết quả.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Không đọc được văn bản: {ex.Message}",
                "So sánh văn bản",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task BrowseLeft() => await BrowseFileAsync(isLeft: true);

    [RelayCommand]
    private async Task BrowseRight() => await BrowseFileAsync(isLeft: false);

    private async Task BrowseFileAsync(bool isLeft)
    {
        var dialog = new OpenFileDialog
        {
            Title = isLeft ? "Chọn văn bản thứ nhất" : "Chọn văn bản thứ hai",
            Filter = "Văn bản hỗ trợ (*.txt;*.docx;*.pdf;*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff)|*.txt;*.docx;*.pdf;*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff|PDF và ảnh scan (*.pdf;*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff)|*.pdf;*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff|Text và Word (*.txt;*.docx)|*.txt;*.docx"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            SummaryDisplay = "Đang OCR file scan trên máy, vui lòng chờ...";
            var text = await Task.Run(() => _documentTextService.ReadFile(dialog.FileName));
            if (isLeft)
            {
                SelectedLeftDocument = null;
                LeftText = text;
            }
            else
            {
                SelectedRightDocument = null;
                RightText = text;
            }

            SummaryDisplay = string.IsNullOrWhiteSpace(text)
                ? "OCR hoàn tất nhưng không nhận diện được nội dung."
                : "Đã đọc nội dung. Bấm So sánh để xem kết quả.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Không đọc được file: {ex.Message}",
                "So sánh văn bản",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void Compare()
    {
        if (string.IsNullOrWhiteSpace(LeftText) || string.IsNullOrWhiteSpace(RightText))
        {
            MessageBox.Show(
                "Vui lòng nhập hoặc chọn đủ hai văn bản.",
                "So sánh văn bản",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        try
        {
            var result = _comparisonService.Compare(LeftText, RightText);
            DiffRows = new ObservableCollection<DiffRow>(result.Rows);
            SimilarityDisplay = $"Độ tương đồng TF-IDF: {result.Similarity:P2}";
            SummaryDisplay =
                $"Thêm: {result.AddedCount}  |  Xóa: {result.RemovedCount}  |  " +
                $"Thay đổi: {result.ModifiedCount}  |  Tổng dòng hiển thị: {result.Rows.Count}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Không thể so sánh: {ex.Message}",
                "So sánh văn bản",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void Clear()
    {
        SelectedLeftDocument = null;
        SelectedRightDocument = null;
        LeftText = string.Empty;
        RightText = string.Empty;
        DiffRows.Clear();
        SimilarityDisplay = "Chưa so sánh";
        SummaryDisplay = "Nhập hoặc chọn hai văn bản, sau đó bấm So sánh.";
    }
}

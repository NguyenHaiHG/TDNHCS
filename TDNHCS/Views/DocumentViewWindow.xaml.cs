using System.IO;
using System.Windows;
using TDNHCS.Models;

namespace TDNHCS.Views;

public partial class DocumentViewWindow : Window
{
    public DocumentViewWindow(Document document)
    {
        InitializeComponent();
        LoadDocumentInfo(document);
        LoadFilePreview(document.FilePath);
    }

    // Hiển thị thông tin metadata của văn bản
    private void LoadDocumentInfo(Document doc)
    {
        txtDocumentNumber.Text = doc.DocumentNumber;
        txtTitle.Text = doc.Title;
        txtType.Text = doc.Type switch
        {
            DocumentType.Incoming => "Văn bản đến",
            DocumentType.Outgoing => "Văn bản đi",
            DocumentType.Internal => "Văn bản nội bộ",
            _ => doc.Type.ToString()
        };
        txtCategory.Text = doc.Category?.Name ?? "(Chưa có)";
        txtIssueDate.Text = doc.IssueDate.ToString("dd/MM/yyyy");
        txtReceivedDate.Text = doc.ReceivedDate.ToString("dd/MM/yyyy");
        txtCreatedBy.Text = doc.CreatedBy;
        txtSummary.Text = string.IsNullOrWhiteSpace(doc.Summary) ? "(Chưa có)" : doc.Summary;
        txtNotes.Text = string.IsNullOrWhiteSpace(doc.Notes) ? "(Chưa có)" : doc.Notes;
        txtFilePath.Text = string.IsNullOrWhiteSpace(doc.FilePath) ? "(Chưa đính kèm)" : doc.FilePath;
    }

    // Chọn cách hiển thị phù hợp theo loại file
    private void LoadFilePreview(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            pnlNoFile.Visibility = Visibility.Visible;
            return;
        }

        pnlNoFile.Visibility = Visibility.Collapsed;

        var ext = Path.GetExtension(filePath).ToLowerInvariant();

        switch (ext)
        {
            case ".pdf":
                ShowPdfPreview(filePath);
                break;

            case ".txt":
                ShowTextPreview(filePath);
                break;

            case ".png":
            case ".jpg":
            case ".jpeg":
            case ".bmp":
            case ".gif":
                ShowImagePreview(filePath);
                break;

            default:
                ShowNoPreviewMessage(ext);
                break;
        }
    }

    private void ShowPdfPreview(string filePath)
    {
        txtPreviewHeader.Text = "XEM NỘI DUNG FILE — PDF";
        webBrowser.Visibility = Visibility.Visible;
        webBrowser.Navigate(new Uri(filePath));
    }

    private void ShowTextPreview(string filePath)
    {
        txtPreviewHeader.Text = "XEM NỘI DUNG FILE — TEXT";
        txtFileContent.Visibility = Visibility.Visible;
        txtFileContent.Text = File.ReadAllText(filePath);
    }

    private void ShowImagePreview(string filePath)
    {
        txtPreviewHeader.Text = "XEM NỘI DUNG FILE — ẢNH";
        webBrowser.Visibility = Visibility.Visible;
        // Dùng WebBrowser để hiển thị ảnh không cần thêm control
        webBrowser.Navigate(new Uri(filePath));
    }

    private void ShowNoPreviewMessage(string ext)
    {
        txtPreviewHeader.Text = "XEM NỘI DUNG FILE";
        pnlNoPreview.Visibility = Visibility.Visible;
        txtNoPreviewMsg.Text = $"Không hỗ trợ xem trực tiếp file \"{ext.ToUpper()}\"\n\nVui lòng xem nội dung tóm tắt ở bên trái.";
    }
}

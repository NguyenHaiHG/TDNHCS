using System.IO;
using System.Windows;
using TDNHCS.Models;
using TDNHCS.Services;

namespace TDNHCS.Views;

public partial class DocumentViewWindow : Window
{
    public DocumentViewWindow(Document document)
    {
        InitializeComponent();
        LoadDocumentInfo(document);
        LoadPreview(document);
    }

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
        txtContent.Text = string.IsNullOrWhiteSpace(doc.Content) ? "(Chưa có)" : doc.Content;
        txtNotes.Text = string.IsNullOrWhiteSpace(doc.Notes) ? "(Chưa có)" : doc.Notes;
        txtFilePath.Text = string.IsNullOrWhiteSpace(doc.DisplayFileName) ? "(Chưa đính kèm)" : doc.DisplayFileName;
        txtStorageLocation.Text = doc.FileLocationDisplay;
    }

    private void LoadPreview(Document document)
    {
        HidePreviewPanels();

        if (!string.IsNullOrWhiteSpace(document.ResolvedFilePath) && File.Exists(document.ResolvedFilePath))
        {
            var ext = Path.GetExtension(document.ResolvedFilePath).ToLowerInvariant();
            switch (ext)
            {
                case ".pdf":
                    ShowPdfPreview(document.ResolvedFilePath);
                    return;

                case ".txt":
                    ShowTextPreview(document.ResolvedFilePath);
                    return;

                case ".docx":
                    ShowDocxPreview(document.ResolvedFilePath);
                    return;

                case ".png":
                case ".jpg":
                case ".jpeg":
                case ".bmp":
                case ".gif":
                    ShowImagePreview(document.ResolvedFilePath);
                    return;
            }
        }

        if (!string.IsNullOrWhiteSpace(document.Content))
        {
            ShowDatabaseContentPreview(document.Content);
            return;
        }

        if (string.IsNullOrWhiteSpace(document.FilePath))
        {
            pnlNoFile.Visibility = Visibility.Visible;
            return;
        }

        ShowNoPreviewMessage(Path.GetExtension(document.ResolvedFilePath ?? document.FilePath));
    }

    private void HidePreviewPanels()
    {
        webBrowser.Visibility = Visibility.Collapsed;
        txtFileContent.Visibility = Visibility.Collapsed;
        pnlNoPreview.Visibility = Visibility.Collapsed;
        pnlNoFile.Visibility = Visibility.Collapsed;
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

    private void ShowDocxPreview(string filePath)
    {
        txtPreviewHeader.Text = "XEM NỘI DUNG FILE — WORD";
        txtFileContent.Visibility = Visibility.Visible;

        try
        {
            var text = DocxPreviewService.ExtractPlainText(filePath);
            txtFileContent.Text = string.IsNullOrWhiteSpace(text)
                ? "Không đọc được nội dung file Word."
                : text;
        }
        catch (Exception ex)
        {
            txtFileContent.Text = $"Không thể đọc file Word: {ex.Message}";
        }
    }

    private void ShowDatabaseContentPreview(string content)
    {
        txtPreviewHeader.Text = "XEM NỘI DUNG VĂN BẢN";
        txtFileContent.Visibility = Visibility.Visible;
        txtFileContent.Text = content;
    }

    private void ShowImagePreview(string filePath)
    {
        txtPreviewHeader.Text = "XEM NỘI DUNG FILE — ẢNH";
        webBrowser.Visibility = Visibility.Visible;
        webBrowser.Navigate(new Uri(filePath));
    }

    private void ShowNoPreviewMessage(string ext)
    {
        txtPreviewHeader.Text = "XEM NỘI DUNG FILE";
        pnlNoPreview.Visibility = Visibility.Visible;
        txtNoPreviewMsg.Text = $"Không hỗ trợ xem trực tiếp file \"{ext.ToUpper()}\".\n\nVui lòng xem phần nội dung văn bản bên trái.";
    }
}

using System.IO;
using System.Text;
using TDNHCS.Models;
using UglyToad.PdfPig;

namespace TDNHCS.Services;

public sealed class DocumentTextService
{
    private readonly OcrService _ocrService;

    public DocumentTextService(OcrService ocrService)
    {
        _ocrService = ocrService;
    }

    public string GetText(Document document)
    {
        if (!string.IsNullOrWhiteSpace(document.Content))
        {
            return NormalizeText(document.Content);
        }

        var filePath = document.ResolvedFilePath;
        if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
        {
            return ReadFile(filePath);
        }

        return NormalizeText(document.Summary ?? string.Empty);
    }

    public string ReadFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".txt" => NormalizeText(File.ReadAllText(filePath)),
            ".docx" => NormalizeText(DocxPreviewService.ExtractPlainText(filePath)),
            ".pdf" => ReadPdf(filePath),
            ".png" or ".jpg" or ".jpeg" or ".bmp" or ".tif" or ".tiff"
                => NormalizeText(_ocrService.RecognizeFile(filePath)),
            _ => throw new NotSupportedException(
                "Chỉ hỗ trợ TXT, DOCX, PDF và ảnh PNG/JPG/BMP/TIFF.")
        };
    }

    /// <summary>
    /// Đọc PDF: thử PdfPig lấy text nhúng trước (nhanh, chính xác).
    /// Nếu ít hơn 50 ký tự (PDF scan/ảnh) thì fallback OCR Tesseract.
    /// </summary>
    private string ReadPdf(string filePath)
    {
        // Thử đọc text nhúng trước (PDF text/digital)
        try
        {
            var text = ExtractPdfText(filePath);
            if (text.Length >= 50)
            {
                return text;
            }
        }
        catch
        {
            // PDF bị mã hóa hoặc định dạng lạ → fallback OCR
        }

        // PDF scan → OCR Tesseract qua PDFtoImage
        return NormalizeText(_ocrService.RecognizeFile(filePath));
    }

    private static string ExtractPdfText(string filePath)
    {
        var sb = new StringBuilder();
        using var pdf = PdfDocument.Open(filePath);
        foreach (var page in pdf.GetPages())
        {
            sb.AppendLine(page.Text);
        }

        return NormalizeText(sb.ToString());
    }

    /// <summary>
    /// Chuẩn hóa văn bản: bỏ dòng trống thừa, chuẩn hóa khoảng trắng,
    /// giữ nguyên ký tự Unicode tiếng Việt.
    /// </summary>
    public static string NormalizeText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var normalized = lines
            .Select(l => l.Trim())
            .Where(l => l.Length > 0);
        return string.Join("\n", normalized);
    }
}

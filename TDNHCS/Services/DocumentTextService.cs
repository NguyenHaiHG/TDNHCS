using System.IO;
using TDNHCS.Models;

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
            return document.Content;
        }

        var filePath = document.ResolvedFilePath;
        if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
        {
            return ReadFile(filePath);
        }

        return document.Summary ?? string.Empty;
    }

    public string ReadFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".txt" => File.ReadAllText(filePath),
            ".docx" => DocxPreviewService.ExtractPlainText(filePath),
            ".pdf" or ".png" or ".jpg" or ".jpeg" or ".bmp" or ".tif" or ".tiff"
                => _ocrService.RecognizeFile(filePath),
            _ => throw new NotSupportedException(
                "Chỉ hỗ trợ TXT, DOCX, PDF scan và ảnh PNG/JPG/BMP/TIFF.")
        };
    }
}

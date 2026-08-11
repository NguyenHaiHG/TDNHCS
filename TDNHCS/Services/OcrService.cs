using PDFtoImage;
using System.IO;
using System.Text;
using TesseractOCR;
using TesseractOCR.Enums;

namespace TDNHCS.Services;

/// <summary>
/// OCR cục bộ bằng Tesseract + PDFtoImage. Hoàn toàn offline, không Internet.
/// </summary>
public sealed class OcrService
{
    private const int RenderDpi = 300;
    private static readonly string[] ImageExtensions =
        [".png", ".jpg", ".jpeg", ".bmp", ".tif", ".tiff"];

    private string TessDataPath => Path.Combine(AppContext.BaseDirectory, "tessdata");

    public string RecognizeFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        if (ImageExtensions.Contains(extension))
        {
            return RecognizeImage(filePath);
        }

        if (extension == ".pdf")
        {
            return RecognizePdf(filePath);
        }

        throw new NotSupportedException(
            "OCR chỉ hỗ trợ PDF scan và ảnh PNG, JPG, BMP, TIFF.");
    }

    private string RecognizeImage(string imagePath)
    {
        EnsureLanguageData();
        using var engine = new Engine(TessDataPath, "vie+eng", EngineMode.Default);
        using var image = TesseractOCR.Pix.Image.LoadFromFile(imagePath);
        using var page = engine.Process(image);
        return page.Text.Trim();
    }

    private string RecognizePdf(string pdfPath)
    {
        EnsureLanguageData();

        var pdfBytes = File.ReadAllBytes(pdfPath);
        var result = new StringBuilder();
        var pageIndex = 0;

        using var engine = new Engine(TessDataPath, "vie+eng", EngineMode.Default);

        var renderOptions = new RenderOptions(Dpi: RenderDpi);
        foreach (var bitmap in Conversion.ToImages(pdfBytes, password: null, options: renderOptions))
        {
            var tempPath = Path.Combine(
                Path.GetTempPath(),
                $"TDNHCS_OCR_{Guid.NewGuid():N}.png");
            try
            {
                // Lưu bitmap ra file PNG tạm
                using var fs = File.OpenWrite(tempPath);
                bitmap.Encode(fs, SkiaSharp.SKEncodedImageFormat.Png, 100);
                fs.Flush();

                using var tessImage = TesseractOCR.Pix.Image.LoadFromFile(tempPath);
                using var tessPage = engine.Process(tessImage);
                var text = tessPage.Text.Trim();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    if (result.Length > 0) result.AppendLine();
                    result.AppendLine($"--- Trang {++pageIndex} ---");
                    result.Append(text);
                }
                else
                {
                    pageIndex++;
                }
            }
            finally
            {
                bitmap.Dispose();
                if (File.Exists(tempPath)) File.Delete(tempPath);
            }
        }

        return result.ToString().Trim();
    }

    private void EnsureLanguageData()
    {
        var vietnameseData = Path.Combine(TessDataPath, "vie.traineddata");
        var englishData = Path.Combine(TessDataPath, "eng.traineddata");
        if (!File.Exists(vietnameseData) || !File.Exists(englishData))
        {
            throw new FileNotFoundException(
                "Thiếu dữ liệu OCR tiếng Việt/Anh trong thư mục tessdata.");
        }
    }
}

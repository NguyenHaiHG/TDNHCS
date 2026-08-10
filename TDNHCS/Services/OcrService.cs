using System.IO;
using System.Text;
using TesseractOCR;
using TesseractOCR.Enums;
using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Streams;

namespace TDNHCS.Services;

/// <summary>
/// OCR cục bộ bằng Tesseract. Không gửi tài liệu hoặc dữ liệu ra Internet.
/// </summary>
public sealed class OcrService
{
    private const uint RenderDpi = 300;
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
            return RecognizePdfAsync(filePath).GetAwaiter().GetResult();
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

    private async Task<string> RecognizePdfAsync(string pdfPath)
    {
        EnsureLanguageData();

        var storageFile = await StorageFile.GetFileFromPathAsync(Path.GetFullPath(pdfPath));
        var pdf = await PdfDocument.LoadFromFileAsync(storageFile);
        var result = new StringBuilder();

        using var engine = new Engine(TessDataPath, "vie+eng", EngineMode.Default);
        for (uint pageIndex = 0; pageIndex < pdf.PageCount; pageIndex++)
        {
            using var page = pdf.GetPage(pageIndex);
            using var renderedPage = new InMemoryRandomAccessStream();
            var scale = RenderDpi / 96d;
            var renderOptions = new PdfPageRenderOptions
            {
                DestinationWidth = Math.Max(
                    1,
                    (uint)Math.Ceiling(page.Dimensions.MediaBox.Width * scale)),
                DestinationHeight = Math.Max(
                    1,
                    (uint)Math.Ceiling(page.Dimensions.MediaBox.Height * scale))
            };

            await page.RenderToStreamAsync(renderedPage, renderOptions);
            var tempImagePath = await SaveRenderedPageAsync(renderedPage);

            try
            {
                using var image = TesseractOCR.Pix.Image.LoadFromFile(tempImagePath);
                using var recognizedPage = engine.Process(image);
                var text = recognizedPage.Text.Trim();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    if (result.Length > 0)
                    {
                        result.AppendLine().AppendLine();
                    }

                    result.AppendLine($"--- Trang {pageIndex + 1} ---");
                    result.Append(text);
                }
            }
            finally
            {
                File.Delete(tempImagePath);
            }
        }

        return result.ToString().Trim();
    }

    private static async Task<string> SaveRenderedPageAsync(
        InMemoryRandomAccessStream renderedPage)
    {
        renderedPage.Seek(0);
        using var input = renderedPage.GetInputStreamAt(0);
        using var reader = new DataReader(input);
        var loaded = await reader.LoadAsync((uint)renderedPage.Size);
        var bytes = new byte[loaded];
        reader.ReadBytes(bytes);

        var tempPath = Path.Combine(
            Path.GetTempPath(),
            $"TDNHCS_OCR_{Guid.NewGuid():N}.png");
        await File.WriteAllBytesAsync(tempPath, bytes);
        return tempPath;
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

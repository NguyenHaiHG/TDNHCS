using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

namespace TDNHCS.Services;

public static class DocxPreviewService
{
    public static string ExtractPlainText(string filePath)
    {
        using var archive = ZipFile.OpenRead(filePath);
        var entry = archive.GetEntry("word/document.xml");
        if (entry == null)
        {
            return string.Empty;
        }

        using var stream = entry.Open();
        var document = XDocument.Load(stream);
        XNamespace wordNs = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

        var builder = new StringBuilder();
        foreach (var paragraph in document.Descendants(wordNs + "p"))
        {
            var line = string.Concat(paragraph.Descendants(wordNs + "t").Select(node => node.Value));
            if (!string.IsNullOrWhiteSpace(line))
            {
                builder.AppendLine(line);
            }
        }

        return builder.ToString().Trim();
    }
}

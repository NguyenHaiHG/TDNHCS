using ClosedXML.Excel;
using TDNHCS.Models;

namespace TDNHCS.Services;

public class ExportService
{
    public void ExportToExcel(List<Document> documents, string filePath)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Danh sách văn bản");

        worksheet.Cell(1, 1).Value = "Số VB";
        worksheet.Cell(1, 2).Value = "Tiêu đề";
        worksheet.Cell(1, 3).Value = "Loại VB";
        worksheet.Cell(1, 4).Value = "Danh mục";
        worksheet.Cell(1, 5).Value = "Ngày ban hành";
        worksheet.Cell(1, 6).Value = "Ngày nhận";
        worksheet.Cell(1, 7).Value = "Người tạo";
        worksheet.Cell(1, 8).Value = "Tóm tắt";

        var headerRow = worksheet.Range(1, 1, 1, 8);
        headerRow.Style.Font.Bold = true;
        headerRow.Style.Fill.BackgroundColor = XLColor.LightBlue;
        headerRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        int row = 2;
        foreach (var doc in documents)
        {
            worksheet.Cell(row, 1).Value = doc.DocumentNumber;
            worksheet.Cell(row, 2).Value = doc.Title;
            worksheet.Cell(row, 3).Value = doc.Type.ToString();
            worksheet.Cell(row, 4).Value = doc.Category?.Name ?? "";
            worksheet.Cell(row, 5).Value = doc.IssueDate.ToString("dd/MM/yyyy");
            worksheet.Cell(row, 6).Value = doc.ReceivedDate.ToString("dd/MM/yyyy");
            worksheet.Cell(row, 7).Value = doc.CreatedBy;
            worksheet.Cell(row, 8).Value = doc.Summary ?? "";
            row++;
        }

        worksheet.Columns().AdjustToContents();

        workbook.SaveAs(filePath);
    }
}

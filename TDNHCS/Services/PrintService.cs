using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using TDNHCS.Models;

namespace TDNHCS.Services;

public class PrintService
{
    public void PrintDocuments(List<Document> documents)
    {
        var printDialog = new PrintDialog();
        
        if (printDialog.ShowDialog() == true)
        {
            var doc = new FlowDocument();
            doc.PagePadding = new Thickness(50);
            doc.ColumnWidth = double.PositiveInfinity;

            var title = new Paragraph(new Run("DANH SÁCH VĂN BẢN"))
            {
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };
            doc.Blocks.Add(title);

            var table = new Table();
            table.CellSpacing = 0;
            table.BorderBrush = System.Windows.Media.Brushes.Black;
            table.BorderThickness = new Thickness(1);

            table.Columns.Add(new TableColumn { Width = new GridLength(80) });
            table.Columns.Add(new TableColumn { Width = new GridLength(200) });
            table.Columns.Add(new TableColumn { Width = new GridLength(80) });
            table.Columns.Add(new TableColumn { Width = new GridLength(100) });
            table.Columns.Add(new TableColumn { Width = new GridLength(80) });

            var headerGroup = new TableRowGroup();
            var headerRow = new TableRow();
            headerRow.Background = System.Windows.Media.Brushes.LightGray;

            AddCell(headerRow, "Số VB", true);
            AddCell(headerRow, "Tiêu đề", true);
            AddCell(headerRow, "Loại VB", true);
            AddCell(headerRow, "Danh mục", true);
            AddCell(headerRow, "Ngày nhận", true);

            headerGroup.Rows.Add(headerRow);
            table.RowGroups.Add(headerGroup);

            var bodyGroup = new TableRowGroup();
            foreach (var document in documents)
            {
                var row = new TableRow();
                AddCell(row, document.DocumentNumber);
                AddCell(row, document.Title);
                AddCell(row, document.Type.ToString());
                AddCell(row, document.Category?.Name ?? "");
                AddCell(row, document.ReceivedDate.ToString("dd/MM/yyyy"));
                bodyGroup.Rows.Add(row);
            }
            table.RowGroups.Add(bodyGroup);

            doc.Blocks.Add(table);

            var footer = new Paragraph(new Run($"\nNgày in: {DateTime.Now:dd/MM/yyyy HH:mm}"))
            {
                FontSize = 10,
                FontStyle = FontStyles.Italic,
                Margin = new Thickness(0, 20, 0, 0)
            };
            doc.Blocks.Add(footer);

            var paginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;
            printDialog.PrintDocument(paginator, "In danh sách văn bản");
        }
    }

    private void AddCell(TableRow row, string text, bool isHeader = false)
    {
        var cell = new TableCell(new Paragraph(new Run(text)))
        {
            BorderBrush = System.Windows.Media.Brushes.Black,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(5)
        };

        if (isHeader)
        {
            cell.FontWeight = FontWeights.Bold;
        }

        row.Cells.Add(cell);
    }
}

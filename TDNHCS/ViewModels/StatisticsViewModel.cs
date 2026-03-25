using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using TDNHCS.Models;
using TDNHCS.Services;

namespace TDNHCS.ViewModels;

public partial class StatisticsViewModel : ObservableObject
{
    private readonly DocumentService _documentService;

    [ObservableProperty]
    private int _totalDocuments;

    [ObservableProperty]
    private int _incomingDocuments;

    [ObservableProperty]
    private int _outgoingDocuments;

    [ObservableProperty]
    private int _internalDocuments;

    [ObservableProperty]
    private ObservableCollection<MonthlyStatistic> _monthlyStatistics = new();

    [ObservableProperty]
    private ObservableCollection<CategoryStatistic> _categoryStatistics = new();

    public StatisticsViewModel(DocumentService documentService)
    {
        _documentService = documentService;
        LoadStatisticsAsync();
    }

    private async void LoadStatisticsAsync()
    {
        var documents = await _documentService.GetAllDocumentsAsync();

        TotalDocuments = documents.Count;
        IncomingDocuments = documents.Count(d => d.Type == DocumentType.Incoming);
        OutgoingDocuments = documents.Count(d => d.Type == DocumentType.Outgoing);
        InternalDocuments = documents.Count(d => d.Type == DocumentType.Internal);

        var currentYear = DateTime.Now.Year;
        var monthlyStats = new List<MonthlyStatistic>();

        for (int month = 1; month <= 12; month++)
        {
            var monthDocs = documents.Where(d => d.ReceivedDate.Year == currentYear && d.ReceivedDate.Month == month).ToList();
            
            monthlyStats.Add(new MonthlyStatistic
            {
                Month = $"Tháng {month}/{currentYear}",
                Incoming = monthDocs.Count(d => d.Type == DocumentType.Incoming),
                Outgoing = monthDocs.Count(d => d.Type == DocumentType.Outgoing),
                Internal = monthDocs.Count(d => d.Type == DocumentType.Internal),
                Total = monthDocs.Count
            });
        }

        MonthlyStatistics = new ObservableCollection<MonthlyStatistic>(monthlyStats);

        var categories = await _documentService.GetAllCategoriesAsync();
        var categoryStats = categories.Select(c => new CategoryStatistic
        {
            CategoryName = c.Name,
            Count = documents.Count(d => d.CategoryId == c.Id),
            Percentage = TotalDocuments > 0 ? (documents.Count(d => d.CategoryId == c.Id) * 100.0 / TotalDocuments) : 0
        }).OrderByDescending(x => x.Count).ToList();

        CategoryStatistics = new ObservableCollection<CategoryStatistic>(categoryStats);
    }
}

public class MonthlyStatistic
{
    public string Month { get; set; } = string.Empty;
    public int Incoming { get; set; }
    public int Outgoing { get; set; }
    public int Internal { get; set; }
    public int Total { get; set; }
}

public class CategoryStatistic
{
    public string CategoryName { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

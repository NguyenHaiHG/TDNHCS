using System.Windows;
using TDNHCS.ViewModels;

namespace TDNHCS;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly DocumentCompareViewModel _documentCompareViewModel;

    public MainWindow(
        MainViewModel viewModel,
        DocumentCompareViewModel documentCompareViewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _documentCompareViewModel = documentCompareViewModel;
        DataContext = _viewModel;
        DocumentCompareView.DataContext = _documentCompareViewModel;

        // Load dữ liệu khi khởi động
        Loaded += async (s, e) =>
        {
            await _viewModel.LoadDataCommand.ExecuteAsync(null);
            await _documentCompareViewModel.LoadDocumentsCommand.ExecuteAsync(null);
            _viewModel.PromptDefaultPasswordChange();
            await _viewModel.CheckForUpdatesSilentlyAsync();
        };
    }

    private void DataGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {

    }
}
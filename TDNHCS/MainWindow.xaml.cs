using System.Windows;
using TDNHCS.ViewModels;

namespace TDNHCS;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = _viewModel;

        // Load dữ liệu khi khởi động
        Loaded += async (s, e) => await _viewModel.LoadDataCommand.ExecuteAsync(null);
    }
}
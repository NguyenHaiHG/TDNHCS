using System.Windows;
using TDNHCS.ViewModels;

namespace TDNHCS.Views;

public partial class StatisticsWindow : Window
{
    public StatisticsWindow(StatisticsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}

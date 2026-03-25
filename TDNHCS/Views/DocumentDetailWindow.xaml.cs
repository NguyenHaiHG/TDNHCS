using System.Windows;
using TDNHCS.ViewModels;

namespace TDNHCS.Views;

public partial class DocumentDetailWindow : Window
{
    public DocumentDetailWindow(DocumentDetailViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        
        viewModel.CloseRequested += (saved) => 
        {
            DialogResult = saved;
            Close();
        };
    }
}

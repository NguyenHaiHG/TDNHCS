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

    private void cboType_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {

    }

    private void cboCategory_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {

    }
}

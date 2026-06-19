using System.Windows;

namespace TDNHCS.Views;

public partial class UpdateProgressWindow : Window
{
    public UpdateProgressWindow()
    {
        InitializeComponent();
    }

    public void UpdateProgress(int percent)
    {
        progressBar.Value = percent;
        txtStatus.Text = percent >= 0 ? $"{percent}%" : "Đang tải...";
    }
}

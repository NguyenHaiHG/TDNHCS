using System.Windows;
using TDNHCS.ViewModels;

namespace TDNHCS.Views;

public partial class ChangePasswordWindow : Window
{
    private readonly ChangePasswordViewModel _viewModel;

    public ChangePasswordWindow(ChangePasswordViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        viewModel.CloseRequested += () => Close();
    }

    // PasswordBox không hỗ trợ binding trực tiếp — cập nhật thủ công
    private void pbOldPassword_PasswordChanged(object sender, RoutedEventArgs e)
        => _viewModel.OldPassword = pbOldPassword.Password;

    private void pbNewPassword_PasswordChanged(object sender, RoutedEventArgs e)
        => _viewModel.NewPassword = pbNewPassword.Password;

    private void pbConfirmPassword_PasswordChanged(object sender, RoutedEventArgs e)
        => _viewModel.ConfirmPassword = pbConfirmPassword.Password;
}

using System.Windows;
using System.Windows.Controls;
using TDNHCS.ViewModels;

namespace TDNHCS.Views;

public partial class ChangePasswordWindow : Window
{
    private readonly ChangePasswordViewModel _viewModel;
    private bool _isSynchronizingPasswords;

    public ChangePasswordWindow(ChangePasswordViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        viewModel.CloseRequested += () => Close();
    }

    // PasswordBox không hỗ trợ binding trực tiếp — cập nhật thủ công
    private void pbOldPassword_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_isSynchronizingPasswords) return;
        _viewModel.OldPassword = pbOldPassword.Password;
        SynchronizeVisibleText(txtOldPassword, pbOldPassword.Password);
    }

    private void pbNewPassword_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_isSynchronizingPasswords) return;
        _viewModel.NewPassword = pbNewPassword.Password;
        SynchronizeVisibleText(txtNewPassword, pbNewPassword.Password);
    }

    private void pbConfirmPassword_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_isSynchronizingPasswords) return;
        _viewModel.ConfirmPassword = pbConfirmPassword.Password;
        SynchronizeVisibleText(txtConfirmPassword, pbConfirmPassword.Password);
    }

    private void txtOldPassword_TextChanged(object sender, TextChangedEventArgs e)
        => SynchronizePasswordBox(pbOldPassword, txtOldPassword.Text, value => _viewModel.OldPassword = value);

    private void txtNewPassword_TextChanged(object sender, TextChangedEventArgs e)
        => SynchronizePasswordBox(pbNewPassword, txtNewPassword.Text, value => _viewModel.NewPassword = value);

    private void txtConfirmPassword_TextChanged(object sender, TextChangedEventArgs e)
        => SynchronizePasswordBox(pbConfirmPassword, txtConfirmPassword.Text, value => _viewModel.ConfirmPassword = value);

    private void SynchronizeVisibleText(TextBox textBox, string value)
    {
        _isSynchronizingPasswords = true;
        textBox.Text = value;
        _isSynchronizingPasswords = false;
    }

    private void SynchronizePasswordBox(PasswordBox passwordBox, string value, Action<string> updateViewModel)
    {
        if (_isSynchronizingPasswords) return;
        updateViewModel(value);
        _isSynchronizingPasswords = true;
        passwordBox.Password = value;
        _isSynchronizingPasswords = false;
    }

    private void ShowPasswords_Changed(object sender, RoutedEventArgs e)
    {
        var show = sender is CheckBox { IsChecked: true };
        var passwordVisibility = show ? Visibility.Collapsed : Visibility.Visible;
        var textVisibility = show ? Visibility.Visible : Visibility.Collapsed;

        pbOldPassword.Visibility = passwordVisibility;
        pbNewPassword.Visibility = passwordVisibility;
        pbConfirmPassword.Visibility = passwordVisibility;
        txtOldPassword.Visibility = textVisibility;
        txtNewPassword.Visibility = textVisibility;
        txtConfirmPassword.Visibility = textVisibility;
    }
}

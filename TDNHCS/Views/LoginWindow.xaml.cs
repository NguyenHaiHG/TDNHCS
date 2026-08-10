using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TDNHCS.ViewModels;

namespace TDNHCS.Views;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _viewModel;
    private bool _isPasswordVisible;
    private bool _isSynchronizingPassword;

    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        _viewModel.LoginSuccessful += () =>
        {
            DialogResult = true;
            Close();
        };

        txtUsername.Focus();

        PreviewKeyDown += (_, e) =>
        {
            if ((e.Key == Key.Return || e.Key == Key.Enter) && _viewModel.LoginCommand.CanExecute(null))
            {
                _viewModel.LoginCommand.Execute(null);
                e.Handled = true;
            }
        };
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (!_isSynchronizingPassword && sender is PasswordBox passwordBox)
        {
            _viewModel.Password = passwordBox.Password;
            _isSynchronizingPassword = true;
            txtPasswordVisible.Text = passwordBox.Password;
            _isSynchronizingPassword = false;
        }
    }

    private void VisiblePassword_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isSynchronizingPassword)
        {
            return;
        }

        _viewModel.Password = txtPasswordVisible.Text;
        _isSynchronizingPassword = true;
        txtPassword.Password = txtPasswordVisible.Text;
        _isSynchronizingPassword = false;
    }

    private void TogglePasswordVisibility_Click(object sender, RoutedEventArgs e)
    {
        _isPasswordVisible = !_isPasswordVisible;
        txtPassword.Visibility = _isPasswordVisible ? Visibility.Collapsed : Visibility.Visible;
        txtPasswordVisible.Visibility = _isPasswordVisible ? Visibility.Visible : Visibility.Collapsed;
        btnTogglePassword.Content = _isPasswordVisible ? "🙈" : "👁";
        btnTogglePassword.ToolTip = _isPasswordVisible ? "Ẩn mật khẩu" : "Hiện mật khẩu";

        if (_isPasswordVisible)
        {
            txtPasswordVisible.Focus();
            txtPasswordVisible.CaretIndex = txtPasswordVisible.Text.Length;
        }
        else
        {
            txtPassword.Focus();
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}

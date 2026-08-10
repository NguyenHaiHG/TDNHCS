using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using TDNHCS.Services;

namespace TDNHCS.ViewModels;

public partial class ChangePasswordViewModel : ObservableObject
{
    private readonly UserService _userService;

    [ObservableProperty]
    private string _username;

    [ObservableProperty]
    private string _oldPassword = string.Empty;

    [ObservableProperty]
    private string _newPassword = string.Empty;

    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    public event Action? CloseRequested;

    public ChangePasswordViewModel(UserService userService)
    {
        _userService = userService;
        _username = LoginViewModel.CurrentUser?.Username ?? string.Empty;
    }

    [RelayCommand]
    private async Task ChangePassword()
    {
        HasError = false;
        ErrorMessage = string.Empty;

        Username = Username.Trim();
        if (Username.Length < 3 || Username.Length > 100 || Username.Any(char.IsWhiteSpace))
        {
            ErrorMessage = "Tên đăng nhập phải từ 3 đến 100 ký tự và không chứa khoảng trắng!";
            HasError = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(OldPassword))
        {
            ErrorMessage = "Vui lòng nhập mật khẩu cũ!";
            HasError = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(NewPassword) || NewPassword.Length < 6)
        {
            ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự!";
            HasError = true;
            return;
        }

        if (NewPassword != ConfirmPassword)
        {
            ErrorMessage = "Mật khẩu xác nhận không khớp!";
            HasError = true;
            return;
        }

        var currentUsername = LoginViewModel.CurrentUser?.Username ?? string.Empty;
        var result = await _userService.UpdateCredentialsAsync(
            currentUsername,
            Username,
            OldPassword,
            NewPassword);

        if (result == CredentialUpdateResult.Success)
        {
            if (LoginViewModel.CurrentUser != null)
            {
                LoginViewModel.CurrentUser.Username = Username;
                LoginViewModel.CurrentUser.PasswordHash =
                    TDNHCS.Data.DocumentDbContext.HashPassword(NewPassword);
            }

            MessageBox.Show("Cập nhật tên đăng nhập và mật khẩu thành công!", "Thông báo",
                MessageBoxButton.OK, MessageBoxImage.Information);
            CloseRequested?.Invoke();
        }
        else
        {
            ErrorMessage = result switch
            {
                CredentialUpdateResult.UsernameTaken => "Tên đăng nhập này đã được sử dụng!",
                CredentialUpdateResult.NotInitialized => "Dữ liệu ứng dụng chưa được khởi tạo!",
                _ => "Mật khẩu cũ không đúng!"
            };
            HasError = true;
        }
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke();
}

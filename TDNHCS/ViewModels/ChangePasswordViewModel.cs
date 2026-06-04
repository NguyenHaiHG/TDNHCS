using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using TDNHCS.Services;

namespace TDNHCS.ViewModels;

public partial class ChangePasswordViewModel : ObservableObject
{
    private readonly UserService _userService;

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
    }

    [RelayCommand]
    private async Task ChangePassword()
    {
        HasError = false;
        ErrorMessage = string.Empty;

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

        var username = LoginViewModel.CurrentUser?.Username ?? string.Empty;
        var success = await _userService.ChangePasswordAsync(username, OldPassword, NewPassword);

        if (success)
        {
            MessageBox.Show("Đổi mật khẩu thành công!", "Thông báo",
                MessageBoxButton.OK, MessageBoxImage.Information);
            CloseRequested?.Invoke();
        }
        else
        {
            ErrorMessage = "Mật khẩu cũ không đúng!";
            HasError = true;
        }
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke();
}

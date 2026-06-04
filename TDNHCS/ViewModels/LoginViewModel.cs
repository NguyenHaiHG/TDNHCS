using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TDNHCS.Models;
using TDNHCS.Services;

namespace TDNHCS.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly UserService _userService;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    public event Action? LoginSuccessful;

    // Lưu thông tin user đang đăng nhập để dùng toàn app
    public static User? CurrentUser { get; private set; }

    public LoginViewModel(UserService userService)
    {
        _userService = userService;
    }

    [RelayCommand]
    private async Task Login()
    {
        if (string.IsNullOrWhiteSpace(Username))
        {
            ErrorMessage = "Vui lòng nhập tên đăng nhập!";
            HasError = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Vui lòng nhập mật khẩu!";
            HasError = true;
            return;
        }

        var user = await _userService.LoginAsync(Username, Password);
        if (user != null)
        {
            CurrentUser = user;
            HasError = false;
            LoginSuccessful?.Invoke();
        }
        else
        {
            ErrorMessage = "Tên đăng nhập hoặc mật khẩu không đúng!";
            HasError = true;
        }
    }
}

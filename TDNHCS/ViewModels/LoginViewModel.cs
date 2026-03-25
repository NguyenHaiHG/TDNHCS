using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TDNHCS.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    public event Action? LoginSuccessful;

    public static string? CurrentUser { get; private set; }

    [RelayCommand]
    private void Login()
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

        if (Username == "admin" && Password == "admin123")
        {
            CurrentUser = Username;
            HasError = false;
            LoginSuccessful?.Invoke();
        }
        else if (Username == "user" && Password == "user123")
        {
            CurrentUser = Username;
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

using System.Windows;
using StudentInnovation.Shared.Models.Dtos;
using StudentInnovation.WpfApp.Commands;
using StudentInnovation.WpfApp.Services;
using StudentInnovation.WpfApp.Views;

namespace StudentInnovation.WpfApp.ViewModels;

public class LoginViewModel : ObservableObject
{
    private readonly ApiClient _apiClient;
    private string _username = "admin";
    private string _password = "123456";
    private string _message = string.Empty;

    public LoginViewModel(ApiClient apiClient)
    {
        _apiClient = apiClient;
        LoginCommand = new RelayCommand(async _ => await LoginAsync());
    }

    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }

    public RelayCommand LoginCommand { get; }

    private async Task LoginAsync()
    {
        var response = await _apiClient.LoginAsync(new LoginRequest
        {
            Username = Username,
            Password = Password
        });

        if (response is null || !response.Success)
        {
            Message = response?.Message ?? "登录失败";
            return;
        }

        _apiClient.SetBearerToken(response.Token);
        var window = new MainWindow(_apiClient, response);
        window.Show();

        Application.Current.Windows
            .OfType<Window>()
            .FirstOrDefault(w => w is LoginWindow)
            ?.Close();
    }
}

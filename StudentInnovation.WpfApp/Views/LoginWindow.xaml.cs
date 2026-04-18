using System.Windows;
using StudentInnovation.WpfApp.Services;
using StudentInnovation.WpfApp.ViewModels;

namespace StudentInnovation.WpfApp.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
        DataContext = new LoginViewModel(new ApiClient("http://localhost:5197/"));
    }
}

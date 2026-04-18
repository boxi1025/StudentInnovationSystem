using System.Windows;
using StudentInnovation.WpfApp.Services;

namespace StudentInnovation.WpfApp.Views;

public partial class ChangePasswordDialog : Window
{
    private readonly ApiClient _apiClient;

    public ChangePasswordDialog(ApiClient apiClient)
    {
        InitializeComponent();
        _apiClient = apiClient;
    }

    private async void Ok_Click(object sender, RoutedEventArgs e)
    {
        var oldPwd = OldPasswordBox.Password;
        var newPwd = NewPasswordBox.Password;
        var confirmPwd = ConfirmPasswordBox.Password;

        if (string.IsNullOrWhiteSpace(oldPwd) || string.IsNullOrWhiteSpace(newPwd) || string.IsNullOrWhiteSpace(confirmPwd))
        {
            MessageBox.Show("密码不能为空", "提示");
            return;
        }

        if (!string.Equals(newPwd, confirmPwd, StringComparison.Ordinal))
        {
            MessageBox.Show("两次输入的新密码不一致", "提示");
            return;
        }

        try
        {
            var ok = await _apiClient.ChangePasswordAsync(new ChangePasswordRequest
            {
                OldPassword = oldPwd,
                NewPassword = newPwd,
                ConfirmPassword = confirmPwd
            });

            if (!ok)
            {
                MessageBox.Show("修改失败，请检查原密码是否正确", "提示");
                return;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"修改密码失败：{ex.Message}", "提示");
            return;
        }

        DialogResult = true;
        Close();
    }
}


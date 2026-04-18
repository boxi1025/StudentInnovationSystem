using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using StudentInnovation.Shared.Models;
using StudentInnovation.WpfApp.Services;

namespace StudentInnovation.WpfApp.Views;

public partial class UserDialog : Window, INotifyPropertyChanged
{
    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _confirmPassword = string.Empty;
    private string _role = "Teacher";
    private string _fullName = string.Empty;
    private string _department = string.Empty;
    private string _studentNo = string.Empty;
    private string _employeeNo = string.Empty;

    public UserDialog()
    {
        InitializeComponent();
        DataContext = this;
    }

    public UserDialog(User user) : this()
    {
        Username = user.Username;
        // 密码不显示，留空
        Role = user.Role;
        FullName = user.FullName;
        Department = user.Department;
        StudentNo = user.StudentNo;
        EmployeeNo = user.EmployeeNo;
        IsEditMode = true;
        Title = "编辑用户";
    }

    public bool IsEditMode { get; set; }

    public string Username
    {
        get => _username;
        set => SetField(ref _username, value);
    }

    public string Password
    {
        get => _password;
        set => SetField(ref _password, value);
    }

    public string ConfirmPassword
    {
        get => _confirmPassword;
        set => SetField(ref _confirmPassword, value);
    }

    public string Role
    {
        get => _role;
        set
        {
            if (SetField(ref _role, value))
            {
                OnPropertyChanged(nameof(IsStudent));
                OnPropertyChanged(nameof(IsTeacher));
            }
        }
    }

    public string FullName
    {
        get => _fullName;
        set => SetField(ref _fullName, value);
    }

    public string Department
    {
        get => _department;
        set => SetField(ref _department, value);
    }

    public string StudentNo
    {
        get => _studentNo;
        set => SetField(ref _studentNo, value);
    }

    public string EmployeeNo
    {
        get => _employeeNo;
        set => SetField(ref _employeeNo, value);
    }

    public bool IsStudent => Role == "Student";
    public bool IsTeacher => Role == "Teacher";

    public UserCreateRequest GetCreateRequest()
    {
        return new UserCreateRequest
        {
            Username = Username,
            Password = Password,
            Role = Role,
            FullName = FullName,
            Department = Department,
            StudentNo = StudentNo,
            EmployeeNo = EmployeeNo
        };
    }

    public UserUpdateRequest GetUpdateRequest()
    {
        var request = new UserUpdateRequest();
        if (!string.IsNullOrWhiteSpace(FullName)) request.FullName = FullName;
        if (!string.IsNullOrWhiteSpace(Department)) request.Department = Department;
        if (!string.IsNullOrWhiteSpace(StudentNo)) request.StudentNo = StudentNo;
        if (!string.IsNullOrWhiteSpace(EmployeeNo)) request.EmployeeNo = EmployeeNo;
        if (!string.IsNullOrWhiteSpace(Password)) request.Password = Password;
        return request;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        // 验证
        if (string.IsNullOrWhiteSpace(Username))
        {
            MessageBox.Show("请输入用户名");
            return;
        }

        if (!IsEditMode && string.IsNullOrWhiteSpace(Password))
        {
            MessageBox.Show("请输入密码");
            return;
        }

        if (!IsEditMode && Password != ConfirmPassword)
        {
            MessageBox.Show("两次输入的密码不一致");
            return;
        }

        if (string.IsNullOrWhiteSpace(FullName))
        {
            MessageBox.Show("请输入姓名");
            return;
        }

        if (string.IsNullOrWhiteSpace(Department))
        {
            MessageBox.Show("请输入学院");
            return;
        }

        if (Role == "Student" && string.IsNullOrWhiteSpace(StudentNo))
        {
            MessageBox.Show("请输入学号");
            return;
        }

        if (Role == "Teacher" && string.IsNullOrWhiteSpace(EmployeeNo))
        {
            MessageBox.Show("请输入工号");
            return;
        }

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
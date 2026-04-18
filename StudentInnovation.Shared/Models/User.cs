namespace StudentInnovation.Shared.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "Student";
    public string FullName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string StudentNo { get; set; } = string.Empty;
    public string EmployeeNo { get; set; } = string.Empty;
}

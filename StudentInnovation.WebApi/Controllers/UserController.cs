using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentInnovation.Shared.Models;
using StudentInnovation.WebApi.Data;
using StudentInnovation.WebApi.Services;
using System.Security.Claims;

namespace StudentInnovation.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly StudentInnovationDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;

    public UserController(StudentInnovationDbContext dbContext, IPasswordHasher passwordHasher)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<User>>> GetAll()
    {
        var users = await _dbContext.Users.Where(u => u.Role != "Admin").ToListAsync();
        return Ok(users);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<User>> Create([FromBody] UserCreateRequest request)
    {
        if (await _dbContext.Users.AnyAsync(u => u.Username == request.Username))
        {
            return BadRequest("用户名已存在");
        }

        var user = new User
        {
            Username = request.Username,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = request.Role,
            FullName = request.FullName,
            Department = request.Department,
            StudentNo = request.StudentNo,
            EmployeeNo = request.EmployeeNo
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
        return Ok(user);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> Update(int id, [FromBody] UserUpdateRequest request)
    {
        var (username, role) = GetUserInfo();
        var user = await _dbContext.Users.FindAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        // 非管理员只能修改自己的信息
        if (role != "Admin" && user.Username != username)
        {
            return Forbid();
        }

        if (!string.IsNullOrWhiteSpace(request.FullName))
            user.FullName = request.FullName;
        if (!string.IsNullOrWhiteSpace(request.Department))
            user.Department = request.Department;
        if (!string.IsNullOrWhiteSpace(request.StudentNo))
            user.StudentNo = request.StudentNo;
        if (!string.IsNullOrWhiteSpace(request.EmployeeNo))
            user.EmployeeNo = request.EmployeeNo;
        if (!string.IsNullOrWhiteSpace(request.Password))
            user.PasswordHash = _passwordHasher.Hash(request.Password);

        await _dbContext.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Delete(int id)
    {
        var user = await _dbContext.Users.FindAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        if (user.Role == "Admin")
        {
            return BadRequest("不能删除管理员账户");
        }

        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("change-password")]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var (username, _) = GetUserInfo();

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user is null)
        {
            return Unauthorized();
        }

        if (!_passwordHasher.Verify(request.OldPassword, user.PasswordHash))
        {
            return BadRequest("原密码错误");
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword) || string.IsNullOrWhiteSpace(request.ConfirmPassword))
        {
            return BadRequest("新密码不能为空");
        }

        if (!string.Equals(request.NewPassword, request.ConfirmPassword, StringComparison.Ordinal))
        {
            return BadRequest("两次输入的新密码不一致");
        }

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        await _dbContext.SaveChangesAsync();
        return NoContent();
    }

    private (string Username, string Role) GetUserInfo()
    {
        var username = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
        var role = User.FindFirstValue(ClaimTypes.Role) ?? "Student";
        return (username, role);
    }
}

public class UserCreateRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "Student";
    public string FullName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string StudentNo { get; set; } = string.Empty;
    public string EmployeeNo { get; set; } = string.Empty;
}

public class UserUpdateRequest
{
    public string? FullName { get; set; }
    public string? Department { get; set; }
    public string? StudentNo { get; set; }
    public string? EmployeeNo { get; set; }
    public string? Password { get; set; }
}

public class ChangePasswordRequest
{
    public string OldPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}
using Microsoft.EntityFrameworkCore;
using StudentInnovation.Shared.Models.Dtos;
using StudentInnovation.WebApi.Data;

namespace StudentInnovation.WebApi.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
}

public class AuthService : IAuthService
{
    private readonly StudentInnovationDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(StudentInnovationDbContext dbContext, IPasswordHasher passwordHasher, IJwtTokenService jwtTokenService)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Username == request.Username);
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return new LoginResponse { Success = false, Message = "用户名或密码错误" };
        }

        var token = _jwtTokenService.Generate(user);
        return new LoginResponse
        {
            Success = true,
            Token = token,
            Message = "登录成功",
            Username = user.Username,
            FullName = user.FullName,
            Department = user.Department,
            Role = user.Role,
            StudentNo = user.StudentNo,
            EmployeeNo = user.EmployeeNo
        };
    }
}

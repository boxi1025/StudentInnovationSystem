using Microsoft.AspNetCore.Mvc;
using StudentInnovation.Shared.Models.Dtos;
using StudentInnovation.WebApi.Services;

namespace StudentInnovation.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LoginController : ControllerBase
{
    private readonly IAuthService _authService;

    public LoginController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        if (!result.Success)
        {
            return Unauthorized(result);
        }

        return Ok(result);
    }
}

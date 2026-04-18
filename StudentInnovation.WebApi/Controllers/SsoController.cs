using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentInnovation.WebApi.Data;

namespace StudentInnovation.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/sso")]
public class SsoController : ControllerBase
{
    private readonly StudentInnovationDbContext _dbContext;

    public SsoController(StudentInnovationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("profile/{username}")]
    public async Task<ActionResult<object>> GetProfile(string username)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Username == username);
        if (user is null) return NotFound();
        return Ok(new
        {
            user.Username,
            user.FullName,
            user.Role,
            user.Department,
            user.StudentNo,
            user.EmployeeNo
        });
    }
}

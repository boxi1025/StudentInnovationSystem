using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentInnovation.Shared.Models;
using StudentInnovation.Shared.Models.Dtos;
using StudentInnovation.WebApi.Data;
using System.Security.Claims;
using System.Text;

namespace StudentInnovation.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class AchievementController : ControllerBase
{
    private readonly StudentInnovationDbContext _dbContext;

    public AchievementController(StudentInnovationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<List<Achievement>>> GetAll([FromQuery] AchievementQueryDto query)
    {
        var (username, role) = GetUserInfo();
        var q = _dbContext.Achievements.AsQueryable();

        if (string.Equals(role, "Student", StringComparison.OrdinalIgnoreCase))
        {
            q = q.Where(x => x.OwnerUsername == username);
        }
        else
        {
            // 草稿仅允许其创建者查看；其它角色隐藏其它学生的草稿
            q = q.Where(x => x.Status != "草稿" || x.OwnerUsername == username);
        }

        if (!string.IsNullOrWhiteSpace(query.Department))
        {
            q = q.Where(x => x.Department == query.Department);
        }

        if (query.Year.HasValue)
        {
            q = q.Where(x => x.Year == query.Year.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Level))
        {
            q = q.Where(x => x.Level == query.Level);
        }

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            q = q.Where(x => x.Category == query.Category);
        }

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            q = q.Where(x => x.Title.Contains(query.Keyword)
                || x.Description.Contains(query.Keyword)
                || x.StudentName.Contains(query.Keyword)
                || x.StudentId.Contains(query.Keyword)
                || x.Department.Contains(query.Keyword)
                || x.Advisor.Contains(query.Keyword)
                || x.Status.Contains(query.Keyword)
                || x.Category.Contains(query.Keyword)
                || x.Level.Contains(query.Keyword)
                || x.TeamName.Contains(query.Keyword)
                || x.Id.ToString().Contains(query.Keyword)
                || x.CreditScore.ToString().Contains(query.Keyword)
                || x.Year.ToString().Contains(query.Keyword));
        }

        var list = await q.Include(x => x.Attachments).Include(x => x.AuditLogs).OrderByDescending(x => x.Id).ToListAsync();
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Achievement>> GetById(int id)
    {
        var (username, role) = GetUserInfo();
        var q = _dbContext.Achievements.AsQueryable();

        if (string.Equals(role, "Student", StringComparison.OrdinalIgnoreCase))
        {
            q = q.Where(x => x.OwnerUsername == username);
        }
        else
        {
            q = q.Where(x => x.Status != "草稿" || x.OwnerUsername == username);
        }

        var item = await q
            .Include(x => x.Attachments)
            .Include(x => x.AuditLogs)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (item is null)
        {
            return NotFound();
        }

        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<Achievement>> Create([FromBody] Achievement achievement)
    {
        var (username, _) = GetUserInfo();
        achievement.OwnerUsername = username;
        achievement.Status = "草稿";
        if (!TryValidateCreditScore(achievement.CreditScore, out var createCreditError))
        {
            return BadRequest(createCreditError);
        }

        achievement.CreditScore = decimal.Round(achievement.CreditScore, 1, MidpointRounding.AwayFromZero);
        _dbContext.Achievements.Add(achievement);
        await _dbContext.SaveChangesAsync();
        return Ok(achievement);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> Update(int id, [FromBody] Achievement achievement)
    {
        var existing = await _dbContext.Achievements.FirstOrDefaultAsync(x => x.Id == id);
        if (existing is null)
        {
            return NotFound();
        }

        existing.Title = achievement.Title;
        existing.Category = achievement.Category;
        existing.StudentName = achievement.StudentName;
        existing.StudentId = achievement.StudentId;
        existing.Advisor = achievement.Advisor;
        existing.AchievedOn = achievement.AchievedOn;
        existing.Department = achievement.Department;
        existing.Level = achievement.Level;
        existing.Year = achievement.Year;
        existing.TeamName = achievement.TeamName;
        existing.ExtraJson = achievement.ExtraJson;
        if (!TryValidateCreditScore(achievement.CreditScore, out var updateCreditError))
        {
            return BadRequest(updateCreditError);
        }

        existing.CreditScore = decimal.Round(achievement.CreditScore, 1, MidpointRounding.AwayFromZero);
        existing.Description = achievement.Description;

        await _dbContext.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var existing = await _dbContext.Achievements.FirstOrDefaultAsync(x => x.Id == id);
        if (existing is null)
        {
            return NotFound();
        }

        _dbContext.Achievements.Remove(existing);
        await _dbContext.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:int}/submit")]
    public async Task<ActionResult> SubmitForReview(int id)
    {
        var (username, _) = GetUserInfo();
        var existing = await _dbContext.Achievements.FirstOrDefaultAsync(x => x.Id == id);
        if (existing is null)
        {
            return NotFound();
        }

        if (!string.Equals(existing.OwnerUsername, username, StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        // 学生提交必填校验：成果名称、所属学院不能为空
        if (string.IsNullOrWhiteSpace(existing.Title))
        {
            return BadRequest("成果名称不能为空");
        }

        if (string.IsNullOrWhiteSpace(existing.Department))
        {
            return BadRequest("所属学院不能为空");
        }

        // 提交后自动生成项目编号（避免学生填写）
        if (string.IsNullOrWhiteSpace(existing.ProjectNumber))
        {
            existing.ProjectNumber = await GenerateProjectNumberAsync();
        }

        existing.Status = "待教师初审";
        _dbContext.AchievementAuditLogs.Add(new AchievementAuditLog
        {
            AchievementId = id,
            Action = "Submit",
            Reviewer = username,
            Comment = "学生提交审核"
        });
        await _dbContext.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:int}/review")]
    [Authorize(Roles = "Teacher,Admin")]
    public async Task<ActionResult> Review(int id, [FromBody] AchievementReviewRequest request)
    {
        var (username, role) = GetUserInfo();
        var existing = await _dbContext.Achievements.FirstOrDefaultAsync(x => x.Id == id);
        if (existing is null)
        {
            return NotFound();
        }

        var action = request.Action ?? string.Empty;
        var isAdmin = string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
        var isTeacher = string.Equals(role, "Teacher", StringComparison.OrdinalIgnoreCase);

        // 一旦成果已驳回，教师/admin 不能再继续处理（需由学生修改后重新提交）
        if (string.Equals(existing.Status, "已驳回", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("该成果已驳回，需学生修改后重新提交，当前账号不可继续处理。");
        }

        // 终审权限：仅管理员可做终审（Teacher 只能做 TeacherApprove）
        if (!isAdmin && string.Equals(action, "SchoolApprove", StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        if (isTeacher && string.Equals(action, "TeacherApprove", StringComparison.OrdinalIgnoreCase) && !string.Equals(existing.Status, "待教师初审", StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        if (isAdmin && string.Equals(action, "SchoolApprove", StringComparison.OrdinalIgnoreCase) && !string.Equals(existing.Status, "待学校终审", StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        existing.Status = action switch
        {
            "TeacherApprove" => "待学校终审",
            "SchoolApprove" => "已通过",
            "Reject" => "已驳回",
            _ => existing.Status
        };

        _dbContext.AchievementAuditLogs.Add(new AchievementAuditLog
        {
            AchievementId = id,
            Action = action,
            Reviewer = $"{username}({role})",
            Comment = request.Comment
        });

        await _dbContext.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// 数字荣誉墙仪表盘：全校「已通过」成果统计与明细（不限角色，与列表筛选无关）。
    /// </summary>
    [HttpGet("honor-wall/dashboard")]
    public async Task<ActionResult<HonorWallDashboardDto>> GetHonorWallDashboard()
    {
        var approved = await _dbContext.Achievements
            .AsNoTracking()
            .Where(x => x.Status == "已通过")
            .ToListAsync();

        var cy = DateTime.UtcNow.Year;
        static string D(string? v, string fallback) =>
            string.IsNullOrWhiteSpace(v) ? fallback : v.Trim();

        var byDept = approved
            .GroupBy(x => D(x.Department, "未填学院"))
            .Select(g => new HonorWallNameCountDto { Name = g.Key, Value = g.Count() })
            .OrderByDescending(x => x.Value)
            .ThenBy(x => x.Name)
            .ToList();

        var byLevel = approved
            .GroupBy(x => D(x.Level, "未分级"))
            .Select(g => new HonorWallNameCountDto { Name = g.Key, Value = g.Count() })
            .OrderByDescending(x => x.Value)
            .ThenBy(x => x.Name)
            .ToList();

        var byCategory = approved
            .GroupBy(x => D(x.Category, "未分类"))
            .Select(g => new HonorWallNameCountDto { Name = g.Key, Value = g.Count() })
            .OrderByDescending(x => x.Value)
            .ThenBy(x => x.Name)
            .ToList();

        var yearStart = cy - 4;
        var byYear = Enumerable.Range(yearStart, 5)
            .Select(year => new HonorWallYearCountDto { Year = year, Count = approved.Count(x => x.Year == year) })
            .ToList();

        var detailRows = approved
            .GroupBy(x => new { x.Department, x.Level, x.Category })
            .Select(g => new
            {
                g.Key.Department,
                g.Key.Level,
                g.Key.Category,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Department)
            .ThenBy(x => x.Level)
            .ThenBy(x => x.Category)
            .ToList();

        var detailLines = detailRows.Select(r =>
                $"{D(r.Department, "未填学院")} | {D(r.Level, "未分级")} · {D(r.Category, "未分类")} {r.Count} 项")
            .ToList();

        return Ok(new HonorWallDashboardDto
        {
            TotalApproved = approved.Count,
            CurrentYearApproved = approved.Count(x => x.Year == cy),
            ByDepartment = byDept,
            ByLevel = byLevel,
            ByCategory = byCategory,
            ByYearLast5 = byYear,
            DetailLines = detailLines
        });
    }

    [HttpGet("statistics")]
    public async Task<ActionResult<object>> GetStatistics([FromQuery] int? year)
    {
        var (username, role) = GetUserInfo();
        var q = _dbContext.Achievements.AsQueryable();
        if (string.Equals(role, "Student", StringComparison.OrdinalIgnoreCase))
        {
            q = q.Where(x => x.OwnerUsername == username);
        }

        if (year.HasValue)
        {
            q = q.Where(x => x.Year == year.Value);
        }

        var byCategory = await q.GroupBy(x => x.Category).Select(g => new { name = g.Key, value = g.Count() }).ToListAsync();
        var byLevel = await q.GroupBy(x => x.Level).Select(g => new { name = g.Key, value = g.Count() }).ToListAsync();
        var totalCredits = await q.SumAsync(x => x.CreditScore);
        return Ok(new { byCategory, byLevel, totalCredits });
    }

    [HttpGet("mine/count")]
    public async Task<ActionResult<int>> GetMineCount()
    {
        var (username, role) = GetUserInfo();
        var q = _dbContext.Achievements.AsQueryable();
        if (string.Equals(role, "Student", StringComparison.OrdinalIgnoreCase))
        {
            q = q.Where(x => x.OwnerUsername == username);
        }

        var count = await q.CountAsync();
        return Ok(count);
    }

    [HttpGet("export/csv")]
    [Authorize(Roles = "Teacher,Admin")]
    public async Task<IActionResult> ExportCsv()
    {
        var list = await _dbContext.Achievements.OrderByDescending(x => x.Id).ToListAsync();
        var sb = new StringBuilder();
        sb.AppendLine("Id,Title,Category,Level,Year,Department,StudentName,StudentId,Advisor,Status,CreditScore");
        foreach (var x in list)
        {
            sb.AppendLine($"{x.Id},\"{x.Title}\",{x.Category},{x.Level},{x.Year},\"{x.Department}\",\"{x.StudentName}\",{x.StudentId},\"{x.Advisor}\",{x.Status},{x.CreditScore}");
        }

        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"achievements-{DateTime.Now:yyyyMMddHHmmss}.csv");
    }

    [HttpPost("import/mock")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> ImportMock([FromBody] List<Achievement> items)
    {
        foreach (var item in items)
        {
            item.Id = 0;
            item.Status = string.IsNullOrWhiteSpace(item.Status) ? "草稿" : item.Status;
            item.CreditScore = CalculateCredit(item);
            if (string.IsNullOrWhiteSpace(item.OwnerUsername))
            {
                item.OwnerUsername = "admin";
            }
        }

        _dbContext.Achievements.AddRange(items);
        await _dbContext.SaveChangesAsync();
        return Ok(new { imported = items.Count });
    }

    private (string Username, string Role) GetUserInfo()
    {
        var username = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
        var role = User.FindFirstValue(ClaimTypes.Role) ?? "Student";
        return (username, role);
    }

    /// <summary>创新学分：0~100，步长 0.5。</summary>
    private static bool TryValidateCreditScore(decimal value, out string? error)
    {
        if (value < 0m || value > 100m)
        {
            error = "创新学分必须在0~100之间";
            return false;
        }

        var doubled = value * 2m;
        if (doubled != decimal.Round(doubled, 0, MidpointRounding.AwayFromZero))
        {
            error = "创新学分步长须为0.5";
            return false;
        }

        error = null;
        return true;
    }

    private static decimal CalculateCredit(Achievement achievement)
    {
        decimal baseScore = achievement.Level switch
        {
            "国家级" => 8m,
            "省部级" => 5m,
            _ => 2m
        };

        decimal categoryBonus = achievement.Category switch
        {
            "专利" => 2m,
            "论文" => 1.5m,
            "竞赛作品" => 1m,
            _ => 0m
        };

        return baseScore + categoryBonus;
    }

    private async Task<string> GenerateProjectNumberAsync()
    {
        var year = DateTime.UtcNow.Year;
        var count = await _dbContext.Achievements.CountAsync(x => x.Year == year);
        return $"ACH-{year}-{count + 1:000}";
    }
}

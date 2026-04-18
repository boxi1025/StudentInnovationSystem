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
[Route("api/achievements/{achievementId:int}/attachments")]
public class AttachmentController : ControllerBase
{
    private readonly StudentInnovationDbContext _dbContext;
    private readonly IFileStorageService _fileStorageService;

    public AttachmentController(StudentInnovationDbContext dbContext, IFileStorageService fileStorageService)
    {
        _dbContext = dbContext;
        _fileStorageService = fileStorageService;
    }

    [HttpPost]
    [RequestSizeLimit(50_000_000)]
    public async Task<ActionResult<AchievementAttachment>> Upload(int achievementId, IFormFile file, CancellationToken cancellationToken)
    {
        var achievement = await _dbContext.Achievements.FindAsync(achievementId);
        if (achievement is null)
        {
            return NotFound();
        }

        var url = await _fileStorageService.SaveAsync(file, cancellationToken);
        // 返回绝对地址，方便 WPF 直接用于 Image.Source
        var absoluteUrl = $"{Request.Scheme}://{Request.Host}{url}";
        var attachment = new AchievementAttachment
        {
            AchievementId = achievementId,
            FileName = file.FileName,
            FileType = file.ContentType,
            FileUrl = absoluteUrl
        };

        _dbContext.AchievementAttachments.Add(attachment);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Ok(attachment);
    }

    [HttpDelete("{attachmentId:int}")]
    public async Task<ActionResult> Delete(int achievementId, int attachmentId)
    {
        var username = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
        var role = User.FindFirstValue(ClaimTypes.Role) ?? "Student";

        var achievement = await _dbContext.Achievements.FirstOrDefaultAsync(a => a.Id == achievementId);
        if (achievement is null)
        {
            return NotFound();
        }

        // 权限：学生只能删自己成果的附件，教师/管理员可删所有
        if (string.Equals(role, "Student", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.Equals(achievement.OwnerUsername, username, StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }
        }

        var attachment = await _dbContext.AchievementAttachments.FirstOrDefaultAsync(x =>
            x.Id == attachmentId && x.AchievementId == achievementId);

        if (attachment is null)
        {
            return NotFound();
        }

        _dbContext.AchievementAttachments.Remove(attachment);
        await _dbContext.SaveChangesAsync();
        return NoContent();
    }
}

namespace StudentInnovation.Shared.Models;

public class AchievementAuditLog
{
    public int Id { get; set; }
    public int AchievementId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Reviewer { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

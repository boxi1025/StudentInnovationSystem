namespace StudentInnovation.Shared.Models;

public class Achievement
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Level { get; set; } = "校级";
    public int Year { get; set; } = DateTime.UtcNow.Year;
    public string Department { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public string Advisor { get; set; } = string.Empty;
    public string OwnerUsername { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public DateTime AchievedOn { get; set; } = DateTime.UtcNow.Date;
    public decimal CreditScore { get; set; }
    public string Status { get; set; } = "草稿";
    public string ExtraJson { get; set; } = "{}";
    public string Description { get; set; } = string.Empty;
    public string ProjectNumber { get; set; } = string.Empty;
    public List<AchievementAttachment> Attachments { get; set; } = new();
    public List<AchievementAuditLog> AuditLogs { get; set; } = new();
}

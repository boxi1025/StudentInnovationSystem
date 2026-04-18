namespace StudentInnovation.Shared.Models;

public class AchievementAttachment
{
    public int Id { get; set; }
    public int AchievementId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}

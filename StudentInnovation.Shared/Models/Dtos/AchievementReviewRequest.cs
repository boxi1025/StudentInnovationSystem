namespace StudentInnovation.Shared.Models.Dtos;

public class AchievementReviewRequest
{
    public string Action { get; set; } = "TeacherApprove";
    public string Comment { get; set; } = string.Empty;
}

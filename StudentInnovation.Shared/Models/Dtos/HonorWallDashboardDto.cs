namespace StudentInnovation.Shared.Models.Dtos;

public class HonorWallDashboardDto
{
    public int TotalApproved { get; set; }
    public int CurrentYearApproved { get; set; }
    public List<HonorWallNameCountDto> ByDepartment { get; set; } = new();
    public List<HonorWallNameCountDto> ByLevel { get; set; } = new();
    public List<HonorWallNameCountDto> ByCategory { get; set; } = new();
    public List<HonorWallYearCountDto> ByYearLast5 { get; set; } = new();
    public List<string> DetailLines { get; set; } = new();
}

public class HonorWallNameCountDto
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}

public class HonorWallYearCountDto
{
    public int Year { get; set; }
    public int Count { get; set; }
}

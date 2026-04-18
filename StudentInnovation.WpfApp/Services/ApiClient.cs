using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using StudentInnovation.Shared.Models;
using StudentInnovation.Shared.Models.Dtos;

namespace StudentInnovation.WpfApp.Services;

public class ApiClient
{
    private static readonly JsonSerializerOptions JsonReadOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _httpClient;

    public ApiClient(string baseUrl)
    {
        _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/login", request);
        if (!response.IsSuccessStatusCode)
        {
            return new LoginResponse { Success = false, Message = "登录失败，请检查用户名和密码" };
        }

        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return result;
    }

    public void SetBearerToken(string token)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public void ClearBearerToken()
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }

    public async Task<List<Achievement>> GetAchievementsAsync(AchievementQueryDto? query = null)
    {
        var queryString = "api/achievement";
        if (query is not null)
        {
            var items = new List<string>();
            if (!string.IsNullOrWhiteSpace(query.Department)) items.Add($"department={Uri.EscapeDataString(query.Department)}");
            if (query.Year.HasValue) items.Add($"year={query.Year.Value}");
            if (!string.IsNullOrWhiteSpace(query.Level)) items.Add($"level={Uri.EscapeDataString(query.Level)}");
            if (!string.IsNullOrWhiteSpace(query.Category)) items.Add($"category={Uri.EscapeDataString(query.Category)}");
            if (!string.IsNullOrWhiteSpace(query.Keyword)) items.Add($"keyword={Uri.EscapeDataString(query.Keyword)}");
            if (items.Count > 0)
            {
                queryString += "?" + string.Join("&", items);
            }
        }

        var result = await _httpClient.GetFromJsonAsync<List<Achievement>>(queryString);
        return result ?? new List<Achievement>();
    }

    public async Task<Achievement?> GetAchievementByIdAsync(int id)
    {
        return await _httpClient.GetFromJsonAsync<Achievement>($"api/achievement/{id}");
    }

    public async Task<Achievement?> CreateAchievementAsync(Achievement achievement)
    {
        var response = await _httpClient.PostAsJsonAsync("api/achievement", achievement);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var result = await response.Content.ReadFromJsonAsync<Achievement>();
        return result;
    }

    public async Task<bool> UpdateAchievementAsync(Achievement achievement)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/achievement/{achievement.Id}", achievement);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteAchievementAsync(int id)
    {
        var response = await _httpClient.DeleteAsync($"api/achievement/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> SubmitAchievementAsync(int id)
    {
        var response = await _httpClient.PostAsync($"api/achievement/{id}/submit", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ReviewAchievementAsync(int id, string action, string comment = "")
    {
        var response = await _httpClient.PostAsJsonAsync($"api/achievement/{id}/review",
            new AchievementReviewRequest { Action = action, Comment = comment });
        return response.IsSuccessStatusCode;
    }

    public async Task<HonorWallDashboardDto?> GetHonorWallDashboardAsync()
    {
        var response = await _httpClient.GetAsync("api/achievement/honor-wall/dashboard");
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<HonorWallDashboardDto>(JsonReadOptions);
    }

    public async Task<string> GetStatisticsTextAsync()
    {
        var response = await _httpClient.GetAsync("api/achievement/statistics");
        if (!response.IsSuccessStatusCode)
        {
            return "统计加载失败";
        }

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var totalCredits = doc.RootElement.GetProperty("totalCredits").GetDecimal();
        return $"当前查询总创新学分: {totalCredits:0.##}";
    }

    public async Task<bool> UploadAttachmentAsync(int achievementId, string filePath)
    {
        using var content = new MultipartFormDataContent();
        using var fileStream = File.OpenRead(filePath);
        var fileContent = new StreamContent(fileStream);

        // 根据文件扩展名设置正确的ContentType
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        var mimeType = extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };

        fileContent.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
        content.Add(fileContent, "file", Path.GetFileName(filePath));

        var response = await _httpClient.PostAsync($"api/achievements/{achievementId}/attachments", content);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteAttachmentAsync(int achievementId, int attachmentId)
    {
        var response = await _httpClient.DeleteAsync($"api/achievements/{achievementId}/attachments/{attachmentId}");
        return response.IsSuccessStatusCode;
    }

    public async Task<int> GetMyAchievementCountAsync()
    {
        var result = await _httpClient.GetFromJsonAsync<int>("api/achievement/mine/count");
        return result;
    }

    public async Task<bool> ChangePasswordAsync(ChangePasswordRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/user/change-password", request);
        return response.IsSuccessStatusCode;
    }

    // 用户管理相关方法
    public async Task<List<User>> GetUsersAsync()
    {
        var result = await _httpClient.GetFromJsonAsync<List<User>>("api/user");
        return result ?? new List<User>();
    }

    public async Task<bool> CreateUserAsync(UserCreateRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/user", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateUserAsync(int id, UserUpdateRequest request)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/user/{id}", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteUserAsync(int id)
    {
        var response = await _httpClient.DeleteAsync($"api/user/{id}");
        return response.IsSuccessStatusCode;
    }
}

// 用户管理请求类
public class UserCreateRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "Student";
    public string FullName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string StudentNo { get; set; } = string.Empty;
    public string EmployeeNo { get; set; } = string.Empty;
}

public class UserUpdateRequest
{
    public string? FullName { get; set; }
    public string? Department { get; set; }
    public string? StudentNo { get; set; }
    public string? EmployeeNo { get; set; }
    public string? Password { get; set; }
}

public class ChangePasswordRequest
{
    public string OldPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

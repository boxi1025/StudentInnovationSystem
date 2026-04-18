using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StudentInnovation.Shared.Models;
using StudentInnovation.WebApi.Data;
using StudentInnovation.WebApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();

builder.Services.AddDbContext<StudentInnovationDbContext>(options =>
{
    var conn = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(conn))
    {
        throw new InvalidOperationException("缺少 MySQL 连接字符串：ConnectionStrings:DefaultConnection");
    }

    // 避免 AutoDetect 在启动早期强制连库导致异常信息不直观。
    var serverVersion = new MySqlServerVersion(new Version(8, 0, 36));
    options.UseMySql(conn, serverVersion, mysql => mysql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(3), null));
});

builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecurityKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

static List<Achievement> BuildDemoAchievements(int y)
{
    var student1DemoRows = new (string Title, string Category, string Level, string Department, int YearOffset, decimal Credit)[]
    {
        ("智慧实验室物联调度系统", "科研项目", "国家级", "计算机学院", 0, 10m),
        ("挑战杯国赛三等奖", "竞赛作品", "国家级", "计算机学院", 0, 8m),
        ("互联网+大学生创新创业大赛金奖", "创业计划", "省部级", "计算机学院", 0, 7.5m),
        ("SCI 二区学术论文", "论文", "国家级", "计算机学院", -1, 9m),
        ("发明专利-工业边缘网关", "专利", "省部级", "计算机学院", -1, 6m),
        ("省级大学生创新项目", "科研项目", "省部级", "自动化学院", -1, 5.5m),
        ("机器人创意赛全国二等奖", "竞赛作品", "省部级", "自动化学院", -2, 6m),
        ("校级 SRTP 重点项目", "科研项目", "校级", "计算机学院", -2, 2m),
        ("经管案例分析大赛一等奖", "竞赛作品", "校级", "经管学院", -3, 3m),
        ("创业路演最佳商业计划", "创业计划", "省部级", "经管学院", -2, 5m),
        ("软件著作权登记", "专利", "校级", "计算机学院", -3, 2.5m),
        ("数学建模国赛省一等奖", "竞赛作品", "省部级", "计算机学院", -4, 5m),
        ("碳中和主题社会实践报告", "论文", "校级", "自动化学院", -4, 2m),
        ("ACM 校赛金牌", "竞赛作品", "校级", "计算机学院", 0, 1.5m)
    };

    var achievements = new List<Achievement>();
    var dayOffset = 0;
    foreach (var row in student1DemoRows)
    {
        achievements.Add(new Achievement
        {
            Title = row.Title,
            Category = row.Category,
            Level = row.Level,
            Year = y + row.YearOffset,
            Department = row.Department,
            StudentName = "张三",
            StudentId = "20220001",
            Advisor = "李老师",
            OwnerUsername = "student1",
            TeamName = "创新示范团队",
            AchievedOn = DateTime.UtcNow.Date.AddDays(-(dayOffset++ * 5 + 8)),
            Status = "已通过",
            CreditScore = row.Credit,
            Description = "荣誉墙可视化示例数据。",
            ExtraJson = "{}"
        });
    }

    achievements.Add(new Achievement
    {
        Title = "互联网+省赛银奖（审核中）",
        Category = "竞赛作品",
        Level = "省部级",
        Year = y,
        Department = "自动化学院",
        StudentName = "李四",
        StudentId = "20220002",
        Advisor = "王老师",
        OwnerUsername = "student2",
        TeamName = "智控创客团队",
        AchievedOn = DateTime.UtcNow.Date.AddDays(-12),
        Status = "待教师初审",
        CreditScore = 6m,
        Description = "基于边缘计算的工业设备预测性维护平台。",
        ExtraJson = "{}"
    });

    return achievements;
}

static void AddSeedUsers(StudentInnovationDbContext db, IPasswordHasher hasher)
{
    var seedUsers = new List<User>
    {
        new() { Username = "admin", PasswordHash = hasher.Hash("123456"), Role = "Admin", FullName = "系统管理员", Department = "教务处", EmployeeNo = "T0001" },
        new() { Username = "teacher1", PasswordHash = hasher.Hash("123456"), Role = "Teacher", FullName = "李老师", Department = "计算机学院", EmployeeNo = "T1024" },
        new() { Username = "student1", PasswordHash = hasher.Hash("123456"), Role = "Student", FullName = "张三", Department = "计算机学院", StudentNo = "20220001" },
        new() { Username = "student2", PasswordHash = hasher.Hash("123456"), Role = "Student", FullName = "李四", Department = "自动化学院", StudentNo = "20220002" }
    };

    db.Users.AddRange(seedUsers);
}

static void SeedSampleData(StudentInnovationDbContext db, IPasswordHasher hasher)
{
    AddSeedUsers(db, hasher);
    db.Achievements.AddRange(BuildDemoAchievements(DateTime.UtcNow.Year));
    db.SaveChanges();
}

static void InitializeDatabase(IServiceProvider services, IConfiguration configuration, IWebHostEnvironment env)
{
    using var scope = services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<StudentInnovationDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    var rebuildOnStartup = configuration.GetValue<bool>("Database:RebuildOnStartup", true);
    var replaceDemoAchievements = configuration.GetValue<bool>("Database:ReplaceDemoAchievementsOnStartup", false);

    Console.WriteLine($"[数据库初始化] 环境={env.EnvironmentName}, RebuildOnStartup={rebuildOnStartup}, ReplaceDemoAchievementsOnStartup={replaceDemoAchievements}");

    if (rebuildOnStartup)
    {
        Console.WriteLine("[数据库初始化] 已启用 RebuildOnStartup：删除并重建库表，写入完整种子数据。");
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();
        SeedSampleData(db, hasher);
        return;
    }

    db.Database.EnsureCreated();
    if (!HasRequiredSchema(db))
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("检测到旧版数据库结构，自动重建数据库并写入样例数据。");
        Console.ResetColor();
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();
        SeedSampleData(db, hasher);
        return;
    }

    if (replaceDemoAchievements)
    {
        if (!db.Users.Any())
        {
            AddSeedUsers(db, hasher);
            db.SaveChanges();
        }

        var removed = db.Achievements.ExecuteDelete();
        db.Achievements.AddRange(BuildDemoAchievements(DateTime.UtcNow.Year));
        db.SaveChanges();
        Console.WriteLine($"[数据库初始化] 已按 ReplaceDemoAchievementsOnStartup 替换演示成果（删除 {removed} 条后重新写入）。");
        return;
    }

    if (!db.Users.Any() && !db.Achievements.Any())
    {
        Console.WriteLine("[数据库初始化] 空库：写入完整种子数据。");
        SeedSampleData(db, hasher);
        return;
    }

    if (db.Users.Any() && !db.Achievements.Any())
    {
        Console.WriteLine("[数据库初始化] 仅有账户、无成果：补写演示成果。");
        db.Achievements.AddRange(BuildDemoAchievements(DateTime.UtcNow.Year));
        db.SaveChanges();
        return;
    }

    Console.WriteLine("[数据库初始化] 检测到已有用户与成果，跳过种子。若需最新示例数据：① Development 环境默认会合并 appsettings.Development.json（RebuildOnStartup=true）；② 或将主配置中 Database:RebuildOnStartup 设为 true 后重启；③ 或临时将 Database:ReplaceDemoAchievementsOnStartup 设为 true 后重启一次再改回 false。");
}

static bool HasRequiredSchema(StudentInnovationDbContext db)
{
    return ColumnExists(db, "Users", "Department")
           && ColumnExists(db, "Users", "EmployeeNo")
           && ColumnExists(db, "Users", "StudentNo")
           && ColumnExists(db, "Achievements", "Level")
           && ColumnExists(db, "Achievements", "OwnerUsername")
           && ColumnExists(db, "Achievements", "CreditScore")
           && ColumnExists(db, "Achievements", "Status");
}

static bool ColumnExists(StudentInnovationDbContext db, string tableName, string columnName)
{
    var connection = db.Database.GetDbConnection();
    var openedHere = false;
    if (connection.State != System.Data.ConnectionState.Open)
    {
        connection.Open();
        openedHere = true;
    }

    try
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"""
                               SELECT COUNT(*)
                               FROM information_schema.columns
                               WHERE table_schema = DATABASE()
                                 AND table_name = '{tableName}'
                                 AND column_name = '{columnName}';
                               """;
        var count = Convert.ToInt32(command.ExecuteScalar());
        return count > 0;
    }
    finally
    {
        if (openedHere && connection.State == System.Data.ConnectionState.Open)
        {
            connection.Close();
        }
    }
}

try
{
    InitializeDatabase(app.Services, app.Configuration, app.Environment);
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("MySQL 初始化失败，请检查：MySQL 服务是否启动、连接串主机端口、账号密码、数据库权限。");
    Console.WriteLine($"详细错误：{ex.Message}");
    Console.ResetColor();
    throw;
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var uploadsRoot = Path.Combine(app.Environment.ContentRootPath, "uploads");
Directory.CreateDirectory(uploadsRoot);

app.UseStaticFiles(new StaticFileOptions
{
    RequestPath = "/files",
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
        uploadsRoot)
});
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

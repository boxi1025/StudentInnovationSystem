using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using OxyPlot;
using StudentInnovation.Shared.Models;
using StudentInnovation.Shared.Models.Dtos;
using StudentInnovation.WpfApp.Commands;
using StudentInnovation.WpfApp.Services;
using StudentInnovation.WpfApp.Views;

namespace StudentInnovation.WpfApp.ViewModels;

public class MainViewModel : ObservableObject
{
    private static readonly IReadOnlyList<decimal> CreditScoreOptionsSource =
        Enumerable.Range(0, 201).Select(i => i * 0.5m).ToList();

    private readonly ApiClient _apiClient;
    private readonly string _role;
    private Achievement? _selectedAchievement;
    private bool _isFormOpen;
    private GridLength _formColumnWidth = new GridLength(0);
    private string _title = string.Empty;
    private string _category = "科研项目";
    private string _studentName = string.Empty;
    private string _studentId = string.Empty;
    private string _advisor = string.Empty;
    private DateTime _achievedOn = DateTime.Today;
    private string _description = string.Empty;
    private string _currentUser = string.Empty;
    private string _level = "校级";
    private int _year = DateTime.Today.Year;
    private string _department = "计算机学院";
    private string _teamName = string.Empty;
    private string _status = "草稿";
    private string _statisticsText = "统计加载中...";
    private string _filterDepartment = string.Empty;
    private string _filterLevel = string.Empty;
    private string _filterCategory = string.Empty;
    private string _filterKeyword = string.Empty;
    private string _projectCode = string.Empty;
    private string _awardLevel = string.Empty;
    private string _patentNo = string.Empty;
    private string _journal = string.Empty;
    private string _startupName = string.Empty;
    private int _achievementsCount;
    private DispatcherTimer _filterTimer;
    private string _accountManagementHeader = string.Empty;
    private ObservableCollection<User> _users = new();
    private User? _selectedUser;
    private string _currentUserFullName = string.Empty;
    private string _currentUserDepartment = string.Empty;
    private string _currentUserRoleId = string.Empty;
    private string _rejectReason = string.Empty;
    private decimal _selectedCreditScore;
    private int _honorWallTotalApproved;
    private int _honorWallCurrentYearApproved;
    private PlotModel? _honorDepartmentPieModel;
    private PlotModel? _honorCategoryPieModel;
    private PlotModel? _honorLevelBarModel;
    private PlotModel? _honorYearBarModel;

    private readonly ObservableCollection<AchievementAttachment> _imageAttachments = new();

    public MainViewModel(ApiClient apiClient, LoginResponse loginResponse)
    {
        _apiClient = apiClient;
        _role = loginResponse.Role;

        // 设置个人信息
        CurrentUserFullName = loginResponse.FullName;
        CurrentUserDepartment = loginResponse.Department;
        if (loginResponse.Role == "Student")
        {
            CurrentUserRoleId = loginResponse.StudentNo;
        }
        else if (loginResponse.Role == "Teacher")
        {
            CurrentUserRoleId = loginResponse.EmployeeNo;
        }
        else if (loginResponse.Role == "Admin")
        {
            CurrentUserRoleId = loginResponse.EmployeeNo;
        }

        var headerDisplayName = string.IsNullOrWhiteSpace(loginResponse.FullName)
            ? loginResponse.Username
            : loginResponse.FullName.Trim();
        _currentUser = $"{headerDisplayName}（{FormatRoleForDisplay(loginResponse.Role)}）";
        WindowChromeTitle = $"学生创新成果管理 — {_currentUser}";

        // 自动填充学生信息（如果当前用户是学生）
        if (loginResponse.Role == "Student" && !string.IsNullOrEmpty(loginResponse.FullName))
        {
            StudentName = loginResponse.FullName;
            StudentId = loginResponse.StudentNo;
        }

        // 设置账户管理/个人信息标题
        _accountManagementHeader = loginResponse.Role == "Admin" ? "账户管理" : "个人信息";
        Achievements = new ObservableCollection<Achievement>();
        HonorWallItems = new ObservableCollection<string> { "荣誉墙数据加载中…" };
        ResetHonorWallCharts();
        GalleryItems = new ObservableCollection<string>
        {
            "智慧实验室系统 - 视频路演",
            "工业设备预测性维护平台 - 作品图集",
            "AI 学习助手创业计划 - 商业计划书"
        };

        LoadCommand = new RelayCommand(async _ => await LoadAsync());
        // “保存”与上传解耦：无论新建(Id=0)还是已创建草稿(Id>0)都允许点击保存（受学生锁定态约束）
        AddCommand = new RelayCommand(async _ => await AddAsync(), _ => SelectedAchievement is not null && IsCurrentFormEditable);
        UpdateCommand = new RelayCommand(async _ => await UpdateAsync(), _ => SelectedAchievement is not null && SelectedAchievement.Id > 0 && IsCurrentFormEditable);
        DeleteCommand = new RelayCommand(async _ => await DeleteAsync(), _ => SelectedAchievement is not null && SelectedAchievement.Id > 0 && IsCurrentFormEditable);
        SubmitCommand = new RelayCommand(async _ => await SubmitAsync(), _ => SelectedAchievement is not null && (SelectedAchievement.Id > 0 || IsStudent) && IsCurrentFormEditable);
        TeacherApproveCommand = new RelayCommand(async _ => await ReviewAsync("TeacherApprove"), _ => SelectedAchievement is not null && SelectedAchievement.Id > 0 && CanTeacherApprove && !IsRejectedSelected);
        SchoolApproveCommand = new RelayCommand(async _ => await ReviewAsync("SchoolApprove"), _ => SelectedAchievement is not null && SelectedAchievement.Id > 0 && CanSchoolApproveCurrent);
        RejectCommand = new RelayCommand(async _ => await ReviewAsync("Reject"), _ => SelectedAchievement is not null && SelectedAchievement.Id > 0 && CanReview && !IsRejectedSelected);
        ClearCommand = new RelayCommand(_ => ClearForm());
        // 学生端：允许在添加项目(Id=0)时先上传图片（会静默创建成果生成 Id）
        UploadAttachmentCommand = new RelayCommand(async _ => await UploadAttachmentAsync(), _ =>
            SelectedAchievement is not null && (SelectedAchievement.Id > 0 || IsStudent) && IsCurrentFormEditable);

        // 用户管理命令初始化
        AddTeacherCommand = new RelayCommand(async _ => await AddTeacherAsync());
        AddStudentCommand = new RelayCommand(async _ => await AddStudentAsync());
        UpdateUserCommand = new RelayCommand(async _ => await UpdateUserAsync(), _ => SelectedUser is not null);
        DeleteUserCommand = new RelayCommand(async _ => await DeleteUserAsync(), _ => SelectedUser is not null);
        UpdatePersonalInfoCommand = new RelayCommand(async _ => await UpdatePersonalInfoAsync());
        LoadUsersCommand = new RelayCommand(async _ => await LoadUsersAsync());
        LogoutCommand = new RelayCommand(_ => Logout());
        AddProjectCommand = new RelayCommand(_ => OpenAddForm());
        DeleteAttachmentCommand = new RelayCommand(async p => await DeleteAttachmentAsync(p as AchievementAttachment));
        ChangePasswordCommand = new RelayCommand(async _ => await OpenChangePasswordAsync());

        _filterTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _filterTimer.Tick += async (s, e) =>
        {
            _filterTimer.Stop();
            await LoadAsync();
        };
    }

    public ObservableCollection<Achievement> Achievements { get; }
    public ObservableCollection<AchievementAttachment> ImageAttachments => _imageAttachments;
    public ObservableCollection<string> HonorWallItems { get; }
    public int HonorWallTotalApproved { get => _honorWallTotalApproved; set => SetProperty(ref _honorWallTotalApproved, value); }
    public int HonorWallCurrentYearApproved { get => _honorWallCurrentYearApproved; set => SetProperty(ref _honorWallCurrentYearApproved, value); }
    public PlotModel? HonorDepartmentPieModel { get => _honorDepartmentPieModel; set => SetProperty(ref _honorDepartmentPieModel, value); }
    public PlotModel? HonorCategoryPieModel { get => _honorCategoryPieModel; set => SetProperty(ref _honorCategoryPieModel, value); }
    public PlotModel? HonorLevelBarModel { get => _honorLevelBarModel; set => SetProperty(ref _honorLevelBarModel, value); }
    public PlotModel? HonorYearBarModel { get => _honorYearBarModel; set => SetProperty(ref _honorYearBarModel, value); }
    public ObservableCollection<string> GalleryItems { get; }
    public RelayCommand LoadCommand { get; }
    public RelayCommand AddCommand { get; }
    public RelayCommand UpdateCommand { get; }
    public RelayCommand DeleteCommand { get; }
    public RelayCommand SubmitCommand { get; }
    public RelayCommand TeacherApproveCommand { get; }
    public RelayCommand SchoolApproveCommand { get; }
    public RelayCommand RejectCommand { get; }
    public RelayCommand ClearCommand { get; }
    public RelayCommand UploadAttachmentCommand { get; }
    public RelayCommand AddProjectCommand { get; }
    public RelayCommand DeleteAttachmentCommand { get; }
    public RelayCommand AddTeacherCommand { get; }
    public RelayCommand AddStudentCommand { get; }
    public RelayCommand UpdateUserCommand { get; }
    public RelayCommand DeleteUserCommand { get; }
    public RelayCommand UpdatePersonalInfoCommand { get; }
    public RelayCommand LoadUsersCommand { get; }
    public RelayCommand LogoutCommand { get; }
    public RelayCommand ChangePasswordCommand { get; }
    public bool CanReview => _role is "Teacher" or "Admin";
    public bool CanTeacherApprove => _role == "Teacher";
    public bool CanSchoolApprove => _role == "Admin";
    public bool CanSchoolApproveCurrent =>
        _role == "Admin" &&
        SelectedAchievement is not null &&
        string.Equals(SelectedAchievement.Status, "待学校终审", StringComparison.OrdinalIgnoreCase);
    public bool IsAdmin => _role == "Admin";
    public bool IsTeacher => _role == "Teacher";
    public bool IsStudent => _role == "Student";
    public bool IsStudentLockedStatus =>
        IsStudent &&
        (string.Equals(SelectedAchievement?.Status, "待学校终审", StringComparison.OrdinalIgnoreCase)
         || string.Equals(SelectedAchievement?.Status, "已通过", StringComparison.OrdinalIgnoreCase));
    public bool IsCurrentFormEditable => !IsStudentLockedStatus;
    public bool IsRejectedSelected => string.Equals(SelectedAchievement?.Status, "已驳回", StringComparison.OrdinalIgnoreCase);
    public bool CanEditStudentIdentity => !IsStudent;
    public Visibility RejectedHintVisibility => (IsStudent && IsRejectedSelected) ? Visibility.Visible : Visibility.Collapsed;
    public Visibility AddButtonVisibility => IsTeacher ? Visibility.Collapsed : Visibility.Visible;
    public Visibility SubmitButtonVisibility => IsTeacher ? Visibility.Collapsed : Visibility.Visible;
    public Visibility UploadButtonVisibility => IsTeacher ? Visibility.Collapsed : Visibility.Visible;
    public Visibility ClearButtonVisibility => IsTeacher ? Visibility.Collapsed : Visibility.Visible;
    public Visibility UpdateButtonVisibility => (IsStudent || IsTeacher) ? Visibility.Collapsed : Visibility.Visible;
    public Visibility DeleteButtonVisibility
    {
        get
        {
            if (IsTeacher)
            {
                return Visibility.Collapsed;
            }

            if (!IsStudent)
            {
                return Visibility.Visible;
            }

            return string.Equals(SelectedAchievement?.Status, "草稿", StringComparison.OrdinalIgnoreCase)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }

    public string AccountManagementHeader => _accountManagementHeader;
    public bool IsFormOpen
    {
        get => _isFormOpen;
        set
        {
            if (SetProperty(ref _isFormOpen, value))
            {
                // 表单关闭时右侧列宽为 0，避免右侧空白占位；打开时展开以挤压左侧展示控件。
                FormColumnWidth = value ? new GridLength(520) : new GridLength(0);
            }
        }
    }
    public GridLength FormColumnWidth
    {
        get => _formColumnWidth;
        private set => SetProperty(ref _formColumnWidth, value);
    }
    public string CurrentUser { get => _currentUser; set => SetProperty(ref _currentUser, value); }
    /// <summary>窗口标题栏展示：系统名 + 当前用户（姓名与角色）。</summary>
    public string WindowChromeTitle { get; }
    public string StatisticsText { get => _statisticsText; set => SetProperty(ref _statisticsText, value); }
    public int AchievementsCount { get => _achievementsCount; set => SetProperty(ref _achievementsCount, value); }
    public string Title { get => _title; set => SetProperty(ref _title, value); }
    public string Category { get => _category; set => SetProperty(ref _category, value); }
    public string StudentName { get => _studentName; set => SetProperty(ref _studentName, value); }
    public string StudentId { get => _studentId; set => SetProperty(ref _studentId, value); }
    public string Advisor { get => _advisor; set => SetProperty(ref _advisor, value); }
    public DateTime AchievedOn { get => _achievedOn; set => SetProperty(ref _achievedOn, value); }
    public string Description { get => _description; set => SetProperty(ref _description, value); }
    public string Level { get => _level; set => SetProperty(ref _level, value); }
    public int Year { get => _year; set => SetProperty(ref _year, value); }
    public string Department { get => _department; set => SetProperty(ref _department, value); }
    public string TeamName { get => _teamName; set => SetProperty(ref _teamName, value); }
    public string Status { get => _status; set => SetProperty(ref _status, value); }
    public string FilterDepartment { get => _filterDepartment; set { if (SetProperty(ref _filterDepartment, value)) ScheduleFilterRefresh(); } }
    public string FilterLevel { get => _filterLevel; set { if (SetProperty(ref _filterLevel, value)) ScheduleFilterRefresh(); } }
    public string FilterCategory { get => _filterCategory; set { if (SetProperty(ref _filterCategory, value)) ScheduleFilterRefresh(); } }
    public string FilterKeyword { get => _filterKeyword; set { if (SetProperty(ref _filterKeyword, value)) ScheduleFilterRefresh(); } }
    public string ProjectCode { get => _projectCode; set => SetProperty(ref _projectCode, value); }
    public string AwardLevel { get => _awardLevel; set => SetProperty(ref _awardLevel, value); }
    public string PatentNo { get => _patentNo; set => SetProperty(ref _patentNo, value); }
    public string Journal { get => _journal; set => SetProperty(ref _journal, value); }
    public string StartupName { get => _startupName; set => SetProperty(ref _startupName, value); }

    // 用户管理属性
    public ObservableCollection<User> Users { get => _users; set => SetProperty(ref _users, value); }
    public User? SelectedUser { get => _selectedUser; set => SetProperty(ref _selectedUser, value); }
    public string CurrentUserFullName { get => _currentUserFullName; set => SetProperty(ref _currentUserFullName, value); }
    public string CurrentUserDepartment { get => _currentUserDepartment; set => SetProperty(ref _currentUserDepartment, value); }
    public string CurrentUserRoleId { get => _currentUserRoleId; set => SetProperty(ref _currentUserRoleId, value); }
    public string CurrentUserRoleIdLabel => _role == "Student" ? "学号:" : "工号:";
    public string RejectReason { get => _rejectReason; set => SetProperty(ref _rejectReason, value); }
    /// <summary>创新学分下拉项：0 ~ 100，步长 0.5。</summary>
    public IReadOnlyList<decimal> CreditScoreOptions => CreditScoreOptionsSource;
    /// <summary>当前选中的创新学分（对应 <see cref="CreditScoreOptions"/> 中的一项）。</summary>
    public decimal SelectedCreditScore
    {
        get => _selectedCreditScore;
        set => SetProperty(ref _selectedCreditScore, value);
    }
    public Visibility RejectReasonVisibility =>
        (IsStudent && string.Equals(SelectedAchievement?.Status, "已驳回", StringComparison.OrdinalIgnoreCase))
            ? Visibility.Visible
            : Visibility.Collapsed;

    public Achievement? SelectedAchievement
    {
        get => _selectedAchievement;
        set
        {
            if (!SetProperty(ref _selectedAchievement, value)) return;

            if (value is not null)
            {
                Title = value.Title;
                Category = value.Category;
                StudentName = value.StudentName;
                StudentId = value.StudentId;
                Advisor = value.Advisor;
                AchievedOn = value.AchievedOn;
                Description = value.Description;
                Level = value.Level;
                Department = value.Department;
                Year = value.Year;
                TeamName = value.TeamName;
                Status = value.Status;
                SelectedCreditScore = SnapCreditToHalfStepGrid(value.CreditScore);
                IsFormOpen = true;
                RejectReason = GetLatestRejectReason(value);
            }
            else
            {
                IsFormOpen = false;
                RejectReason = string.Empty;
            }

            RefreshImageAttachments(value);
            AddCommand.RaiseCanExecuteChanged();
            UpdateCommand.RaiseCanExecuteChanged();
            DeleteCommand.RaiseCanExecuteChanged();
            SubmitCommand.RaiseCanExecuteChanged();
            TeacherApproveCommand.RaiseCanExecuteChanged();
            SchoolApproveCommand.RaiseCanExecuteChanged();
            RejectCommand.RaiseCanExecuteChanged();
            UploadAttachmentCommand.RaiseCanExecuteChanged();
            DeleteAttachmentCommand.RaiseCanExecuteChanged();
            OnPropertyChanged(nameof(AddButtonVisibility));
            OnPropertyChanged(nameof(SubmitButtonVisibility));
            OnPropertyChanged(nameof(UploadButtonVisibility));
            OnPropertyChanged(nameof(ClearButtonVisibility));
            OnPropertyChanged(nameof(IsStudentLockedStatus));
            OnPropertyChanged(nameof(IsCurrentFormEditable));
            OnPropertyChanged(nameof(UpdateButtonVisibility));
            OnPropertyChanged(nameof(DeleteButtonVisibility));
            OnPropertyChanged(nameof(RejectReasonVisibility));
            OnPropertyChanged(nameof(IsRejectedSelected));
            OnPropertyChanged(nameof(RejectedHintVisibility));
        }
    }

    public async Task LoadAsync()
    {
        try
        {
            var selectedId = SelectedAchievement?.Id ?? 0;
            Achievements.Clear();
            var list = await _apiClient.GetAchievementsAsync(new AchievementQueryDto
            {
                Department = string.IsNullOrWhiteSpace(FilterDepartment) ? null : FilterDepartment,
                Level = string.IsNullOrWhiteSpace(FilterLevel) ? null : FilterLevel,
                Category = string.IsNullOrWhiteSpace(FilterCategory) ? null : FilterCategory,
                Keyword = string.IsNullOrWhiteSpace(FilterKeyword) ? null : FilterKeyword,
                // 搜索约束暂不默认按年度过滤，避免“只能看到当前年份”的误导体验
                Year = null
            });
            foreach (var item in list) Achievements.Add(item);
            StatisticsText = await _apiClient.GetStatisticsTextAsync();

            if (IsStudent)
            {
                try
                {
                    AchievementsCount = await _apiClient.GetMyAchievementCountAsync();
                }
                catch
                {
                    AchievementsCount = Achievements.Count;
                }
            }

            // 保持当前选中项，避免上传/刷新后表单关闭
            if (selectedId > 0)
            {
                SelectedAchievement = Achievements.FirstOrDefault(x => x.Id == selectedId);
            }
        }
        catch
        {
            MessageBox.Show("加载成果失败，请确认 API 与数据库已正常启动。");
        }
        finally
        {
            await RefreshHonorWallAsync();
        }
    }

    private void ResetHonorWallCharts()
    {
        var cy = DateTime.UtcNow.Year;
        var emptyYears = Enumerable.Range(cy - 4, 5)
            .Select(y => new HonorWallYearCountDto { Year = y, Count = 0 })
            .ToList();
        HonorWallTotalApproved = 0;
        HonorWallCurrentYearApproved = 0;
        HonorDepartmentPieModel = HonorWallChartBuilder.DepartmentPie(Array.Empty<HonorWallNameCountDto>());
        HonorCategoryPieModel = HonorWallChartBuilder.CategoryPie(Array.Empty<HonorWallNameCountDto>());
        HonorLevelBarModel = HonorWallChartBuilder.LevelColumns(Array.Empty<HonorWallNameCountDto>());
        HonorYearBarModel = HonorWallChartBuilder.YearColumns(emptyYears);
    }

    private async Task RefreshHonorWallAsync()
    {
        try
        {
            var dash = await _apiClient.GetHonorWallDashboardAsync();
            HonorWallItems.Clear();
            if (dash is null)
            {
                HonorWallItems.Add("荣誉墙数据加载失败，请检查网络或 API。");
                ResetHonorWallCharts();
                return;
            }

            HonorWallTotalApproved = dash.TotalApproved;
            HonorWallCurrentYearApproved = dash.CurrentYearApproved;
            HonorDepartmentPieModel = HonorWallChartBuilder.DepartmentPie(dash.ByDepartment);
            HonorCategoryPieModel = HonorWallChartBuilder.CategoryPie(dash.ByCategory);
            HonorLevelBarModel = HonorWallChartBuilder.LevelColumns(dash.ByLevel);
            HonorYearBarModel = HonorWallChartBuilder.YearColumns(dash.ByYearLast5);

            if (dash.DetailLines.Count == 0)
            {
                HonorWallItems.Add("暂无「已通过」成果：数据库中尚无终审通过记录，荣誉墙将在有数据后自动展示。");
                return;
            }

            foreach (var line in dash.DetailLines)
            {
                HonorWallItems.Add(line);
            }
        }
        catch
        {
            HonorWallItems.Clear();
            HonorWallItems.Add("荣誉墙数据加载失败。");
            ResetHonorWallCharts();
        }
    }

    private async Task AddAsync()
    {
        if (SelectedAchievement is null)
        {
            return;
        }

        // 新建成果
        if (SelectedAchievement.Id <= 0)
        {
            var created = await _apiClient.CreateAchievementAsync(BuildModel(0, SelectedCreditScore));
            if (created is null)
            {
                MessageBox.Show("保存失败，请检查网络/API。");
                return;
            }

            ReplaceAchievementInList(created);
            SelectedAchievement = created;
            // 保持表单打开，便于继续上传图片或提交
            return;
        }

        // 已创建草稿/项目的保存：按更新处理
        var ok = await _apiClient.UpdateAchievementAsync(BuildModel(SelectedAchievement.Id, SelectedCreditScore));
        if (!ok)
        {
            MessageBox.Show("保存失败，请检查网络/API。");
            return;
        }

        var refreshed = await _apiClient.GetAchievementByIdAsync(SelectedAchievement.Id);
        if (refreshed is not null)
        {
            ReplaceAchievementInList(refreshed);
            SelectedAchievement = refreshed;
            Status = refreshed.Status;
            RefreshImageAttachments(refreshed);
        }
    }

    private async Task UpdateAsync()
    {
        if (SelectedAchievement is null) return;
        if (await _apiClient.UpdateAchievementAsync(BuildModel(SelectedAchievement.Id, SelectedCreditScore)))
        {
            await LoadAsync();
        }
    }

    private async Task DeleteAsync()
    {
        if (SelectedAchievement is null) return;
        if (await _apiClient.DeleteAchievementAsync(SelectedAchievement.Id))
        {
            await LoadAsync();
            ClearForm();
        }
    }

    private async Task SubmitAsync()
    {
        if (SelectedAchievement is null) return;
        if (IsStudent)
        {
            if (string.IsNullOrWhiteSpace(Title))
            {
                MessageBox.Show("成果名称不能为空");
                return;
            }

            if (string.IsNullOrWhiteSpace(Department))
            {
                MessageBox.Show("所属学院不能为空");
                return;
            }
        }

        // 学生新增时允许“直接提交”：先创建成果拿到 Id，再提交审核
        if (IsStudent && SelectedAchievement.Id <= 0)
        {
            var created = await _apiClient.CreateAchievementAsync(BuildModel(0, SelectedCreditScore));
            if (created is null)
            {
                MessageBox.Show("提交失败：未能创建成果（请检查必填项）。");
                return;
            }

            SelectedAchievement = created;
        }
        else if (IsStudent && SelectedAchievement.Id > 0)
        {
            // 学生在“已驳回”状态可修改后重新提交，先保存修改内容再提交
            var updated = await _apiClient.UpdateAchievementAsync(BuildModel(SelectedAchievement.Id, SelectedCreditScore));
            if (!updated)
            {
                MessageBox.Show("提交失败：保存修改内容失败。");
                return;
            }
        }

        if (await _apiClient.SubmitAchievementAsync(SelectedAchievement.Id))
        {
            // 只刷新当前成果详情，避免重新加载列表导致表单重弹
            var refreshed = await _apiClient.GetAchievementByIdAsync(SelectedAchievement.Id);
            if (refreshed is not null)
            {
                // 替换列表中的同一项，确保 DataGrid 状态列立即刷新
                ReplaceAchievementInList(refreshed);
                SelectedAchievement = refreshed;
                Status = refreshed.Status;
                RejectReason = GetLatestRejectReason(refreshed);
                RefreshImageAttachments(refreshed);
                OnPropertyChanged(nameof(IsRejectedSelected));
                OnPropertyChanged(nameof(RejectReasonVisibility));
            }
        }
    }

    private async Task ReviewAsync(string action)
    {
        if (SelectedAchievement is null) return;
        if (string.Equals(action, "SchoolApprove", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(SelectedAchievement.Status, "待学校终审", StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show("终审通过仅可用于“待学校终审”状态的成果。");
            return;
        }

        if (IsRejectedSelected)
        {
            MessageBox.Show("该成果已驳回，需学生修改后重新提交。");
            return;
        }
        string comment = $"{CurrentUser} 执行 {action}";
        if (string.Equals(action, "Reject", StringComparison.OrdinalIgnoreCase))
        {
            var dialog = new RejectReasonDialog();
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            var reason = dialog.ReasonText?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(reason))
            {
                MessageBox.Show("请输入驳回理由");
                return;
            }

            comment = reason;
        }

        if (await _apiClient.ReviewAchievementAsync(SelectedAchievement.Id, action, comment))
        {
            await LoadAsync();
        }
        else
        {
            MessageBox.Show("操作失败：请检查当前成果状态是否符合审核流程。");
        }
    }

    private Achievement BuildModel(int id, decimal creditScore)
    {
        return new Achievement
        {
            Id = id,
            Title = Title.Trim(),
            Category = Category.Trim(),
            StudentName = (IsStudent ? CurrentUserFullName : StudentName).Trim(),
            StudentId = (IsStudent ? CurrentUserRoleId : StudentId).Trim(),
            Advisor = Advisor.Trim(),
            Level = Level,
            Year = Year,
            Department = Department.Trim(),
            TeamName = TeamName.Trim(),
            Status = Status,
            AchievedOn = AchievedOn,
            ExtraJson = BuildExtraJson(),
            Description = Description.Trim(),
            CreditScore = decimal.Round(creditScore, 1, MidpointRounding.AwayFromZero)
        };
    }

    /// <summary>将任意历史学分约束到 0~100 且为 0.5 步长，与下拉项一致。</summary>
    private static string FormatRoleForDisplay(string role) =>
        role switch
        {
            "Student" => "学生",
            "Teacher" => "教师",
            "Admin" => "管理员",
            _ => string.IsNullOrWhiteSpace(role) ? "用户" : role
        };

    private static decimal SnapCreditToHalfStepGrid(decimal value)
    {
        if (value < 0m)
        {
            return 0m;
        }

        if (value > 100m)
        {
            return 100m;
        }

        return decimal.Round(value * 2m, 0, MidpointRounding.AwayFromZero) / 2m;
    }

    private string BuildExtraJson()
    {
        var data = Category switch
        {
            "科研项目" => new Dictionary<string, string> { ["projectCode"] = ProjectCode },
            "竞赛作品" => new Dictionary<string, string> { ["awardLevel"] = AwardLevel },
            "专利" => new Dictionary<string, string> { ["patentNo"] = PatentNo },
            "论文" => new Dictionary<string, string> { ["journal"] = Journal },
            "创业计划" => new Dictionary<string, string> { ["startupName"] = StartupName },
            _ => new Dictionary<string, string>()
        };
        return JsonSerializer.Serialize(data);
    }

    private async Task UploadAttachmentAsync()
    {
        if (SelectedAchievement is null)
        {
            MessageBox.Show("请先选择一个成果");
            return;
        }

        if (SelectedAchievement.Id <= 0)
        {
            if (IsStudent)
            {
                // 学生：允许在“添加项目”(Id=0)时上传图片，会先静默创建成果生成真实 Id
                var created = await _apiClient.CreateAchievementAsync(BuildModel(0, SelectedCreditScore));
                if (created is null)
                {
                    MessageBox.Show("上传失败：未能创建成果。");
                    return;
                }

                SelectedAchievement = created;
            }
            else
            {
                MessageBox.Show("请先选择一个已保存的成果后再上传图片。");
                return;
            }
        }

        var openFileDialog = new OpenFileDialog
        {
            Filter = "图片文件 (*.jpg;*.jpeg;*.png;*.gif;*.bmp)|*.jpg;*.jpeg;*.png;*.gif;*.bmp|所有文件 (*.*)|*.*",
            Title = "选择证明图片"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            try
            {
                var success = await _apiClient.UploadAttachmentAsync(SelectedAchievement.Id, openFileDialog.FileName);
                if (success)
                {
                    MessageBox.Show("图片上传成功");

                    // 只刷新当前成果附件，避免重新加载列表导致表单“重弹出”
                    var refreshed = await _apiClient.GetAchievementByIdAsync(SelectedAchievement.Id);
                    if (refreshed is not null)
                    {
                        SelectedAchievement.Attachments = refreshed.Attachments ?? new List<AchievementAttachment>();
                        SelectedAchievement.AuditLogs = refreshed.AuditLogs ?? new List<AchievementAuditLog>();
                        RefreshImageAttachments(SelectedAchievement);
                    }
                }
                else
                {
                    MessageBox.Show("图片上传失败");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"上传出错: {ex.Message}");
            }
        }
    }

    private void ClearForm()
    {
        Title = string.Empty;
        Category = "科研项目";
        StudentName = string.Empty;
        StudentId = string.Empty;
        Advisor = string.Empty;
        TeamName = string.Empty;
        Level = "校级";
        Status = "草稿";
        AchievedOn = DateTime.Today;
        Description = string.Empty;
        ProjectCode = string.Empty;
        AwardLevel = string.Empty;
        PatentNo = string.Empty;
        Journal = string.Empty;
        StartupName = string.Empty;
        SelectedCreditScore = 0m;
        SelectedAchievement = null;
    }

    private void OpenAddForm()
    {
        // 打开“添加项目”面板：清空表单并以当前账户信息预填学生姓名/学号
        var draft = new Achievement
        {
            Id = 0,
            Title = string.Empty,
            Category = "科研项目",
            Level = "校级",
            Year = DateTime.UtcNow.Year,
            Department = CurrentUserDepartment,
            StudentName = StudentName,
            StudentId = StudentId,
            Advisor = string.Empty,
            TeamName = string.Empty,
            Status = "草稿",
            AchievedOn = DateTime.Today,
            Description = string.Empty,
            ExtraJson = "{}",
            CreditScore = 0
        };

        SelectedAchievement = draft;

        // 将“学生信息”同步到表单字段（SelectedAchievement setter 也会同步一次，这里用于确保对象为空时的默认值）
        StudentName = draft.StudentName;
        StudentId = draft.StudentId;
        Department = draft.Department;
        Category = draft.Category;
        Level = draft.Level;
        Year = draft.Year;
        Status = draft.Status;
        SelectedCreditScore = 0m;
    }

    private void RefreshImageAttachments(Achievement? achievement)
    {
        _imageAttachments.Clear();
        if (achievement?.Attachments is null) return;

        foreach (var att in achievement.Attachments)
        {
            // 只展示图片类型证明材料
            if (string.IsNullOrWhiteSpace(att.FileType))
            {
                _imageAttachments.Add(att);
                continue;
            }

            if (att.FileType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                _imageAttachments.Add(att);
            }
        }
    }

    private static string GetLatestRejectReason(Achievement achievement)
    {
        var log = achievement.AuditLogs?
            .Where(x => string.Equals(x.Action, "Reject", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefault();

        return log?.Comment ?? string.Empty;
    }

    private void ReplaceAchievementInList(Achievement refreshed)
    {
        var idx = Achievements.ToList().FindIndex(x => x.Id == refreshed.Id);
        if (idx >= 0)
        {
            Achievements[idx] = refreshed;
            return;
        }

        // 新建后直接提交的场景：列表中尚无该条目，插入到顶部实现“立即刷新”
        Achievements.Insert(0, refreshed);
    }

    private async Task DeleteAttachmentAsync(AchievementAttachment? attachment)
    {
        if (attachment is null || attachment.Id <= 0) return;
        if (SelectedAchievement is null || SelectedAchievement.Id <= 0) return;

        try
        {
            var ok = await _apiClient.DeleteAttachmentAsync(SelectedAchievement.Id, attachment.Id);
            if (ok)
            {
                // 只刷新当前成果附件，避免重新加载列表导致表单“重弹出”
                var refreshed = await _apiClient.GetAchievementByIdAsync(SelectedAchievement.Id);
                if (refreshed is not null)
                {
                    SelectedAchievement.Attachments = refreshed.Attachments ?? new List<AchievementAttachment>();
                    SelectedAchievement.AuditLogs = refreshed.AuditLogs ?? new List<AchievementAuditLog>();
                    RefreshImageAttachments(SelectedAchievement);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"删除失败: {ex.Message}");
        }
    }

    private Task OpenChangePasswordAsync()
    {
        try
        {
            var owner = Application.Current?.MainWindow;
            var dialog = new ChangePasswordDialog(_apiClient)
            {
                Owner = owner
            };
            dialog.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"弹窗打开失败：{ex.Message}", "提示");
        }
        return Task.CompletedTask;
    }

    private void ScheduleFilterRefresh()
    {
        _filterTimer.Stop();
        _filterTimer.Start();
    }

    // 用户管理方法
    public async Task LoadUsersAsync()
    {
        if (!IsAdmin) return;
        try
        {
            var users = await _apiClient.GetUsersAsync();
            Users.Clear();
            foreach (var user in users) Users.Add(user);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载用户列表失败: {ex.Message}");
        }
    }

    private async Task AddTeacherAsync()
    {
        var dialog = new UserDialog();
        dialog.Role = "Teacher";
        dialog.Title = "添加教师账户";
        if (dialog.ShowDialog() == true)
        {
            var request = dialog.GetCreateRequest();
            request.Role = "Teacher"; // 确保角色为Teacher
            var success = await _apiClient.CreateUserAsync(request);
            if (success)
            {
                MessageBox.Show("教师账户添加成功");
                await LoadUsersAsync();
            }
            else
            {
                MessageBox.Show("教师账户添加失败，请检查用户名是否已存在");
            }
        }
    }

    private async Task AddStudentAsync()
    {
        var dialog = new UserDialog();
        dialog.Role = "Student";
        dialog.Title = "添加学生账户";
        if (dialog.ShowDialog() == true)
        {
            var request = dialog.GetCreateRequest();
            request.Role = "Student"; // 确保角色为Student
            var success = await _apiClient.CreateUserAsync(request);
            if (success)
            {
                MessageBox.Show("学生账户添加成功");
                await LoadUsersAsync();
            }
            else
            {
                MessageBox.Show("学生账户添加失败，请检查用户名是否已存在");
            }
        }
    }

    private async Task UpdateUserAsync()
    {
        if (SelectedUser is null) return;

        var dialog = new UserDialog(SelectedUser);
        dialog.Title = "编辑用户账户";
        if (dialog.ShowDialog() == true)
        {
            var request = dialog.GetUpdateRequest();
            var success = await _apiClient.UpdateUserAsync(SelectedUser.Id, request);
            if (success)
            {
                MessageBox.Show("用户信息更新成功");
                await LoadUsersAsync();
            }
            else
            {
                MessageBox.Show("用户信息更新失败");
            }
        }
    }

    private async Task DeleteUserAsync()
    {
        if (SelectedUser is null) return;

        if (MessageBox.Show($"确定要删除用户 '{SelectedUser.Username}' 吗？", "确认删除", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
        {
            var success = await _apiClient.DeleteUserAsync(SelectedUser.Id);
            if (success)
            {
                MessageBox.Show("用户删除成功");
                await LoadUsersAsync();
            }
            else
            {
                MessageBox.Show("用户删除失败");
            }
        }
    }

    private async Task UpdatePersonalInfoAsync()
    {
        MessageBox.Show("个人信息更新功能正在开发中，当前版本暂不支持。");
    }

    private void Logout()
    {
        // 清除API客户端中的令牌
        _apiClient.ClearBearerToken();

        // 重新打开登录窗口
        var loginWindow = new LoginWindow();
        loginWindow.Show();

        // 关闭当前主窗口
        var mainWindow = Application.Current.Windows
            .OfType<Window>()
            .FirstOrDefault(w => w is MainWindow);
        mainWindow?.Close();
    }
}

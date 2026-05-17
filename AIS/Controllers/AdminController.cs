using System.Security.Claims;
using System.Text;
using AIS.Data;
using AIS.Extensions;
using AIS.Models;
using AIS.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AIS.Controllers;

[Authorize(Roles = nameof(UserRole.Admin))]
public class AdminController(
    AppDbContext dbContext,
    IPasswordHasher<User> passwordHasher) : AppControllerBase
{
    private readonly AppDbContext _dbContext = dbContext;
    private readonly IPasswordHasher<User> _passwordHasher = passwordHasher;

    [HttpGet]
    public async Task<IActionResult> Index(string? search, string status = AdminStatusFilterOption.All)
    {
        var now = DateTime.Now;
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var normalizedSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim().ToLowerInvariant();
        var selectedStatus = NormalizeStatusFilter(status);

        var users = await _dbContext.Users
            .AsNoTracking()
            .Include(user => user.Shifts)
            .OrderBy(user => user.FullName)
            .ToListAsync();

        var employees = users
            .Select(user =>
            {
                var activeShift = user.Shifts
                    .Where(shift => shift.IsActive)
                    .OrderByDescending(shift => shift.StartTime)
                    .FirstOrDefault();

                var lastShift = user.Shifts
                    .OrderByDescending(shift => shift.StartTime)
                    .FirstOrDefault();

                var todayShifts = user.Shifts
                    .Where(shift => shift.StartTime >= today && shift.StartTime < tomorrow)
                    .OrderByDescending(shift => shift.StartTime)
                    .ToList();

                return new AdminEmployeeRowViewModel
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Username = user.Username,
                    Role = user.Role,
                    IsOnShift = activeShift is not null,
                    ActiveShiftStartTime = activeShift?.StartTime,
                    LastShiftStartTime = lastShift?.StartTime,
                    LastShiftEndTime = lastShift?.EndTime,
                    ShiftsTodayCount = todayShifts.Count,
                    WorkedMinutesToday = todayShifts.Sum(shift =>
                    {
                        var endTime = shift.IsActive ? now : shift.EndTime ?? shift.StartTime;
                        var minutes = (int)Math.Floor((endTime - shift.StartTime).TotalMinutes);
                        return Math.Max(0, minutes);
                    }),
                    NeedsAttention = activeShift is not null && activeShift.StartTime <= now.AddHours(-10)
                };
            })
            .Where(employee => normalizedSearch is null
                || employee.FullName.ToLowerInvariant().Contains(normalizedSearch)
                || employee.Username.ToLowerInvariant().Contains(normalizedSearch))
            .Where(employee => status switch
            {
                _ when selectedStatus == AdminStatusFilterOption.OnShift => employee.IsOnShift,
                _ when selectedStatus == AdminStatusFilterOption.OffShift => !employee.IsOnShift,
                _ when selectedStatus == AdminStatusFilterOption.Admins => employee.Role == UserRole.Admin,
                _ => true
            })
            .ToList();

        var viewModel = new AdminIndexViewModel
        {
            Employees = employees,
            TotalEmployees = users.Count,
            ActiveEmployees = users.Count(user => user.Shifts.Any(shift => shift.IsActive)),
            LongRunningShiftsCount = users.Count(user => user.Shifts.Any(shift => shift.IsActive && shift.StartTime <= now.AddHours(-10))),
            FilteredEmployeesCount = employees.Count,
            SearchQuery = search?.Trim() ?? string.Empty,
            StatusFilter = selectedStatus
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Settings()
    {
        var currentUserId = GetCurrentUserId();

        var user = await _dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == currentUserId && item.Role == UserRole.Admin);

        if (user is null)
        {
            return NotFound();
        }

        return View(new AdminSettingsViewModel
        {
            Username = user.Username
        });
    }

    [HttpGet("/admin/shift-log")]
    public async Task<IActionResult> ShiftLog(string? search, string period = "today", string? date = null, string attendance = AdminAttendanceFilterOption.All)
    {
        var now = DateTime.Now;
        var report = await BuildShiftLogReportAsync(search, period, date, attendance, now);
        return View("History", report);
    }

    [HttpGet("/admin/shift-log/export")]
    public async Task<IActionResult> ShiftLogExport(string? search, string period = "today", string? date = null, string attendance = AdminAttendanceFilterOption.All, string mode = "rows")
    {
        var now = DateTime.Now;
        var report = await BuildShiftLogReportAsync(search, period, date, attendance, now);
        var exportMode = mode == "employees" ? "employees" : "rows";
        var csv = exportMode == "employees"
            ? BuildEmployeeSummaryCsv(report.Employees)
            : BuildShiftRowsCsv(report.Rows);

        var dateSuffix = !string.IsNullOrWhiteSpace(report.ExactDate)
            ? report.ExactDate
            : report.Period;
        var attendanceSuffix = report.AttendanceFilter switch
        {
            AdminAttendanceFilterOption.Worked => "worked",
            AdminAttendanceFilterOption.Absent => "absent",
            _ => "all"
        };
        var fileName = exportMode == "employees"
            ? $"employees-{attendanceSuffix}-{dateSuffix}.csv"
            : $"shift-log-{dateSuffix}.csv";

        return File(new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(csv), "text/csv; charset=utf-8", fileName);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Settings(AdminSettingsViewModel model)
    {
        model.Username = Normalize(model.Username);
        model.Password = string.IsNullOrWhiteSpace(model.Password) ? null : model.Password.Trim();
        model.ConfirmPassword = string.IsNullOrWhiteSpace(model.ConfirmPassword) ? null : model.ConfirmPassword.Trim();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var currentUserId = GetCurrentUserId();

        var user = await _dbContext.Users
            .SingleOrDefaultAsync(item => item.Id == currentUserId && item.Role == UserRole.Admin);

        if (user is null)
        {
            return NotFound();
        }

        var usernameTaken = await _dbContext.Users
            .AnyAsync(item => item.Username == model.Username && item.Id != currentUserId);

        if (usernameTaken)
        {
            ModelState.AddModelError(nameof(model.Username), "Пользователь с таким логином уже существует.");
            return View(model);
        }

        user.Username = model.Username;

        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);
        }

        await _dbContext.SaveChangesAsync();

        SetStatus("success", "Настройки администратора обновлены.");
        return RedirectToAction(nameof(Settings));
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new UserCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserCreateViewModel model)
    {
        model.FullName = Normalize(model.FullName);
        model.Username = Normalize(model.Username);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var usernameTaken = await _dbContext.Users.AnyAsync(user => user.Username == model.Username);
        if (usernameTaken)
        {
            ModelState.AddModelError(nameof(model.Username), "Пользователь с таким логином уже существует.");
            return View(model);
        }

        var user = new User
        {
            FullName = model.FullName,
            Username = model.Username,
            Role = model.Role
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        SetStatus("success", $"Сотрудник «{user.FullName}» успешно создан.");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id);

        if (user is null)
        {
            return NotFound();
        }

        return View(new UserEditViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Username = user.Username,
            Role = user.Role
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserEditViewModel model)
    {
        model.FullName = Normalize(model.FullName);
        model.Username = Normalize(model.Username);
        model.Password = string.IsNullOrWhiteSpace(model.Password) ? null : model.Password.Trim();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _dbContext.Users.SingleOrDefaultAsync(item => item.Id == model.Id);
        if (user is null)
        {
            return NotFound();
        }

        var usernameTaken = await _dbContext.Users
            .AnyAsync(item => item.Username == model.Username && item.Id != model.Id);

        if (usernameTaken)
        {
            ModelState.AddModelError(nameof(model.Username), "Пользователь с таким логином уже существует.");
            return View(model);
        }

        if (await IsLastAdminAsync(user.Id) && user.Role == UserRole.Admin && model.Role != UserRole.Admin)
        {
            ModelState.AddModelError(nameof(model.Role), "Нельзя сменить роль у последнего администратора.");
            return View(model);
        }

        user.FullName = model.FullName;
        user.Username = model.Username;
        user.Role = model.Role;

        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);
        }

        await _dbContext.SaveChangesAsync();

        SetStatus("success", $"Данные сотрудника «{user.FullName}» обновлены.");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .Include(item => item.Shifts)
            .SingleOrDefaultAsync(item => item.Id == id);

        if (user is null)
        {
            return NotFound();
        }

        return View(new UserDeleteViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Username = user.Username,
            Role = user.Role,
            IsOnShift = user.Shifts.Any(shift => shift.IsActive),
            ShiftsCount = user.Shifts.Count,
            IsCurrentUser = user.Id == GetCurrentUserId(),
            IsLastAdmin = await IsLastAdminAsync(user.Id)
        });
    }

    [HttpPost, ActionName(nameof(Delete))]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var user = await _dbContext.Users.SingleOrDefaultAsync(item => item.Id == id);
        if (user is null)
        {
            return NotFound();
        }

        if (user.Id == GetCurrentUserId())
        {
            SetStatus("error", "Нельзя удалить текущую учетную запись администратора.");
            return RedirectToAction(nameof(Index));
        }

        if (user.Role == UserRole.Admin && await IsLastAdminAsync(user.Id))
        {
            SetStatus("error", "Нельзя удалить последнего администратора системы.");
            return RedirectToAction(nameof(Index));
        }

        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync();

        SetStatus("success", $"Сотрудник «{user.FullName}» удален.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartShift(int id)
    {
        if (!await _dbContext.Users.AnyAsync(user => user.Id == id))
        {
            return NotFound();
        }

        var hasActiveShift = await _dbContext.Shifts.AnyAsync(shift => shift.UserId == id && shift.IsActive);
        if (hasActiveShift)
        {
            SetStatus("warning", "У сотрудника уже есть активная смена.");
            return RedirectToAction(nameof(Index));
        }

        _dbContext.Shifts.Add(new Shift
        {
            UserId = id,
            StartTime = DateTime.Now,
            IsActive = true
        });

        await _dbContext.SaveChangesAsync();

        SetStatus("success", "Смена сотрудника успешно начата.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EndShift(int id)
    {
        var activeShift = await _dbContext.Shifts
            .Where(shift => shift.UserId == id && shift.IsActive)
            .OrderByDescending(shift => shift.StartTime)
            .FirstOrDefaultAsync();

        if (activeShift is null)
        {
            SetStatus("warning", "У сотрудника нет активной смены.");
            return RedirectToAction(nameof(Index));
        }

        activeShift.EndTime = DateTime.Now;
        activeShift.IsActive = false;

        await _dbContext.SaveChangesAsync();

        SetStatus("success", "Смена сотрудника завершена.");
        return RedirectToAction(nameof(Index));
    }

    private int GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.Parse(userId!);
    }

    private async Task<bool> IsLastAdminAsync(int userId)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == userId);

        if (user is null || user.Role != UserRole.Admin)
        {
            return false;
        }

        var adminsCount = await _dbContext.Users.CountAsync(item => item.Role == UserRole.Admin);
        return adminsCount <= 1;
    }

    private static string Normalize(string value) => value.Trim();

    private static string NormalizeStatusFilter(string? value) => value switch
    {
        AdminStatusFilterOption.OnShift => AdminStatusFilterOption.OnShift,
        AdminStatusFilterOption.OffShift => AdminStatusFilterOption.OffShift,
        AdminStatusFilterOption.Admins => AdminStatusFilterOption.Admins,
        _ => AdminStatusFilterOption.All
    };

    private static string NormalizeAttendanceFilter(string? value) => value switch
    {
        AdminAttendanceFilterOption.Worked => AdminAttendanceFilterOption.Worked,
        AdminAttendanceFilterOption.Absent => AdminAttendanceFilterOption.Absent,
        _ => AdminAttendanceFilterOption.All
    };

    private static string NormalizePeriod(string? period, bool includeAll, string defaultValue) => period switch
    {
        "today" => "today",
        "week" => "week",
        "month" => "month",
        "all" when includeAll => "all",
        _ => defaultValue
    };

    private static (DateTime? Start, DateTime? End, string Label) GetPeriodRange(string period, DateTime now) => period switch
    {
        "today" => (now.Date, now.Date.AddDays(1), "Сегодня"),
        "week" => (now.Date.AddDays(-6), now.Date.AddDays(1), "Последние 7 дней"),
        "month" => (new DateTime(now.Year, now.Month, 1), new DateTime(now.Year, now.Month, 1).AddMonths(1), "Текущий месяц"),
        "all" => (null, null, "Все время"),
        _ => (now.Date, now.Date.AddDays(1), "Сегодня")
    };

    private static int CalculateDurationMinutes(DateTime startTime, DateTime? endTime, bool isActive, DateTime now)
    {
        var actualEnd = isActive ? now : endTime ?? startTime;
        return Math.Max(0, (int)Math.Floor((actualEnd - startTime).TotalMinutes));
    }

    private async Task<AdminShiftLogViewModel> BuildShiftLogReportAsync(string? search, string period, string? date, string attendance, DateTime now)
    {
        var normalizedSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim().ToLowerInvariant();
        var normalizedPeriod = NormalizePeriod(period, includeAll: true, defaultValue: "today");
        var normalizedAttendance = NormalizeAttendanceFilter(attendance);
        var exactDate = ParseDate(date);
        var (rangeStart, rangeEnd, rangeLabel) = exactDate.HasValue
            ? (exactDate.Value.Date, exactDate.Value.Date.AddDays(1), exactDate.Value.ToString("dd.MM.yyyy"))
            : GetPeriodRange(normalizedPeriod, now);

        var shiftsQuery = _dbContext.Shifts
            .AsNoTracking()
            .Include(shift => shift.User)
            .AsQueryable();

        if (rangeStart.HasValue && rangeEnd.HasValue)
        {
            shiftsQuery = shiftsQuery.Where(shift => shift.StartTime >= rangeStart.Value && shift.StartTime < rangeEnd.Value);
        }

        if (normalizedSearch is not null)
        {
            shiftsQuery = shiftsQuery.Where(shift =>
                shift.User != null &&
                (shift.User.FullName.ToLower().Contains(normalizedSearch)
                 || shift.User.Username.ToLower().Contains(normalizedSearch)));
        }

        var shifts = await shiftsQuery
            .OrderByDescending(shift => shift.StartTime)
            .ToListAsync();

        var rows = shifts
            .Where(shift => shift.User is not null)
            .Select(shift => new AdminShiftLogRowViewModel
            {
                UserId = shift.UserId,
                FullName = shift.User!.FullName,
                Username = shift.User.Username,
                Role = shift.User.Role,
                StartTime = shift.StartTime,
                EndTime = shift.EndTime,
                IsActive = shift.IsActive,
                DurationMinutes = CalculateDurationMinutes(shift.StartTime, shift.EndTime, shift.IsActive, now)
            })
            .ToList();

        var usersQuery = _dbContext.Users
            .AsNoTracking()
            .AsQueryable();

        if (normalizedSearch is not null)
        {
            usersQuery = usersQuery.Where(user =>
                user.FullName.ToLower().Contains(normalizedSearch)
                || user.Username.ToLower().Contains(normalizedSearch));
        }

        var users = await usersQuery
            .OrderBy(user => user.FullName)
            .ToListAsync();

        var rowsByUserId = rows
            .GroupBy(row => row.UserId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var employees = users
            .Select(user =>
            {
                rowsByUserId.TryGetValue(user.Id, out var employeeRows);
                employeeRows ??= [];

                return new AdminShiftLogEmployeeSummaryViewModel
                {
                    UserId = user.Id,
                    FullName = user.FullName,
                    Username = user.Username,
                    Role = user.Role,
                    ShiftsCount = employeeRows.Count,
                    WorkedMinutes = employeeRows.Sum(item => item.DurationMinutes),
                    FirstStartTime = employeeRows.Count > 0 ? employeeRows.Min(item => item.StartTime) : null,
                    LastEndTime = employeeRows.Where(item => item.EndTime.HasValue).Max(item => item.EndTime),
                    HasActiveShift = employeeRows.Any(item => item.IsActive),
                    WasOnShiftInPeriod = employeeRows.Count > 0
                };
            })
            .ToList();

        var employeesMatchedCount = employees.Count;
        var employeesWorkedCount = employees.Count(item => item.WasOnShiftInPeriod);
        var employeesAbsentCount = employees.Count - employeesWorkedCount;

        employees = employees
            .Where(item => normalizedAttendance switch
            {
                AdminAttendanceFilterOption.Worked => item.WasOnShiftInPeriod,
                AdminAttendanceFilterOption.Absent => !item.WasOnShiftInPeriod,
                _ => true
            })
            .ToList();

        if (normalizedAttendance != AdminAttendanceFilterOption.All)
        {
            var employeeIds = employees.Select(item => item.UserId).ToHashSet();
            rows = rows
                .Where(item => employeeIds.Contains(item.UserId))
                .ToList();
        }

        return new AdminShiftLogViewModel
        {
            SearchQuery = search?.Trim() ?? string.Empty,
            Period = normalizedPeriod,
            ExactDate = exactDate?.ToString("yyyy-MM-dd") ?? string.Empty,
            AttendanceFilter = normalizedAttendance,
            RangeLabel = rangeLabel,
            TotalRecords = rows.Count,
            ActiveRecords = rows.Count(item => item.IsActive),
            EmployeesMatchedCount = employeesMatchedCount,
            EmployeesInScope = employees.Count,
            EmployeesWorkedCount = employeesWorkedCount,
            EmployeesAbsentCount = employeesAbsentCount,
            TotalWorkedMinutes = rows.Sum(item => item.DurationMinutes),
            Employees = employees.OrderBy(item => item.FullName).ToList(),
            Rows = rows
        };
    }

    private static DateTime? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateTime.TryParse(value, out var parsedDate)
            ? parsedDate.Date
            : null;
    }

    private static string BuildShiftRowsCsv(IReadOnlyCollection<AdminShiftLogRowViewModel> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Сотрудник;Логин;Роль;Начало;Окончание;Статус;Минуты;Длительность");

        foreach (var row in rows)
        {
            builder.AppendLine(string.Join(";",
                EscapeCsv(row.FullName),
                EscapeCsv(row.Username),
                EscapeCsv(row.Role.GetDisplayName()),
                EscapeCsv(row.StartTime.ToString("dd.MM.yyyy HH:mm")),
                EscapeCsv(row.EndTime?.ToString("dd.MM.yyyy HH:mm") ?? string.Empty),
                EscapeCsv(row.IsActive ? "Активна" : "Завершена"),
                row.DurationMinutes.ToString(),
                EscapeCsv(FormatDuration(row.DurationMinutes))));
        }

        return builder.ToString();
    }

    private static string BuildEmployeeSummaryCsv(IReadOnlyCollection<AdminShiftLogEmployeeSummaryViewModel> employees)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Сотрудник;Логин;Роль;Статус за период;Смен;Минуты;Длительность;Первый приход;Последний уход;Активная смена");

        foreach (var employee in employees)
        {
            builder.AppendLine(string.Join(";",
                EscapeCsv(employee.FullName),
                EscapeCsv(employee.Username),
                EscapeCsv(employee.Role.GetDisplayName()),
                EscapeCsv(employee.WasOnShiftInPeriod ? "Был на смене" : "Не был на смене"),
                employee.ShiftsCount.ToString(),
                employee.WorkedMinutes.ToString(),
                EscapeCsv(FormatDuration(employee.WorkedMinutes)),
                EscapeCsv(employee.FirstStartTime?.ToString("dd.MM.yyyy HH:mm") ?? string.Empty),
                EscapeCsv(employee.LastEndTime?.ToString("dd.MM.yyyy HH:mm") ?? string.Empty),
                EscapeCsv(employee.HasActiveShift ? "Да" : "Нет")));
        }

        return builder.ToString();
    }

    private static string EscapeCsv(string value)
    {
        var normalized = value.Replace("\"", "\"\"");
        return $"\"{normalized}\"";
    }

    private static string FormatDuration(int totalMinutes)
    {
        var safeMinutes = Math.Max(0, totalMinutes);
        var hours = safeMinutes / 60;
        var minutes = safeMinutes % 60;
        return $"{hours:00}:{minutes:00}";
    }
}

using System.Diagnostics;
using System.Security.Claims;
using AIS.Data;
using AIS.ViewModels.Home;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AIS.Controllers;

[Authorize]
public class HomeController(AppDbContext dbContext) : AppControllerBase
{
    private readonly AppDbContext _dbContext = dbContext;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var currentUserId = GetCurrentUserId();
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var now = DateTime.Now;

        var user = await _dbContext.Users
            .AsNoTracking()
            .SingleAsync(item => item.Id == currentUserId);

        var activeShift = await _dbContext.Shifts
            .AsNoTracking()
            .Where(shift => shift.UserId == currentUserId && shift.IsActive)
            .OrderByDescending(shift => shift.StartTime)
            .FirstOrDefaultAsync();

        var todayShifts = await _dbContext.Shifts
            .AsNoTracking()
            .Where(shift => shift.UserId == currentUserId
                && shift.StartTime >= today
                && shift.StartTime < tomorrow)
            .OrderByDescending(shift => shift.StartTime)
            .ToListAsync();

        var latestShiftToday = todayShifts
            .OrderByDescending(shift => shift.StartTime)
            .FirstOrDefault();

        var recentShifts = await _dbContext.Shifts
            .AsNoTracking()
            .Where(shift => shift.UserId == currentUserId)
            .OrderByDescending(shift => shift.StartTime)
            .Take(5)
            .ToListAsync();

        var workedMinutesToday = todayShifts.Sum(shift =>
        {
            var endTime = shift.IsActive ? now : shift.EndTime ?? shift.StartTime;
            var minutes = (int)Math.Floor((endTime - shift.StartTime).TotalMinutes);
            return Math.Max(0, minutes);
        });

        var viewModel = new DashboardViewModel
        {
            FullName = user.FullName,
            Username = user.Username,
            Role = user.Role,
            HasActiveShift = activeShift is not null,
            ActiveShiftStartTime = activeShift?.StartTime,
            LatestShiftStartTimeToday = latestShiftToday?.StartTime,
            LatestShiftEndTimeToday = latestShiftToday?.EndTime,
            FirstShiftStartToday = todayShifts
                .OrderBy(shift => shift.StartTime)
                .Select(shift => (DateTime?)shift.StartTime)
                .FirstOrDefault(),
            ShiftsTodayCount = todayShifts.Count,
            WorkedMinutesToday = workedMinutesToday,
            RecentShifts = recentShifts
                .Select(shift => new DashboardShiftHistoryItemViewModel
                {
                    StartTime = shift.StartTime,
                    EndTime = shift.EndTime,
                    IsActive = shift.IsActive,
                    DurationMinutes = Math.Max(
                        0,
                        (int)Math.Floor((((shift.IsActive ? now : shift.EndTime) ?? shift.StartTime) - shift.StartTime).TotalMinutes))
                })
                .ToList()
        };

        return View(viewModel);
    }

    [HttpGet]
    public IActionResult Faq()
    {
        return View();
    }

    [HttpGet("/me/shifts")]
    public async Task<IActionResult> Shifts(string period = "week")
    {
        var currentUserId = GetCurrentUserId();
        var now = DateTime.Now;
        var normalizedPeriod = NormalizePeriod(period, includeAll: true, defaultValue: "week");
        var (rangeStart, rangeEnd, rangeLabel) = GetPeriodRange(normalizedPeriod, now);

        var shiftsQuery = _dbContext.Shifts
            .AsNoTracking()
            .Where(shift => shift.UserId == currentUserId);

        if (rangeStart.HasValue && rangeEnd.HasValue)
        {
            shiftsQuery = shiftsQuery.Where(shift => shift.StartTime >= rangeStart.Value && shift.StartTime < rangeEnd.Value);
        }

        var shifts = await shiftsQuery
            .OrderByDescending(shift => shift.StartTime)
            .Take(50)
            .ToListAsync();

        var rows = shifts
            .Select(shift => new HistoryShiftRowViewModel
            {
                StartTime = shift.StartTime,
                EndTime = shift.EndTime,
                IsActive = shift.IsActive,
                DurationMinutes = CalculateDurationMinutes(shift.StartTime, shift.EndTime, shift.IsActive, now)
            })
            .ToList();

        var viewModel = new HistoryViewModel
        {
            Period = normalizedPeriod,
            RangeLabel = rangeLabel,
            TotalShifts = rows.Count,
            ActiveShifts = rows.Count(item => item.IsActive),
            WorkedMinutes = rows.Sum(item => item.DurationMinutes),
            Rows = rows
        };

        return View("History", viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartShift()
    {
        var currentUserId = GetCurrentUserId();
        var hasActiveShift = await _dbContext.Shifts.AnyAsync(shift => shift.UserId == currentUserId && shift.IsActive);

        if (hasActiveShift)
        {
            SetStatus("warning", "У вас уже есть активная смена.");
            return RedirectToAction(nameof(Index));
        }

        _dbContext.Shifts.Add(new Models.Shift
        {
            UserId = currentUserId,
            StartTime = DateTime.Now,
            IsActive = true
        });

        await _dbContext.SaveChangesAsync();
        SetStatus("success", "Смена успешно начата.");

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EndShift()
    {
        var currentUserId = GetCurrentUserId();

        var activeShift = await _dbContext.Shifts
            .Where(shift => shift.UserId == currentUserId && shift.IsActive)
            .OrderByDescending(shift => shift.StartTime)
            .FirstOrDefaultAsync();

        if (activeShift is null)
        {
            SetStatus("warning", "Активная смена не найдена.");
            return RedirectToAction(nameof(Index));
        }

        activeShift.EndTime = DateTime.Now;
        activeShift.IsActive = false;

        await _dbContext.SaveChangesAsync();
        SetStatus("success", "Смена завершена.");

        return RedirectToAction(nameof(Index));
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new Models.ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }

    private int GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.Parse(userId!);
    }

    private static int CalculateDurationMinutes(DateTime startTime, DateTime? endTime, bool isActive, DateTime now)
    {
        var actualEnd = isActive ? now : endTime ?? startTime;
        return Math.Max(0, (int)Math.Floor((actualEnd - startTime).TotalMinutes));
    }

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
        _ => (now.Date.AddDays(-6), now.Date.AddDays(1), "Последние 7 дней")
    };
}

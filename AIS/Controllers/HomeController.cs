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

        var user = await _dbContext.Users
            .AsNoTracking()
            .SingleAsync(item => item.Id == currentUserId);

        var activeShift = await _dbContext.Shifts
            .AsNoTracking()
            .Where(shift => shift.UserId == currentUserId && shift.IsActive)
            .OrderByDescending(shift => shift.StartTime)
            .FirstOrDefaultAsync();

        var latestShiftToday = await _dbContext.Shifts
            .AsNoTracking()
            .Where(shift => shift.UserId == currentUserId
                && shift.StartTime >= today
                && shift.StartTime < tomorrow)
            .OrderByDescending(shift => shift.StartTime)
            .FirstOrDefaultAsync();

        var viewModel = new DashboardViewModel
        {
            FullName = user.FullName,
            Username = user.Username,
            Role = user.Role,
            HasActiveShift = activeShift is not null,
            ActiveShiftStartTime = activeShift?.StartTime,
            LatestShiftStartTimeToday = latestShiftToday?.StartTime,
            LatestShiftEndTimeToday = latestShiftToday?.EndTime
        };

        return View(viewModel);
    }

    [HttpGet]
    public IActionResult Faq()
    {
        return View();
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
}

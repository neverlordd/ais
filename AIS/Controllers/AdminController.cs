using System.Security.Claims;
using AIS.Data;
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
    public async Task<IActionResult> Index()
    {
        var employees = await _dbContext.Users
            .AsNoTracking()
            .OrderBy(user => user.FullName)
            .Select(user => new AdminEmployeeRowViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Username = user.Username,
                Role = user.Role,
                IsOnShift = user.Shifts.Any(shift => shift.IsActive),
                ActiveShiftStartTime = user.Shifts
                    .Where(shift => shift.IsActive)
                    .OrderByDescending(shift => shift.StartTime)
                    .Select(shift => (DateTime?)shift.StartTime)
                    .FirstOrDefault()
            })
            .ToListAsync();

        var viewModel = new AdminIndexViewModel
        {
            Employees = employees,
            TotalEmployees = employees.Count,
            ActiveEmployees = employees.Count(item => item.IsOnShift)
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
}

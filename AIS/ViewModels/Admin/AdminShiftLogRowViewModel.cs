using AIS.Models;

namespace AIS.ViewModels.Admin;

public class AdminShiftLogRowViewModel
{
    public int UserId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public int DurationMinutes { get; set; }

    public bool IsActive { get; set; }
}

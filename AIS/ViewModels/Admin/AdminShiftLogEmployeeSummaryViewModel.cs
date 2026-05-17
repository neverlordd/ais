using AIS.Models;

namespace AIS.ViewModels.Admin;

public class AdminShiftLogEmployeeSummaryViewModel
{
    public int UserId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public int ShiftsCount { get; set; }

    public int WorkedMinutes { get; set; }

    public DateTime? FirstStartTime { get; set; }

    public DateTime? LastEndTime { get; set; }

    public bool HasActiveShift { get; set; }

    public bool WasOnShiftInPeriod { get; set; }
}

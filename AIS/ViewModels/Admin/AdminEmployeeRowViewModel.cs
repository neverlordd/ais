using AIS.Models;

namespace AIS.ViewModels.Admin;

public class AdminEmployeeRowViewModel
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public bool IsOnShift { get; set; }

    public DateTime? ActiveShiftStartTime { get; set; }

    public DateTime? LastShiftStartTime { get; set; }

    public DateTime? LastShiftEndTime { get; set; }

    public int ShiftsTodayCount { get; set; }

    public int WorkedMinutesToday { get; set; }

    public bool NeedsAttention { get; set; }
}

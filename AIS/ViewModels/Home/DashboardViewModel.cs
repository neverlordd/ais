using AIS.Models;

namespace AIS.ViewModels.Home;

public class DashboardViewModel
{
    public string FullName { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public bool HasActiveShift { get; set; }

    public DateTime? ActiveShiftStartTime { get; set; }

    public DateTime? LatestShiftStartTimeToday { get; set; }

    public DateTime? LatestShiftEndTimeToday { get; set; }

    public DateTime? FirstShiftStartToday { get; set; }

    public int ShiftsTodayCount { get; set; }

    public int WorkedMinutesToday { get; set; }

    public IReadOnlyCollection<DashboardShiftHistoryItemViewModel> RecentShifts { get; set; } = [];
}

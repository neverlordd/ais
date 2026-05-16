namespace AIS.ViewModels.Home;

public class DashboardShiftHistoryItemViewModel
{
    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public int DurationMinutes { get; set; }

    public bool IsActive { get; set; }
}

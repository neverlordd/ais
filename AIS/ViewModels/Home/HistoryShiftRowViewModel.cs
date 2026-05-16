namespace AIS.ViewModels.Home;

public class HistoryShiftRowViewModel
{
    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public int DurationMinutes { get; set; }

    public bool IsActive { get; set; }
}

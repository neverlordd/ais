namespace AIS.ViewModels.Home;

public class HistoryViewModel
{
    public string Period { get; set; } = "week";

    public string RangeLabel { get; set; } = string.Empty;

    public int TotalShifts { get; set; }

    public int ActiveShifts { get; set; }

    public int WorkedMinutes { get; set; }

    public IReadOnlyCollection<HistoryShiftRowViewModel> Rows { get; set; } = [];
}

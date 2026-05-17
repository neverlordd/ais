namespace AIS.ViewModels.Admin;

public class AdminShiftLogViewModel
{
    public string SearchQuery { get; set; } = string.Empty;

    public string Period { get; set; } = "today";

    public string ExactDate { get; set; } = string.Empty;

    public string AttendanceFilter { get; set; } = AdminAttendanceFilterOption.All;

    public string RangeLabel { get; set; } = string.Empty;

    public int TotalRecords { get; set; }

    public int ActiveRecords { get; set; }

    public int EmployeesMatchedCount { get; set; }

    public int EmployeesInScope { get; set; }

    public int EmployeesWorkedCount { get; set; }

    public int EmployeesAbsentCount { get; set; }

    public int TotalWorkedMinutes { get; set; }

    public IReadOnlyCollection<AdminShiftLogEmployeeSummaryViewModel> Employees { get; set; } = [];

    public IReadOnlyCollection<AdminShiftLogRowViewModel> Rows { get; set; } = [];
}

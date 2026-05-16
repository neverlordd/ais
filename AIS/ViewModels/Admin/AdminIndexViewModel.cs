namespace AIS.ViewModels.Admin;

public class AdminIndexViewModel
{
    public IReadOnlyCollection<AdminEmployeeRowViewModel> Employees { get; set; } = [];

    public int TotalEmployees { get; set; }

    public int ActiveEmployees { get; set; }

    public int LongRunningShiftsCount { get; set; }

    public int FilteredEmployeesCount { get; set; }

    public string SearchQuery { get; set; } = string.Empty;

    public string StatusFilter { get; set; } = AdminStatusFilterOption.All;
}

namespace AIS.ViewModels.Admin;

public class AdminIndexViewModel
{
    public IReadOnlyCollection<AdminEmployeeRowViewModel> Employees { get; set; } = [];

    public int TotalEmployees { get; set; }

    public int ActiveEmployees { get; set; }
}

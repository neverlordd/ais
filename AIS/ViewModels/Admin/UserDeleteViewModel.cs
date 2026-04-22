using AIS.Models;

namespace AIS.ViewModels.Admin;

public class UserDeleteViewModel
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public bool IsOnShift { get; set; }

    public int ShiftsCount { get; set; }

    public bool IsCurrentUser { get; set; }

    public bool IsLastAdmin { get; set; }
}

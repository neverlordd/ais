using System.ComponentModel.DataAnnotations;

namespace AIS.Models;

public class User
{
    public int Id { get; set; }

    [Required]
    [StringLength(64, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [StringLength(512)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    public UserRole Role { get; set; }

    public ICollection<Shift> Shifts { get; set; } = new List<Shift>();
}

using System.ComponentModel.DataAnnotations;

namespace AIS.Models;

public enum UserRole
{
    [Display(Name = "Администратор")]
    Admin = 1,

    [Display(Name = "Сотрудник")]
    Employee = 2
}

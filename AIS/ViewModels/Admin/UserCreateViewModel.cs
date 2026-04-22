using System.ComponentModel.DataAnnotations;
using AIS.Models;

namespace AIS.ViewModels.Admin;

public class UserCreateViewModel
{
    [Required(ErrorMessage = "Введите ФИО сотрудника.")]
    [StringLength(128, ErrorMessage = "ФИО не должно превышать 128 символов.")]
    [Display(Name = "ФИО")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите логин.")]
    [StringLength(64, MinimumLength = 3, ErrorMessage = "Логин должен содержать от 3 до 64 символов.")]
    [Display(Name = "Логин")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Выберите роль.")]
    [Display(Name = "Роль")]
    public UserRole Role { get; set; } = UserRole.Employee;

    [Required(ErrorMessage = "Введите пароль.")]
    [StringLength(100, MinimumLength = 4, ErrorMessage = "Пароль должен содержать минимум 4 символа.")]
    [DataType(DataType.Password)]
    [Display(Name = "Пароль")]
    public string Password { get; set; } = string.Empty;
}

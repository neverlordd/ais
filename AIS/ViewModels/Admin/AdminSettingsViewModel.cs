using System.ComponentModel.DataAnnotations;

namespace AIS.ViewModels.Admin;

public class AdminSettingsViewModel
{
    [Required(ErrorMessage = "Введите логин.")]
    [StringLength(64, MinimumLength = 3, ErrorMessage = "Логин должен содержать от 3 до 64 символов.")]
    [Display(Name = "Логин")]
    public string Username { get; set; } = string.Empty;

    [StringLength(100, MinimumLength = 4, ErrorMessage = "Пароль должен содержать минимум 4 символа.")]
    [DataType(DataType.Password)]
    [Display(Name = "Новый пароль")]
    public string? Password { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Подтверждение пароля")]
    [Compare(nameof(Password), ErrorMessage = "Пароли не совпадают.")]
    public string? ConfirmPassword { get; set; }
}

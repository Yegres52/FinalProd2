using System.ComponentModel.DataAnnotations;

namespace Blog.ViewModels;

public class CreateUserInput
{
    [Required(ErrorMessage = "Введите логин."), StringLength(256, ErrorMessage = "Логин не должен превышать 256 символов.")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите имя."), StringLength(100, ErrorMessage = "Имя не должно превышать 100 символов.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите фамилию."), StringLength(100, ErrorMessage = "Фамилия не должна превышать 100 символов.")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите почтовый адрес."), EmailAddress(ErrorMessage = "Введите корректный почтовый адрес.")]
    [StringLength(256, ErrorMessage = "Почтовый адрес не должен превышать 256 символов.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите пароль."), MinLength(6, ErrorMessage = "Пароль должен содержать не менее 6 символов.")]
    public string Password { get; set; } = string.Empty;
}

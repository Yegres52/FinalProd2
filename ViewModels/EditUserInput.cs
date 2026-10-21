using System.ComponentModel.DataAnnotations;

namespace Blog.ViewModels;

public class EditUserInput
{
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите логин.")]
    [StringLength(256)]
    public string UserName { get; set; } = string.Empty;

    [StringLength(100)]
    public string? FirstName { get; set; }

    [StringLength(100)]
    public string? LastName { get; set; }

    [Required(ErrorMessage = "Введите почтовый адрес.")]
    [EmailAddress(ErrorMessage = "Введите корректный почтовый адрес.")]
    public string Email { get; set; } = string.Empty;

    public string? CurrentPassword { get; set; }

    [MinLength(6, ErrorMessage = "Пароль должен содержать не менее 6 символов.")]
    public string? NewPassword { get; set; }

    public List<string> SelectedRoles { get; set; } = [];

    public IReadOnlyList<string> AvailableRoles { get; set; } = [];
    public bool CanEditRoles { get; set; }
    public bool HasBaseUserRole { get; set; }
    public bool IsSelf { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace Blog.ViewModels;

public class EditRoleInput
{
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите название роли.")]
    [StringLength(256, ErrorMessage = "Название не должно превышать 256 символов.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите описание роли.")]
    public string Description { get; set; } = string.Empty;
}

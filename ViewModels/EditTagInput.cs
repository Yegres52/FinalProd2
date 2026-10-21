using System.ComponentModel.DataAnnotations;

namespace Blog.ViewModels;

public class EditTagInput
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Введите название тега.")]
    [StringLength(50, ErrorMessage = "Название не должно быть длиннее 50 символов.")]
    public string Name { get; set; } = string.Empty;
}

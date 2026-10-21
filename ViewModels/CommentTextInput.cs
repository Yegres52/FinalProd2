using System.ComponentModel.DataAnnotations;

namespace Blog.ViewModels;

public class CommentTextInput
{
    [Required(ErrorMessage = "Введите комментарий.")]
    [StringLength(2000, ErrorMessage = "Комментарий не должен превышать 2000 символов.")]
    public string Content { get; set; } = string.Empty;
}

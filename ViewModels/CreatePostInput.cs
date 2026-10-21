using System.ComponentModel.DataAnnotations;
using Blog.Services;

namespace Blog.ViewModels;

public class CreatePostInput
{
    [Required(ErrorMessage = "Введите заголовок.")]
    [StringLength(200, ErrorMessage = "Заголовок не должен превышать 200 символов.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите текст статьи.")]
    public string Content { get; set; } = string.Empty;

    public List<int> TagIds { get; set; } = [];

    public IReadOnlyList<TagResponse> AvailableTags { get; set; } = [];
}

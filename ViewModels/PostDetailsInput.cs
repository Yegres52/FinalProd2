using System.ComponentModel.DataAnnotations;
using Blog.Services;

namespace Blog.ViewModels;

public class AddPostCommentInput
{
    [Required(ErrorMessage = "Введите комментарий.")]
    [StringLength(2000, ErrorMessage = "Комментарий не должен превышать 2000 символов.")]
    public string CommentText { get; set; } = string.Empty;
}

public class PostDetailsInput : AddPostCommentInput
{
    public PostDetailsResponse Details { get; set; } = null!;
}

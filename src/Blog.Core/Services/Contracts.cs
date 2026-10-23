using System.ComponentModel.DataAnnotations;

namespace Blog.Services;

public enum OperationStatus { Success, NotFound, Forbidden, Conflict, Invalid }

public record OperationResult<T>(OperationStatus Status, T? Value = default, string[]? Errors = null)
{
    public static OperationResult<T> Ok(T value) => new(OperationStatus.Success, value);
    public static OperationResult<T> Fail(OperationStatus status, params string[] errors) => new(status, default, errors);
}

public record RegisterUserRequest(
    [Required, StringLength(256)] string UserName,
    [Required, EmailAddress] string Email,
    [Required, MinLength(6)] string Password,
    [Required, StringLength(100)] string FirstName,
    [Required, StringLength(100)] string LastName);

public record UpdateUserRequest(
    [Required, StringLength(256)] string UserName,
    [Required, EmailAddress] string Email,
    [StringLength(100)] string? FirstName = null,
    [StringLength(100)] string? LastName = null,
    string? CurrentPassword = null,
    string? NewPassword = null,
    string[]? Roles = null);

public record UserResponse(string Id, string? UserName, string[] Roles);

public record UserListResponse(
    string Id, string? UserName, string? FirstName, string? LastName,
    string? Email, string[] Roles);

public record UserEditResponse(
    string Id, string? UserName, string? Email,
    string? FirstName, string? LastName, string[] Roles);

public record SavePostRequest(
    [Required, StringLength(200)] string Title,
    [Required] string Content,
    int[]? TagIds = null);

public record PostResponse(int Id, string Title, string Content, DateTime CreatedAtUtc, string AuthorId);

public record PostEditResponse(int Id, string Title, string Content, string AuthorId, int[] TagIds);

public record PostTagLinkResponse(int Id, string Name);

public record PostListResponse(
    int Id, string Title, string Content, DateTime CreatedAtUtc,
    string AuthorId, PostTagLinkResponse[] Tags);

public record PostCommentDetailsResponse(
    int Id, string Content, DateTime CreatedAtUtc, string AuthorName);

public record PostDetailsResponse(
    int Id, string Title, string Content, DateTime CreatedAtUtc,
    string AuthorId, string AuthorName, PostTagLinkResponse[] Tags,
    PostCommentDetailsResponse[] Comments);

public record SaveTagRequest(
    [Required(ErrorMessage = "Введите название тега.")]
    [StringLength(50, ErrorMessage = "Название не должно быть длиннее 50 символов.")]
    string Name);
public record TagResponse(int Id, string Name, string? CreatorId);
public record TagListResponse(int Id, string Name, string? CreatorId, int PostCount);

public record SaveCommentRequest(
    [Required, StringLength(2000)] string Content,
    [Range(1, int.MaxValue)] int PostId);

public record CommentResponse(int Id, string Content, DateTime CreatedAtUtc, int PostId, string AuthorId);

public record CommentDisplayResponse(
    int Id, string Content, DateTime CreatedAtUtc, int PostId,
    string PostTitle, string AuthorId, string AuthorName);

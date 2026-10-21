namespace Blog.Services;

public interface IUserService
{
    Task<IReadOnlyList<UserResponse>> GetAllAsync();
    Task<IReadOnlyList<UserListResponse>> GetAllForAdminAsync();
    Task<UserResponse?> GetByIdAsync(string id);
    Task<OperationResult<UserEditResponse>> GetForEditAsync(string id, ActorContext actor);
    Task<OperationResult<UserResponse>> RegisterAsync(RegisterUserRequest request);
    Task<OperationResult<UserResponse>> UpdateAsync(string id, UpdateUserRequest request, ActorContext actor);
    Task<OperationResult<bool>> DeleteAsync(string id, ActorContext actor);
}

public interface IPostService
{
    Task<IReadOnlyList<PostResponse>> GetAllAsync(string? authorId);
    Task<PostResponse?> GetByIdAsync(int id);
    Task<OperationResult<PostEditResponse>> GetForEditAsync(int id, ActorContext actor);
    Task<OperationResult<PostResponse>> CreateAsync(SavePostRequest request, ActorContext actor);
    Task<OperationResult<PostResponse>> UpdateAsync(int id, SavePostRequest request, ActorContext actor);
    Task<OperationResult<bool>> DeleteAsync(int id, ActorContext actor);
}

public interface ITagService
{
    Task<IReadOnlyList<TagResponse>> GetAllAsync();
    Task<TagResponse?> GetByIdAsync(int id);
    Task<OperationResult<TagResponse>> CreateAsync(SaveTagRequest request, ActorContext actor);
    Task<OperationResult<TagResponse>> UpdateAsync(int id, SaveTagRequest request, ActorContext actor);
    Task<OperationResult<bool>> DeleteAsync(int id, ActorContext actor);
}

public interface ICommentService
{
    Task<IReadOnlyList<CommentResponse>> GetAllAsync();
    Task<CommentResponse?> GetByIdAsync(int id);
    Task<OperationResult<CommentResponse>> CreateAsync(SaveCommentRequest request, ActorContext actor);
    Task<OperationResult<CommentResponse>> UpdateAsync(int id, SaveCommentRequest request, ActorContext actor);
    Task<OperationResult<bool>> DeleteAsync(int id, ActorContext actor);
}

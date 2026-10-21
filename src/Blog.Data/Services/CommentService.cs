using Blog.Models;
using Blog.Services;
using Microsoft.EntityFrameworkCore;

namespace Blog.Data.Services;

public class CommentService(ApplicationDbContext db) : ICommentService
{
    public async Task<IReadOnlyList<CommentResponse>> GetAllAsync() =>
        await db.Comments.AsNoTracking().OrderByDescending(comment => comment.CreatedAtUtc)
            .Select(comment => new CommentResponse(comment.Id, comment.Content,
                comment.CreatedAtUtc, comment.PostId, comment.AuthorId))
            .ToListAsync();

    public async Task<IReadOnlyList<CommentDisplayResponse>> GetAllForCatalogAsync() =>
        await db.Comments.AsNoTracking().OrderByDescending(comment => comment.CreatedAtUtc)
            .Select(comment => new CommentDisplayResponse(comment.Id, comment.Content,
                comment.CreatedAtUtc, comment.PostId, comment.Post.Title,
                comment.AuthorId, comment.Author.UserName ?? "Пользователь"))
            .ToListAsync();

    public async Task<CommentDisplayResponse?> GetDetailsAsync(int id) =>
        await db.Comments.AsNoTracking().Where(comment => comment.Id == id)
            .Select(comment => new CommentDisplayResponse(comment.Id, comment.Content,
                comment.CreatedAtUtc, comment.PostId, comment.Post.Title,
                comment.AuthorId, comment.Author.UserName ?? "Пользователь"))
            .FirstOrDefaultAsync();

    public async Task<CommentResponse?> GetByIdAsync(int id) =>
        await db.Comments.AsNoTracking().Where(comment => comment.Id == id)
            .Select(comment => new CommentResponse(comment.Id, comment.Content,
                comment.CreatedAtUtc, comment.PostId, comment.AuthorId))
            .FirstOrDefaultAsync();

    public async Task<OperationResult<CommentResponse>> CreateAsync(SaveCommentRequest request, ActorContext actor)
    {
        if (actor.IsModerator && !actor.IsAdmin)
            return OperationResult<CommentResponse>.Fail(OperationStatus.Forbidden);
        if (!await db.Posts.AnyAsync(post => post.Id == request.PostId))
            return OperationResult<CommentResponse>.Fail(OperationStatus.NotFound, "Статья не найдена.");

        var comment = new Comment
        {
            Content = request.Content.Trim(),
            PostId = request.PostId,
            AuthorId = actor.Id,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Comments.Add(comment);
        await db.SaveChangesAsync();
        return OperationResult<CommentResponse>.Ok(Map(comment));
    }

    public async Task<OperationResult<CommentResponse>> UpdateAsync(int id, SaveCommentRequest request, ActorContext actor)
    {
        var comment = await db.Comments.FindAsync(id);
        if (comment is null) return OperationResult<CommentResponse>.Fail(OperationStatus.NotFound);
        if (comment.AuthorId != actor.Id && !actor.IsAdmin && !actor.IsModerator)
            return OperationResult<CommentResponse>.Fail(OperationStatus.Forbidden);
        if (!await db.Posts.AnyAsync(post => post.Id == request.PostId))
            return OperationResult<CommentResponse>.Fail(OperationStatus.NotFound, "Статья не найдена.");

        comment.Content = request.Content.Trim();
        comment.PostId = request.PostId;
        await db.SaveChangesAsync();
        return OperationResult<CommentResponse>.Ok(Map(comment));
    }

    public async Task<OperationResult<bool>> DeleteAsync(int id, ActorContext actor)
    {
        var comment = await db.Comments.FindAsync(id);
        if (comment is null) return OperationResult<bool>.Fail(OperationStatus.NotFound);
        if (!actor.IsAdmin && (actor.IsModerator || comment.AuthorId != actor.Id))
            return OperationResult<bool>.Fail(OperationStatus.Forbidden);

        db.Comments.Remove(comment);
        await db.SaveChangesAsync();
        return OperationResult<bool>.Ok(true);
    }

    private static CommentResponse Map(Comment comment) =>
        new(comment.Id, comment.Content, comment.CreatedAtUtc, comment.PostId, comment.AuthorId);
}

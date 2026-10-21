using Blog.Models;
using Blog.Services;
using Microsoft.EntityFrameworkCore;

namespace Blog.Data.Services;

public class PostService(ApplicationDbContext db) : IPostService
{
    public async Task<IReadOnlyList<PostResponse>> GetAllAsync(string? authorId) =>
        await db.Posts.AsNoTracking()
            .Where(post => authorId == null || post.AuthorId == authorId)
            .OrderByDescending(post => post.CreatedAtUtc)
            .Select(post => new PostResponse(post.Id, post.Title, post.Content,
                post.CreatedAtUtc, post.AuthorId))
            .ToListAsync();

    public async Task<PostResponse?> GetByIdAsync(int id) =>
        await db.Posts.AsNoTracking().Where(post => post.Id == id)
            .Select(post => new PostResponse(post.Id, post.Title, post.Content,
                post.CreatedAtUtc, post.AuthorId))
            .FirstOrDefaultAsync();

    public async Task<OperationResult<PostResponse>> CreateAsync(SavePostRequest request, ActorContext actor)
    {
        if (actor.IsModerator && !actor.IsAdmin)
            return OperationResult<PostResponse>.Fail(OperationStatus.Forbidden);
        var post = new Post
        {
            Title = request.Title.Trim(),
            Content = request.Content.Trim(),
            AuthorId = actor.Id,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Posts.Add(post);
        await db.SaveChangesAsync();
        return OperationResult<PostResponse>.Ok(Map(post));
    }

    public async Task<OperationResult<PostResponse>> UpdateAsync(int id, SavePostRequest request, ActorContext actor)
    {
        var post = await db.Posts.FindAsync(id);
        if (post is null) return OperationResult<PostResponse>.Fail(OperationStatus.NotFound);
        if (post.AuthorId != actor.Id && !actor.IsAdmin && !actor.IsModerator)
            return OperationResult<PostResponse>.Fail(OperationStatus.Forbidden);

        post.Title = request.Title.Trim();
        post.Content = request.Content.Trim();
        await db.SaveChangesAsync();
        return OperationResult<PostResponse>.Ok(Map(post));
    }

    public async Task<OperationResult<bool>> DeleteAsync(int id, ActorContext actor)
    {
        var post = await db.Posts.FindAsync(id);
        if (post is null) return OperationResult<bool>.Fail(OperationStatus.NotFound);
        if (!actor.IsAdmin && (actor.IsModerator || post.AuthorId != actor.Id))
            return OperationResult<bool>.Fail(OperationStatus.Forbidden);

        db.Posts.Remove(post);
        await db.SaveChangesAsync();
        return OperationResult<bool>.Ok(true);
    }

    private static PostResponse Map(Post post) =>
        new(post.Id, post.Title, post.Content, post.CreatedAtUtc, post.AuthorId);
}

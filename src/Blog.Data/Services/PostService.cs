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

    public async Task<IReadOnlyList<PostListResponse>> GetAllForCatalogAsync(int? tagId) =>
        await db.Posts.AsNoTracking()
            .Where(post => tagId == null ||
                post.PostTags.Any(postTag => postTag.TagId == tagId.Value))
            .OrderByDescending(post => post.CreatedAtUtc)
            .Select(post => new PostListResponse(post.Id, post.Title, post.Content,
                post.CreatedAtUtc, post.AuthorId,
                post.PostTags.OrderBy(postTag => postTag.Tag.Name)
                    .Select(postTag => new PostTagLinkResponse(postTag.TagId, postTag.Tag.Name))
                    .ToArray()))
            .ToListAsync();

    public async Task<IReadOnlyList<PostResponse>> GetByTagAsync(int tagId) =>
        await db.Posts.AsNoTracking()
            .Where(post => post.PostTags.Any(postTag => postTag.TagId == tagId))
            .OrderByDescending(post => post.CreatedAtUtc)
            .Select(post => new PostResponse(post.Id, post.Title, post.Content,
                post.CreatedAtUtc, post.AuthorId))
            .ToListAsync();

    public async Task<PostResponse?> GetByIdAsync(int id) =>
        await db.Posts.AsNoTracking().Where(post => post.Id == id)
            .Select(post => new PostResponse(post.Id, post.Title, post.Content,
                post.CreatedAtUtc, post.AuthorId))
            .FirstOrDefaultAsync();

    public async Task<OperationResult<PostEditResponse>> GetForEditAsync(int id, ActorContext actor)
    {
        var post = await db.Posts.AsNoTracking().Where(post => post.Id == id)
            .Select(post => new PostEditResponse(post.Id, post.Title, post.Content,
                post.AuthorId, post.PostTags.Select(postTag => postTag.TagId).ToArray()))
            .FirstOrDefaultAsync();
        if (post is null) return OperationResult<PostEditResponse>.Fail(OperationStatus.NotFound);
        if (post.AuthorId != actor.Id && !actor.IsAdmin && !actor.IsModerator)
            return OperationResult<PostEditResponse>.Fail(OperationStatus.Forbidden);
        return OperationResult<PostEditResponse>.Ok(post);
    }

    public async Task<OperationResult<PostResponse>> CreateAsync(SavePostRequest request, ActorContext actor)
    {
        if (actor.IsModerator && !actor.IsAdmin)
            return OperationResult<PostResponse>.Fail(OperationStatus.Forbidden);
        var tagIds = request.TagIds?.Distinct().ToArray() ?? [];
        if (!await TagsExistAsync(tagIds))
            return OperationResult<PostResponse>.Fail(OperationStatus.Invalid, "Выбран несуществующий тег.");

        var post = new Post
        {
            Title = request.Title.Trim(),
            Content = request.Content.Trim(),
            AuthorId = actor.Id,
            CreatedAtUtc = DateTime.UtcNow,
            PostTags = tagIds.Select(tagId => new PostTag { TagId = tagId }).ToList()
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

        var tagIds = request.TagIds?.Distinct().ToArray();
        if (tagIds is not null && !await TagsExistAsync(tagIds))
            return OperationResult<PostResponse>.Fail(OperationStatus.Invalid, "Выбран несуществующий тег.");

        post.Title = request.Title.Trim();
        post.Content = request.Content.Trim();
        if (tagIds is not null)
        {
            var existing = await db.PostTags.Where(postTag => postTag.PostId == id).ToListAsync();
            db.PostTags.RemoveRange(existing.Where(postTag => !tagIds.Contains(postTag.TagId)));
            var existingIds = existing.Select(postTag => postTag.TagId).ToHashSet();
            foreach (var tagId in tagIds.Where(tagId => !existingIds.Contains(tagId)))
                db.PostTags.Add(new PostTag { PostId = id, TagId = tagId });
        }
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

    private async Task<bool> TagsExistAsync(int[] tagIds) =>
        tagIds.Length == 0 ||
        await db.Tags.CountAsync(tag => tagIds.Contains(tag.Id)) == tagIds.Length;
}

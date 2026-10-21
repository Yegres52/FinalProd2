using Blog.Models;
using Blog.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Blog.Data.Services;

public class TagService(ApplicationDbContext db) : ITagService
{
    public async Task<IReadOnlyList<TagResponse>> GetAllAsync() =>
        await db.Tags.AsNoTracking().OrderBy(tag => tag.Name)
            .Select(tag => new TagResponse(tag.Id, tag.Name, tag.CreatorId)).ToListAsync();

    public async Task<IReadOnlyList<TagListResponse>> GetAllWithPostCountsAsync() =>
        await db.Tags.AsNoTracking().OrderBy(tag => tag.Name)
            .Select(tag => new TagListResponse(tag.Id, tag.Name, tag.CreatorId,
                tag.PostTags.Count))
            .ToListAsync();

    public async Task<TagResponse?> GetByIdAsync(int id) =>
        await db.Tags.AsNoTracking().Where(tag => tag.Id == id)
            .Select(tag => new TagResponse(tag.Id, tag.Name, tag.CreatorId)).FirstOrDefaultAsync();

    public async Task<OperationResult<TagResponse>> CreateAsync(SaveTagRequest request, ActorContext actor)
    {
        if (actor.IsModerator && !actor.IsAdmin)
            return OperationResult<TagResponse>.Fail(OperationStatus.Forbidden);
        var name = request.Name.Trim();
        if (await db.Tags.AnyAsync(tag => tag.Name == name))
            return OperationResult<TagResponse>.Fail(OperationStatus.Conflict, "Тег уже существует.");

        var tag = new Tag { Name = name, CreatorId = actor.Id };
        db.Tags.Add(tag);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException error) when (error.InnerException is SqliteException { SqliteErrorCode: 19 })
        {
            return OperationResult<TagResponse>.Fail(OperationStatus.Conflict, "Тег уже существует.");
        }
        return OperationResult<TagResponse>.Ok(new TagResponse(tag.Id, tag.Name, tag.CreatorId));
    }

    public async Task<OperationResult<TagResponse>> UpdateAsync(int id, SaveTagRequest request, ActorContext actor)
    {
        var tag = await db.Tags.FindAsync(id);
        if (tag is null) return OperationResult<TagResponse>.Fail(OperationStatus.NotFound);
        if (!actor.IsAdmin && (actor.IsModerator || tag.CreatorId != actor.Id))
            return OperationResult<TagResponse>.Fail(OperationStatus.Forbidden);

        var name = request.Name.Trim();
        if (await db.Tags.AnyAsync(other => other.Id != id && other.Name == name))
            return OperationResult<TagResponse>.Fail(OperationStatus.Conflict, "Тег уже существует.");

        tag.Name = name;
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException error) when (error.InnerException is SqliteException { SqliteErrorCode: 19 })
        {
            return OperationResult<TagResponse>.Fail(OperationStatus.Conflict, "Тег уже существует.");
        }
        return OperationResult<TagResponse>.Ok(new TagResponse(tag.Id, tag.Name, tag.CreatorId));
    }

    public async Task<OperationResult<bool>> DeleteAsync(int id, ActorContext actor)
    {
        var tag = await db.Tags.FindAsync(id);
        if (tag is null) return OperationResult<bool>.Fail(OperationStatus.NotFound);
        if (!actor.IsAdmin && (actor.IsModerator || tag.CreatorId != actor.Id))
            return OperationResult<bool>.Fail(OperationStatus.Forbidden);
        db.Tags.Remove(tag);
        await db.SaveChangesAsync();
        return OperationResult<bool>.Ok(true);
    }
}

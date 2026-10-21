using Blog.Models;
using Blog.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Blog.Data.Services;

public class UserService(
    ApplicationDbContext db,
    UserManager<ApplicationUser> users,
    RoleManager<ApplicationRole> roles) : IUserService
{
    public const string UserRole = RoleNames.User;

    public async Task<IReadOnlyList<UserResponse>> GetAllAsync() =>
        (await db.Users.AsNoTracking()
            .Include(user => user.UserRoles).ThenInclude(userRole => userRole.Role)
            .OrderBy(user => user.UserName)
            .ToListAsync())
            .Select(Map).ToList();

    public async Task<UserResponse?> GetByIdAsync(string id)
    {
        var user = await db.Users.AsNoTracking()
            .Include(item => item.UserRoles).ThenInclude(userRole => userRole.Role)
            .FirstOrDefaultAsync(item => item.Id == id);
        return user is null ? null : Map(user);
    }

    public async Task<OperationResult<UserResponse>> RegisterAsync(RegisterUserRequest request)
    {
        var user = new ApplicationUser
        {
            UserName = request.UserName.Trim(),
            Email = request.Email.Trim()
        };

        var created = await users.CreateAsync(user, request.Password);
        if (!created.Succeeded)
            return OperationResult<UserResponse>.Fail(OperationStatus.Invalid, Errors(created));

        if (!await roles.RoleExistsAsync(UserRole))
        {
            var roleCreated = await roles.CreateAsync(new ApplicationRole { Name = UserRole });
            if (!roleCreated.Succeeded && !await roles.RoleExistsAsync(UserRole))
            {
                await users.DeleteAsync(user);
                return OperationResult<UserResponse>.Fail(OperationStatus.Invalid, Errors(roleCreated));
            }
        }

        var assigned = await users.AddToRoleAsync(user, UserRole);
        if (!assigned.Succeeded)
        {
            await users.DeleteAsync(user);
            return OperationResult<UserResponse>.Fail(OperationStatus.Invalid, Errors(assigned));
        }

        return OperationResult<UserResponse>.Ok(new UserResponse(user.Id, user.UserName, [UserRole]));
    }

    public async Task<OperationResult<UserResponse>> UpdateAsync(string id, UpdateUserRequest request, ActorContext actor)
    {
        if (actor.IsModerator && !actor.IsAdmin)
            return OperationResult<UserResponse>.Fail(OperationStatus.Forbidden);
        if (id != actor.Id && !actor.IsAdmin)
            return OperationResult<UserResponse>.Fail(OperationStatus.Forbidden);
        var user = await users.FindByIdAsync(id);
        if (user is null) return OperationResult<UserResponse>.Fail(OperationStatus.NotFound);

        user.UserName = request.UserName.Trim();
        user.Email = request.Email.Trim();
        var updated = await users.UpdateAsync(user);
        return updated.Succeeded
            ? OperationResult<UserResponse>.Ok(new UserResponse(user.Id, user.UserName,
                (await users.GetRolesAsync(user)).ToArray()))
            : OperationResult<UserResponse>.Fail(OperationStatus.Invalid, Errors(updated));
    }

    public async Task<OperationResult<bool>> DeleteAsync(string id, ActorContext actor)
    {
        if (actor.IsModerator && !actor.IsAdmin)
            return OperationResult<bool>.Fail(OperationStatus.Forbidden);
        if (id != actor.Id && !actor.IsAdmin)
            return OperationResult<bool>.Fail(OperationStatus.Forbidden);
        var user = await users.FindByIdAsync(id);
        if (user is null) return OperationResult<bool>.Fail(OperationStatus.NotFound);
        if (!actor.IsAdmin &&
            (await db.Posts.AnyAsync(post => post.AuthorId == id) ||
             await db.Comments.AnyAsync(comment => comment.AuthorId == id) ||
             await db.Tags.AnyAsync(tag => tag.CreatorId == id)))
            return OperationResult<bool>.Fail(OperationStatus.Conflict,
                "Сначала удалите статьи, комментарии и теги пользователя.");

        if (actor.IsAdmin)
        {
            await using var transaction = await db.Database.BeginTransactionAsync();
            await db.Comments.Where(comment => comment.AuthorId == id).ExecuteDeleteAsync();
            await db.Posts.Where(post => post.AuthorId == id).ExecuteDeleteAsync();
            await db.Tags.Where(tag => tag.CreatorId == id).ExecuteDeleteAsync();
            var removed = await db.Users.Where(item => item.Id == id).ExecuteDeleteAsync();
            if (removed != 1)
                return OperationResult<bool>.Fail(OperationStatus.NotFound);
            await transaction.CommitAsync();
            return OperationResult<bool>.Ok(true);
        }

        var deleted = await users.DeleteAsync(user);
        return deleted.Succeeded
            ? OperationResult<bool>.Ok(true)
            : OperationResult<bool>.Fail(OperationStatus.Invalid, Errors(deleted));
    }

    private static string[] Errors(IdentityResult result) =>
        result.Errors.Select(error => error.Description).ToArray();

    private static UserResponse Map(ApplicationUser user) =>
        new(user.Id, user.UserName,
            user.Roles.Select(role => role.Name!).Where(name => name is not null).ToArray());
}

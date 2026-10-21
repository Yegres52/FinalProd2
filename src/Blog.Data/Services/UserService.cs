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

    public async Task<IReadOnlyList<UserListResponse>> GetAllForAdminAsync() =>
        (await db.Users.AsNoTracking()
            .Include(user => user.UserRoles).ThenInclude(userRole => userRole.Role)
            .OrderBy(user => user.UserName)
            .ToListAsync())
            .Select(user => new UserListResponse(
                user.Id, user.UserName, user.FirstName, user.LastName, user.Email,
                user.UserRoles.Select(userRole => userRole.Role.Name!)
                    .Where(name => name is not null)
                    .OrderBy(name => name)
                    .ToArray()))
            .ToList();

    public async Task<UserResponse?> GetByIdAsync(string id)
    {
        var user = await db.Users.AsNoTracking()
            .Include(item => item.UserRoles).ThenInclude(userRole => userRole.Role)
            .FirstOrDefaultAsync(item => item.Id == id);
        return user is null ? null : Map(user);
    }

    public async Task<OperationResult<UserEditResponse>> GetForEditAsync(string id, ActorContext actor)
    {
        if (actor.IsModerator && !actor.IsAdmin || id != actor.Id && !actor.IsAdmin)
            return OperationResult<UserEditResponse>.Fail(OperationStatus.Forbidden);

        var user = await users.FindByIdAsync(id);
        if (user is null)
            return OperationResult<UserEditResponse>.Fail(OperationStatus.NotFound);

        return OperationResult<UserEditResponse>.Ok(new UserEditResponse(
            user.Id, user.UserName, user.Email, user.FirstName, user.LastName,
            (await users.GetRolesAsync(user)).ToArray()));
    }

    public async Task<OperationResult<UserResponse>> RegisterAsync(RegisterUserRequest request)
    {
        var user = new ApplicationUser
        {
            UserName = request.UserName.Trim(),
            Email = request.Email.Trim(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim()
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
        if (request.Roles is not null && !actor.IsAdmin)
            return OperationResult<UserResponse>.Fail(OperationStatus.Forbidden);

        var user = await users.FindByIdAsync(id);
        if (user is null) return OperationResult<UserResponse>.Fail(OperationStatus.NotFound);

        var currentRoles = await users.GetRolesAsync(user);
        string[]? desiredRoles = null;
        if (request.Roles is not null)
        {
            desiredRoles = request.Roles
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            // A user's base role cannot be removed through the edit form or API.
            if (currentRoles.Contains(UserRole, StringComparer.OrdinalIgnoreCase) &&
                !desiredRoles.Contains(UserRole, StringComparer.OrdinalIgnoreCase))
                desiredRoles = [.. desiredRoles, UserRole];

            if (desiredRoles.Length == 0)
                return OperationResult<UserResponse>.Fail(OperationStatus.Invalid,
                    "У пользователя должна остаться хотя бы одна роль.");

            foreach (var roleName in desiredRoles)
                if (!await roles.RoleExistsAsync(roleName))
                    return OperationResult<UserResponse>.Fail(OperationStatus.Invalid,
                        $"Роль «{roleName}» не существует.");

            if (currentRoles.Contains(RoleNames.Admin, StringComparer.OrdinalIgnoreCase) &&
                !desiredRoles.Contains(RoleNames.Admin, StringComparer.OrdinalIgnoreCase))
            {
                var adminRole = await roles.FindByNameAsync(RoleNames.Admin);
                if (adminRole is not null &&
                    await db.UserRoles.CountAsync(userRole => userRole.RoleId == adminRole.Id) <= 1)
                    return OperationResult<UserResponse>.Fail(OperationStatus.Invalid,
                        "Нельзя снять роль с последнего администратора.");
            }
        }

        if (!string.IsNullOrWhiteSpace(request.NewPassword) &&
            (id == actor.Id || !actor.IsAdmin) &&
            string.IsNullOrEmpty(request.CurrentPassword))
            return OperationResult<UserResponse>.Fail(OperationStatus.Invalid,
                "Введите текущий пароль.");

        await using var transaction = await db.Database.BeginTransactionAsync();

        user.UserName = request.UserName.Trim();
        user.Email = request.Email.Trim();
        if (request.FirstName is not null)
            user.FirstName = request.FirstName.Trim();
        if (request.LastName is not null)
            user.LastName = request.LastName.Trim();
        var updated = await users.UpdateAsync(user);
        if (!updated.Succeeded)
            return OperationResult<UserResponse>.Fail(OperationStatus.Invalid, Errors(updated));

        if (!string.IsNullOrWhiteSpace(request.NewPassword))
        {
            IdentityResult passwordResult;
            if (actor.IsAdmin && id != actor.Id)
            {
                var token = await users.GeneratePasswordResetTokenAsync(user);
                passwordResult = await users.ResetPasswordAsync(user, token, request.NewPassword);
            }
            else
            {
                passwordResult = await users.ChangePasswordAsync(
                    user, request.CurrentPassword!, request.NewPassword);
            }

            if (!passwordResult.Succeeded)
                return OperationResult<UserResponse>.Fail(OperationStatus.Invalid, Errors(passwordResult));
        }

        if (desiredRoles is not null)
        {
            var removed = await users.RemoveFromRolesAsync(user,
                currentRoles.Except(desiredRoles, StringComparer.OrdinalIgnoreCase));
            if (!removed.Succeeded)
                return OperationResult<UserResponse>.Fail(OperationStatus.Invalid, Errors(removed));

            var added = await users.AddToRolesAsync(user,
                desiredRoles.Except(currentRoles, StringComparer.OrdinalIgnoreCase));
            if (!added.Succeeded)
                return OperationResult<UserResponse>.Fail(OperationStatus.Invalid, Errors(added));

            if (!currentRoles.Order(StringComparer.OrdinalIgnoreCase)
                    .SequenceEqual(desiredRoles.Order(StringComparer.OrdinalIgnoreCase),
                        StringComparer.OrdinalIgnoreCase))
            {
                var stamp = await users.UpdateSecurityStampAsync(user);
                if (!stamp.Succeeded)
                    return OperationResult<UserResponse>.Fail(OperationStatus.Invalid, Errors(stamp));
            }
        }

        await transaction.CommitAsync();
        return OperationResult<UserResponse>.Ok(new UserResponse(user.Id, user.UserName,
            (await users.GetRolesAsync(user)).ToArray()));
    }

    public async Task<OperationResult<bool>> DeleteAsync(string id, ActorContext actor)
    {
        if (actor.IsModerator && !actor.IsAdmin)
            return OperationResult<bool>.Fail(OperationStatus.Forbidden);
        if (id != actor.Id && !actor.IsAdmin)
            return OperationResult<bool>.Fail(OperationStatus.Forbidden);
        var user = await users.FindByIdAsync(id);
        if (user is null) return OperationResult<bool>.Fail(OperationStatus.NotFound);
        if (await users.IsInRoleAsync(user, RoleNames.Admin))
        {
            var adminRole = await roles.FindByNameAsync(RoleNames.Admin);
            if (adminRole is not null &&
                await db.UserRoles.CountAsync(userRole => userRole.RoleId == adminRole.Id) <= 1)
                return OperationResult<bool>.Fail(OperationStatus.Invalid,
                    "Нельзя удалить последнего администратора.");
        }
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

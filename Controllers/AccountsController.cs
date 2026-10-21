using System.Security.Claims;
using Blog.Models;
using Blog.Services;
using Blog.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Blog.Controllers;

[Authorize]
public class AccountsController(
    IUserService users,
    RoleManager<ApplicationRole> roles,
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager) : Controller
{
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> Index() => View(await users.GetAllForAdminAsync());

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var actor = GetActor();
        if (actor is null) return Forbid();

        var result = await users.GetForEditAsync(id, actor);
        if (result.Status == OperationStatus.Forbidden) return Forbid();
        if (result.Status == OperationStatus.NotFound) return NotFound();

        var user = result.Value!;
        var input = new EditUserInput
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email ?? string.Empty,
            SelectedRoles = user.Roles.ToList()
        };
        await PopulateRoleOptionsAsync(input, actor, user.Roles);
        return View(input);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, EditUserInput input)
    {
        var actor = GetActor();
        if (actor is null) return Forbid();

        var current = await users.GetForEditAsync(id, actor);
        if (current.Status == OperationStatus.Forbidden) return Forbid();
        if (current.Status == OperationStatus.NotFound) return NotFound();

        input.Id = id;
        await PopulateRoleOptionsAsync(input, actor, current.Value!.Roles);
        if (!ModelState.IsValid) return View(input);

        var request = new UpdateUserRequest(input.UserName, input.Email,
            input.FirstName, input.LastName, input.CurrentPassword, input.NewPassword,
            actor.IsAdmin ? input.SelectedRoles.ToArray() : null);
        var result = await users.UpdateAsync(id, request, actor);
        if (result.Status == OperationStatus.Forbidden) return Forbid();
        if (result.Status == OperationStatus.NotFound) return NotFound();
        if (result.Status != OperationStatus.Success)
        {
            foreach (var error in result.Errors ?? ["Не удалось сохранить пользователя."])
                ModelState.AddModelError(string.Empty, error);
            return View(input);
        }

        if (id == actor.Id)
        {
            var user = await userManager.FindByIdAsync(id);
            if (user is not null) await signInManager.RefreshSignInAsync(user);
        }

        TempData["Success"] = "Изменения сохранены.";
        return actor.IsAdmin && id != actor.Id
            ? RedirectToAction(nameof(Index))
            : RedirectToAction(nameof(Edit), new { id });
    }

    private ActorContext? GetActor()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return id is null ? null : new ActorContext(id,
            User.IsInRole(RoleNames.Admin), User.IsInRole(RoleNames.Moderator));
    }

    private async Task PopulateRoleOptionsAsync(EditUserInput input, ActorContext actor,
        IReadOnlyCollection<string> currentRoles)
    {
        input.IsSelf = input.Id == actor.Id;
        input.CanEditRoles = actor.IsAdmin;
        input.HasBaseUserRole = currentRoles.Contains(RoleNames.User);
        if (actor.IsAdmin)
            input.AvailableRoles = await roles.Roles.AsNoTracking()
                .OrderBy(role => role.Name)
                .Select(role => role.Name!)
                .ToListAsync();
    }
}

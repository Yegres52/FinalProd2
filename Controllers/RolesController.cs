using Blog.Models;
using Blog.Services;
using Blog.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Blog.Controllers;

[Authorize(Roles = RoleNames.Admin)]
public class RolesController(RoleManager<ApplicationRole> roles) : Controller
{
    public async Task<IActionResult> Index() =>
        View(await roles.Roles.AsNoTracking().OrderBy(role => role.Name).ToListAsync());

    [HttpGet]
    public IActionResult Create() => View(new CreateRoleInput());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateRoleInput input)
    {
        if (!ModelState.IsValid)
            return View(input);

        var name = input.Name.Trim();
        if (await roles.RoleExistsAsync(name))
        {
            ModelState.AddModelError(nameof(input.Name), "Роль с таким названием уже существует.");
            return View(input);
        }

        var result = await roles.CreateAsync(new ApplicationRole
        {
            Name = name,
            Description = input.Description.Trim()
        });
        if (result.Succeeded)
            return RedirectToAction(nameof(Index));

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);
        return View(input);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var role = await roles.FindByIdAsync(id);
        if (role is null) return NotFound();
        if (IsStandardRole(role.Name)) return Forbid();

        return View(new EditRoleInput
        {
            Id = role.Id,
            Name = role.Name ?? string.Empty,
            Description = role.Description ?? string.Empty
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, EditRoleInput input)
    {
        var role = await roles.FindByIdAsync(id);
        if (role is null) return NotFound();
        if (IsStandardRole(role.Name)) return Forbid();

        input.Id = id;
        if (!ModelState.IsValid) return View(input);

        var name = input.Name.Trim();
        var description = input.Description.Trim();
        if (name.Length == 0)
            ModelState.AddModelError(nameof(input.Name), "Введите название роли.");
        if (description.Length == 0)
            ModelState.AddModelError(nameof(input.Description), "Введите описание роли.");
        if (!ModelState.IsValid) return View(input);

        if (IsStandardRole(name))
        {
            ModelState.AddModelError(nameof(input.Name), "Название стандартной роли использовать нельзя.");
            return View(input);
        }

        var existing = await roles.FindByNameAsync(name);
        if (existing is not null && existing.Id != id)
        {
            ModelState.AddModelError(nameof(input.Name), "Роль с таким названием уже существует.");
            return View(input);
        }

        role.Name = name;
        role.Description = description;
        var result = await roles.UpdateAsync(role);
        if (result.Succeeded) return RedirectToAction(nameof(Index));

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);
        return View(input);
    }

    private static bool IsStandardRole(string? name) =>
        string.Equals(name, RoleNames.Admin, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(name, RoleNames.Moderator, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(name, RoleNames.User, StringComparison.OrdinalIgnoreCase);
}

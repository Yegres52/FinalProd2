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
}

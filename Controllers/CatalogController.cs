using System.Security.Claims;
using Blog.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Blog.Controllers;

public class CatalogController(IPostService posts, ITagService tags) : Controller
{
    public async Task<IActionResult> Posts() => View(await posts.GetAllAsync(null));

    public async Task<IActionResult> Post(int id)
    {
        var post = await posts.GetByIdAsync(id);
        return post is null ? NotFound() : View(post);
    }

    public async Task<IActionResult> Tags() => View(await tags.GetAllAsync());

    [HttpGet]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.User)]
    public IActionResult CreateTag() => View(new SaveTagRequest(string.Empty));

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.User)]
    public async Task<IActionResult> CreateTag(SaveTagRequest request)
    {
        if (!ModelState.IsValid)
            return View(request);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Forbid();

        var actor = new ActorContext(userId,
            User.IsInRole(RoleNames.Admin), User.IsInRole(RoleNames.Moderator));
        var result = await tags.CreateAsync(request, actor);
        if (result.Status == OperationStatus.Success)
            return RedirectToAction(nameof(Tags));
        if (result.Status == OperationStatus.Forbidden)
            return Forbid();

        foreach (var error in result.Errors ?? ["Не удалось добавить тег."])
            ModelState.AddModelError(nameof(request.Name), error);
        return View(request);
    }
}

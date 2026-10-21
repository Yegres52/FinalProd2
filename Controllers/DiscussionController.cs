using System.Security.Claims;
using Blog.Services;
using Blog.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Blog.Controllers;

public class DiscussionController(ICommentService comments, IPostService posts) : Controller
{
    public async Task<IActionResult> Index() => View(await comments.GetAllForCatalogAsync());

    public async Task<IActionResult> Details(int id)
    {
        var comment = await comments.GetDetailsAsync(id);
        return comment is null ? NotFound() : View(comment);
    }

    [HttpGet]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.User)]
    public async Task<IActionResult> Create(int postId)
    {
        var post = await posts.GetByIdAsync(postId);
        if (post is null) return NotFound();
        if (User.IsInRole(RoleNames.Moderator) && !User.IsInRole(RoleNames.Admin)) return Forbid();
        ViewData["PostTitle"] = post.Title;
        ViewData["PostId"] = postId;
        return View(new CommentTextInput());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.User)]
    public async Task<IActionResult> Create(int postId, CommentTextInput input)
    {
        var actor = GetActor();
        if (actor is null) return Forbid();
        if (actor.IsModerator && !actor.IsAdmin) return Forbid();
        var post = await posts.GetByIdAsync(postId);
        if (post is null) return NotFound();
        ViewData["PostTitle"] = post.Title;
        ViewData["PostId"] = postId;
        if (!ModelState.IsValid) return View(input);

        var result = await comments.CreateAsync(new SaveCommentRequest(input.Content, postId), actor);
        if (result.Status == OperationStatus.Forbidden) return Forbid();
        if (result.Status == OperationStatus.NotFound) return NotFound();
        if (result.Status == OperationStatus.Success)
            return RedirectToAction(nameof(Details), new { id = result.Value!.Id });
        foreach (var error in result.Errors ?? ["Не удалось сохранить комментарий."])
            ModelState.AddModelError(nameof(input.Content), error);
        return View(input);
    }

    [HttpGet]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.Moderator + "," + RoleNames.User)]
    public async Task<IActionResult> Edit(int id)
    {
        var comment = await comments.GetDetailsAsync(id);
        if (comment is null) return NotFound();
        if (!CanEdit(comment)) return Forbid();
        ViewData["CommentId"] = id;
        return View(new CommentTextInput { Content = comment.Content });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.Moderator + "," + RoleNames.User)]
    public async Task<IActionResult> Edit(int id, CommentTextInput input)
    {
        var comment = await comments.GetDetailsAsync(id);
        if (comment is null) return NotFound();
        if (!CanEdit(comment)) return Forbid();
        ViewData["CommentId"] = id;
        if (!ModelState.IsValid) return View(input);

        var actor = GetActor();
        if (actor is null) return Forbid();
        var result = await comments.UpdateAsync(id,
            new SaveCommentRequest(input.Content, comment.PostId), actor);
        if (result.Status == OperationStatus.Forbidden) return Forbid();
        if (result.Status == OperationStatus.NotFound) return NotFound();
        if (result.Status == OperationStatus.Success)
            return RedirectToAction(nameof(Details), new { id });
        foreach (var error in result.Errors ?? ["Не удалось сохранить комментарий."])
            ModelState.AddModelError(nameof(input.Content), error);
        return View(input);
    }

    [HttpGet]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.User)]
    public async Task<IActionResult> Delete(int id)
    {
        var comment = await comments.GetDetailsAsync(id);
        if (comment is null) return NotFound();
        return CanDelete(comment) ? View(comment) : Forbid();
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.User)]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var actor = GetActor();
        if (actor is null) return Forbid();
        var result = await comments.DeleteAsync(id, actor);
        if (result.Status == OperationStatus.Forbidden) return Forbid();
        if (result.Status == OperationStatus.NotFound) return NotFound();
        return result.Status == OperationStatus.Success
            ? RedirectToAction(nameof(Index))
            : StatusCode(500);
    }

    private ActorContext? GetActor()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return id is null ? null : new ActorContext(id,
            User.IsInRole(RoleNames.Admin), User.IsInRole(RoleNames.Moderator));
    }

    private bool CanEdit(CommentDisplayResponse comment) =>
        User.IsInRole(RoleNames.Admin) || User.IsInRole(RoleNames.Moderator) ||
        comment.AuthorId == User.FindFirstValue(ClaimTypes.NameIdentifier);

    private bool CanDelete(CommentDisplayResponse comment) =>
        User.IsInRole(RoleNames.Admin) ||
        (!User.IsInRole(RoleNames.Moderator) &&
         comment.AuthorId == User.FindFirstValue(ClaimTypes.NameIdentifier));
}

using System.Security.Claims;
using Blog.Services;
using Blog.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Blog.Controllers;

public class CatalogController(IPostService posts, ITagService tags, ICommentService comments) : Controller
{
    public async Task<IActionResult> Posts(int? tagId)
    {
        if (tagId is not null)
        {
            var tag = await tags.GetByIdAsync(tagId.Value);
            if (tag is null) return NotFound();
            ViewData["TagName"] = tag.Name;
        }

        return View(await posts.GetAllForCatalogAsync(tagId));
    }

    public async Task<IActionResult> Post(int id)
    {
        var post = await posts.GetDetailsAsync(id);
        return post is null ? NotFound() : View(new PostDetailsInput { Details = post });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.User)]
    public async Task<IActionResult> AddComment(int id, AddPostCommentInput input)
    {
        var actor = GetActor();
        if (actor is null) return Forbid();

        var post = await posts.GetDetailsAsync(id);
        if (post is null) return NotFound();

        if (!ModelState.IsValid)
            return View(nameof(Post), new PostDetailsInput
                { Details = post, CommentText = input.CommentText });

        var result = await comments.CreateAsync(
            new SaveCommentRequest(input.CommentText, id), actor);
        if (result.Status == OperationStatus.Forbidden) return Forbid();
        if (result.Status == OperationStatus.NotFound) return NotFound();
        if (result.Status == OperationStatus.Success)
            return RedirectToAction(nameof(Post), new { id });

        foreach (var error in result.Errors ?? ["Не удалось сохранить комментарий."])
            ModelState.AddModelError(nameof(input.CommentText), error);
        return View(nameof(Post), new PostDetailsInput
            { Details = post, CommentText = input.CommentText });
    }

    [HttpGet]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.User)]
    public async Task<IActionResult> CreatePost() => View(new CreatePostInput
    {
        AvailableTags = await tags.GetAllAsync()
    });

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.User)]
    public async Task<IActionResult> CreatePost(CreatePostInput input)
    {
        if (!ModelState.IsValid)
        {
            input.AvailableTags = await tags.GetAllAsync();
            return View(input);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Forbid();

        var actor = new ActorContext(userId,
            User.IsInRole(RoleNames.Admin), User.IsInRole(RoleNames.Moderator));
        var request = new SavePostRequest(input.Title, input.Content, input.TagIds?.ToArray());
        var result = await posts.CreateAsync(request, actor);
        if (result.Status == OperationStatus.Success)
            return RedirectToAction(nameof(Post), new { id = result.Value!.Id });
        if (result.Status == OperationStatus.Forbidden)
            return Forbid();

        foreach (var error in result.Errors ?? ["Не удалось добавить статью."])
            ModelState.AddModelError(string.Empty, error);
        input.AvailableTags = await tags.GetAllAsync();
        return View(input);
    }

    [HttpGet]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.Moderator + "," + RoleNames.User)]
    public async Task<IActionResult> EditPost(int id)
    {
        var actor = GetActor();
        if (actor is null) return Forbid();

        var result = await posts.GetForEditAsync(id, actor);
        if (result.Status == OperationStatus.NotFound) return NotFound();
        if (result.Status == OperationStatus.Forbidden) return Forbid();

        var post = result.Value!;
        return View(new EditPostInput
        {
            Id = post.Id,
            Title = post.Title,
            Content = post.Content,
            TagIds = post.TagIds.ToList(),
            AvailableTags = await tags.GetAllAsync()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.Moderator + "," + RoleNames.User)]
    public async Task<IActionResult> EditPost(int id, EditPostInput input)
    {
        var actor = GetActor();
        if (actor is null) return Forbid();

        var current = await posts.GetForEditAsync(id, actor);
        if (current.Status == OperationStatus.NotFound) return NotFound();
        if (current.Status == OperationStatus.Forbidden) return Forbid();

        input.Id = id;
        if (!ModelState.IsValid)
        {
            input.AvailableTags = await tags.GetAllAsync();
            return View(input);
        }

        var request = new SavePostRequest(input.Title, input.Content, input.TagIds.ToArray());
        var result = await posts.UpdateAsync(id, request, actor);
        if (result.Status == OperationStatus.Success)
            return RedirectToAction(nameof(Post), new { id });
        if (result.Status == OperationStatus.NotFound) return NotFound();
        if (result.Status == OperationStatus.Forbidden) return Forbid();

        foreach (var error in result.Errors ?? ["Не удалось сохранить статью."])
            ModelState.AddModelError(string.Empty, error);
        input.AvailableTags = await tags.GetAllAsync();
        return View(input);
    }

    public async Task<IActionResult> Tags() => View(await tags.GetAllWithPostCountsAsync());

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

    [HttpGet]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.User)]
    public async Task<IActionResult> EditTag(int id)
    {
        var tag = await tags.GetByIdAsync(id);
        if (tag is null) return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null || (!User.IsInRole(RoleNames.Admin) && tag.CreatorId != userId))
            return Forbid();

        return View(new EditTagInput { Id = tag.Id, Name = tag.Name });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.User)]
    public async Task<IActionResult> EditTag(int id, EditTagInput input)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Forbid();

        input.Id = id;
        if (!ModelState.IsValid) return View(input);

        var actor = new ActorContext(userId,
            User.IsInRole(RoleNames.Admin), User.IsInRole(RoleNames.Moderator));
        var result = await tags.UpdateAsync(id, new SaveTagRequest(input.Name), actor);
        if (result.Status == OperationStatus.Success)
            return RedirectToAction(nameof(Tags));
        if (result.Status == OperationStatus.Forbidden)
            return Forbid();
        if (result.Status == OperationStatus.NotFound)
            return NotFound();

        foreach (var error in result.Errors ?? ["Не удалось сохранить тег."])
            ModelState.AddModelError(nameof(input.Name), error);
        return View(input);
    }

    private ActorContext? GetActor()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return userId is null ? null : new ActorContext(userId,
            User.IsInRole(RoleNames.Admin), User.IsInRole(RoleNames.Moderator));
    }
}

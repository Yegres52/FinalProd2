using Blog.Services;
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
}

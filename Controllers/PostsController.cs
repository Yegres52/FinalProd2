using Blog.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Blog.Controllers;

[Route("api/posts")]
public class PostsController(IPostService posts) : BlogApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? authorId) =>
        Ok(await posts.GetAllAsync(authorId));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var post = await posts.GetByIdAsync(id);
        return post is null ? NotFound() : Ok(post);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(SavePostRequest request)
    {
        var result = await posts.CreateAsync(request, CurrentActor);
        return result.Status == OperationStatus.Success
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : FromResult(result);
    }

    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Update(int id, SavePostRequest request) =>
        FromResult(await posts.UpdateAsync(id, request, CurrentActor));

    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id) =>
        FromResult(await posts.DeleteAsync(id, CurrentActor));
}

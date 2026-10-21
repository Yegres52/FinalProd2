using Blog.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Blog.Controllers;

[Route("api/tags")]
public class TagsController(ITagService tags) : BlogApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await tags.GetAllAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var tag = await tags.GetByIdAsync(id);
        return tag is null ? NotFound() : Ok(tag);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(SaveTagRequest request)
    {
        var result = await tags.CreateAsync(request, CurrentActor);
        return result.Status == OperationStatus.Success
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : FromResult(result);
    }

    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Update(int id, SaveTagRequest request) =>
        FromResult(await tags.UpdateAsync(id, request, CurrentActor));

    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id) =>
        FromResult(await tags.DeleteAsync(id, CurrentActor));
}

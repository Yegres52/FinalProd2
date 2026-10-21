using Blog.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Blog.Controllers;

[Route("api/comments")]
public class CommentsController(ICommentService comments) : BlogApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await comments.GetAllAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var comment = await comments.GetByIdAsync(id);
        return comment is null ? NotFound() : Ok(comment);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(SaveCommentRequest request)
    {
        var result = await comments.CreateAsync(request, CurrentActor);
        return result.Status == OperationStatus.Success
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : FromResult(result);
    }

    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Update(int id, SaveCommentRequest request) =>
        FromResult(await comments.UpdateAsync(id, request, CurrentActor));

    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id) =>
        FromResult(await comments.DeleteAsync(id, CurrentActor));
}

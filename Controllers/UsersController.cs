using Blog.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Blog.Controllers;

[Route("api/users")]
public class UsersController(IUserService users) : BlogApiController
{
    [HttpGet]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> GetAll() => Ok(await users.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var user = await users.GetByIdAsync(id);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterUserRequest request)
    {
        if (User.Identity?.IsAuthenticated == true && !User.IsInRole(RoleNames.Admin))
            return Forbid();
        var result = await users.RegisterAsync(request);
        return result.Status == OperationStatus.Success
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : FromResult(result);
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> Update(string id, UpdateUserRequest request) =>
        FromResult(await users.UpdateAsync(id, request, CurrentActor));

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(string id) =>
        FromResult(await users.DeleteAsync(id, CurrentActor));
}

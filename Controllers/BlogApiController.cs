using Blog.Services;
using Microsoft.AspNetCore.Mvc;

namespace Blog.Controllers;

[ApiController]
public abstract class BlogApiController : ControllerBase
{
    protected IActionResult FromResult<T>(OperationResult<T> result) => result.Status switch
    {
        OperationStatus.Success => Ok(result.Value),
        OperationStatus.NotFound => NotFound(result.Errors),
        OperationStatus.Forbidden => Forbid(),
        OperationStatus.Conflict => Conflict(result.Errors),
        _ => BadRequest(result.Errors)
    };

    protected ActorContext CurrentActor => new(
        User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? throw new InvalidOperationException("Идентификатор пользователя не найден."),
        User.IsInRole(RoleNames.Admin),
        User.IsInRole(RoleNames.Moderator));
}

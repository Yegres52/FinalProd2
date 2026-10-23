using Microsoft.AspNetCore.Diagnostics;

namespace Blog.Middleware;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var originalPath = context.Features.Get<IExceptionHandlerPathFeature>()?.Path;
        var path = originalPath is null
            ? context.Request.Path.ToUriComponent()
            : new PathString(originalPath).ToUriComponent();

        logger.LogError(exception,
            "Непредвиденная ошибка при обработке {Method} {Path}. Запрос {RequestId}",
            context.Request.Method,
            path,
            context.TraceIdentifier);

        context.Response.Redirect($"{context.Request.PathBase}/errors/500");
        return ValueTask.FromResult(true);
    }
}

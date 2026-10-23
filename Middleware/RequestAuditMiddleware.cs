using System.Diagnostics;
using System.Security.Claims;

namespace Blog.Middleware;

public sealed class RequestAuditMiddleware(RequestDelegate next, ILoggerFactory loggerFactory)
{
    private readonly ILogger _actions = loggerFactory.CreateLogger("Blog.UserActions");
    private readonly ILogger _errors = loggerFactory.CreateLogger("Blog.HttpErrors");

    public async Task InvokeAsync(HttpContext context)
    {
        var watch = Stopwatch.StartNew();
        var method = context.Request.Method;
        var path = context.Request.Path.ToUriComponent();
        var requestId = context.TraceIdentifier;
        var unhandledException = false;

        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            unhandledException = true;
            _errors.LogError(exception,
                "Необработанная ошибка. Запрос {RequestId}: {Method} {Path}",
                requestId, method, path);
            throw;
        }
        finally
        {
            watch.Stop();
            var statusCode = unhandledException
                ? StatusCodes.Status500InternalServerError
                : context.Response.StatusCode;
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "анонимный";

            _actions.LogInformation(
                "Пользователь {UserId} | {Method} {Path} | HTTP {StatusCode} | {ElapsedMs} мс | запрос {RequestId}",
                userId, method, path, statusCode, watch.ElapsedMilliseconds, requestId);

            if (statusCode >= StatusCodes.Status400BadRequest && !unhandledException)
                _errors.LogWarning(
                    "HTTP {StatusCode}. Пользователь {UserId} | {Method} {Path} | запрос {RequestId}",
                    statusCode, userId, method, path, requestId);
        }
    }
}

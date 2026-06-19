namespace MarbleCraftOMS.Api.Middleware;

public class AuditMiddleware(RequestDelegate next, ILogger<AuditMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        await next(context);

        var user = context.User.Identity?.Name ?? "anonymous";
        logger.LogInformation(
            "Audit Method={Method} Path={Path} User={User} Status={Status} Timestamp={Timestamp}",
            context.Request.Method,
            context.Request.Path,
            user,
            context.Response.StatusCode,
            DateTime.UtcNow);
    }
}

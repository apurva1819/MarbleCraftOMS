namespace MarbleCraftOMS.Api.Middleware;

public class AuditMiddleware(RequestDelegate next, ILogger<AuditMiddleware> logger)
{
    // Caps path length in audit logs — prevents log bloat from oversized-path attacks
    private const int MaxPathLog = 200;

    public async Task InvokeAsync(HttpContext context)
    {
        await next(context);

        var user = context.User.Identity?.Name ?? "anonymous";

        // ReadOnlySpan<char> slices without allocating a new string unless truncation is needed
        ReadOnlySpan<char> rawPath = (context.Request.Path.Value ?? string.Empty).AsSpan();
        var path = rawPath.Length <= MaxPathLog
            ? context.Request.Path.Value
            : new string(rawPath[..MaxPathLog]);

        logger.LogInformation(
            "Audit Method={Method} Path={Path} User={User} Status={Status} Timestamp={Timestamp}",
            context.Request.Method,
            path,
            user,
            context.Response.StatusCode,
            DateTime.UtcNow);
    }
}

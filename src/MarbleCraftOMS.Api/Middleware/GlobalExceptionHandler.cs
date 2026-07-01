using MarbleCraftOMS.Core.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace MarbleCraftOMS.Api.Middleware;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            KeyNotFoundException           => (StatusCodes.Status404NotFound,            "Resource not found"),
            UnprocessableEntityException   => (StatusCodes.Status422UnprocessableEntity, "Unprocessable entity"),
            ArgumentException              => (StatusCodes.Status400BadRequest,          "Invalid request"),
            InvalidOperationException      => (StatusCodes.Status409Conflict,            "Operation not allowed"),
            _                              => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
        };

        logger.LogError(exception, "Unhandled exception [{StatusCode}]: {Message}", statusCode, exception.Message);

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = statusCode,
                Title  = title,
                Detail = exception.Message
            },
            cancellationToken);

        return true;
    }
}

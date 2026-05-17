using LongPd.CleanArchitecture.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace LongPd.CleanArchitecture.Api.Middleware;

/// <summary>
/// Global exception handler — catches ALL unhandled exceptions and returns RFC 7807 ProblemDetails.
/// Registered via app.UseExceptionHandler() in Program.cs.
///
/// Mapping:
///   NotFoundException      → 404 Not Found
///   DomainException        → 400 Bad Request
///   Everything else        → 500 Internal Server Error (with traceId)
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken ct)
    {
        var (statusCode, title) = exception switch
        {
            NotFoundException ex => (StatusCodes.Status404NotFound, ex.Message),
            DomainException ex => (StatusCodes.Status400BadRequest, ex.Message),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
        else
            logger.LogWarning(exception, "Domain/NotFound exception: {Message}", exception.Message);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = $"https://httpstatuses.io/{statusCode}",
            Extensions =
            {
                ["traceId"] = httpContext.TraceIdentifier,
                ["exceptionType"] = exception.GetType().Name
            }
        };

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problemDetails, ct);
        return true; // Handled — stop further processing
    }
}

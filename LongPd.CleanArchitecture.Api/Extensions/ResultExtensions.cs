using LongPd.CleanArchitecture.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace LongPd.CleanArchitecture.Api.Extensions;

/// <summary>
/// Extension methods to map Result/Result{T} to typed HTTP responses.
/// Keeps endpoint handlers clean — no if/else on IsSuccess.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Maps Result{T} to an IResult using Match pattern.
    /// Success → 200 OK with value.
    /// Failure → appropriate HTTP error based on error code.
    /// </summary>
    public static IResult ToHttpResult<T>(this Result<T> result)
        => result.Match(
            onSuccess: value => Results.Ok(value),
            onFailure: error => error.ToHttpError());

    /// <summary>Maps Result (no value) — Success → 204 No Content.</summary>
    public static IResult ToHttpResult(this Result result)
        => result.IsSuccess
            ? Results.NoContent()
            : result.Error.ToHttpError();

    /// <summary>Success → 201 Created with location header and body.</summary>
    public static IResult ToCreatedResult<T>(this Result<T> result, string routePattern, Func<T, object> getRouteValues)
        => result.Match(
            onSuccess: value => Results.CreatedAtRoute(routePattern, getRouteValues(value), value),
            onFailure: error => error.ToHttpError());

    /// <summary>
    /// Maps an Error to the appropriate HTTP status code using RFC 7807 ProblemDetails.
    /// Convention-based: error Code prefix determines status code.
    /// </summary>
    public static IResult ToHttpError(this Error error)
    {
        var statusCode = error.Code switch
        {
            var c when c.EndsWith(".NotFound") => StatusCodes.Status404NotFound,
            var c when c.EndsWith(".ConcurrencyConflict") => StatusCodes.Status409Conflict,
            var c when c.StartsWith("Validation") => StatusCodes.Status422UnprocessableEntity,
            var c when c.EndsWith(".Unauthorized") => StatusCodes.Status401Unauthorized,
            var c when c.EndsWith(".Forbidden") => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status400BadRequest
        };

        return Results.Problem(
            detail: error.Description,
            statusCode: statusCode,
            title: GetTitle(statusCode),
            extensions: new Dictionary<string, object?> { ["errorCode"] = error.Code });
    }

    private static string GetTitle(int statusCode) => statusCode switch
    {
        404 => "Not Found",
        409 => "Conflict",
        422 => "Validation Error",
        401 => "Unauthorized",
        403 => "Forbidden",
        _ => "Bad Request"
    };
}
